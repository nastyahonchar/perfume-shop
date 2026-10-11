SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
GO
USE PerfumeOrdersDb;
GO

INSERT INTO dbo.Customers (Email, FullName, Phone, CreatedBy)
SELECT v.Email, v.FullName, v.Phone, N'seed'
FROM (VALUES
    (N'olena.kovalenko@example.com',  N'Олена Коваленко', N'+380501112233'),
    (N'andrii.melnyk@example.com',    N'Андрій Мельник',  N'+380672223344'),
    (N'iryna.shevchenko@example.com', N'Ірина Шевченко',  NULL)
) AS v (Email, FullName, Phone)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Customers c WHERE c.Email = v.Email);

-- копія довідника Каталогу: Id збігаються з Id парфумів у БД Каталогу
INSERT INTO dbo.Products (Id, Name, BrandName, Season, Price, Stock)
SELECT v.Id, v.Name, v.BrandName, v.Season, v.Price, v.Stock
FROM (VALUES
    (1,  N'Pumpkin Velours', N'Maison Aurelle', N'Autumn',  2890.00, 25),
    (2,  N'Citrus Riviera',  N'Maison Aurelle', N'Summer',  2450.00, 30),
    (3,  N'Cherry Meadow',   N'Nordic Bloom',   N'Spring',  2190.00, 20),
    (4,  N'Cedar Snow',      N'Nordic Bloom',   N'Winter',  3250.00, 15),
    (5,  N'Spiced Orchard',  N'Velvet Atelier', N'Autumn',  3100.00, 18),
    (6,  N'Black Iris',      N'Velvet Atelier', N'AllYear', 4200.00, 12),
    (7,  N'Amber Harvest',   N'Casa Ambra',     N'Autumn',  3490.00, 14),
    (8,  N'Sea Salt Noon',   N'Casa Ambra',     N'Summer',  2290.00, 22),
    (9,  N'Smoked Fig',      N'Oud & Co',       N'Autumn',  4590.00, 8),
    (10, N'Snow Vanilla',    N'Oud & Co',       N'Winter',  3790.00, 16)
) AS v (Id, Name, BrandName, Season, Price, Stock)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Products p WHERE p.Id = v.Id);

-- осіння акція: дати рахуються від моменту запуску скрипта
INSERT INTO dbo.Promotions (Name, Season, DiscountPercent, StartsAt, EndsAt, CreatedBy)
SELECT N'Осінній гарбузовий сезон', N'Autumn', 15.00,
       DATEADD(DAY, -7, SYSUTCDATETIME()), DATEADD(DAY, 45, SYSUTCDATETIME()), N'seed'
WHERE NOT EXISTS (SELECT 1 FROM dbo.Promotions WHERE Name = N'Осінній гарбузовий сезон');
GO

-- тестові замовлення створюються лише якщо таблиця Orders порожня
IF NOT EXISTS (SELECT 1 FROM dbo.Orders)
BEGIN
    DECLARE @c1 BIGINT = (SELECT Id FROM dbo.Customers WHERE Email = N'olena.kovalenko@example.com');
    DECLARE @c2 BIGINT = (SELECT Id FROM dbo.Customers WHERE Email = N'andrii.melnyk@example.com');
    DECLARE @c3 BIGINT = (SELECT Id FROM dbo.Customers WHERE Email = N'iryna.shevchenko@example.com');
    DECLARE @o1 BIGINT, @o2 BIGINT, @o3 BIGINT;

    -- замовлення 1: повний шлях до статусу Paid
    EXEC dbo.usp_CreateOrder @CustomerId = @c1, @ShippingAddress = N'м. Київ, вул. Хрещатик, 1, кв. 10',
         @Comment = N'Подарункова упаковка', @CreatedBy = N'seed', @OrderId = @o1 OUTPUT;
    EXEC dbo.usp_AddOrderItem @OrderId = @o1, @ProductId = 1, @Units = 2;
    EXEC dbo.usp_AddOrderItem @OrderId = @o1, @ProductId = 6, @Units = 1;
    EXEC dbo.usp_ConfirmOrder @OrderId = @o1, @ChangedBy = N'seed';
    EXEC dbo.usp_ChangeOrderStatus @OrderId = @o1, @NewStatus = N'Paid', @ChangedBy = N'seed';

    -- замовлення 2: наявність підтверджено, чекає оплати
    EXEC dbo.usp_CreateOrder @CustomerId = @c2, @ShippingAddress = N'м. Львів, пр. Свободи, 5',
         @Comment = NULL, @CreatedBy = N'seed', @OrderId = @o2 OUTPUT;
    EXEC dbo.usp_AddOrderItem @OrderId = @o2, @ProductId = 5, @Units = 1;
    EXEC dbo.usp_AddOrderItem @OrderId = @o2, @ProductId = 9, @Units = 1;
    EXEC dbo.usp_ConfirmOrder @OrderId = @o2, @ChangedBy = N'seed';

    -- замовлення 3: щойно оформлене (Submitted)
    EXEC dbo.usp_CreateOrder @CustomerId = @c3, @ShippingAddress = N'м. Одеса, вул. Дерибасівська, 12',
         @Comment = N'Подзвонити перед доставкою', @CreatedBy = N'seed', @OrderId = @o3 OUTPUT;
    EXEC dbo.usp_AddOrderItem @OrderId = @o3, @ProductId = 3, @Units = 1;
    EXEC dbo.usp_AddOrderItem @OrderId = @o3, @ProductId = 7, @Units = 3;
END
GO