SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
GO
USE PerfumeOrdersDb;
GO

-- 1. Усі зовнішні ключі лише всередині цієї бази
SELECT fk.name AS ForeignKey,
       OBJECT_NAME(fk.parent_object_id)     AS ChildTable,
       OBJECT_NAME(fk.referenced_object_id) AS ParentTable,
       fk.delete_referential_action_desc    AS OnDelete
FROM sys.foreign_keys fk
ORDER BY ChildTable;
GO

-- 2. Індекси
SELECT OBJECT_NAME(i.object_id) AS TableName, i.name AS IndexName, i.has_filter, i.filter_definition
FROM sys.indexes i
WHERE i.object_id IN (SELECT object_id FROM sys.tables) AND i.name LIKE N'IX[_]%'
ORDER BY TableName, IndexName;
GO

-- 3. Недопустимий перехід: Submitted -> Shipped (очікується помилка 50002)
DECLARE @Id BIGINT = (SELECT TOP 1 Id FROM dbo.Orders WHERE Status = N'Submitted' ORDER BY Id);
BEGIN TRY
    EXEC dbo.usp_ChangeOrderStatus @OrderId = @Id, @NewStatus = N'Shipped', @ChangedBy = N'check';
    PRINT N'ПОМИЛКА ТЕСТУ: перехід мав бути відхилений';
END TRY
BEGIN CATCH
    PRINT CONCAT(N'Відхилено: ', ERROR_NUMBER(), N' - ', ERROR_MESSAGE());
END CATCH
GO

-- 4. Повторне підтвердження вже оплаченого замовлення (очікується помилка 50002)
DECLARE @Paid BIGINT = (SELECT TOP 1 Id FROM dbo.Orders WHERE Status = N'Paid' ORDER BY Id);
BEGIN TRY
    EXEC dbo.usp_ConfirmOrder @OrderId = @Paid;
    PRINT N'ПОМИЛКА ТЕСТУ';
END TRY
BEGIN CATCH
    PRINT CONCAT(N'Відхилено: ', ERROR_NUMBER(), N' - ', ERROR_MESSAGE());
END CATCH
GO

-- 5. Знімок ціни в OrderItems не змінюється, коли змінюється ціна в довіднику
UPDATE dbo.Products SET Price = Price + 100 WHERE Id = 1;
SELECT oi.OrderId, oi.ProductName, oi.UnitPrice AS SnapshotPrice, p.Price AS CurrentPrice
FROM dbo.OrderItems oi JOIN dbo.Products p ON p.Id = oi.ProductId
WHERE oi.ProductId = 1;
UPDATE dbo.Products SET Price = Price - 100 WHERE Id = 1;
GO

-- 6. Читання замовлення й історії клієнта (пагінація)
DECLARE @First BIGINT = (SELECT MIN(Id) FROM dbo.Orders);
EXEC dbo.usp_GetOrderById @OrderId = @First;
DECLARE @Cust BIGINT = (SELECT CustomerId FROM dbo.Orders WHERE Id = @First);
EXEC dbo.usp_GetCustomerOrders @CustomerId = @Cust, @Status = NULL, @Page = 1, @PageSize = 10;
GO

-- 7. CRUD для клієнта
INSERT INTO dbo.Customers (Email, FullName, CreatedBy) VALUES (N'test.user@example.com', N'Тестовий Користувач', N'check');
SELECT Id, Email, FullName FROM dbo.Customers WHERE Email = N'test.user@example.com' AND IsDeleted = 0;
UPDATE dbo.Customers SET Phone = N'+380000000000', UpdatedAt = SYSUTCDATETIME(), UpdatedBy = N'check' WHERE Email = N'test.user@example.com';
UPDATE dbo.Customers SET IsDeleted = 1, UpdatedAt = SYSUTCDATETIME() WHERE Email = N'test.user@example.com';
DELETE FROM dbo.Customers WHERE Email = N'test.user@example.com';
GO

-- 8. Оптимістична конкурентність: оновлення проходить, лише якщо RowVersion не змінився
DECLARE @rv BINARY(8) = (SELECT RowVersion FROM dbo.Customers WHERE Id = (SELECT MIN(Id) FROM dbo.Customers));
UPDATE dbo.Customers SET FullName = FullName WHERE Id = (SELECT MIN(Id) FROM dbo.Customers) AND RowVersion = @rv;
PRINT CONCAT(N'Оновлено рядків: ', @@ROWCOUNT);
GO