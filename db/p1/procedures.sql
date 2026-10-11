SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
GO
USE PerfumeOrdersDb;
GO

-- 1. Створення замовлення
CREATE OR ALTER PROCEDURE dbo.usp_CreateOrder
    @CustomerId      BIGINT,
    @ShippingAddress NVARCHAR(400),
    @Comment         NVARCHAR(1000) = NULL,
    @CreatedBy       NVARCHAR(100)  = NULL,
    @OrderId         BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Id = @CustomerId AND IsDeleted = 0)
        THROW 50006, 'Customer not found', 1;

    INSERT INTO dbo.Orders (CustomerId, CreatedBy) VALUES (@CustomerId, @CreatedBy);
    SET @OrderId = SCOPE_IDENTITY();

    INSERT INTO dbo.OrderDetails (OrderId, ShippingAddress, Comment)
    VALUES (@OrderId, @ShippingAddress, @Comment);

    INSERT INTO dbo.OrderStatusHistory (OrderId, FromStatus, ToStatus, ChangedBy)
    VALUES (@OrderId, NULL, N'Submitted', @CreatedBy);

    COMMIT TRANSACTION;
END
GO

-- 2. Додавання позиції: фіксує назву й ціну і застосовує сезонну знижку
CREATE OR ALTER PROCEDURE dbo.usp_AddOrderItem
    @OrderId   BIGINT,
    @ProductId INT,
    @Units     INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    DECLARE @Status NVARCHAR(30) =
        (SELECT Status FROM dbo.Orders WITH (UPDLOCK) WHERE Id = @OrderId AND IsDeleted = 0);

    IF @Status IS NULL THROW 50001, 'Order not found', 1;
    IF @Status <> N'Submitted' THROW 50005, 'Items can be changed only while the order is Submitted', 1;
    IF @Units <= 0 THROW 50008, 'Units must be positive', 1;

    DECLARE @Name NVARCHAR(300), @Price DECIMAL(18,2), @Season NVARCHAR(20);
    SELECT @Name   = BrandName + N' ' + Name,
           @Price  = Price,
           @Season = Season
    FROM dbo.Products
    WHERE Id = @ProductId AND IsActive = 1;

    IF @Name IS NULL THROW 50004, 'Product not found or inactive', 1;

    DECLARE @Pct DECIMAL(5,2) = ISNULL((
        SELECT MAX(DiscountPercent)
        FROM dbo.Promotions
        WHERE IsActive = 1 AND Season = @Season
          AND SYSUTCDATETIME() BETWEEN StartsAt AND EndsAt), 0);
    DECLARE @Discount DECIMAL(18,2) = ROUND(@Price * @Pct / 100.0, 2);

    IF EXISTS (SELECT 1 FROM dbo.OrderItems WHERE OrderId = @OrderId AND ProductId = @ProductId)
        UPDATE dbo.OrderItems
        SET Units = Units + @Units
        WHERE OrderId = @OrderId AND ProductId = @ProductId;
    ELSE
        INSERT INTO dbo.OrderItems (OrderId, ProductId, ProductName, UnitPrice, Discount, Units)
        VALUES (@OrderId, @ProductId, @Name, @Price, @Discount, @Units);

    UPDATE dbo.Orders SET UpdatedAt = SYSUTCDATETIME() WHERE Id = @OrderId;

    COMMIT TRANSACTION;
END
GO

-- 3. Підтвердження наявності: перевірка й списання залишку, статус StockConfirmed
CREATE OR ALTER PROCEDURE dbo.usp_ConfirmOrder
    @OrderId   BIGINT,
    @ChangedBy NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    DECLARE @Status NVARCHAR(30) =
        (SELECT Status FROM dbo.Orders WITH (UPDLOCK) WHERE Id = @OrderId AND IsDeleted = 0);

    IF @Status IS NULL THROW 50001, 'Order not found', 1;
    IF @Status NOT IN (N'Submitted', N'AwaitingValidation')
        THROW 50002, 'Stock can be confirmed only for a new order', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.OrderItems WHERE OrderId = @OrderId)
        THROW 50009, 'Order has no items', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.OrderItems oi
        JOIN dbo.Products p WITH (UPDLOCK) ON p.Id = oi.ProductId
        WHERE oi.OrderId = @OrderId AND p.Stock < oi.Units)
        THROW 50003, 'Insufficient stock for one or more items', 1;

    UPDATE p
    SET p.Stock = p.Stock - oi.Units, p.UpdatedAt = SYSUTCDATETIME()
    FROM dbo.Products p
    JOIN dbo.OrderItems oi ON oi.ProductId = p.Id
    WHERE oi.OrderId = @OrderId;

    UPDATE dbo.Orders
    SET Status = N'StockConfirmed', UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @ChangedBy
    WHERE Id = @OrderId;

    INSERT INTO dbo.OrderStatusHistory (OrderId, FromStatus, ToStatus, ChangedBy)
    VALUES (@OrderId, @Status, N'StockConfirmed', @ChangedBy);

    COMMIT TRANSACTION;
END
GO

-- 4. Зміна статусу лише за допустимим переходом (таблиця OrderStatusTransitions)
CREATE OR ALTER PROCEDURE dbo.usp_ChangeOrderStatus
    @OrderId   BIGINT,
    @NewStatus NVARCHAR(30),
    @ChangedBy NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    DECLARE @Status NVARCHAR(30) =
        (SELECT Status FROM dbo.Orders WITH (UPDLOCK) WHERE Id = @OrderId AND IsDeleted = 0);

    IF @Status IS NULL THROW 50001, 'Order not found', 1;

    IF @NewStatus = N'StockConfirmed'
        THROW 50007, 'Use usp_ConfirmOrder to set StockConfirmed', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.OrderStatusTransitions
                   WHERE FromStatus = @Status AND ToStatus = @NewStatus)
    BEGIN
        DECLARE @Msg NVARCHAR(200) = CONCAT(N'Transition ', @Status, N' -> ', @NewStatus, N' is not allowed');
        THROW 50002, @Msg, 1;
    END

    -- скасування після підтвердження наявності повертає залишок на склад
    IF @NewStatus = N'Cancelled' AND @Status = N'StockConfirmed'
        UPDATE p
        SET p.Stock = p.Stock + oi.Units, p.UpdatedAt = SYSUTCDATETIME()
        FROM dbo.Products p
        JOIN dbo.OrderItems oi ON oi.ProductId = p.Id
        WHERE oi.OrderId = @OrderId;

    UPDATE dbo.Orders
    SET Status = @NewStatus, UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @ChangedBy
    WHERE Id = @OrderId;

    INSERT INTO dbo.OrderStatusHistory (OrderId, FromStatus, ToStatus, ChangedBy)
    VALUES (@OrderId, @Status, @NewStatus, @ChangedBy);

    COMMIT TRANSACTION;
END
GO

-- 5. Одне замовлення: шапка, позиції, історія статусів (3 набори результатів)
CREATE OR ALTER PROCEDURE dbo.usp_GetOrderById
    @OrderId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE Id = @OrderId AND IsDeleted = 0)
        THROW 50001, 'Order not found', 1;

    SELECT o.Id, o.CustomerId, c.FullName AS CustomerName, c.Email,
           o.Status, o.OrderDate, d.ShippingAddress, d.Comment,
           ISNULL((SELECT SUM((oi.UnitPrice - oi.Discount) * oi.Units)
                   FROM dbo.OrderItems oi WHERE oi.OrderId = o.Id), 0) AS TotalAmount,
           o.RowVersion
    FROM dbo.Orders o
    JOIN dbo.Customers c    ON c.Id = o.CustomerId
    JOIN dbo.OrderDetails d ON d.OrderId = o.Id
    WHERE o.Id = @OrderId;

    SELECT oi.ProductId, oi.ProductName, oi.UnitPrice, oi.Discount, oi.Units,
           (oi.UnitPrice - oi.Discount) * oi.Units AS LineTotal
    FROM dbo.OrderItems oi
    WHERE oi.OrderId = @OrderId
    ORDER BY oi.ProductId;

    SELECT h.FromStatus, h.ToStatus, h.ChangedAt, h.ChangedBy
    FROM dbo.OrderStatusHistory h
    WHERE h.OrderId = @OrderId
    ORDER BY h.ChangedAt, h.Id;
END
GO

-- 6. Історія замовлень клієнта: фільтр за статусом і пагінація
CREATE OR ALTER PROCEDURE dbo.usp_GetCustomerOrders
    @CustomerId BIGINT,
    @Status     NVARCHAR(30) = NULL,
    @Page       INT = 1,
    @PageSize   INT = 20
AS
BEGIN
    SET NOCOUNT ON;
    SET @Page     = IIF(@Page < 1, 1, @Page);
    SET @PageSize = IIF(@PageSize < 1, 20, IIF(@PageSize > 100, 100, @PageSize));

    SELECT o.Id, o.Status, o.OrderDate,
           ISNULL((SELECT SUM((oi.UnitPrice - oi.Discount) * oi.Units)
                   FROM dbo.OrderItems oi WHERE oi.OrderId = o.Id), 0) AS TotalAmount,
           COUNT(*) OVER () AS TotalCount
    FROM dbo.Orders o
    WHERE o.CustomerId = @CustomerId
      AND o.IsDeleted = 0
      AND (@Status IS NULL OR o.Status = @Status)
    ORDER BY o.OrderDate DESC, o.Id DESC
    OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO