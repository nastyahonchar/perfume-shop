SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
GO
IF DB_ID(N'PerfumeOrdersDb') IS NULL CREATE DATABASE PerfumeOrdersDb;
GO
USE PerfumeOrdersDb;
GO
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
CREATE TABLE dbo.Customers (
    Id         BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
    Email      NVARCHAR(256) NOT NULL CONSTRAINT UQ_Customers_Email UNIQUE,
    FullName   NVARCHAR(200) NOT NULL,
    Phone      NVARCHAR(30)  NULL,
    CreatedAt  DATETIME2     NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy  NVARCHAR(100) NULL,
    UpdatedAt  DATETIME2     NULL,
    UpdatedBy  NVARCHAR(100) NULL,
    IsDeleted  BIT           NOT NULL CONSTRAINT DF_Customers_IsDeleted DEFAULT 0,
    RowVersion ROWVERSION    NOT NULL
);
GO

-- Products: локальна копія довідника з БД Каталогу (той самий Id, без FK між базами).
-- Name, BrandName, Season, Price, IsActive оновлюються подіями з Каталогу.
-- Stock належить цій базі.
IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
CREATE TABLE dbo.Products (
    Id              INT           NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
    Name            NVARCHAR(200) NOT NULL,
    BrandName       NVARCHAR(100) NOT NULL,
    Season          NVARCHAR(20)  NOT NULL
        CONSTRAINT CK_Products_Season CHECK (Season IN (N'Spring', N'Summer', N'Autumn', N'Winter', N'AllYear')),
    Price           DECIMAL(18,2) NOT NULL CONSTRAINT CK_Products_Price CHECK (Price > 0),
    Stock           INT           NOT NULL CONSTRAINT DF_Products_Stock DEFAULT 0 CONSTRAINT CK_Products_Stock CHECK (Stock >= 0),
    IsActive        BIT           NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT 1,
    SourceUpdatedAt DATETIME2     NULL,
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2     NULL,
    RowVersion      ROWVERSION    NOT NULL
);
GO

IF OBJECT_ID(N'dbo.Promotions', N'U') IS NULL
CREATE TABLE dbo.Promotions (
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Promotions PRIMARY KEY,
    Name            NVARCHAR(200) NOT NULL CONSTRAINT UQ_Promotions_Name UNIQUE,
    Season          NVARCHAR(20)  NOT NULL
        CONSTRAINT CK_Promotions_Season CHECK (Season IN (N'Spring', N'Summer', N'Autumn', N'Winter', N'AllYear')),
    DiscountPercent DECIMAL(5,2)  NOT NULL CONSTRAINT CK_Promotions_Percent CHECK (DiscountPercent > 0 AND DiscountPercent <= 90),
    StartsAt        DATETIME2     NOT NULL,
    EndsAt          DATETIME2     NOT NULL,
    IsActive        BIT           NOT NULL CONSTRAINT DF_Promotions_IsActive DEFAULT 1,
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_Promotions_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy       NVARCHAR(100) NULL,
    UpdatedAt       DATETIME2     NULL,
    UpdatedBy       NVARCHAR(100) NULL,
    CONSTRAINT CK_Promotions_Dates CHECK (EndsAt > StartsAt)
);
GO

IF OBJECT_ID(N'dbo.OrderStatusTransitions', N'U') IS NULL
CREATE TABLE dbo.OrderStatusTransitions (
    FromStatus NVARCHAR(30) NOT NULL,
    ToStatus   NVARCHAR(30) NOT NULL,
    CONSTRAINT PK_OrderStatusTransitions PRIMARY KEY (FromStatus, ToStatus),
    CONSTRAINT CK_OST_From CHECK (FromStatus IN (N'Submitted', N'AwaitingValidation', N'StockConfirmed', N'Paid', N'Shipped', N'Cancelled')),
    CONSTRAINT CK_OST_To   CHECK (ToStatus   IN (N'Submitted', N'AwaitingValidation', N'StockConfirmed', N'Paid', N'Shipped', N'Cancelled'))
);
GO

IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
CREATE TABLE dbo.Orders (
    Id         BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
    CustomerId BIGINT NOT NULL CONSTRAINT FK_Orders_Customers REFERENCES dbo.Customers(Id),
    Status     NVARCHAR(30) NOT NULL CONSTRAINT DF_Orders_Status DEFAULT N'Submitted'
        CONSTRAINT CK_Orders_Status CHECK (Status IN (N'Submitted', N'AwaitingValidation', N'StockConfirmed', N'Paid', N'Shipped', N'Cancelled')),
    OrderDate  DATETIME2 NOT NULL CONSTRAINT DF_Orders_OrderDate DEFAULT SYSUTCDATETIME(),
    CreatedAt  DATETIME2 NOT NULL CONSTRAINT DF_Orders_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy  NVARCHAR(100) NULL,
    UpdatedAt  DATETIME2 NULL,
    UpdatedBy  NVARCHAR(100) NULL,
    IsDeleted  BIT NOT NULL CONSTRAINT DF_Orders_IsDeleted DEFAULT 0,
    RowVersion ROWVERSION NOT NULL
);
GO

IF OBJECT_ID(N'dbo.OrderDetails', N'U') IS NULL
CREATE TABLE dbo.OrderDetails (
    OrderId         BIGINT NOT NULL CONSTRAINT PK_OrderDetails PRIMARY KEY
        CONSTRAINT FK_OrderDetails_Orders REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
    ShippingAddress NVARCHAR(400)  NOT NULL,
    Comment         NVARCHAR(1000) NULL
);
GO

IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NULL
CREATE TABLE dbo.OrderItems (
    OrderId     BIGINT NOT NULL CONSTRAINT FK_OrderItems_Orders   REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
    ProductId   INT    NOT NULL CONSTRAINT FK_OrderItems_Products REFERENCES dbo.Products(Id),
    ProductName NVARCHAR(300) NOT NULL, -- знімок назви на момент замовлення, не оновлюється
    UnitPrice   DECIMAL(18,2) NOT NULL CONSTRAINT CK_OrderItems_UnitPrice CHECK (UnitPrice > 0), -- знімок ціни, не оновлюється
    Discount    DECIMAL(18,2) NOT NULL CONSTRAINT DF_OrderItems_Discount DEFAULT 0 CONSTRAINT CK_OrderItems_Discount CHECK (Discount >= 0), -- знімок знижки за 1 шт.
    Units       INT NOT NULL CONSTRAINT CK_OrderItems_Units CHECK (Units > 0),
    CONSTRAINT PK_OrderItems PRIMARY KEY (OrderId, ProductId),
    CONSTRAINT CK_OrderItems_DiscountLtPrice CHECK (Discount < UnitPrice)
);
GO

IF OBJECT_ID(N'dbo.OrderStatusHistory', N'U') IS NULL
CREATE TABLE dbo.OrderStatusHistory (
    Id         BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderStatusHistory PRIMARY KEY,
    OrderId    BIGINT NOT NULL CONSTRAINT FK_OSH_Orders REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
    FromStatus NVARCHAR(30) NULL,
    ToStatus   NVARCHAR(30) NOT NULL,
    ChangedAt  DATETIME2 NOT NULL CONSTRAINT DF_OSH_ChangedAt DEFAULT SYSUTCDATETIME(),
    ChangedBy  NVARCHAR(100) NULL
);
GO

-- Індекси. Біля кожного вказано запит, під який він створений.

-- замовлення клієнта з певним статусом
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Orders_CustomerId_Status' AND object_id = OBJECT_ID(N'dbo.Orders'))
    CREATE INDEX IX_Orders_CustomerId_Status ON dbo.Orders (CustomerId, Status) INCLUDE (OrderDate);

-- активні (не видалені) замовлення клієнта
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Orders_Active' AND object_id = OBJECT_ID(N'dbo.Orders'))
    CREATE INDEX IX_Orders_Active ON dbo.Orders (CustomerId) WHERE IsDeleted = 0;

-- у яких замовленнях є певний товар
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OrderItems_ProductId' AND object_id = OBJECT_ID(N'dbo.OrderItems'))
    CREATE INDEX IX_OrderItems_ProductId ON dbo.OrderItems (ProductId) INCLUDE (Units);

-- історія статусів замовлення за часом
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OSH_OrderId_ChangedAt' AND object_id = OBJECT_ID(N'dbo.OrderStatusHistory'))
    CREATE INDEX IX_OSH_OrderId_ChangedAt ON dbo.OrderStatusHistory (OrderId, ChangedAt);

-- активні товари певного сезону
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_Season_Active' AND object_id = OBJECT_ID(N'dbo.Products'))
    CREATE INDEX IX_Products_Season_Active ON dbo.Products (Season) INCLUDE (Price) WHERE IsActive = 1;

-- чинні акції на сезон за поточною датою
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Promotions_Season_Dates' AND object_id = OBJECT_ID(N'dbo.Promotions'))
    CREATE INDEX IX_Promotions_Season_Dates ON dbo.Promotions (Season, StartsAt, EndsAt) INCLUDE (DiscountPercent) WHERE IsActive = 1;
GO

INSERT INTO dbo.OrderStatusTransitions (FromStatus, ToStatus)
SELECT v.FromStatus, v.ToStatus
FROM (VALUES
    (N'Submitted',          N'AwaitingValidation'),
    (N'Submitted',          N'StockConfirmed'),
    (N'AwaitingValidation', N'StockConfirmed'),
    (N'StockConfirmed',     N'Paid'),
    (N'Paid',               N'Shipped'),
    (N'Submitted',          N'Cancelled'),
    (N'AwaitingValidation', N'Cancelled'),
    (N'StockConfirmed',     N'Cancelled')
) AS v (FromStatus, ToStatus)
WHERE NOT EXISTS (SELECT 1 FROM dbo.OrderStatusTransitions t WHERE t.FromStatus = v.FromStatus AND t.ToStatus = v.ToStatus);
GO