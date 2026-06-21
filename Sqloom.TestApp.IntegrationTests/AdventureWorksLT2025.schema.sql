CREATE TABLE [SalesLT].[SalesOrderHeader] (
    [SalesOrderID]           INT                   IDENTITY (1, 1) NOT FOR REPLICATION NOT NULL,
    [RevisionNumber]         TINYINT               CONSTRAINT [DF_SalesOrderHeader_RevisionNumber] DEFAULT ((0)) NOT NULL,
    [OrderDate]              DATETIME              CONSTRAINT [DF_SalesOrderHeader_OrderDate] DEFAULT (getdate()) NOT NULL,
    [DueDate]                DATETIME              NOT NULL,
    [ShipDate]               DATETIME              NULL,
    [Status]                 TINYINT               CONSTRAINT [DF_SalesOrderHeader_Status] DEFAULT ((1)) NOT NULL,
    [OnlineOrderFlag]        [dbo].[Flag]          CONSTRAINT [DF_SalesOrderHeader_OnlineOrderFlag] DEFAULT ((1)) NOT NULL,
    [SalesOrderNumber]       AS                    (isnull(N'SO'+CONVERT([nvarchar](23),[SalesOrderID]),N'*** ERROR ***')),
    [PurchaseOrderNumber]    [dbo].[OrderNumber]   NULL,
    [AccountNumber]          [dbo].[AccountNumber] NULL,
    [CustomerID]             INT                   NOT NULL,
    [ShipToAddressID]        INT                   NULL,
    [BillToAddressID]        INT                   NULL,
    [ShipMethod]             NVARCHAR (50)         NOT NULL,
    [CreditCardApprovalCode] VARCHAR (15)          NULL,
    [SubTotal]               MONEY                 CONSTRAINT [DF_SalesOrderHeader_SubTotal] DEFAULT ((0.00)) NOT NULL,
    [TaxAmt]                 MONEY                 CONSTRAINT [DF_SalesOrderHeader_TaxAmt] DEFAULT ((0.00)) NOT NULL,
    [Freight]                MONEY                 CONSTRAINT [DF_SalesOrderHeader_Freight] DEFAULT ((0.00)) NOT NULL,
    [TotalDue]               AS                    (isnull(([SubTotal]+[TaxAmt])+[Freight],(0))),
    [Comment]                NVARCHAR (MAX)        NULL,
    [rowguid]                UNIQUEIDENTIFIER      CONSTRAINT [DF_SalesOrderHeader_rowguid] DEFAULT (newid()) ROWGUIDCOL NOT NULL,
    [ModifiedDate]           DATETIME              CONSTRAINT [DF_SalesOrderHeader_ModifiedDate] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_SalesOrderHeader_SalesOrderID] PRIMARY KEY CLUSTERED ([SalesOrderID] ASC),
    CONSTRAINT [CK_SalesOrderHeader_DueDate] CHECK ([DueDate]>=[OrderDate]),
    CONSTRAINT [CK_SalesOrderHeader_Freight] CHECK ([Freight]>=(0.00)),
    CONSTRAINT [CK_SalesOrderHeader_ShipDate] CHECK ([ShipDate]>=[OrderDate] OR [ShipDate] IS NULL),
    CONSTRAINT [CK_SalesOrderHeader_Status] CHECK ([Status]>=(0) AND [Status]<=(8)),
    CONSTRAINT [CK_SalesOrderHeader_SubTotal] CHECK ([SubTotal]>=(0.00)),
    CONSTRAINT [CK_SalesOrderHeader_TaxAmt] CHECK ([TaxAmt]>=(0.00)),
    CONSTRAINT [FK_SalesOrderHeader_Address_BillTo_AddressID] FOREIGN KEY ([BillToAddressID]) REFERENCES [SalesLT].[Address] ([AddressID]),
    CONSTRAINT [FK_SalesOrderHeader_Address_ShipTo_AddressID] FOREIGN KEY ([ShipToAddressID]) REFERENCES [SalesLT].[Address] ([AddressID]),
    CONSTRAINT [FK_SalesOrderHeader_Customer_CustomerID] FOREIGN KEY ([CustomerID]) REFERENCES [SalesLT].[Customer] ([CustomerID]),
    CONSTRAINT [AK_SalesOrderHeader_rowguid] UNIQUE NONCLUSTERED ([rowguid] ASC),
    CONSTRAINT [AK_SalesOrderHeader_SalesOrderNumber] UNIQUE NONCLUSTERED ([SalesOrderNumber] ASC)
);


GO

CREATE TABLE [SalesLT].[ProductCategory] (
    [ProductCategoryID]       INT              IDENTITY (1, 1) NOT NULL,
    [ParentProductCategoryID] INT              NULL,
    [Name]                    [dbo].[Name]     NOT NULL,
    [rowguid]                 UNIQUEIDENTIFIER CONSTRAINT [DF_ProductCategory_rowguid] DEFAULT (newid()) ROWGUIDCOL NOT NULL,
    [ModifiedDate]            DATETIME         CONSTRAINT [DF_ProductCategory_ModifiedDate] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_ProductCategory_ProductCategoryID] PRIMARY KEY CLUSTERED ([ProductCategoryID] ASC),
    CONSTRAINT [FK_ProductCategory_ProductCategory_ParentProductCategoryID_ProductCategoryID] FOREIGN KEY ([ParentProductCategoryID]) REFERENCES [SalesLT].[ProductCategory] ([ProductCategoryID]),
    CONSTRAINT [AK_ProductCategory_Name] UNIQUE NONCLUSTERED ([Name] ASC),
    CONSTRAINT [AK_ProductCategory_rowguid] UNIQUE NONCLUSTERED ([rowguid] ASC)
);


GO

CREATE TABLE [SalesLT].[CustomerAddress] (
    [CustomerID]   INT              NOT NULL,
    [AddressID]    INT              NOT NULL,
    [AddressType]  [dbo].[Name]     NOT NULL,
    [rowguid]      UNIQUEIDENTIFIER CONSTRAINT [DF_CustomerAddress_rowguid] DEFAULT (newid()) ROWGUIDCOL NOT NULL,
    [ModifiedDate] DATETIME         CONSTRAINT [DF_CustomerAddress_ModifiedDate] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_CustomerAddress_CustomerID_AddressID] PRIMARY KEY CLUSTERED ([CustomerID] ASC, [AddressID] ASC),
    CONSTRAINT [FK_CustomerAddress_Address_AddressID] FOREIGN KEY ([AddressID]) REFERENCES [SalesLT].[Address] ([AddressID]),
    CONSTRAINT [FK_CustomerAddress_Customer_CustomerID] FOREIGN KEY ([CustomerID]) REFERENCES [SalesLT].[Customer] ([CustomerID]),
    CONSTRAINT [AK_CustomerAddress_rowguid] UNIQUE NONCLUSTERED ([rowguid] ASC)
);


GO

CREATE TABLE [SalesLT].[Product] (
    [ProductID]              INT              IDENTITY (1, 1) NOT NULL,
    [Name]                   [dbo].[Name]     NOT NULL,
    [ProductNumber]          NVARCHAR (25)    NOT NULL,
    [Color]                  NVARCHAR (15)    NULL,
    [StandardCost]           MONEY            NOT NULL,
    [ListPrice]              MONEY            NOT NULL,
    [Size]                   NVARCHAR (5)     NULL,
    [Weight]                 DECIMAL (8, 2)   NULL,
    [ProductCategoryID]      INT              NULL,
    [ProductModelID]         INT              NULL,
    [SellStartDate]          DATETIME         NOT NULL,
    [SellEndDate]            DATETIME         NULL,
    [DiscontinuedDate]       DATETIME         NULL,
    [ThumbNailPhoto]         VARBINARY (MAX)  NULL,
    [ThumbnailPhotoFileName] NVARCHAR (50)    NULL,
    [rowguid]                UNIQUEIDENTIFIER CONSTRAINT [DF_Product_rowguid] DEFAULT (newid()) ROWGUIDCOL NOT NULL,
    [ModifiedDate]           DATETIME         CONSTRAINT [DF_Product_ModifiedDate] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_Product_ProductID] PRIMARY KEY CLUSTERED ([ProductID] ASC),
    CONSTRAINT [CK_Product_ListPrice] CHECK ([ListPrice]>=(0.00)),
    CONSTRAINT [CK_Product_SellEndDate] CHECK ([SellEndDate]>=[SellStartDate] OR [SellEndDate] IS NULL),
    CONSTRAINT [CK_Product_StandardCost] CHECK ([StandardCost]>=(0.00)),
    CONSTRAINT [CK_Product_Weight] CHECK ([Weight]>(0.00)),
    CONSTRAINT [FK_Product_ProductCategory_ProductCategoryID] FOREIGN KEY ([ProductCategoryID]) REFERENCES [SalesLT].[ProductCategory] ([ProductCategoryID]),
    CONSTRAINT [FK_Product_ProductModel_ProductModelID] FOREIGN KEY ([ProductModelID]) REFERENCES [SalesLT].[ProductModel] ([ProductModelID]),
    CONSTRAINT [AK_Product_Name] UNIQUE NONCLUSTERED ([Name] ASC),
    CONSTRAINT [AK_Product_ProductNumber] UNIQUE NONCLUSTERED ([ProductNumber] ASC),
    CONSTRAINT [AK_Product_rowguid] UNIQUE NONCLUSTERED ([rowguid] ASC)
);


GO

CREATE TABLE [dbo].[ErrorLog] (
    [ErrorLogID]     INT             IDENTITY (1, 1) NOT NULL,
    [ErrorTime]      DATETIME        CONSTRAINT [DF_ErrorLog_ErrorTime] DEFAULT (getdate()) NOT NULL,
    [UserName]       [sysname]       NOT NULL,
    [ErrorNumber]    INT             NOT NULL,
    [ErrorSeverity]  INT             NULL,
    [ErrorState]     INT             NULL,
    [ErrorProcedure] NVARCHAR (126)  NULL,
    [ErrorLine]      INT             NULL,
    [ErrorMessage]   NVARCHAR (4000) NOT NULL,
    CONSTRAINT [PK_ErrorLog_ErrorLogID] PRIMARY KEY CLUSTERED ([ErrorLogID] ASC)
);


GO

CREATE TABLE [SalesLT].[Customer] (
    [CustomerID]   INT               IDENTITY (1, 1) NOT FOR REPLICATION NOT NULL,
    [NameStyle]    [dbo].[NameStyle] CONSTRAINT [DF_Customer_NameStyle] DEFAULT ((0)) NOT NULL,
    [Title]        NVARCHAR (8)      NULL,
    [FirstName]    [dbo].[Name]      NOT NULL,
    [MiddleName]   [dbo].[Name]      NULL,
    [LastName]     [dbo].[Name]      NOT NULL,
    [Suffix]       NVARCHAR (10)     NULL,
    [CompanyName]  NVARCHAR (128)    NULL,
    [SalesPerson]  NVARCHAR (256)    NULL,
    [EmailAddress] NVARCHAR (50)     NULL,
    [Phone]        [dbo].[Phone]     NULL,
    [PasswordHash] VARCHAR (128)     NOT NULL,
    [PasswordSalt] VARCHAR (10)      NOT NULL,
    [rowguid]      UNIQUEIDENTIFIER  CONSTRAINT [DF_Customer_rowguid] DEFAULT (newid()) ROWGUIDCOL NOT NULL,
    [ModifiedDate] DATETIME          CONSTRAINT [DF_Customer_ModifiedDate] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_Customer_CustomerID] PRIMARY KEY CLUSTERED ([CustomerID] ASC),
    CONSTRAINT [AK_Customer_rowguid] UNIQUE NONCLUSTERED ([rowguid] ASC)
);


GO

CREATE TABLE [dbo].[BuildVersion] (
    [SystemInformationID] TINYINT       IDENTITY (1, 1) NOT NULL,
    [Database Version]    NVARCHAR (25) NOT NULL,
    [VersionDate]         DATETIME      NOT NULL,
    [ModifiedDate]        DATETIME      CONSTRAINT [DF_BuildVersion_ModifiedDate] DEFAULT (getdate()) NOT NULL
);


GO

CREATE TABLE [SalesLT].[ProductModel] (
    [ProductModelID]     INT                                                         IDENTITY (1, 1) NOT NULL,
    [Name]               [dbo].[Name]                                                NOT NULL,
    [CatalogDescription] XML(CONTENT [SalesLT].[ProductDescriptionSchemaCollection]) NULL,
    [rowguid]            UNIQUEIDENTIFIER                                            CONSTRAINT [DF_ProductModel_rowguid] DEFAULT (newid()) ROWGUIDCOL NOT NULL,
    [ModifiedDate]       DATETIME                                                    CONSTRAINT [DF_ProductModel_ModifiedDate] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_ProductModel_ProductModelID] PRIMARY KEY CLUSTERED ([ProductModelID] ASC),
    CONSTRAINT [AK_ProductModel_Name] UNIQUE NONCLUSTERED ([Name] ASC),
    CONSTRAINT [AK_ProductModel_rowguid] UNIQUE NONCLUSTERED ([rowguid] ASC)
);


GO

CREATE TABLE [SalesLT].[Address] (
    [AddressID]     INT              IDENTITY (1, 1) NOT FOR REPLICATION NOT NULL,
    [AddressLine1]  NVARCHAR (60)    NOT NULL,
    [AddressLine2]  NVARCHAR (60)    NULL,
    [City]          NVARCHAR (30)    NOT NULL,
    [StateProvince] [dbo].[Name]     NOT NULL,
    [CountryRegion] [dbo].[Name]     NOT NULL,
    [PostalCode]    NVARCHAR (15)    NOT NULL,
    [rowguid]       UNIQUEIDENTIFIER CONSTRAINT [DF_Address_rowguid] DEFAULT (newid()) ROWGUIDCOL NOT NULL,
    [ModifiedDate]  DATETIME         CONSTRAINT [DF_Address_ModifiedDate] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_Address_AddressID] PRIMARY KEY CLUSTERED ([AddressID] ASC),
    CONSTRAINT [AK_Address_rowguid] UNIQUE NONCLUSTERED ([rowguid] ASC)
);


GO

CREATE TABLE [SalesLT].[ProductDescription] (
    [ProductDescriptionID] INT              IDENTITY (1, 1) NOT NULL,
    [Description]          NVARCHAR (400)   NOT NULL,
    [rowguid]              UNIQUEIDENTIFIER CONSTRAINT [DF_ProductDescription_rowguid] DEFAULT (newid()) ROWGUIDCOL NOT NULL,
    [ModifiedDate]         DATETIME         CONSTRAINT [DF_ProductDescription_ModifiedDate] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_ProductDescription_ProductDescriptionID] PRIMARY KEY CLUSTERED ([ProductDescriptionID] ASC),
    CONSTRAINT [AK_ProductDescription_rowguid] UNIQUE NONCLUSTERED ([rowguid] ASC)
);


GO

CREATE TABLE [SalesLT].[ProductModelProductDescription] (
    [ProductModelID]       INT              NOT NULL,
    [ProductDescriptionID] INT              NOT NULL,
    [Culture]              NCHAR (6)        NOT NULL,
    [rowguid]              UNIQUEIDENTIFIER CONSTRAINT [DF_ProductModelProductDescription_rowguid] DEFAULT (newid()) ROWGUIDCOL NOT NULL,
    [ModifiedDate]         DATETIME         CONSTRAINT [DF_ProductModelProductDescription_ModifiedDate] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_ProductModelProductDescription_ProductModelID_ProductDescriptionID_Culture] PRIMARY KEY CLUSTERED ([ProductModelID] ASC, [ProductDescriptionID] ASC, [Culture] ASC),
    CONSTRAINT [FK_ProductModelProductDescription_ProductDescription_ProductDescriptionID] FOREIGN KEY ([ProductDescriptionID]) REFERENCES [SalesLT].[ProductDescription] ([ProductDescriptionID]),
    CONSTRAINT [FK_ProductModelProductDescription_ProductModel_ProductModelID] FOREIGN KEY ([ProductModelID]) REFERENCES [SalesLT].[ProductModel] ([ProductModelID]),
    CONSTRAINT [AK_ProductModelProductDescription_rowguid] UNIQUE NONCLUSTERED ([rowguid] ASC)
);


GO

CREATE TABLE [SalesLT].[SalesOrderDetail] (
    [SalesOrderID]       INT              NOT NULL,
    [SalesOrderDetailID] INT              IDENTITY (1, 1) NOT NULL,
    [OrderQty]           SMALLINT         NOT NULL,
    [ProductID]          INT              NOT NULL,
    [UnitPrice]          MONEY            NOT NULL,
    [UnitPriceDiscount]  MONEY            CONSTRAINT [DF_SalesOrderDetail_UnitPriceDiscount] DEFAULT ((0.0)) NOT NULL,
    [LineTotal]          AS               (isnull(([UnitPrice]*((1.0)-[UnitPriceDiscount]))*[OrderQty],(0.0))),
    [rowguid]            UNIQUEIDENTIFIER CONSTRAINT [DF_SalesOrderDetail_rowguid] DEFAULT (newid()) ROWGUIDCOL NOT NULL,
    [ModifiedDate]       DATETIME         CONSTRAINT [DF_SalesOrderDetail_ModifiedDate] DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_SalesOrderDetail_SalesOrderID_SalesOrderDetailID] PRIMARY KEY CLUSTERED ([SalesOrderID] ASC, [SalesOrderDetailID] ASC),
    CONSTRAINT [CK_SalesOrderDetail_OrderQty] CHECK ([OrderQty]>(0)),
    CONSTRAINT [CK_SalesOrderDetail_UnitPrice] CHECK ([UnitPrice]>=(0.00)),
    CONSTRAINT [CK_SalesOrderDetail_UnitPriceDiscount] CHECK ([UnitPriceDiscount]>=(0.00)),
    CONSTRAINT [FK_SalesOrderDetail_Product_ProductID] FOREIGN KEY ([ProductID]) REFERENCES [SalesLT].[Product] ([ProductID]),
    CONSTRAINT [FK_SalesOrderDetail_SalesOrderHeader_SalesOrderID] FOREIGN KEY ([SalesOrderID]) REFERENCES [SalesLT].[SalesOrderHeader] ([SalesOrderID]) ON DELETE CASCADE,
    CONSTRAINT [AK_SalesOrderDetail_rowguid] UNIQUE NONCLUSTERED ([rowguid] ASC)
);


GO


CREATE VIEW [SalesLT].[vGetAllCategories]
WITH SCHEMABINDING 
AS 
-- Returns the CustomerID, first name, and last name for the specified customer.

WITH CategoryCTE([ParentProductCategoryID], [ProductCategoryID], [Name]) AS 
(
	SELECT [ParentProductCategoryID], [ProductCategoryID], [Name]
	FROM SalesLT.ProductCategory
	WHERE ParentProductCategoryID IS NULL

UNION ALL

	SELECT C.[ParentProductCategoryID], C.[ProductCategoryID], C.[Name]
	FROM SalesLT.ProductCategory AS C
	INNER JOIN CategoryCTE AS BC ON BC.ProductCategoryID = C.ParentProductCategoryID
)

SELECT PC.[Name] AS [ParentProductCategoryName], CCTE.[Name] as [ProductCategoryName], CCTE.[ProductCategoryID]  
FROM CategoryCTE AS CCTE
JOIN SalesLT.ProductCategory AS PC 
ON PC.[ProductCategoryID] = CCTE.[ParentProductCategoryID]

GO



CREATE VIEW [SalesLT].[vProductAndDescription] 
WITH SCHEMABINDING 
AS 
-- View (indexed or standard) to display products and product descriptions by language.
SELECT 
    p.[ProductID] 
    ,p.[Name] 
    ,pm.[Name] AS [ProductModel] 
    ,pmx.[Culture] 
    ,pd.[Description] 
FROM [SalesLT].[Product] p 
    INNER JOIN [SalesLT].[ProductModel] pm 
    ON p.[ProductModelID] = pm.[ProductModelID] 
    INNER JOIN [SalesLT].[ProductModelProductDescription] pmx 
    ON pm.[ProductModelID] = pmx.[ProductModelID] 
    INNER JOIN [SalesLT].[ProductDescription] pd 
    ON pmx.[ProductDescriptionID] = pd.[ProductDescriptionID];

GO


CREATE VIEW [SalesLT].[vProductModelCatalogDescription] 
AS 
SELECT 
    [ProductModelID] 
    ,[Name] 
    ,[CatalogDescription].value(N'declare namespace p1="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ProductModelDescription"; 
        declare namespace html="http://www.w3.org/1999/xhtml"; 
        (/p1:ProductDescription/p1:Summary/html:p)[1]', 'nvarchar(max)') AS [Summary] 
    ,[CatalogDescription].value(N'declare namespace p1="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ProductModelDescription"; 
        (/p1:ProductDescription/p1:Manufacturer/p1:Name)[1]', 'nvarchar(max)') AS [Manufacturer] 
    ,[CatalogDescription].value(N'declare namespace p1="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ProductModelDescription"; 
        (/p1:ProductDescription/p1:Manufacturer/p1:Copyright)[1]', 'nvarchar(30)') AS [Copyright] 
    ,[CatalogDescription].value(N'declare namespace p1="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ProductModelDescription"; 
        (/p1:ProductDescription/p1:Manufacturer/p1:ProductURL)[1]', 'nvarchar(256)') AS [ProductURL] 
    ,[CatalogDescription].value(N'declare namespace p1="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ProductModelDescription"; 
        declare namespace wm="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ProductModelWarrAndMain"; 
        (/p1:ProductDescription/p1:Features/wm:Warranty/wm:WarrantyPeriod)[1]', 'nvarchar(256)') AS [WarrantyPeriod] 
    ,[CatalogDescription].value(N'declare namespace p1="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ProductModelDescription"; 
        declare namespace wm="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ProductModelWarrAndMain"; 
        (/p1:ProductDescription/p1:Features/wm:Warranty/wm:Description)[1]', 'nvarchar(256)') AS [WarrantyDescription] 
    ,[CatalogDescription].value(N'declare namespace p1="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ProductModelDescription"; 
        declare namespace wm="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ProductModelWarrAndMain"; 
        (/p1:ProductDescription/p1:Features/wm:Maintenance/wm:NoOfYears)[1]', 'nvarchar(256)') AS [NoOfYears] 
    ,[CatalogDescription].value(N'declare namespace p1="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ProductModelDescription"; 
        declare namespace wm="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ProductModelWarrAndMain"; 
        (/p1:ProductDescription/p1:Features/wm:Maintenance/wm:Description)[1]', 'nvarchar(256)') AS [MaintenanceDescription] 
    ,[CatalogDescription].value(N'declare namespace p1="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ProductModelDescription"; 
        declare namespace wf="http://www.adventure-works.com/schemas/OtherFeatures"; 
        (/p1:ProductDescription/p1:Features/wf:wheel)[1]', 'nvarchar(256)') AS [Wheel] 
    ,[CatalogDescription].value(N'declare namespace p1="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ProductModelDescription"; 
        declare namespace wf="http://www.adventure-works.com/schemas/OtherFeatures"; 
        (/p1:ProductDescription/p1:Features/wf:saddle)[1]', 'nvarchar(256)') AS [Saddle] 
    ,[CatalogDescription].value(N'declare namespace p1="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ProductModelDescription"; 
        declare namespace wf="http://www.adventure-works.com/schemas/OtherFeatures"; 
        (/p1:ProductDescription/p1:Features/wf:pedal)[1]', 'nvarchar(256)') AS [Pedal] 
    ,[CatalogDescription].value(N'declare namespace p1="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ProductModelDescription"; 
        declare namespace wf="http://www.adventure-works.com/schemas/OtherFeatures"; 
        (/p1:ProductDescription/p1:Features/wf:BikeFrame)[1]', 'nvarchar(max)') AS [BikeFrame] 
    ,[CatalogDescription].value(N'declare namespace p1="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ProductModelDescription"; 
        declare namespace wf="http://www.adventure-works.com/schemas/OtherFeatures"; 
        (/p1:ProductDescription/p1:Features/wf:crankset)[1]', 'nvarchar(256)') AS [Crankset] 
    ,[CatalogDescription].value(N'declare namespace p1="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ProductModelDescription"; 
        (/p1:ProductDescription/p1:Picture/p1:Angle)[1]', 'nvarchar(256)') AS [PictureAngle] 
    ,[CatalogDescription].value(N'declare namespace p1="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ProductModelDescription"; 
        (/p1:ProductDescription/p1:Picture/p1:Size)[1]', 'nvarchar(256)') AS [PictureSize] 
    ,[CatalogDescription].value(N'declare namespace p1="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ProductModelDescription"; 
        (/p1:ProductDescription/p1:Picture/p1:ProductPhotoID)[1]', 'nvarchar(256)') AS [ProductPhotoID] 
    ,[CatalogDescription].value(N'declare namespace p1="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ProductModelDescription"; 
        (/p1:ProductDescription/p1:Specifications/Material)[1]', 'nvarchar(256)') AS [Material] 
    ,[CatalogDescription].value(N'declare namespace p1="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ProductModelDescription"; 
        (/p1:ProductDescription/p1:Specifications/Color)[1]', 'nvarchar(256)') AS [Color] 
    ,[CatalogDescription].value(N'declare namespace p1="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ProductModelDescription"; 
        (/p1:ProductDescription/p1:Specifications/ProductLine)[1]', 'nvarchar(256)') AS [ProductLine] 
    ,[CatalogDescription].value(N'declare namespace p1="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ProductModelDescription"; 
        (/p1:ProductDescription/p1:Specifications/Style)[1]', 'nvarchar(256)') AS [Style] 
    ,[CatalogDescription].value(N'declare namespace p1="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ProductModelDescription"; 
        (/p1:ProductDescription/p1:Specifications/RiderExperience)[1]', 'nvarchar(1024)') AS [RiderExperience] 
    ,[rowguid] 
    ,[ModifiedDate]
FROM [SalesLT].[ProductModel] 
WHERE [CatalogDescription] IS NOT NULL;

GO

CREATE NONCLUSTERED INDEX [IX_Address_AddressLine1_AddressLine2_City_StateProvince_PostalCode_CountryRegion]
    ON [SalesLT].[Address]([AddressLine1] ASC, [AddressLine2] ASC, [City] ASC, [StateProvince] ASC, [PostalCode] ASC, [CountryRegion] ASC);


GO

CREATE NONCLUSTERED INDEX [IX_Address_StateProvince]
    ON [SalesLT].[Address]([StateProvince] ASC);


GO

CREATE NONCLUSTERED INDEX [IX_Customer_EmailAddress]
    ON [SalesLT].[Customer]([EmailAddress] ASC);


GO

CREATE NONCLUSTERED INDEX [IX_SalesOrderDetail_ProductID]
    ON [SalesLT].[SalesOrderDetail]([ProductID] ASC);


GO

CREATE NONCLUSTERED INDEX [IX_SalesOrderHeader_CustomerID]
    ON [SalesLT].[SalesOrderHeader]([CustomerID] ASC);


GO

CREATE UNIQUE CLUSTERED INDEX [IX_vProductAndDescription]
    ON [SalesLT].[vProductAndDescription]([Culture] ASC, [ProductID] ASC);


GO

CREATE PRIMARY XML INDEX [PXML_ProductModel_CatalogDescription]
    ON [SalesLT].[ProductModel]([CatalogDescription])
    WITH (PAD_INDEX = OFF);


GO



CREATE FUNCTION [dbo].[ufnGetSalesOrderStatusText](@Status [tinyint])
RETURNS [nvarchar](15) 
AS 
-- Returns the sales order status text representation for the status value.
BEGIN
    DECLARE @ret [nvarchar](15);

    SET @ret = 
        CASE @Status
            WHEN 1 THEN 'In process'
            WHEN 2 THEN 'Approved'
            WHEN 3 THEN 'Backordered'
            WHEN 4 THEN 'Rejected'
            WHEN 5 THEN 'Shipped'
            WHEN 6 THEN 'Cancelled'
            ELSE '** Invalid **'
        END;
    
    RETURN @ret
END;

GO


-- DROP FUNCTION [dbo].[ufnGetAllCategories]

CREATE FUNCTION [dbo].[ufnGetAllCategories]()
RETURNS @retCategoryInformation TABLE 
(
    -- Columns returned by the function
    [ParentProductCategoryName] [nvarchar](50) NULL, 
    [ProductCategoryName] [nvarchar](50) NOT NULL,
	[ProductCategoryID] [int] NOT NULL
)
AS 
-- Returns the CustomerID, first name, and last name for the specified customer.
BEGIN
	WITH CategoryCTE([ParentProductCategoryID], [ProductCategoryID], [Name]) AS 
	(
		SELECT [ParentProductCategoryID], [ProductCategoryID], [Name]
		FROM SalesLT.ProductCategory
		WHERE ParentProductCategoryID IS NULL

	UNION ALL

		SELECT C.[ParentProductCategoryID], C.[ProductCategoryID], C.[Name]
		FROM SalesLT.ProductCategory AS C
		INNER JOIN CategoryCTE AS BC ON BC.ProductCategoryID = C.ParentProductCategoryID
	)

	INSERT INTO @retCategoryInformation
	SELECT PC.[Name] AS [ParentProductCategoryName], CCTE.[Name] as [ProductCategoryName], CCTE.[ProductCategoryID]  
	FROM CategoryCTE AS CCTE
	JOIN SalesLT.ProductCategory AS PC 
	ON PC.[ProductCategoryID] = CCTE.[ParentProductCategoryID];
	RETURN;
END;

GO


CREATE FUNCTION [dbo].[ufnGetCustomerInformation](@CustomerID int)
RETURNS TABLE 
AS 
-- Returns the CustomerID, first name, and last name for the specified customer.
RETURN (
    SELECT 
        CustomerID, 
        FirstName, 
        LastName
    FROM [SalesLT].[Customer] 
    WHERE [CustomerID] = @CustomerID
);

GO


-- uspLogError logs error information in the ErrorLog table about the 
-- error that caused execution to jump to the CATCH block of a 
-- TRY...CATCH construct. This should be executed from within the scope 
-- of a CATCH block otherwise it will return without inserting error 
-- information. 
CREATE PROCEDURE [dbo].[uspLogError] 
    @ErrorLogID [int] = 0 OUTPUT -- contains the ErrorLogID of the row inserted
AS                               -- by uspLogError in the ErrorLog table
BEGIN
    SET NOCOUNT ON;

    -- Output parameter value of 0 indicates that error 
    -- information was not logged
    SET @ErrorLogID = 0;

    BEGIN TRY
        -- Return if there is no error information to log
        IF ERROR_NUMBER() IS NULL
            RETURN;

        -- Return if inside an uncommittable transaction.
        -- Data insertion/modification is not allowed when 
        -- a transaction is in an uncommittable state.
        IF XACT_STATE() = -1
        BEGIN
            PRINT 'Cannot log error since the current transaction is in an uncommittable state. ' 
                + 'Rollback the transaction before executing uspLogError in order to successfully log error information.';
            RETURN;
        END

        INSERT [dbo].[ErrorLog] 
            (
            [UserName], 
            [ErrorNumber], 
            [ErrorSeverity], 
            [ErrorState], 
            [ErrorProcedure], 
            [ErrorLine], 
            [ErrorMessage]
            ) 
        VALUES 
            (
            CONVERT(sysname, CURRENT_USER), 
            ERROR_NUMBER(),
            ERROR_SEVERITY(),
            ERROR_STATE(),
            ERROR_PROCEDURE(),
            ERROR_LINE(),
            ERROR_MESSAGE()
            );

        -- Pass back the ErrorLogID of the row inserted
        SET @ErrorLogID = @@IDENTITY;
    END TRY
    BEGIN CATCH
        PRINT 'An error occurred in stored procedure uspLogError: ';
        EXECUTE [dbo].[uspPrintError];
        RETURN -1;
    END CATCH
END;

GO


-- uspPrintError prints error information about the error that caused 
-- execution to jump to the CATCH block of a TRY...CATCH construct. 
-- Should be executed from within the scope of a CATCH block otherwise 
-- it will return without printing any error information.
CREATE PROCEDURE [dbo].[uspPrintError] 
AS
BEGIN
    SET NOCOUNT ON;

    -- Print error information. 
    PRINT 'Error ' + CONVERT(varchar(50), ERROR_NUMBER()) +
          ', Severity ' + CONVERT(varchar(5), ERROR_SEVERITY()) +
          ', State ' + CONVERT(varchar(5), ERROR_STATE()) + 
          ', Procedure ' + ISNULL(ERROR_PROCEDURE(), '-') + 
          ', Line ' + CONVERT(varchar(5), ERROR_LINE());
    PRINT ERROR_MESSAGE();
END;

GO



CREATE TRIGGER [SalesLT].[iduSalesOrderDetail] ON [SalesLT].[SalesOrderDetail] 
AFTER INSERT, DELETE, UPDATE AS 
BEGIN
    DECLARE @Count int;

    SET @Count = @@ROWCOUNT;
    IF @Count = 0 
        RETURN;

    SET NOCOUNT ON;

    BEGIN TRY
        -- If inserting or updating these columns
        IF UPDATE([ProductID]) OR UPDATE([OrderQty]) OR UPDATE([UnitPrice]) OR UPDATE([UnitPriceDiscount]) 

        -- Update SubTotal in SalesOrderHeader record. Note that this causes the 
        -- SalesOrderHeader trigger to fire which will update the RevisionNumber.
        UPDATE [SalesLT].[SalesOrderHeader]
        SET [SalesLT].[SalesOrderHeader].[SubTotal] = 
            (SELECT SUM([SalesLT].[SalesOrderDetail].[LineTotal])
                FROM [SalesLT].[SalesOrderDetail]
                WHERE [SalesLT].[SalesOrderHeader].[SalesOrderID] = [SalesLT].[SalesOrderDetail].[SalesOrderID])
        WHERE [SalesLT].[SalesOrderHeader].[SalesOrderID] IN (SELECT inserted.[SalesOrderID] FROM inserted);

    END TRY
    BEGIN CATCH
        EXECUTE [dbo].[uspPrintError];

        -- Rollback any active or uncommittable transactions before
        -- inserting information in the ErrorLog
        IF @@TRANCOUNT > 0
        BEGIN
            ROLLBACK TRANSACTION;
        END

        EXECUTE [dbo].[uspLogError];
    END CATCH;
END;

GO


CREATE TRIGGER [SalesLT].[uSalesOrderHeader] ON [SalesLT].[SalesOrderHeader] 
AFTER UPDATE AS 
BEGIN
    DECLARE @Count int;

    SET @Count = @@ROWCOUNT;
    IF @Count = 0 
        RETURN;

    SET NOCOUNT ON;

    BEGIN TRY
        -- Update RevisionNumber for modification of any field EXCEPT the Status.
        IF NOT (UPDATE([Status]) OR UPDATE([RevisionNumber]))
        BEGIN
            UPDATE [SalesLT].[SalesOrderHeader]
            SET [SalesLT].[SalesOrderHeader].[RevisionNumber] = 
                [SalesLT].[SalesOrderHeader].[RevisionNumber] + 1
            WHERE [SalesLT].[SalesOrderHeader].[SalesOrderID] IN 
                (SELECT inserted.[SalesOrderID] FROM inserted);
        END;
    END TRY
    BEGIN CATCH
        EXECUTE [dbo].[uspPrintError];

        -- Rollback any active or uncommittable transactions before
        -- inserting information in the ErrorLog
        IF @@TRANCOUNT > 0
        BEGIN
            ROLLBACK TRANSACTION;
        END

        EXECUTE [dbo].[uspLogError];
    END CATCH;
END;

GO

CREATE TYPE [dbo].[NameStyle]
    FROM BIT NOT NULL;


GO

CREATE TYPE [dbo].[AccountNumber]
    FROM NVARCHAR (15) NULL;


GO

CREATE TYPE [dbo].[OrderNumber]
    FROM NVARCHAR (25) NULL;


GO

CREATE TYPE [dbo].[Flag]
    FROM BIT NOT NULL;


GO

CREATE TYPE [dbo].[Phone]
    FROM NVARCHAR (25) NULL;


GO

CREATE TYPE [dbo].[Name]
    FROM NVARCHAR (50) NULL;


GO

CREATE SCHEMA [SalesLT]
    AUTHORIZATION [dbo];


GO

CREATE XML SCHEMA COLLECTION [SalesLT].[ProductDescriptionSchemaCollection]
    AS N'<xsd:schema xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:t="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ProductModelWarrAndMain" targetNamespace="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ProductModelWarrAndMain" elementFormDefault="qualified">
  <xsd:element name="Maintenance">
    <xsd:complexType>
      <xsd:complexContent>
        <xsd:restriction base="xsd:anyType">
          <xsd:sequence>
            <xsd:element name="NoOfYears" type="xsd:string" />
            <xsd:element name="Description" type="xsd:string" />
          </xsd:sequence>
        </xsd:restriction>
      </xsd:complexContent>
    </xsd:complexType>
  </xsd:element>
  <xsd:element name="Warranty">
    <xsd:complexType>
      <xsd:complexContent>
        <xsd:restriction base="xsd:anyType">
          <xsd:sequence>
            <xsd:element name="WarrantyPeriod" type="xsd:string" />
            <xsd:element name="Description" type="xsd:string" />
          </xsd:sequence>
        </xsd:restriction>
      </xsd:complexContent>
    </xsd:complexType>
  </xsd:element>
</xsd:schema>
<xsd:schema xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:ns1="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ProductModelWarrAndMain" xmlns:t="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ProductModelDescription" targetNamespace="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ProductModelDescription" elementFormDefault="qualified">
  <xsd:import namespace="http://schemas.microsoft.com/sqlserver/2004/07/adventure-works/ProductModelWarrAndMain" />
  <xsd:element name="Code" type="xsd:string" />
  <xsd:element name="Description" type="xsd:string" />
  <xsd:element name="ProductDescription" type="t:ProductDescription" />
  <xsd:element name="Taxonomy" type="xsd:string" />
  <xsd:complexType name="Category">
    <xsd:complexContent>
      <xsd:restriction base="xsd:anyType">
        <xsd:sequence>
          <xsd:element ref="t:Taxonomy" />
          <xsd:element ref="t:Code" />
          <xsd:element ref="t:Description" minOccurs="0" />
        </xsd:sequence>
      </xsd:restriction>
    </xsd:complexContent>
  </xsd:complexType>
  <xsd:complexType name="Features" mixed="true">
    <xsd:complexContent mixed="true">
      <xsd:restriction base="xsd:anyType">
        <xsd:sequence>
          <xsd:element ref="ns1:Warranty" />
          <xsd:element ref="ns1:Maintenance" />
          <xsd:any namespace="##other" processContents="skip" minOccurs="0" maxOccurs="unbounded" />
        </xsd:sequence>
      </xsd:restriction>
    </xsd:complexContent>
  </xsd:complexType>
  <xsd:complexType name="Manufacturer">
    <xsd:complexContent>
      <xsd:restriction base="xsd:anyType">
        <xsd:sequence>
          <xsd:element name="Name" type="xsd:string" minOccurs="0" />
          <xsd:element name="CopyrightURL" type="xsd:string" minOccurs="0" />
          <xsd:element name="Copyright" type="xsd:string" minOccurs="0" />
          <xsd:element name="ProductURL" type="xsd:string" minOccurs="0" />
        </xsd:sequence>
      </xsd:restriction>
    </xsd:complexContent>
  </xsd:complexType>
  <xsd:complexType name="Picture">
    <xsd:complexContent>
      <xsd:restriction base="xsd:anyType">
        <xsd:sequence>
          <xsd:element name="Name" type="xsd:string" minOccurs="0" />
          <xsd:element name="Angle" type="xsd:string" minOccurs="0" />
          <xsd:element name="Size" type="xsd:string" minOccurs="0" />
          <xsd:element name="ProductPhotoID" type="xsd:integer" minOccurs="0" />
        </xsd:sequence>
      </xsd:restriction>
    </xsd:complexContent>
  </xsd:complexType>
  <xsd:complexType name="ProductDescription">
    <xsd:complexContent>
      <xsd:restriction base="xsd:anyType">
        <xsd:sequence>
          <xsd:element name="Summary" type="t:Summary" minOccurs="0" />
          <xsd:element name="Manufacturer" type="t:Manufacturer" minOccurs="0" />
          <xsd:element name="Features" type="t:Features" minOccurs="0" maxOccurs="unbounded" />
          <xsd:element name="Picture" type="t:Picture" minOccurs="0" maxOccurs="unbounded" />
          <xsd:element name="Category" type="t:Category" minOccurs="0" maxOccurs="unbounded" />
          <xsd:element name="Specifications" type="t:Specifications" minOccurs="0" maxOccurs="unbounded" />
        </xsd:sequence>
        <xsd:attribute name="ProductModelID" type="xsd:string" />
        <xsd:attribute name="ProductModelName" type="xsd:string" />
      </xsd:restriction>
    </xsd:complexContent>
  </xsd:complexType>
  <xsd:complexType name="Specifications" mixed="true">
    <xsd:complexContent mixed="true">
      <xsd:restriction base="xsd:anyType">
        <xsd:sequence>
          <xsd:any processContents="skip" minOccurs="0" maxOccurs="unbounded" />
        </xsd:sequence>
      </xsd:restriction>
    </xsd:complexContent>
  </xsd:complexType>
  <xsd:complexType name="Summary" mixed="true">
    <xsd:complexContent mixed="true">
      <xsd:restriction base="xsd:anyType">
        <xsd:sequence>
          <xsd:any namespace="http://www.w3.org/1999/xhtml" processContents="skip" minOccurs="0" maxOccurs="unbounded" />
        </xsd:sequence>
      </xsd:restriction>
    </xsd:complexContent>
  </xsd:complexType>
</xsd:schema>';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Version number of the database in 9.yy.mm.dd.00 format.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'BuildVersion', @level2type = N'COLUMN', @level2name = N'Database Version';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Date and time the record was last updated.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'BuildVersion', @level2type = N'COLUMN', @level2name = N'ModifiedDate';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key for BuildVersion records.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'BuildVersion', @level2type = N'COLUMN', @level2name = N'SystemInformationID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Date and time the record was last updated.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'BuildVersion', @level2type = N'COLUMN', @level2name = N'VersionDate';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'The line number at which the error occurred.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'ErrorLog', @level2type = N'COLUMN', @level2name = N'ErrorLine';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key for ErrorLog records.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'ErrorLog', @level2type = N'COLUMN', @level2name = N'ErrorLogID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'The message text of the error that occurred.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'ErrorLog', @level2type = N'COLUMN', @level2name = N'ErrorMessage';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'The error number of the error that occurred.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'ErrorLog', @level2type = N'COLUMN', @level2name = N'ErrorNumber';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'The name of the stored procedure or trigger where the error occurred.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'ErrorLog', @level2type = N'COLUMN', @level2name = N'ErrorProcedure';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'The severity of the error that occurred.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'ErrorLog', @level2type = N'COLUMN', @level2name = N'ErrorSeverity';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'The state number of the error that occurred.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'ErrorLog', @level2type = N'COLUMN', @level2name = N'ErrorState';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'The date and time at which the error occurred.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'ErrorLog', @level2type = N'COLUMN', @level2name = N'ErrorTime';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'The user who executed the batch in which the error occurred.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'ErrorLog', @level2type = N'COLUMN', @level2name = N'UserName';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key for Address records.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Address', @level2type = N'COLUMN', @level2name = N'AddressID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'First street address line.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Address', @level2type = N'COLUMN', @level2name = N'AddressLine1';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Second street address line.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Address', @level2type = N'COLUMN', @level2name = N'AddressLine2';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Name of the city.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Address', @level2type = N'COLUMN', @level2name = N'City';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Date and time the record was last updated.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Address', @level2type = N'COLUMN', @level2name = N'ModifiedDate';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Postal code for the street address.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Address', @level2type = N'COLUMN', @level2name = N'PostalCode';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Address', @level2type = N'COLUMN', @level2name = N'rowguid';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Name of state or province.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Address', @level2type = N'COLUMN', @level2name = N'StateProvince';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'The customer''s organization.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Customer', @level2type = N'COLUMN', @level2name = N'CompanyName';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key for Customer records.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Customer', @level2type = N'COLUMN', @level2name = N'CustomerID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'E-mail address for the person.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Customer', @level2type = N'COLUMN', @level2name = N'EmailAddress';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'First name of the person.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Customer', @level2type = N'COLUMN', @level2name = N'FirstName';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Last name of the person.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Customer', @level2type = N'COLUMN', @level2name = N'LastName';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Middle name or middle initial of the person.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Customer', @level2type = N'COLUMN', @level2name = N'MiddleName';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Date and time the record was last updated.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Customer', @level2type = N'COLUMN', @level2name = N'ModifiedDate';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'0 = The data in FirstName and LastName are stored in western style (first name, last name) order.  1 = Eastern style (last name, first name) order.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Customer', @level2type = N'COLUMN', @level2name = N'NameStyle';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Password for the e-mail account.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Customer', @level2type = N'COLUMN', @level2name = N'PasswordHash';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Random value concatenated with the password string before the password is hashed.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Customer', @level2type = N'COLUMN', @level2name = N'PasswordSalt';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Phone number associated with the person.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Customer', @level2type = N'COLUMN', @level2name = N'Phone';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Customer', @level2type = N'COLUMN', @level2name = N'rowguid';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'The customer''s sales person, an employee of AdventureWorks Cycles.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Customer', @level2type = N'COLUMN', @level2name = N'SalesPerson';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Surname suffix. For example, Sr. or Jr.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Customer', @level2type = N'COLUMN', @level2name = N'Suffix';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'A courtesy title. For example, Mr. or Ms.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Customer', @level2type = N'COLUMN', @level2name = N'Title';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key. Foreign key to Address.AddressID.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'CustomerAddress', @level2type = N'COLUMN', @level2name = N'AddressID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'The kind of Address. One of: Archive, Billing, Home, Main Office, Primary, Shipping', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'CustomerAddress', @level2type = N'COLUMN', @level2name = N'AddressType';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key. Foreign key to Customer.CustomerID.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'CustomerAddress', @level2type = N'COLUMN', @level2name = N'CustomerID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Date and time the record was last updated.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'CustomerAddress', @level2type = N'COLUMN', @level2name = N'ModifiedDate';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'CustomerAddress', @level2type = N'COLUMN', @level2name = N'rowguid';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Product color.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Product', @level2type = N'COLUMN', @level2name = N'Color';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Date the product was discontinued.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Product', @level2type = N'COLUMN', @level2name = N'DiscontinuedDate';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Selling price.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Product', @level2type = N'COLUMN', @level2name = N'ListPrice';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Date and time the record was last updated.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Product', @level2type = N'COLUMN', @level2name = N'ModifiedDate';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Name of the product.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Product', @level2type = N'COLUMN', @level2name = N'Name';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Product is a member of this product category. Foreign key to ProductCategory.ProductCategoryID. ', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Product', @level2type = N'COLUMN', @level2name = N'ProductCategoryID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key for Product records.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Product', @level2type = N'COLUMN', @level2name = N'ProductID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Product is a member of this product model. Foreign key to ProductModel.ProductModelID.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Product', @level2type = N'COLUMN', @level2name = N'ProductModelID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Unique product identification number.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Product', @level2type = N'COLUMN', @level2name = N'ProductNumber';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Product', @level2type = N'COLUMN', @level2name = N'rowguid';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Date the product was no longer available for sale.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Product', @level2type = N'COLUMN', @level2name = N'SellEndDate';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Date the product was available for sale.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Product', @level2type = N'COLUMN', @level2name = N'SellStartDate';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Product size.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Product', @level2type = N'COLUMN', @level2name = N'Size';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Standard cost of the product.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Product', @level2type = N'COLUMN', @level2name = N'StandardCost';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Small image of the product.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Product', @level2type = N'COLUMN', @level2name = N'ThumbNailPhoto';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Small image file name.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Product', @level2type = N'COLUMN', @level2name = N'ThumbnailPhotoFileName';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Product weight.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Product', @level2type = N'COLUMN', @level2name = N'Weight';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Date and time the record was last updated.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductCategory', @level2type = N'COLUMN', @level2name = N'ModifiedDate';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Category description.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductCategory', @level2type = N'COLUMN', @level2name = N'Name';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Product category identification number of immediate ancestor category. Foreign key to ProductCategory.ProductCategoryID.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductCategory', @level2type = N'COLUMN', @level2name = N'ParentProductCategoryID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key for ProductCategory records.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductCategory', @level2type = N'COLUMN', @level2name = N'ProductCategoryID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductCategory', @level2type = N'COLUMN', @level2name = N'rowguid';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Description of the product.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductDescription', @level2type = N'COLUMN', @level2name = N'Description';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Date and time the record was last updated.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductDescription', @level2type = N'COLUMN', @level2name = N'ModifiedDate';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key for ProductDescription records.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductDescription', @level2type = N'COLUMN', @level2name = N'ProductDescriptionID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductDescription', @level2type = N'COLUMN', @level2name = N'rowguid';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'The culture for which the description is written', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductModelProductDescription', @level2type = N'COLUMN', @level2name = N'Culture';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Date and time the record was last updated.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductModelProductDescription', @level2type = N'COLUMN', @level2name = N'ModifiedDate';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key. Foreign key to ProductDescription.ProductDescriptionID.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductModelProductDescription', @level2type = N'COLUMN', @level2name = N'ProductDescriptionID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key. Foreign key to ProductModel.ProductModelID.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductModelProductDescription', @level2type = N'COLUMN', @level2name = N'ProductModelID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Per product subtotal. Computed as UnitPrice * (1 - UnitPriceDiscount) * OrderQty.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderDetail', @level2type = N'COLUMN', @level2name = N'LineTotal';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Date and time the record was last updated.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderDetail', @level2type = N'COLUMN', @level2name = N'ModifiedDate';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Quantity ordered per product.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderDetail', @level2type = N'COLUMN', @level2name = N'OrderQty';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Product sold to customer. Foreign key to Product.ProductID.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderDetail', @level2type = N'COLUMN', @level2name = N'ProductID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderDetail', @level2type = N'COLUMN', @level2name = N'rowguid';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key. One incremental unique number per product sold.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderDetail', @level2type = N'COLUMN', @level2name = N'SalesOrderDetailID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key. Foreign key to SalesOrderHeader.SalesOrderID.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderDetail', @level2type = N'COLUMN', @level2name = N'SalesOrderID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Selling price of a single product.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderDetail', @level2type = N'COLUMN', @level2name = N'UnitPrice';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Discount amount.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderDetail', @level2type = N'COLUMN', @level2name = N'UnitPriceDiscount';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Financial accounting number reference.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'COLUMN', @level2name = N'AccountNumber';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'The ID of the location to send invoices.  Foreign key to the Address table.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'COLUMN', @level2name = N'BillToAddressID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Sales representative comments.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'COLUMN', @level2name = N'Comment';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Approval code provided by the credit card company.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'COLUMN', @level2name = N'CreditCardApprovalCode';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Customer identification number. Foreign key to Customer.CustomerID.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'COLUMN', @level2name = N'CustomerID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Date the order is due to the customer.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'COLUMN', @level2name = N'DueDate';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Shipping cost.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'COLUMN', @level2name = N'Freight';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Date and time the record was last updated.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'COLUMN', @level2name = N'ModifiedDate';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'0 = Order placed by sales person. 1 = Order placed online by customer.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'COLUMN', @level2name = N'OnlineOrderFlag';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Dates the sales order was created.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'COLUMN', @level2name = N'OrderDate';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Customer purchase order number reference. ', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'COLUMN', @level2name = N'PurchaseOrderNumber';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Incremental number to track changes to the sales order over time.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'COLUMN', @level2name = N'RevisionNumber';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'ROWGUIDCOL number uniquely identifying the record. Used to support a merge replication sample.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'COLUMN', @level2name = N'rowguid';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'COLUMN', @level2name = N'SalesOrderID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Unique sales order identification number.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'COLUMN', @level2name = N'SalesOrderNumber';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Date the order was shipped to the customer.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'COLUMN', @level2name = N'ShipDate';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Shipping method. Foreign key to ShipMethod.ShipMethodID.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'COLUMN', @level2name = N'ShipMethod';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'The ID of the location to send goods.  Foreign key to the Address table.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'COLUMN', @level2name = N'ShipToAddressID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Order current status. 1 = In process; 2 = Approved; 3 = Backordered; 4 = Rejected; 5 = Shipped; 6 = Cancelled', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'COLUMN', @level2name = N'Status';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Sales subtotal. Computed as SUM(SalesOrderDetail.LineTotal)for the appropriate SalesOrderID.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'COLUMN', @level2name = N'SubTotal';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Tax amount.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'COLUMN', @level2name = N'TaxAmt';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Total due from customer. Computed as Subtotal + TaxAmt + Freight.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'COLUMN', @level2name = N'TotalDue';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Default constraint value of GETDATE()', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'BuildVersion', @level2type = N'CONSTRAINT', @level2name = N'DF_BuildVersion_ModifiedDate';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Default constraint value of GETDATE()', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'ErrorLog', @level2type = N'CONSTRAINT', @level2name = N'DF_ErrorLog_ErrorTime';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key (clustered) constraint', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'ErrorLog', @level2type = N'CONSTRAINT', @level2name = N'PK_ErrorLog_ErrorLogID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Unique nonclustered constraint. Used to support replication samples.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Address', @level2type = N'CONSTRAINT', @level2name = N'AK_Address_rowguid';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Unique nonclustered constraint. Used to support replication samples.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Customer', @level2type = N'CONSTRAINT', @level2name = N'AK_Customer_rowguid';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Unique nonclustered constraint. Used to support replication samples.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'CustomerAddress', @level2type = N'CONSTRAINT', @level2name = N'AK_CustomerAddress_rowguid';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Unique nonclustered constraint.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Product', @level2type = N'CONSTRAINT', @level2name = N'AK_Product_Name';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Unique nonclustered constraint.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Product', @level2type = N'CONSTRAINT', @level2name = N'AK_Product_ProductNumber';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Unique nonclustered constraint. Used to support replication samples.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Product', @level2type = N'CONSTRAINT', @level2name = N'AK_Product_rowguid';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Unique nonclustered constraint.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductCategory', @level2type = N'CONSTRAINT', @level2name = N'AK_ProductCategory_Name';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Unique nonclustered constraint. Used to support replication samples.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductCategory', @level2type = N'CONSTRAINT', @level2name = N'AK_ProductCategory_rowguid';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Unique nonclustered constraint. Used to support replication samples.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductDescription', @level2type = N'CONSTRAINT', @level2name = N'AK_ProductDescription_rowguid';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Unique nonclustered constraint.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductModel', @level2type = N'CONSTRAINT', @level2name = N'AK_ProductModel_Name';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Unique nonclustered constraint. Used to support replication samples.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductModel', @level2type = N'CONSTRAINT', @level2name = N'AK_ProductModel_rowguid';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Unique nonclustered constraint. Used to support replication samples.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductModelProductDescription', @level2type = N'CONSTRAINT', @level2name = N'AK_ProductModelProductDescription_rowguid';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Unique nonclustered constraint. Used to support replication samples.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderDetail', @level2type = N'CONSTRAINT', @level2name = N'AK_SalesOrderDetail_rowguid';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Unique nonclustered constraint. Used to support replication samples.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'CONSTRAINT', @level2name = N'AK_SalesOrderHeader_rowguid';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Unique nonclustered constraint.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'CONSTRAINT', @level2name = N'AK_SalesOrderHeader_SalesOrderNumber';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Check constraint [ListPrice] >= (0.00)', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Product', @level2type = N'CONSTRAINT', @level2name = N'CK_Product_ListPrice';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Check constraint [SellEndDate] >= [SellStartDate] OR [SellEndDate] IS NULL', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Product', @level2type = N'CONSTRAINT', @level2name = N'CK_Product_SellEndDate';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Check constraint [Weight] > (0.00)', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Product', @level2type = N'CONSTRAINT', @level2name = N'CK_Product_Weight';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Check constraint [OrderQty] > (0)', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderDetail', @level2type = N'CONSTRAINT', @level2name = N'CK_SalesOrderDetail_OrderQty';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Check constraint [UnitPrice] >= (0.00)', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderDetail', @level2type = N'CONSTRAINT', @level2name = N'CK_SalesOrderDetail_UnitPrice';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Check constraint [UnitPriceDiscount] >= (0.00)', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderDetail', @level2type = N'CONSTRAINT', @level2name = N'CK_SalesOrderDetail_UnitPriceDiscount';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Check constraint [DueDate] >= [OrderDate]', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'CONSTRAINT', @level2name = N'CK_SalesOrderHeader_DueDate';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Check constraint [Freight] >= (0.00)', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'CONSTRAINT', @level2name = N'CK_SalesOrderHeader_Freight';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Check constraint [ShipDate] >= [OrderDate] OR [ShipDate] IS NULL', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'CONSTRAINT', @level2name = N'CK_SalesOrderHeader_ShipDate';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Check constraint [Status] BETWEEN (0) AND (8)', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'CONSTRAINT', @level2name = N'CK_SalesOrderHeader_Status';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Check constraint [SubTotal] >= (0.00)', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'CONSTRAINT', @level2name = N'CK_SalesOrderHeader_SubTotal';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Check constraint [TaxAmt] >= (0.00)', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'CONSTRAINT', @level2name = N'CK_SalesOrderHeader_TaxAmt';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Default constraint value of GETDATE()', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Address', @level2type = N'CONSTRAINT', @level2name = N'DF_Address_ModifiedDate';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Default constraint value of NEWID()', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Address', @level2type = N'CONSTRAINT', @level2name = N'DF_Address_rowguid';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Default constraint value of GETDATE()', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Customer', @level2type = N'CONSTRAINT', @level2name = N'DF_Customer_ModifiedDate';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Default constraint value of 0', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Customer', @level2type = N'CONSTRAINT', @level2name = N'DF_Customer_NameStyle';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Default constraint value of NEWID()', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Customer', @level2type = N'CONSTRAINT', @level2name = N'DF_Customer_rowguid';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Default constraint value of NEWID()', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'CustomerAddress', @level2type = N'CONSTRAINT', @level2name = N'DF_CustomerAddress_rowguid';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Default constraint value of GETDATE()', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Product', @level2type = N'CONSTRAINT', @level2name = N'DF_Product_ModifiedDate';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Default constraint value of NEWID()', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Product', @level2type = N'CONSTRAINT', @level2name = N'DF_Product_rowguid';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Default constraint value of GETDATE()', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductCategory', @level2type = N'CONSTRAINT', @level2name = N'DF_ProductCategory_ModifiedDate';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Default constraint value of NEWID()()', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductCategory', @level2type = N'CONSTRAINT', @level2name = N'DF_ProductCategory_rowguid';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Default constraint value of GETDATE()', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductDescription', @level2type = N'CONSTRAINT', @level2name = N'DF_ProductDescription_ModifiedDate';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Default constraint value of NEWID()', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductDescription', @level2type = N'CONSTRAINT', @level2name = N'DF_ProductDescription_rowguid';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Default constraint value of GETDATE()', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductModel', @level2type = N'CONSTRAINT', @level2name = N'DF_ProductModel_ModifiedDate';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Default constraint value of NEWID()', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductModel', @level2type = N'CONSTRAINT', @level2name = N'DF_ProductModel_rowguid';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Default constraint value of GETDATE()', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductModelProductDescription', @level2type = N'CONSTRAINT', @level2name = N'DF_ProductModelProductDescription_ModifiedDate';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Default constraint value of GETDATE()', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderDetail', @level2type = N'CONSTRAINT', @level2name = N'DF_SalesOrderDetail_ModifiedDate';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Default constraint value of NEWID()', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderDetail', @level2type = N'CONSTRAINT', @level2name = N'DF_SalesOrderDetail_rowguid';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Default constraint value of 0.0', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderDetail', @level2type = N'CONSTRAINT', @level2name = N'DF_SalesOrderDetail_UnitPriceDiscount';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Default constraint value of 0.0', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'CONSTRAINT', @level2name = N'DF_SalesOrderHeader_Freight';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Default constraint value of GETDATE()', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'CONSTRAINT', @level2name = N'DF_SalesOrderHeader_ModifiedDate';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Default constraint value of 1 (TRUE)', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'CONSTRAINT', @level2name = N'DF_SalesOrderHeader_OnlineOrderFlag';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Default constraint value of GETDATE()', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'CONSTRAINT', @level2name = N'DF_SalesOrderHeader_OrderDate';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Default constraint value of 0', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'CONSTRAINT', @level2name = N'DF_SalesOrderHeader_RevisionNumber';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Default constraint value of NEWID()', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'CONSTRAINT', @level2name = N'DF_SalesOrderHeader_rowguid';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Default constraint value of 1', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'CONSTRAINT', @level2name = N'DF_SalesOrderHeader_Status';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Default constraint value of 0.0', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'CONSTRAINT', @level2name = N'DF_SalesOrderHeader_SubTotal';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Default constraint value of 0.0', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'CONSTRAINT', @level2name = N'DF_SalesOrderHeader_TaxAmt';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Foreign key constraint referencing Address.AddressID.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'CustomerAddress', @level2type = N'CONSTRAINT', @level2name = N'FK_CustomerAddress_Address_AddressID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Foreign key constraint referencing Customer.CustomerID.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'CustomerAddress', @level2type = N'CONSTRAINT', @level2name = N'FK_CustomerAddress_Customer_CustomerID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Foreign key constraint referencing ProductCategory.ProductCategoryID.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Product', @level2type = N'CONSTRAINT', @level2name = N'FK_Product_ProductCategory_ProductCategoryID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Foreign key constraint referencing ProductModel.ProductModelID.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Product', @level2type = N'CONSTRAINT', @level2name = N'FK_Product_ProductModel_ProductModelID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Foreign key constraint referencing ProductCategory.ProductCategoryID.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductCategory', @level2type = N'CONSTRAINT', @level2name = N'FK_ProductCategory_ProductCategory_ParentProductCategoryID_ProductCategoryID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Foreign key constraint referencing ProductDescription.ProductDescriptionID.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductModelProductDescription', @level2type = N'CONSTRAINT', @level2name = N'FK_ProductModelProductDescription_ProductDescription_ProductDescriptionID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Foreign key constraint referencing ProductModel.ProductModelID.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductModelProductDescription', @level2type = N'CONSTRAINT', @level2name = N'FK_ProductModelProductDescription_ProductModel_ProductModelID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Foreign key constraint referencing SalesOrderHeader.SalesOrderID.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderDetail', @level2type = N'CONSTRAINT', @level2name = N'FK_SalesOrderDetail_SalesOrderHeader_SalesOrderID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Foreign key constraint referencing Address.AddressID for BillTo.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'CONSTRAINT', @level2name = N'FK_SalesOrderHeader_Address_BillTo_AddressID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Foreign key constraint referencing Address.AddressID for ShipTo.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'CONSTRAINT', @level2name = N'FK_SalesOrderHeader_Address_ShipTo_AddressID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Foreign key constraint referencing Customer.CustomerID.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'CONSTRAINT', @level2name = N'FK_SalesOrderHeader_Customer_CustomerID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key (clustered) constraint', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Address', @level2type = N'CONSTRAINT', @level2name = N'PK_Address_AddressID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key (clustered) constraint', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Customer', @level2type = N'CONSTRAINT', @level2name = N'PK_Customer_CustomerID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key (clustered) constraint', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'CustomerAddress', @level2type = N'CONSTRAINT', @level2name = N'PK_CustomerAddress_CustomerID_AddressID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key (clustered) constraint', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Product', @level2type = N'CONSTRAINT', @level2name = N'PK_Product_ProductID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key (clustered) constraint', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductCategory', @level2type = N'CONSTRAINT', @level2name = N'PK_ProductCategory_ProductCategoryID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key (clustered) constraint', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductDescription', @level2type = N'CONSTRAINT', @level2name = N'PK_ProductDescription_ProductDescriptionID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key (clustered) constraint', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductModel', @level2type = N'CONSTRAINT', @level2name = N'PK_ProductModel_ProductModelID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key (clustered) constraint', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductModelProductDescription', @level2type = N'CONSTRAINT', @level2name = N'PK_ProductModelProductDescription_ProductModelID_ProductDescriptionID_Culture';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key (clustered) constraint', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderDetail', @level2type = N'CONSTRAINT', @level2name = N'PK_SalesOrderDetail_SalesOrderID_SalesOrderDetailID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key (clustered) constraint', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'CONSTRAINT', @level2name = N'PK_SalesOrderHeader_SalesOrderID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'AdventureWorksLT 2012 Sample OLTP Database';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'AFTER UPDATE trigger that updates the RevisionNumber and ModifiedDate columns in the SalesOrderHeader table.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'TRIGGER', @level2name = N'uSalesOrderHeader';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary filegroup for the AdventureWorks sample database.', @level0type = N'FILEGROUP', @level0name = N'PRIMARY';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Table value function returning every product category and its parent, if applicable.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'FUNCTION', @level1name = N'ufnGetAllCategories';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Table value function returning the customer ID, first name, and last name for a given customer.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'FUNCTION', @level1name = N'ufnGetCustomerInformation';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Scalar function returning the text representation of the Status column in the SalesOrderHeader table.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'FUNCTION', @level1name = N'ufnGetSalesOrderStatusText';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Nonclustered index.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Address', @level2type = N'INDEX', @level2name = N'IX_Address_AddressLine1_AddressLine2_City_StateProvince_PostalCode_CountryRegion';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Nonclustered index.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Address', @level2type = N'INDEX', @level2name = N'IX_Address_StateProvince';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Nonclustered index.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Customer', @level2type = N'INDEX', @level2name = N'IX_Customer_EmailAddress';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Nonclustered index.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderDetail', @level2type = N'INDEX', @level2name = N'IX_SalesOrderDetail_ProductID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Nonclustered index.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader', @level2type = N'INDEX', @level2name = N'IX_SalesOrderHeader_CustomerID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Clustered index on the view vProductAndDescription.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'VIEW', @level1name = N'vProductAndDescription', @level2type = N'INDEX', @level2name = N'IX_vProductAndDescription';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Logs error information in the ErrorLog table about the error that caused execution to jump to the CATCH block of a TRY...CATCH construct. Should be executed from within the scope of a CATCH block otherwise it will return without inserting error information.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'PROCEDURE', @level1name = N'uspLogError';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Prints error information about the error that caused execution to jump to the CATCH block of a TRY...CATCH construct. Should be executed from within the scope of a CATCH block otherwise it will return without printing any error information.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'PROCEDURE', @level1name = N'uspPrintError';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Contains objects related to products, customers, sales orders, and sales territories.', @level0type = N'SCHEMA', @level0name = N'SalesLT';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Input parameter for the table value function ufnGetCustomerInformation. Enter a valid CustomerID from the Sales.Customer table.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'FUNCTION', @level1name = N'ufnGetCustomerInformation', @level2type = N'PARAMETER', @level2name = N'@CustomerID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Input parameter for the scalar function ufnGetSalesOrderStatusText. Enter a valid integer.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'FUNCTION', @level1name = N'ufnGetSalesOrderStatusText', @level2type = N'PARAMETER', @level2name = N'@Status';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Output parameter for the stored procedure uspLogError. Contains the ErrorLogID value corresponding to the row inserted by uspLogError in the ErrorLog table.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'PROCEDURE', @level1name = N'uspLogError', @level2type = N'PARAMETER', @level2name = N'@ErrorLogID';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Current version number of the AdventureWorksLT 2012 sample database. ', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'BuildVersion';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Audit table tracking errors in the the AdventureWorks database that are caught by the CATCH block of a TRY...CATCH construct. Data is inserted by stored procedure dbo.uspLogError when it is executed from inside the CATCH block of a TRY...CATCH construct.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'ErrorLog';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Street address information for customers.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Address';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Customer information.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Customer';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Cross-reference table mapping customers to their address(es).', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'CustomerAddress';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Products sold or used in the manfacturing of sold products.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'Product';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'High-level product categorization.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductCategory';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Product descriptions in several languages.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductDescription';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Cross-reference table mapping product descriptions and the language the description is written in.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'ProductModelProductDescription';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Individual products associated with a specific sales order. See SalesOrderHeader.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderDetail';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'General sales order information.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'TABLE', @level1name = N'SalesOrderHeader';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Product names and descriptions. Product descriptions are provided in multiple languages.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'VIEW', @level1name = N'vProductAndDescription';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Displays the content from each element in the xml column CatalogDescription for each product in the Sales.ProductModel table that has catalog data.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'VIEW', @level1name = N'vProductModelCatalogDescription';


GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Collection of XML schemas for the CatalogDescription column in the Sales.ProductModel table.', @level0type = N'SCHEMA', @level0name = N'SalesLT', @level1type = N'XML SCHEMA COLLECTION', @level1name = N'ProductDescriptionSchemaCollection';


GO

