CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;
ALTER DATABASE CHARACTER SET utf8mb4;

CREATE TABLE `Admins` (
    `AdminId` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `Email` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `FirebaseUid` varchar(128) CHARACTER SET utf8mb4 NULL,
    `Password` varchar(255) CHARACTER SET utf8mb4 NULL,
    `Role` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_Admins` PRIMARY KEY (`AdminId`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Vendors` (
    `VendorId` int NOT NULL AUTO_INCREMENT,
    `AdminId` int NULL,
    `FirebaseUid` varchar(128) CHARACTER SET utf8mb4 NULL,
    `Name` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `Email` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `Phone` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `Address` varchar(255) CHARACTER SET utf8mb4 NULL,
    `Status` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_Vendors` PRIMARY KEY (`VendorId`),
    CONSTRAINT `FK_Vendors_Admins_AdminId` FOREIGN KEY (`AdminId`) REFERENCES `Admins` (`AdminId`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Shops` (
    `ShopId` int NOT NULL AUTO_INCREMENT,
    `VendorId` int NOT NULL,
    `ShopName` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ShopType` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Address` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Phone` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `PancardNo` varchar(20) CHARACTER SET utf8mb4 NULL,
    `AadhaarCardNo` varchar(20) CHARACTER SET utf8mb4 NULL,
    `AlternateNumber` varchar(20) CHARACTER SET utf8mb4 NULL,
    `ShopActNo` varchar(50) CHARACTER SET utf8mb4 NULL,
    `ShopActCertificateUrl` varchar(500) CHARACTER SET utf8mb4 NULL,
    `LogoUrl` varchar(500) CHARACTER SET utf8mb4 NULL,
    `CatalogUrl` varchar(500) CHARACTER SET utf8mb4 NULL,
    `Slug` varchar(160) CHARACTER SET utf8mb4 NULL,
    `Status` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_Shops` PRIMARY KEY (`ShopId`),
    CONSTRAINT `FK_Shops_Vendors_VendorId` FOREIGN KEY (`VendorId`) REFERENCES `Vendors` (`VendorId`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `Categories` (
    `CategoryId` int NOT NULL AUTO_INCREMENT,
    `ShopId` int NOT NULL,
    `CategoryName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Description` varchar(255) CHARACTER SET utf8mb4 NULL,
    `Status` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_Categories` PRIMARY KEY (`CategoryId`),
    CONSTRAINT `FK_Categories_Shops_ShopId` FOREIGN KEY (`ShopId`) REFERENCES `Shops` (`ShopId`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `QR_Codes` (
    `QrId` int NOT NULL AUTO_INCREMENT,
    `ShopId` int NOT NULL,
    `CatalogUrl` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `QrImagePath` varchar(500) CHARACTER SET utf8mb4 NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_QR_Codes` PRIMARY KEY (`QrId`),
    CONSTRAINT `FK_QR_Codes_Shops_ShopId` FOREIGN KEY (`ShopId`) REFERENCES `Shops` (`ShopId`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `Product_Categories` (
    `ProductCategoryId` int NOT NULL AUTO_INCREMENT,
    `CategoryId` int NOT NULL,
    `CategoryName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Image` varchar(500) CHARACTER SET utf8mb4 NULL,
    `Status` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_Product_Categories` PRIMARY KEY (`ProductCategoryId`),
    CONSTRAINT `FK_Product_Categories_Categories_CategoryId` FOREIGN KEY (`CategoryId`) REFERENCES `Categories` (`CategoryId`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `Products` (
    `ProductId` int NOT NULL AUTO_INCREMENT,
    `ShopId` int NOT NULL,
    `ProductCategoryId` int NULL,
    `CategoryId` int NULL,
    `ProductName` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `ProductType` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Description` longtext CHARACTER SET utf8mb4 NULL,
    `Brand` varchar(100) CHARACTER SET utf8mb4 NULL,
    `BasePrice` decimal(10,2) NOT NULL,
    `Status` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_Products` PRIMARY KEY (`ProductId`),
    CONSTRAINT `FK_Products_Categories_CategoryId` FOREIGN KEY (`CategoryId`) REFERENCES `Categories` (`CategoryId`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Products_Product_Categories_ProductCategoryId` FOREIGN KEY (`ProductCategoryId`) REFERENCES `Product_Categories` (`ProductCategoryId`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Products_Shops_ShopId` FOREIGN KEY (`ShopId`) REFERENCES `Shops` (`ShopId`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `Product_Images` (
    `ImageId` int NOT NULL AUTO_INCREMENT,
    `ProductId` int NOT NULL,
    `ImageUrl` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `IsPrimary` tinyint(1) NOT NULL,
    CONSTRAINT `PK_Product_Images` PRIMARY KEY (`ImageId`),
    CONSTRAINT `FK_Product_Images_Products_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Products` (`ProductId`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `Product_Variants` (
    `VariantId` int NOT NULL AUTO_INCREMENT,
    `ProductId` int NOT NULL,
    `Color` varchar(50) CHARACTER SET utf8mb4 NULL,
    `Size` varchar(50) CHARACTER SET utf8mb4 NULL,
    `Sku` varchar(80) CHARACTER SET utf8mb4 NULL,
    `Price` decimal(10,2) NOT NULL,
    CONSTRAINT `PK_Product_Variants` PRIMARY KEY (`VariantId`),
    CONSTRAINT `FK_Product_Variants_Products_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Products` (`ProductId`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `Inventory` (
    `InventoryId` int NOT NULL AUTO_INCREMENT,
    `VariantId` int NOT NULL,
    `StockQty` int NOT NULL,
    `ReservedQty` int NOT NULL,
    `AvailableQty` int NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_Inventory` PRIMARY KEY (`InventoryId`),
    CONSTRAINT `FK_Inventory_Product_Variants_VariantId` FOREIGN KEY (`VariantId`) REFERENCES `Product_Variants` (`VariantId`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `Stock_History` (
    `MovementId` int NOT NULL AUTO_INCREMENT,
    `VariantId` int NOT NULL,
    `MovementType` varchar(10) CHARACTER SET utf8mb4 NOT NULL,
    `Quantity` int NOT NULL,
    `MovementDate` datetime(6) NOT NULL,
    `Remarks` varchar(255) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_Stock_History` PRIMARY KEY (`MovementId`),
    CONSTRAINT `FK_Stock_History_Product_Variants_VariantId` FOREIGN KEY (`VariantId`) REFERENCES `Product_Variants` (`VariantId`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE UNIQUE INDEX `IX_Admins_Email` ON `Admins` (`Email`);

CREATE INDEX `IX_Categories_ShopId` ON `Categories` (`ShopId`);

CREATE UNIQUE INDEX `IX_Inventory_VariantId` ON `Inventory` (`VariantId`);

CREATE INDEX `IX_Product_Categories_CategoryId` ON `Product_Categories` (`CategoryId`);

CREATE INDEX `IX_Product_Images_ProductId` ON `Product_Images` (`ProductId`);

CREATE INDEX `IX_Product_Variants_ProductId` ON `Product_Variants` (`ProductId`);

CREATE INDEX `IX_Products_CategoryId` ON `Products` (`CategoryId`);

CREATE INDEX `IX_Products_ProductCategoryId` ON `Products` (`ProductCategoryId`);

CREATE INDEX `IX_Products_ShopId` ON `Products` (`ShopId`);

CREATE UNIQUE INDEX `IX_QR_Codes_ShopId` ON `QR_Codes` (`ShopId`);

CREATE UNIQUE INDEX `IX_Shops_ShopName` ON `Shops` (`ShopName`);

CREATE UNIQUE INDEX `IX_Shops_Slug` ON `Shops` (`Slug`);

CREATE INDEX `IX_Shops_VendorId` ON `Shops` (`VendorId`);

CREATE INDEX `IX_Stock_History_VariantId` ON `Stock_History` (`VariantId`);

CREATE INDEX `IX_Vendors_AdminId` ON `Vendors` (`AdminId`);

CREATE UNIQUE INDEX `IX_Vendors_Email` ON `Vendors` (`Email`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260802181749_InitialMySql', '9.0.11');

COMMIT;

