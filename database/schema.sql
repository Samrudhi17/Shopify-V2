IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Admins] (
    [AdminId] int NOT NULL IDENTITY,
    [Name] nvarchar(150) NOT NULL,
    [Email] nvarchar(150) NOT NULL,
    [FirebaseUid] nvarchar(128) NULL,
    [Password] nvarchar(255) NULL,
    [Role] nvarchar(50) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Admins] PRIMARY KEY ([AdminId])
);

CREATE TABLE [Vendors] (
    [VendorId] int NOT NULL IDENTITY,
    [AdminId] int NULL,
    [FirebaseUid] nvarchar(128) NULL,
    [Name] nvarchar(150) NOT NULL,
    [Email] nvarchar(150) NOT NULL,
    [Phone] nvarchar(20) NOT NULL,
    [Address] nvarchar(255) NULL,
    [Status] nvarchar(50) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Vendors] PRIMARY KEY ([VendorId]),
    CONSTRAINT [FK_Vendors_Admins_AdminId] FOREIGN KEY ([AdminId]) REFERENCES [Admins] ([AdminId])
);

CREATE TABLE [Shops] (
    [ShopId] int NOT NULL IDENTITY,
    [VendorId] int NOT NULL,
    [ShopName] nvarchar(150) NOT NULL,
    [ShopType] nvarchar(100) NULL,
    [Address] nvarchar(255) NOT NULL,
    [Phone] nvarchar(20) NOT NULL,
    [PancardNo] nvarchar(20) NULL,
    [AadhaarCardNo] nvarchar(20) NULL,
    [AlternateNumber] nvarchar(20) NULL,
    [ShopActNo] nvarchar(50) NULL,
    [ShopActCertificateUrl] nvarchar(500) NULL,
    [LogoUrl] nvarchar(500) NULL,
    [CatalogUrl] nvarchar(500) NULL,
    [Slug] nvarchar(160) NULL,
    [Status] nvarchar(50) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Shops] PRIMARY KEY ([ShopId]),
    CONSTRAINT [FK_Shops_Vendors_VendorId] FOREIGN KEY ([VendorId]) REFERENCES [Vendors] ([VendorId]) ON DELETE CASCADE
);

CREATE TABLE [Categories] (
    [CategoryId] int NOT NULL IDENTITY,
    [ShopId] int NOT NULL,
    [CategoryName] nvarchar(100) NOT NULL,
    [Description] nvarchar(255) NULL,
    [Status] nvarchar(50) NOT NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([CategoryId]),
    CONSTRAINT [FK_Categories_Shops_ShopId] FOREIGN KEY ([ShopId]) REFERENCES [Shops] ([ShopId]) ON DELETE CASCADE
);

CREATE TABLE [QR_Codes] (
    [QrId] int NOT NULL IDENTITY,
    [ShopId] int NOT NULL,
    [CatalogUrl] nvarchar(500) NOT NULL,
    [QrImagePath] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_QR_Codes] PRIMARY KEY ([QrId]),
    CONSTRAINT [FK_QR_Codes_Shops_ShopId] FOREIGN KEY ([ShopId]) REFERENCES [Shops] ([ShopId]) ON DELETE CASCADE
);

CREATE TABLE [Product_Categories] (
    [ProductCategoryId] int NOT NULL IDENTITY,
    [CategoryId] int NOT NULL,
    [CategoryName] nvarchar(100) NOT NULL,
    [Image] nvarchar(500) NULL,
    [Status] nvarchar(50) NOT NULL,
    CONSTRAINT [PK_Product_Categories] PRIMARY KEY ([ProductCategoryId]),
    CONSTRAINT [FK_Product_Categories_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([CategoryId]) ON DELETE CASCADE
);

CREATE TABLE [Products] (
    [ProductId] int NOT NULL IDENTITY,
    [ShopId] int NOT NULL,
    [ProductCategoryId] int NOT NULL,
    [ProductName] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NULL,
    [Brand] nvarchar(100) NULL,
    [BasePrice] decimal(10,2) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([ProductId]),
    CONSTRAINT [FK_Products_Product_Categories_ProductCategoryId] FOREIGN KEY ([ProductCategoryId]) REFERENCES [Product_Categories] ([ProductCategoryId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Products_Shops_ShopId] FOREIGN KEY ([ShopId]) REFERENCES [Shops] ([ShopId]) ON DELETE CASCADE
);

CREATE TABLE [Product_Images] (
    [ImageId] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [ImageUrl] nvarchar(500) NOT NULL,
    [IsPrimary] bit NOT NULL,
    CONSTRAINT [PK_Product_Images] PRIMARY KEY ([ImageId]),
    CONSTRAINT [FK_Product_Images_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([ProductId]) ON DELETE CASCADE
);

CREATE TABLE [Product_Variants] (
    [VariantId] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [Color] nvarchar(50) NULL,
    [Size] nvarchar(50) NULL,
    [Sku] nvarchar(80) NULL,
    [Price] decimal(10,2) NOT NULL,
    CONSTRAINT [PK_Product_Variants] PRIMARY KEY ([VariantId]),
    CONSTRAINT [FK_Product_Variants_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([ProductId]) ON DELETE CASCADE
);

CREATE TABLE [Inventory] (
    [InventoryId] int NOT NULL IDENTITY,
    [VariantId] int NOT NULL,
    [StockQty] int NOT NULL,
    [ReservedQty] int NOT NULL,
    [AvailableQty] int NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Inventory] PRIMARY KEY ([InventoryId]),
    CONSTRAINT [FK_Inventory_Product_Variants_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [Product_Variants] ([VariantId]) ON DELETE CASCADE
);

CREATE TABLE [Stock_History] (
    [MovementId] int NOT NULL IDENTITY,
    [VariantId] int NOT NULL,
    [MovementType] nvarchar(10) NOT NULL,
    [Quantity] int NOT NULL,
    [MovementDate] datetime2 NOT NULL,
    [Remarks] nvarchar(255) NULL,
    CONSTRAINT [PK_Stock_History] PRIMARY KEY ([MovementId]),
    CONSTRAINT [FK_Stock_History_Product_Variants_VariantId] FOREIGN KEY ([VariantId]) REFERENCES [Product_Variants] ([VariantId]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_Admins_Email] ON [Admins] ([Email]);

CREATE INDEX [IX_Categories_ShopId] ON [Categories] ([ShopId]);

CREATE UNIQUE INDEX [IX_Inventory_VariantId] ON [Inventory] ([VariantId]);

CREATE INDEX [IX_Product_Categories_CategoryId] ON [Product_Categories] ([CategoryId]);

CREATE INDEX [IX_Product_Images_ProductId] ON [Product_Images] ([ProductId]);

CREATE INDEX [IX_Product_Variants_ProductId] ON [Product_Variants] ([ProductId]);

CREATE INDEX [IX_Products_ProductCategoryId] ON [Products] ([ProductCategoryId]);

CREATE INDEX [IX_Products_ShopId] ON [Products] ([ShopId]);

CREATE UNIQUE INDEX [IX_QR_Codes_ShopId] ON [QR_Codes] ([ShopId]);

CREATE UNIQUE INDEX [IX_Shops_ShopName] ON [Shops] ([ShopName]);

CREATE UNIQUE INDEX [IX_Shops_Slug] ON [Shops] ([Slug]) WHERE [Slug] IS NOT NULL;

CREATE INDEX [IX_Shops_VendorId] ON [Shops] ([VendorId]);

CREATE INDEX [IX_Stock_History_VariantId] ON [Stock_History] ([VariantId]);

CREATE INDEX [IX_Vendors_AdminId] ON [Vendors] ([AdminId]);

CREATE UNIQUE INDEX [IX_Vendors_Email] ON [Vendors] ([Email]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260722154417_InitialCreate', N'10.0.10');

COMMIT;
GO

