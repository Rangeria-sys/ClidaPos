/* ====================================================================================================
 NEXUS POS/ERP - CANONICAL DATABASE SCHEMA
 Source: script.sql (as supplied) - reformatted to UTF-8 and organized by functional module.
 No tables, columns, types, or constraints have been altered, renamed, added, or removed.
 This file is the single source of truth for Nexus.Data entity mappings.
 ==================================================================================================== */


-- ------------------------------------------------------------------------------------------------
-- MODULE: Identity And Security
-- Login, per-user module permissions, staff attendance punch clock
-- ------------------------------------------------------------------------------------------------

CREATE TABLE [dbo].[Registration](
	[UserID] [nchar](100) NOT NULL,
	[UserType] [nchar](30) NOT NULL,
	[Password] [nchar](50) NOT NULL,
	[Name] [nchar](150) NOT NULL,
	[ContactNo] [nchar](50) NULL,
	[EmailID] [nchar](150) NULL,
	[JoiningDate] [datetime] NOT NULL,
	[Active] [nchar](10) NULL,
	[SSN] [nchar](50) NULL,
	[PayrollType] [nchar](30) NULL,
	[CardNo] [nchar](50) NULL,
	[AutoLogout] [nchar](10) NULL,
 CONSTRAINT [PK_Registration] PRIMARY KEY CLUSTERED 
(
	[UserID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[UserRights](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[ModuleName] [nchar](200) NULL,
	[UR_Save] [bit] NULL,
	[UR_Update] [bit] NULL,
	[UR_Delete] [bit] NULL,
	[UR_View] [bit] NULL,
	[UserID] [nchar](100) NULL,
 CONSTRAINT [PK_UserRights] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Activation](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[HardwareID] [nchar](100) NOT NULL,
	[ActivationID] [nchar](150) NOT NULL,
 CONSTRAINT [PK_Activation] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[ClockIN](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [nchar](100) NULL,
	[ClockINDate] [datetime] NULL,
 CONSTRAINT [PK_ClockIN] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[ClockOUT](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[ClockInID] [int] NULL,
	[ClockOUTDate] [datetime] NULL,
 CONSTRAINT [PK_ClockOUT] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


-- ------------------------------------------------------------------------------------------------
-- MODULE: Shift And Register
-- Store-wide till/work period open-close tracking
-- ------------------------------------------------------------------------------------------------

CREATE TABLE [dbo].[WorkPeriodStart](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[WPStart] [datetime] NOT NULL,
	[Status] [nchar](20) NOT NULL,
 CONSTRAINT [PK_WorkPeriodStart] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[WorkPeriodEnd](
	[Id] [int] NOT NULL,
	[WPEnd] [datetime] NOT NULL,
 CONSTRAINT [PK_WorkPeriodEnd] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Payment_Withdraw](
	[id] [int] NOT NULL,
	[AccountFrom] [nchar](50) NULL,
	[PaymentMode] [nchar](50) NULL,
	[Date] [datetime] NULL,
	[Amount] [decimal](18, 2) NULL,
	[ReceiverName] [nchar](150) NULL,
	[PhoneNo] [nchar](50) NULL,
	[Notes] [nchar](200) NULL,
 CONSTRAINT [PK_Payment_Withdraw] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


-- ------------------------------------------------------------------------------------------------
-- MODULE: Store Settings And Peripherals
-- Global config: mode toggle inputs, payment gateway, hardware, currency
-- ------------------------------------------------------------------------------------------------

CREATE TABLE [dbo].[CMISetting](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[C4] [int] NULL,
	[C5] [int] NULL,
	[C6] [int] NULL,
 CONSTRAINT [PK_CMISetting] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[EmailSetting](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ServerName] [nchar](150) NOT NULL,
	[SMTPAddress] [nvarchar](250) NOT NULL,
	[Username] [nchar](200) NOT NULL,
	[Password] [nchar](100) NOT NULL,
	[Port] [int] NOT NULL,
	[TLS_SSL_Required] [nchar](10) NOT NULL,
	[IsDefault] [nchar](10) NOT NULL,
	[IsActive] [nchar](10) NOT NULL,
 CONSTRAINT [PK_EmailSetting] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[SMSSetting](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[APIURL] [nvarchar](max) NOT NULL,
	[IsDefault] [nchar](10) NOT NULL,
	[IsEnabled] [nchar](10) NOT NULL,
 CONSTRAINT [PK_SMSSetting] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[MpesaSetting](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[C1] [nchar](20) NULL,
	[C2] [nvarchar](250) NULL,
	[C3] [nchar](150) NULL,
	[C4] [nchar](150) NULL,
	[C5] [nvarchar](250) NULL,
	[C6] [nchar](20) NULL,
 CONSTRAINT [PK_MpesaSetting] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[OtherSetting](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[ParcelCharges] [decimal](18, 2) NULL,
	[HomeDeliveryCharges] [decimal](18, 2) NULL,
	[VAT] [decimal](18, 2) NULL,
	[ServiceTax] [decimal](18, 2) NULL,
	[ServiceCharges] [decimal](18, 2) NULL,
	[TA] [nchar](10) NULL,
	[HD] [nchar](10) NULL,
	[EB] [nchar](10) NULL,
	[KG] [nchar](10) NULL,
	[TaxType] [nchar](20) NULL,
	[PDP] [nchar](20) NULL,
	[TL] [nchar](20) NULL,
	[ECDI] [nchar](10) NULL,
	[ECDB] [nchar](10) NULL,
	[ECTA] [nchar](10) NULL,
	[ECHD] [nchar](10) NULL,
	[ECEB] [nchar](10) NULL,
	[A1] [nchar](10) NULL,
	[ANB] [nchar](10) NULL,
	[TLAP] [nchar](10) NULL,
	[A23X] [nchar](10) NULL,
	[CATX] [nchar](10) NULL,
 CONSTRAINT [PK_OtherCharges] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[PosPrinterSetting](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TillID] [nchar](200) NULL,
	[PrinterName] [nvarchar](250) NULL,
	[IsEnabled] [nchar](20) NULL,
	[CashDrawer] [nchar](20) NULL,
	[CustomerDisplay] [nchar](20) NULL,
	[CDPort] [nchar](20) NULL,
	[CallerID] [nchar](20) NULL,
	[CallerIDPort] [nchar](20) NULL,
	[SMII] [nchar](50) NULL,
	[CCD] [nchar](20) NULL,
	[FilePath] [nvarchar](max) NULL,
	[PT] [nchar](30) NULL,
	[PTipAddress] [nchar](100) NULL,
	[PTPortNo] [nchar](30) NULL,
	[WS] [nchar](30) NULL,
	[WSPortNo] [nchar](30) NULL,
	[W1] [int] NULL,
	[W3] [int] NULL,
	[W2] [nchar](10) NULL,
 CONSTRAINT [PK_PosPrinterSetting] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[LoyaltySetting](
	[LoyaltyName] [nchar](150) NOT NULL,
	[Amount] [decimal](18, 2) NULL,
	[Points] [decimal](18, 2) NULL,
 CONSTRAINT [PK_LoyaltySetting] PRIMARY KEY CLUSTERED 
(
	[LoyaltyName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Currency](
	[CurrencyCode] [nchar](10) NOT NULL,
	[Currencyname] [nchar](200) NOT NULL,
	[Rate] [decimal](18, 2) NOT NULL,
 CONSTRAINT [PK_CurrencyRate] PRIMARY KEY CLUSTERED 
(
	[CurrencyCode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Hotel](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[HotelName] [nchar](150) NULL,
	[AddressLine1] [nvarchar](250) NULL,
	[AddressLine2] [nvarchar](250) NULL,
	[AddressLine3] [nvarchar](250) NULL,
	[ContactNo] [nchar](100) NULL,
	[EmailID] [nchar](150) NULL,
	[TIN] [nchar](30) NULL,
	[STNo] [nchar](30) NULL,
	[CIN] [nchar](30) NULL,
	[Logo] [image] NULL,
	[BaseCurrency] [nchar](200) NULL,
	[CurrencyCode] [nchar](10) NULL,
	[TicketFooterMessage] [nvarchar](250) NULL,
	[ShowLogo] [nchar](20) NULL,
	[CapitalAccount] [decimal](18, 2) NULL,
	[DBLocation] [nvarchar](max) NULL,
 CONSTRAINT [PK_Hotel] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[Kitchen](
	[Kitchenname] [nchar](200) NOT NULL,
	[Printer] [nvarchar](250) NOT NULL,
	[IsEnabled] [nchar](10) NOT NULL,
 CONSTRAINT [PK_Kitchen] PRIMARY KEY CLUSTERED 
(
	[Kitchenname] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[GridGrouping](
	[Id] [int] NOT NULL,
	[Col1] [nvarchar](250) NOT NULL,
	[Col2] [int] NOT NULL,
 CONSTRAINT [PK_GridGrouping] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


-- ------------------------------------------------------------------------------------------------
-- MODULE: HR And Payroll
-- Employee records and payroll
-- ------------------------------------------------------------------------------------------------

CREATE TABLE [dbo].[EmployeeRegistration](
	[EmpId] [int] NOT NULL,
	[EmployeeID] [nchar](15) NOT NULL,
	[EmployeeName] [nchar](150) NOT NULL,
	[Address] [nvarchar](250) NOT NULL,
	[City] [nchar](150) NOT NULL,
	[ContactNo] [nchar](30) NOT NULL,
	[Email] [nchar](150) NOT NULL,
	[DateOfJoining] [datetime] NOT NULL,
	[Photo] [image] NOT NULL,
	[Active] [nchar](20) NULL,
 CONSTRAINT [PK_EmployeeRegistration] PRIMARY KEY CLUSTERED 
(
	[EmpId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[Payroll](
	[ID] [int] NOT NULL,
	[PaymentID] [nchar](30) NULL,
	[PaymentDate] [datetime] NULL,
	[PaymentMode] [nchar](150) NULL,
	[UserID] [nchar](100) NULL,
	[Rate] [decimal](18, 2) NULL,
	[Hours] [decimal](18, 2) NULL,
	[GrossSalary] [decimal](18, 2) NULL,
	[CPPPer] [decimal](18, 2) NULL,
	[CPP] [decimal](18, 2) NULL,
	[EIPer] [decimal](18, 2) NULL,
	[EI] [decimal](18, 2) NULL,
	[FedTaxPer] [decimal](18, 2) NULL,
	[FedTax] [decimal](18, 2) NULL,
	[NetPay] [decimal](18, 2) NULL,
	[Remarks] [nchar](250) NULL,
	[VPPer] [decimal](18, 2) NULL,
	[VP] [decimal](18, 2) NULL,
 CONSTRAINT [PK_Payroll] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Payroll_MB](
	[ID] [int] NOT NULL,
	[PaymentID] [nchar](30) NULL,
	[PaymentDate] [datetime] NULL,
	[PaymentMode] [nchar](150) NULL,
	[UserID] [nchar](100) NULL,
	[GrossSalary] [decimal](18, 2) NULL,
	[CPPPer] [decimal](18, 2) NULL,
	[CPP] [decimal](18, 2) NULL,
	[EIPer] [decimal](18, 2) NULL,
	[EI] [decimal](18, 2) NULL,
	[FedTaxPer] [decimal](18, 2) NULL,
	[FedTax] [decimal](18, 2) NULL,
	[NetPay] [decimal](18, 2) NULL,
	[Month] [nchar](30) NULL,
	[Year] [int] NULL,
	[Remarks] [nchar](250) NULL,
	[VPPer] [decimal](18, 2) NULL,
	[VP] [decimal](18, 2) NULL,
 CONSTRAINT [PK_Payroll_MB] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


-- ------------------------------------------------------------------------------------------------
-- MODULE: Catalog Items Categories
-- Products, categories, recipes, modifiers - mode-dependent catalog data
-- ------------------------------------------------------------------------------------------------

CREATE TABLE [dbo].[Product](
	[PID] [int] NOT NULL,
	[ProductCode] [nchar](30) NOT NULL,
	[ProductName] [nchar](200) NOT NULL,
	[Category] [nchar](150) NULL,
	[Description] [nvarchar](max) NULL,
	[Unit] [nchar](50) NULL,
	[Price] [decimal](18, 2) NOT NULL,
	[ReorderPoint] [int] NOT NULL,
	[P_Supplier] [nchar](150) NULL,
 CONSTRAINT [PK_Product] PRIMARY KEY CLUSTERED 
(
	[PID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[ProductC](
	[PID] [int] NOT NULL,
	[ProductCode] [nchar](30) NOT NULL,
	[ProductName] [nchar](200) NOT NULL,
	[Category] [nchar](150) NULL,
	[Description] [nvarchar](max) NULL,
	[Unit] [nchar](50) NULL,
	[Price] [decimal](18, 2) NOT NULL,
	[ReorderPoint] [int] NOT NULL,
	[P_Supplier] [nchar](150) NULL,
	[pkgUnitCode] [nvarchar](50) NULL,
	[taxCode] [nvarchar](50) NULL,
	[itemTypeCode] [nvarchar](50) NULL,
	[qtyUnitCode] [nvarchar](50) NULL,
	[orgCountryCode] [nvarchar](50) NULL,
	[eTIMSCode] [nvarchar](50) NULL,
	[classCode] [nvarchar](50) NULL,
 CONSTRAINT [PK_ProductC] PRIMARY KEY CLUSTERED 
(
	[PID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[Category](
	[CategoryName] [nvarchar](250) NOT NULL,
	[VAT] [decimal](18, 2) NULL,
	[ST] [decimal](18, 2) NULL,
	[SC] [decimal](18, 2) NULL,
	[BackColor] [int] NULL,
	[Kitchen] [nchar](200) NULL,
	[CAT_ID] [int] NULL,
 CONSTRAINT [PK_Category] PRIMARY KEY CLUSTERED 
(
	[CategoryName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[UnitMaster](
	[Unit] [nchar](50) NOT NULL,
 CONSTRAINT [PK_UnitMaster] PRIMARY KEY CLUSTERED 
(
	[Unit] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Modifiers](
	[MIM_ID] [int] NOT NULL,
	[ModifierName] [nvarchar](250) NULL,
	[Item] [nvarchar](250) NULL,
	[Rate] [decimal](18, 2) NULL,
	[BackColor] [int] NULL,
 CONSTRAINT [PK_Modifiers] PRIMARY KEY CLUSTERED 
(
	[MIM_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Dish](
	[DishName] [nvarchar](250) NOT NULL,
	[Category] [nvarchar](250) NULL,
	[DIRate] [decimal](18, 2) NULL,
	[TARate] [decimal](18, 2) NULL,
	[HDRate] [decimal](18, 2) NULL,
	[BackColor] [int] NULL,
	[Barcode] [nchar](30) NULL,
	[MI_Status] [nchar](20) NULL,
	[DishID] [int] NULL,
	[Photo] [nvarchar](max) NULL,
	[DishNameArabic] [nvarchar](250) NULL,
	[FColor] [nchar](20) NULL,
	[pkgUnitCode] [nvarchar](50) NULL,
	[taxCode] [nvarchar](50) NULL,
	[itemTypeCode] [nvarchar](50) NULL,
	[qtyUnitCode] [nvarchar](50) NULL,
	[orgCountryCode] [nvarchar](50) NULL,
	[eTIMSCode] [nvarchar](50) NULL,
	[classCode] [nvarchar](50) NULL,
 CONSTRAINT [PK_Dish] PRIMARY KEY CLUSTERED 
(
	[DishName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[PizzaMaster](
	[Pizza_ID] [int] NOT NULL,
	[PizzaName] [nchar](200) NULL,
	[PizzaSize] [nchar](100) NULL,
	[Description] [nvarchar](max) NULL,
	[Discount] [decimal](18, 2) NULL,
	[Rate] [decimal](18, 2) NULL,
	[BackColor] [int] NULL,
 CONSTRAINT [PK_PizzaMaster] PRIMARY KEY CLUSTERED 
(
	[Pizza_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[PizzaModifier](
	[PM_ID] [int] NOT NULL,
	[ModifierName] [nchar](150) NULL,
	[PizzaID] [int] NULL,
	[Rate] [decimal](18, 2) NULL,
	[BackColor] [int] NULL,
 CONSTRAINT [PK_PizzaModifier] PRIMARY KEY CLUSTERED 
(
	[PM_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[PizzaSize](
	[Size] [nchar](100) NOT NULL,
 CONSTRAINT [PK_PizzaSize] PRIMARY KEY CLUSTERED 
(
	[Size] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[PizzaTopping](
	[T_ID] [int] NOT NULL,
	[ToppingName] [nchar](200) NULL,
	[ToppingSize] [nchar](50) NULL,
	[PizzaSize] [nchar](100) NULL,
	[Rate] [decimal](18, 2) NULL,
	[BackColor] [int] NULL,
 CONSTRAINT [PK_PizzaTopping] PRIMARY KEY CLUSTERED 
(
	[T_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Recipe](
	[R_ID] [int] NOT NULL,
	[RecipeName] [nchar](200) NOT NULL,
	[Dish] [nvarchar](250) NOT NULL,
	[FixedCost] [decimal](18, 2) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[R_DishNameArabic] [nchar](200) NULL,
 CONSTRAINT [PK_Recipe] PRIMARY KEY CLUSTERED 
(
	[R_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[Recipe_Join](
	[RJ_ID] [int] IDENTITY(1,1) NOT NULL,
	[RecipeID] [int] NOT NULL,
	[ProductID] [int] NOT NULL,
	[Quantity] [decimal](18, 3) NULL,
	[CostPerUnit] [decimal](18, 2) NULL,
	[TotalItemCost] [decimal](18, 2) NULL,
 CONSTRAINT [PK_Recipe_Join] PRIMARY KEY CLUSTERED 
(
	[RJ_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[RMCategory](
	[CategoryName] [nchar](150) NOT NULL,
 CONSTRAINT [PK_RMCategory] PRIMARY KEY CLUSTERED 
(
	[CategoryName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[RM_Used](
	[RM_ID] [int] NOT NULL,
	[BillNo] [nchar](30) NULL,
	[BillDate] [datetime] NULL,
 CONSTRAINT [PK_RM_Used] PRIMARY KEY CLUSTERED 
(
	[RM_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[RM_Used_Join](
	[RMJ_ID] [int] IDENTITY(1,1) NOT NULL,
	[RawMaterialID] [int] NULL,
	[ProductID] [int] NULL,
	[Quantity] [decimal](18, 2) NULL,
 CONSTRAINT [PK_RM_Used_Join] PRIMARY KEY CLUSTERED 
(
	[RMJ_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[PosGrouping](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Col1] [nvarchar](250) NULL,
	[Col2] [decimal](18, 2) NULL,
	[Col3] [decimal](18, 2) NULL,
	[Col4] [decimal](18, 2) NULL,
	[Col5] [decimal](18, 2) NULL,
	[Col6] [decimal](18, 2) NULL,
	[Col7] [decimal](18, 2) NULL,
	[Col8] [decimal](18, 2) NULL,
	[Col9] [decimal](18, 2) NULL,
	[Col10] [decimal](18, 2) NULL,
	[Col11] [decimal](18, 2) NULL,
	[Col12] [decimal](18, 2) NULL,
	[Col13] [decimal](18, 2) NULL,
	[Col14] [nvarchar](max) NULL,
	[Col15] [int] NULL,
	[Col16] [nchar](200) NULL,
	[Col17] [nvarchar](max) NULL,
 CONSTRAINT [PK_PosGroupingTable] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[PosGrouping1](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Col1] [nvarchar](250) NULL,
	[Col2] [decimal](18, 2) NULL,
	[Col3] [decimal](18, 2) NULL,
	[Col4] [decimal](18, 2) NULL,
	[Col5] [decimal](18, 2) NULL,
	[Col6] [decimal](18, 2) NULL,
	[Col7] [decimal](18, 2) NULL,
	[Col8] [decimal](18, 2) NULL,
	[Col9] [decimal](18, 2) NULL,
	[Col10] [decimal](18, 2) NULL,
	[Col11] [decimal](18, 2) NULL,
	[Col12] [decimal](18, 2) NULL,
	[Col13] [decimal](18, 2) NULL,
	[Col14] [nvarchar](max) NULL,
	[Col15] [int] NULL,
	[Col16] [nchar](200) NULL,
	[Col17] [nvarchar](max) NULL,
	[Col18] [nchar](20) NULL,
 CONSTRAINT [PK_PosGrouping1Table] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


-- ------------------------------------------------------------------------------------------------
-- MODULE: Inventory And Stock
-- Stock levels, adjustments, transfers, opening balances
-- ------------------------------------------------------------------------------------------------

CREATE TABLE [dbo].[Product_OpeningStock](
	[PS_ID] [int] IDENTITY(1,1) NOT NULL,
	[ProductID] [int] NOT NULL,
	[Warehouse] [nchar](250) NOT NULL,
	[Qty] [decimal](18, 2) NOT NULL,
	[HasExpiryDate] [nchar](10) NULL,
	[ExpiryDate] [nchar](50) NULL,
 CONSTRAINT [PK_Product_OpeningStock] PRIMARY KEY CLUSTERED 
(
	[PS_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Product_OpeningStockC](
	[PS_ID] [int] IDENTITY(1,1) NOT NULL,
	[ProductID] [int] NOT NULL,
	[Warehouse] [nchar](250) NOT NULL,
	[Qty] [decimal](18, 2) NOT NULL,
	[HasExpiryDate] [nchar](10) NULL,
	[ExpiryDate] [nchar](50) NULL,
 CONSTRAINT [PK_Product_OpeningStockC] PRIMARY KEY CLUSTERED 
(
	[PS_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Stock_Store](
	[St_ID] [int] NOT NULL,
	[Date] [datetime] NOT NULL,
	[Remarks] [nvarchar](250) NOT NULL,
 CONSTRAINT [PK_Stock_Store] PRIMARY KEY CLUSTERED 
(
	[St_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Stock_Store_Join](
	[SSJ_ID] [int] IDENTITY(1,1) NOT NULL,
	[StockID] [int] NOT NULL,
	[Dish] [nvarchar](250) NOT NULL,
	[Qty] [decimal](18, 2) NOT NULL,
 CONSTRAINT [PK_Stock_Store_Join] PRIMARY KEY CLUSTERED 
(
	[SSJ_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[StockAdjustment_MI](
	[SA_ID] [int] NOT NULL,
	[Date] [datetime] NULL,
	[Dish] [nvarchar](250) NULL,
	[AdjustmentType] [nchar](20) NULL,
	[Qty] [decimal](18, 2) NULL,
	[Reason] [nchar](200) NULL,
 CONSTRAINT [PK_StockAdjustment_MI] PRIMARY KEY CLUSTERED 
(
	[SA_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[StockAdjustment_Store](
	[SA_ID] [int] NOT NULL,
	[Date] [datetime] NULL,
	[ProductID] [int] NULL,
	[AdjustmentType] [nchar](20) NULL,
	[Qty] [decimal](18, 3) NULL,
	[Reason] [nchar](200) NULL,
 CONSTRAINT [PK_StockAdjustment_Kitchen] PRIMARY KEY CLUSTERED 
(
	[SA_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[StockAdjustment_Warehouse](
	[SA_ID] [int] NOT NULL,
	[Date] [datetime] NULL,
	[Warehouse] [nchar](250) NULL,
	[ProductID] [int] NULL,
	[AdjustmentType] [nchar](20) NULL,
	[Qty] [decimal](18, 3) NULL,
	[Reason] [nchar](200) NULL,
 CONSTRAINT [PK_StockAdjustment_Warehouse] PRIMARY KEY CLUSTERED 
(
	[SA_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[StockTransfer](
	[ST_ID] [int] NOT NULL,
	[Date] [datetime] NOT NULL,
	[Kitchen] [nchar](200) NOT NULL,
 CONSTRAINT [PK_StockTransfer_1] PRIMARY KEY CLUSTERED 
(
	[ST_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[StockTransfer_Join](
	[STJ_ID] [int] IDENTITY(1,1) NOT NULL,
	[StockTransferID] [int] NOT NULL,
	[Warehouse] [nchar](250) NOT NULL,
	[ProductID] [int] NOT NULL,
	[ExpiryDate] [nchar](50) NULL,
	[Qty] [decimal](18, 2) NOT NULL,
 CONSTRAINT [PK_StockTransfer] PRIMARY KEY CLUSTERED 
(
	[STJ_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Temp_Stock](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ProductID] [int] NOT NULL,
	[Warehouse] [nchar](250) NOT NULL,
	[Qty] [decimal](18, 2) NOT NULL,
	[HasExpiryDate] [nchar](10) NULL,
	[ExpiryDate] [nchar](50) NULL,
 CONSTRAINT [PK_Temp_Stock] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Temp_Stock_RM](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ProductID] [int] NOT NULL,
	[Qty] [decimal](18, 2) NOT NULL,
 CONSTRAINT [PK_Temp_Stock_RM] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Temp_Stock_Store](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Dish] [nvarchar](250) NOT NULL,
	[Qty] [decimal](18, 2) NOT NULL,
 CONSTRAINT [PK_Temp_Stock_Store] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Warehouse](
	[WarehouseName] [nchar](250) NOT NULL,
	[Address] [nvarchar](250) NOT NULL,
	[WarehouseType] [nchar](200) NOT NULL,
	[City] [nchar](200) NOT NULL,
 CONSTRAINT [PK_Warehouse] PRIMARY KEY CLUSTERED 
(
	[WarehouseName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[WarehouseType](
	[Type] [nchar](200) NOT NULL,
 CONSTRAINT [PK_WarehouseType] PRIMARY KEY CLUSTERED 
(
	[Type] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


-- ------------------------------------------------------------------------------------------------
-- MODULE: Purchasing And Suppliers
-- Purchase orders, goods receipt, supplier ledgers
-- ------------------------------------------------------------------------------------------------

CREATE TABLE [dbo].[Purchase](
	[ST_ID] [int] NOT NULL,
	[InvoiceNo] [nchar](30) NOT NULL,
	[Date] [datetime] NOT NULL,
	[PurchaseType] [nchar](20) NOT NULL,
	[Supplier_ID] [int] NOT NULL,
	[SubTotal] [decimal](18, 2) NOT NULL,
	[DiscountPer] [decimal](18, 2) NOT NULL,
	[Discount] [decimal](18, 2) NOT NULL,
	[PreviousDue] [decimal](18, 2) NOT NULL,
	[FreightCharges] [decimal](18, 2) NOT NULL,
	[OtherCharges] [decimal](18, 2) NOT NULL,
	[Total] [decimal](18, 2) NOT NULL,
	[RoundOff] [decimal](18, 2) NOT NULL,
	[GrandTotal] [decimal](18, 2) NOT NULL,
	[TotalPayment] [decimal](18, 2) NOT NULL,
	[PaymentDue] [decimal](18, 2) NOT NULL,
	[Remarks] [nvarchar](max) NULL,
	[HST] [decimal](18, 2) NULL,
	[HSTPer] [decimal](18, 2) NULL,
 CONSTRAINT [PK_Purchase] PRIMARY KEY CLUSTERED 
(
	[ST_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[Purchase_Join](
	[SP_ID] [int] IDENTITY(1,1) NOT NULL,
	[PurchaseID] [int] NOT NULL,
	[ProductID] [int] NOT NULL,
	[Qty] [decimal](18, 2) NOT NULL,
	[Price] [decimal](18, 2) NOT NULL,
	[TotalAmount] [decimal](18, 2) NOT NULL,
	[Warehouse] [nchar](250) NOT NULL,
	[HasExpirydate] [nchar](10) NULL,
	[ExpiryDate] [nchar](50) NULL,
 CONSTRAINT [PK_Purchase_Join] PRIMARY KEY CLUSTERED 
(
	[SP_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[PurchaseOrder](
	[PO_ID] [int] NOT NULL,
	[PONumber] [nchar](50) NOT NULL,
	[Date] [datetime] NULL,
	[Supplier_ID] [int] NULL,
	[Terms] [nvarchar](max) NULL,
	[SubTotal] [decimal](18, 2) NULL,
	[VATPer] [decimal](18, 2) NULL,
	[VATAmount] [decimal](18, 2) NULL,
	[GrandTotal] [decimal](18, 2) NULL,
	[TaxType] [nchar](20) NULL,
 CONSTRAINT [PK_PurchaseOrder] PRIMARY KEY CLUSTERED 
(
	[PO_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[PurchaseOrder_Join](
	[POJ_ID] [int] IDENTITY(1,1) NOT NULL,
	[PurchaseOrderID] [int] NOT NULL,
	[ProductID] [int] NOT NULL,
	[Qty] [decimal](18, 2) NOT NULL,
	[PricePerUnit] [decimal](18, 2) NULL,
	[Amount] [decimal](18, 2) NULL,
 CONSTRAINT [PK_PurchaseOrder_Join] PRIMARY KEY CLUSTERED 
(
	[POJ_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[PurchaseOrder2](
	[PO_ID] [int] NOT NULL,
	[PONumber] [nchar](50) NOT NULL,
	[Date] [datetime] NULL,
	[Supplier_ID] [int] NULL,
	[Terms] [nvarchar](max) NULL,
	[SubTotal] [decimal](18, 2) NULL,
	[VATPer] [decimal](18, 2) NULL,
	[VATAmount] [decimal](18, 2) NULL,
	[GrandTotal] [decimal](18, 2) NULL,
	[TaxType] [nchar](20) NULL,
 CONSTRAINT [PK_PurchaseOrder2] PRIMARY KEY CLUSTERED 
(
	[PO_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[PurchaseOrder2_Join](
	[POJ_ID] [int] IDENTITY(1,1) NOT NULL,
	[PurchaseOrderID] [int] NOT NULL,
	[ProductID] [int] NOT NULL,
	[Qty] [decimal](18, 2) NOT NULL,
	[PricePerUnit] [decimal](18, 2) NULL,
	[Amount] [decimal](18, 2) NULL,
	[Note] [nvarchar](100) NULL,
 CONSTRAINT [PK_PurchaseOrder2_Join] PRIMARY KEY CLUSTERED 
(
	[POJ_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[PurchaseOrder3](
	[PO_ID] [int] NOT NULL,
	[PONumber] [nchar](50) NOT NULL,
	[Date] [datetime] NULL,
	[Supplier_ID] [int] NULL,
	[Terms] [nvarchar](max) NULL,
	[SubTotal] [decimal](18, 2) NULL,
	[VATPer] [decimal](18, 2) NULL,
	[VATAmount] [decimal](18, 2) NULL,
	[GrandTotal] [decimal](18, 2) NULL,
	[TaxType] [nchar](20) NULL,
 CONSTRAINT [PK_PurchaseOrder3] PRIMARY KEY CLUSTERED 
(
	[PO_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[PurchaseOrder3_Join](
	[POJ_ID] [int] IDENTITY(1,1) NOT NULL,
	[PurchaseOrderID] [int] NOT NULL,
	[ProductID] [int] NOT NULL,
	[Qty] [decimal](18, 2) NOT NULL,
	[PricePerUnit] [decimal](18, 2) NULL,
	[Amount] [decimal](18, 2) NULL,
 CONSTRAINT [PK_PurchaseOrder3_Join] PRIMARY KEY CLUSTERED 
(
	[POJ_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Supplier](
	[ID] [int] NOT NULL,
	[SupplierID] [nchar](30) NOT NULL,
	[Name] [nchar](200) NULL,
	[Address] [nvarchar](250) NULL,
	[City] [nchar](200) NULL,
	[State] [nchar](150) NULL,
	[ZipCode] [nchar](15) NULL,
	[ContactNo] [nchar](150) NULL,
	[EmailID] [nchar](200) NULL,
	[Remarks] [nvarchar](max) NULL,
	[TIN] [nchar](100) NULL,
	[STNo] [nchar](100) NULL,
	[CST] [nchar](100) NULL,
	[PAN] [nchar](100) NULL,
	[AccountName] [nchar](150) NULL,
	[AccountNumber] [nchar](100) NULL,
	[Bank] [nchar](150) NULL,
	[Branch] [nchar](150) NULL,
	[IFSCCode] [nchar](50) NULL,
	[OpeningBalance] [decimal](18, 2) NULL,
	[OpeningBalanceType] [nchar](30) NULL,
 CONSTRAINT [PK_Supplier] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[SupplierLedgerBook](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Date] [datetime] NOT NULL,
	[Name] [nchar](200) NOT NULL,
	[LedgerNo] [nchar](50) NOT NULL,
	[Label] [nchar](200) NOT NULL,
	[Debit] [decimal](18, 2) NOT NULL,
	[Credit] [decimal](18, 2) NOT NULL,
	[PartyID] [nchar](20) NULL,
 CONSTRAINT [PK_SupplierLedgerBook] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


-- ------------------------------------------------------------------------------------------------
-- MODULE: Restaurant Floor And Tables
-- Table layout, mapping, reservations - Restaurant mode
-- ------------------------------------------------------------------------------------------------

CREATE TABLE [dbo].[R_Table](
	[TableNo] [nchar](30) NOT NULL,
	[FloorNo] [nchar](50) NULL,
	[Status] [nchar](30) NOT NULL,
	[BkColor] [int] NOT NULL,
	[ShapeX] [nchar](20) NULL,
 CONSTRAINT [PK_Table] PRIMARY KEY CLUSTERED 
(
	[TableNo] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[TableLayout](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[TableNo] [nchar](30) NULL,
	[Px] [decimal](18, 2) NULL,
	[Py] [decimal](18, 2) NULL,
	[Lx] [decimal](18, 2) NULL,
	[Ly] [decimal](18, 2) NULL,
	[FontSize] [decimal](18, 2) NULL,
	[Shape] [nchar](20) NULL,
 CONSTRAINT [PK_TableLayout] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[TableMapping](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TableNo] [nchar](30) NULL,
	[UserID] [nchar](100) NULL,
 CONSTRAINT [PK_TableMapping] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[TableReservation](
	[Id] [int] NOT NULL,
	[CustomerName] [nchar](150) NULL,
	[ContactNo] [nchar](50) NULL,
	[Date] [datetime] NULL,
	[TimeFrom] [datetime] NULL,
	[TimeTo] [datetime] NULL,
	[TableNo] [nchar](30) NULL,
	[NoOfGuests] [int] NULL,
	[Status] [nchar](30) NULL,
 CONSTRAINT [PK_TableReservation] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


-- ------------------------------------------------------------------------------------------------
-- MODULE: Restaurant Billing And KOT
-- Dine-in/Home-delivery/Takeaway/EB billing channels, KOT, catering
-- ------------------------------------------------------------------------------------------------

CREATE TABLE [dbo].[RestaurantPOS_BillingInfoKOT](
	[Id] [int] NOT NULL,
	[BillNo] [nchar](15) NULL,
	[BillDate] [datetime] NULL,
	[ODN] [nchar](30) NULL,
	[KOTDiscountPer] [decimal](18, 4) NULL,
	[KOTDiscountAmt] [decimal](18, 2) NULL,
	[GrandTotal] [decimal](18, 2) NULL,
	[Cash] [decimal](18, 2) NULL,
	[Change] [decimal](18, 2) NULL,
	[Operator] [nchar](100) NULL,
	[PaymentMode] [nchar](100) NULL,
	[ExchangeRate] [decimal](18, 2) NULL,
	[CurrencyCode] [nchar](10) NULL,
	[Member_ID] [nchar](10) NULL,
	[Waiter] [nchar](200) NULL,
	[GiftCardID] [nchar](20) NULL,
	[GiftCardAmount] [decimal](18, 2) NULL,
	[LP] [int] NULL,
	[LA] [decimal](18, 2) NULL,
	[CustomerName] [nchar](150) NULL,
	[PhoneNo] [nchar](50) NULL,
	[EmailID] [nchar](150) NULL,
	[TaxType] [nchar](20) NULL,
	[Card] [decimal](18, 2) NULL,
	[NoofPerson] [int] NULL,
	[DIB_Status] [nchar](40) NULL,
	[NpPaid] [int] NULL,
	[BillType] [nchar](30) NULL,
	[Tip] [decimal](18, 2) NULL,
	[scuReceiptNo] [nchar](50) NULL,
	[totalTaxableAmount] [decimal](18, 2) NULL,
	[totalTaxAmount] [decimal](18, 2) NULL,
	[internalData] [nchar](100) NULL,
	[signature] [nchar](100) NULL,
	[scdcId] [nchar](100) NULL,
	[scuReceiptDate] [datetime] NULL,
	[invoiceVerificationUrl] [nvarchar](max) NULL,
	[zeroRated] [decimal](18, 2) NULL,
	[nonVatable] [decimal](18, 2) NULL,
 CONSTRAINT [PK_RestaurantPOS_BillingInfoKOT] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[RestaurantPOS_BillingInfoHD](
	[Id] [int] NOT NULL,
	[BillNo] [nchar](15) NOT NULL,
	[ODN] [nchar](30) NULL,
	[BillDate] [datetime] NULL,
	[Operator] [nchar](100) NULL,
	[SubTotal] [decimal](18, 2) NULL,
	[HDDiscountPer] [decimal](18, 4) NULL,
	[HDDiscountAmt] [decimal](18, 2) NULL,
	[HomeDeliveryCharges] [decimal](18, 2) NULL,
	[GrandTotal] [decimal](18, 2) NULL,
	[CustomerName] [nchar](200) NULL,
	[Address] [nvarchar](250) NULL,
	[ContactNo] [nchar](50) NULL,
	[Employee_ID] [int] NOT NULL,
	[PaymentMode] [nchar](50) NOT NULL,
	[BillNote] [nvarchar](max) NULL,
	[Member_ID] [nchar](10) NULL,
	[HD_Status] [nchar](30) NULL,
	[GiftCardID] [nchar](20) NULL,
	[GiftCardAmount] [decimal](18, 2) NULL,
	[LP] [int] NULL,
	[LA] [decimal](18, 2) NULL,
	[TaxType] [nchar](20) NULL,
	[Tip] [decimal](18, 2) NULL,
	[AmtReceived] [decimal](18, 2) NULL,
 CONSTRAINT [PK_RestaurantPOS_BillingInfoHD] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[RestaurantPOS_BillingInfoTA](
	[Id] [int] NOT NULL,
	[BillNo] [nchar](15) NOT NULL,
	[ODN] [nchar](30) NULL,
	[BillDate] [datetime] NOT NULL,
	[SubTotal] [decimal](18, 2) NULL,
	[TADiscountPer] [decimal](18, 4) NULL,
	[TADiscountAmt] [decimal](18, 2) NULL,
	[ParcelCharges] [decimal](18, 2) NULL,
	[GrandTotal] [decimal](18, 2) NULL,
	[Cash] [decimal](18, 2) NULL,
	[Change] [decimal](18, 2) NULL,
	[Operator] [nchar](100) NULL,
	[PaymentMode] [nchar](50) NULL,
	[BillNote] [nvarchar](max) NULL,
	[ExchangeRate] [decimal](18, 2) NULL,
	[CurrencyCode] [nchar](10) NULL,
	[Member_ID] [nchar](10) NULL,
	[CustomerName] [nchar](150) NULL,
	[PhoneNo] [nchar](100) NULL,
	[TA_Status] [nchar](30) NULL,
	[GiftCardID] [nchar](20) NULL,
	[GiftCardAmount] [decimal](18, 2) NULL,
	[LP] [int] NULL,
	[LA] [decimal](18, 2) NULL,
	[TaxType] [nchar](20) NULL,
	[Card] [decimal](18, 2) NULL,
	[Tip] [decimal](18, 2) NULL,
	[scuReceiptNo] [nchar](50) NULL,
	[totalTaxableAmount] [decimal](18, 2) NULL,
	[totalTaxAmount] [decimal](18, 2) NULL,
	[internalData] [nchar](100) NULL,
	[signature] [nchar](100) NULL,
	[scdcId] [nchar](100) NULL,
	[scuReceiptDate] [datetime] NULL,
	[invoiceVerificationUrl] [nvarchar](max) NULL,
	[zeroRated] [decimal](18, 2) NULL,
	[nonVatable] [decimal](18, 2) NULL,
 CONSTRAINT [PK_RestaurantPOS_BillingInfoTA] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[RestaurantPOS_BillingInfoEB](
	[Id] [int] NOT NULL,
	[BillNo] [nchar](15) NOT NULL,
	[ODN] [nchar](30) NULL,
	[BillDate] [datetime] NULL,
	[EBDiscountPer] [decimal](18, 4) NULL,
	[EBDiscountAmt] [decimal](18, 2) NULL,
	[GrandTotal] [decimal](18, 2) NULL,
	[Cash] [decimal](18, 2) NULL,
	[Change] [decimal](18, 2) NULL,
	[Operator] [nchar](100) NULL,
	[PaymentMode] [nchar](50) NULL,
	[BillNote] [nvarchar](max) NULL,
	[ExchangeRate] [decimal](18, 2) NOT NULL,
	[CurrencyCode] [nchar](10) NULL,
	[EB_Status] [nchar](20) NULL,
	[Member_ID] [nchar](10) NULL,
	[CustomerName] [nchar](150) NULL,
	[EB_PhoneNo] [nchar](50) NULL,
	[GiftCardID] [nchar](20) NULL,
	[GiftCardAmount] [decimal](18, 2) NULL,
	[LP] [int] NULL,
	[LA] [decimal](18, 2) NULL,
	[TaxType] [nchar](20) NULL,
	[Card] [decimal](18, 2) NULL,
	[Tip] [decimal](18, 2) NULL,
 CONSTRAINT [PK_RestaurantPOS_BillingInfoEB] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[RestaurantPOS_OrderedProductBillKOT](
	[OP_ID] [int] IDENTITY(1,1) NOT NULL,
	[BillID] [int] NULL,
	[Dish] [nvarchar](max) NULL,
	[Rate] [decimal](18, 2) NULL,
	[Quantity] [decimal](18, 2) NULL,
	[Amount] [decimal](18, 2) NULL,
	[VATPer] [decimal](18, 2) NULL,
	[VATAmount] [decimal](18, 3) NULL,
	[STPer] [decimal](18, 2) NULL,
	[STAmount] [decimal](18, 2) NULL,
	[SCPer] [decimal](18, 2) NULL,
	[SCAmount] [decimal](18, 2) NULL,
	[DiscountPer] [decimal](18, 4) NULL,
	[DiscountAmount] [decimal](18, 3) NULL,
	[TotalAmount] [decimal](18, 2) NULL,
	[TableNo] [nchar](30) NULL,
	[Category] [nchar](200) NULL,
	[RCost] [decimal](18, 2) NULL,
	[DishNameArabic] [nvarchar](250) NULL,
	[Waiter1] [nchar](100) NULL,
 CONSTRAINT [PK_RestaurantPOS_OrderedProductBillKOT] PRIMARY KEY CLUSTERED 
(
	[OP_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[RestaurantPOS_OrderedProductBillHD](
	[OP_ID] [int] IDENTITY(1,1) NOT NULL,
	[BillID] [int] NULL,
	[Dish] [nvarchar](max) NULL,
	[Rate] [decimal](18, 2) NULL,
	[Quantity] [decimal](18, 2) NULL,
	[Amount] [decimal](18, 2) NULL,
	[VATPer] [decimal](18, 2) NULL,
	[VATAmount] [decimal](18, 3) NULL,
	[STPer] [decimal](18, 2) NULL,
	[STAmount] [decimal](18, 2) NULL,
	[SCPer] [decimal](18, 2) NULL,
	[SCAmount] [decimal](18, 2) NULL,
	[DiscountPer] [decimal](18, 4) NULL,
	[DiscountAmount] [decimal](18, 3) NULL,
	[TotalAmount] [decimal](18, 2) NULL,
	[Notes] [nvarchar](max) NULL,
	[Category] [nchar](200) NULL,
	[RCost] [decimal](18, 2) NULL,
	[DishNameArabic] [nvarchar](250) NULL,
 CONSTRAINT [PK_RestaurantPOS_OrderedProductBillHD] PRIMARY KEY CLUSTERED 
(
	[OP_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[RestaurantPOS_OrderedProductBillTA](
	[OP_ID] [int] IDENTITY(1,1) NOT NULL,
	[BillID] [int] NULL,
	[Dish] [nvarchar](max) NULL,
	[Rate] [decimal](18, 2) NULL,
	[Quantity] [decimal](18, 2) NULL,
	[Amount] [decimal](18, 2) NULL,
	[VATPer] [decimal](18, 2) NULL,
	[VATAmount] [decimal](18, 3) NULL,
	[STPer] [decimal](18, 2) NULL,
	[STAmount] [decimal](18, 2) NULL,
	[SCPer] [decimal](18, 2) NULL,
	[SCAmount] [decimal](18, 2) NULL,
	[DiscountPer] [decimal](18, 4) NULL,
	[DiscountAmount] [decimal](18, 3) NULL,
	[TotalAmount] [decimal](18, 2) NULL,
	[Notes] [nvarchar](max) NULL,
	[Category] [nchar](200) NULL,
	[RCost] [decimal](18, 2) NULL,
	[ItemStatus] [nchar](30) NULL,
	[DishNameArabic] [nvarchar](250) NULL,
 CONSTRAINT [PK_RestaurantPOS_OrderedProductBillTA] PRIMARY KEY CLUSTERED 
(
	[OP_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[RestaurantPOS_OrderedProductBillEB](
	[OP_ID] [int] IDENTITY(1,1) NOT NULL,
	[BillID] [int] NULL,
	[Dish] [nvarchar](max) NULL,
	[Rate] [decimal](18, 2) NULL,
	[Quantity] [decimal](18, 2) NULL,
	[Amount] [decimal](18, 2) NULL,
	[VATPer] [decimal](18, 2) NULL,
	[VATAmount] [decimal](18, 3) NULL,
	[STPer] [decimal](18, 2) NULL,
	[STAmount] [decimal](18, 2) NULL,
	[SCPer] [decimal](18, 2) NULL,
	[SCAmount] [decimal](18, 2) NULL,
	[DiscountPer] [decimal](18, 4) NULL,
	[DiscountAmount] [decimal](18, 3) NULL,
	[TotalAmount] [decimal](18, 2) NULL,
	[Notes] [nvarchar](max) NULL,
	[Category] [nchar](200) NULL,
	[RCost] [decimal](18, 2) NULL,
	[ItemStatus] [nchar](30) NULL,
	[DishNameArabic] [nvarchar](250) NULL,
 CONSTRAINT [PK_RestaurantPOS_OrderedProductBillEB] PRIMARY KEY CLUSTERED 
(
	[OP_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[RestaurantPOS_OrderInfoKOT](
	[ID] [int] NOT NULL,
	[TicketNo] [nchar](15) NOT NULL,
	[BillDate] [datetime] NOT NULL,
	[GrandTotal] [decimal](18, 2) NULL,
	[TableNo] [nchar](30) NULL,
	[Operator] [nchar](100) NULL,
	[GroupName] [nchar](200) NULL,
	[TicketNote] [nvarchar](max) NULL,
	[KOT_Status] [nchar](30) NULL,
	[TaxType] [nchar](20) NULL,
	[NoOfPerson] [int] NULL,
	[ODNX] [nchar](10) NULL,
 CONSTRAINT [PK_RestaurantPOS_OrderInfoKOT] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[RestaurantPOS_OrderedProductKOT](
	[OP_ID] [int] IDENTITY(1,1) NOT NULL,
	[TicketID] [int] NULL,
	[Dish] [nvarchar](max) NULL,
	[Rate] [decimal](18, 2) NULL,
	[Quantity] [decimal](18, 2) NULL,
	[Amount] [decimal](18, 2) NULL,
	[VATPer] [decimal](18, 2) NULL,
	[VATAmount] [decimal](18, 3) NULL,
	[STPer] [decimal](18, 2) NULL,
	[STAmount] [decimal](18, 2) NULL,
	[SCPer] [decimal](18, 2) NULL,
	[SCAmount] [decimal](18, 2) NULL,
	[DiscountPer] [decimal](18, 2) NULL,
	[DiscountAmount] [decimal](18, 3) NULL,
	[TotalAmount] [decimal](18, 2) NULL,
	[Notes] [nvarchar](max) NULL,
	[Category] [nchar](200) NULL,
	[T_Number] [nchar](30) NULL,
	[DishNameArabic] [nvarchar](250) NULL,
	[ItemStatus] [nchar](20) NULL,
	[Waiter1] [nchar](100) NULL,
 CONSTRAINT [PK_RestaurantPOS_OrderedProductKOT] PRIMARY KEY CLUSTERED 
(
	[OP_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[TempRestaurantPOS_BillingInfoKOT](
	[Id] [int] NOT NULL,
	[BillNo] [nchar](15) NULL,
	[BillDate] [datetime] NULL,
	[ODN] [nchar](30) NULL,
	[KOTDiscountPer] [decimal](18, 4) NULL,
	[KOTDiscountAmt] [decimal](18, 2) NULL,
	[GrandTotal] [decimal](18, 2) NULL,
	[Cash] [decimal](18, 2) NULL,
	[Change] [decimal](18, 2) NULL,
	[Operator] [nchar](100) NULL,
	[PaymentMode] [nchar](100) NULL,
	[ExchangeRate] [decimal](18, 2) NULL,
	[CurrencyCode] [nchar](10) NULL,
	[Member_ID] [nchar](10) NULL,
	[Waiter] [nchar](200) NULL,
	[GiftCardID] [nchar](20) NULL,
	[GiftCardAmount] [decimal](18, 2) NULL,
	[LP] [int] NULL,
	[LA] [decimal](18, 2) NULL,
	[CustomerName] [nchar](150) NULL,
	[PhoneNo] [nchar](50) NULL,
	[EmailID] [nchar](150) NULL,
	[TaxType] [nchar](20) NULL,
	[Card] [decimal](18, 2) NULL,
	[QRCode] [image] NULL,
 CONSTRAINT [PK_TempRestaurantPOS_BillingInfoKOT] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[TempRestaurantPOS_OrderedProductBillKOT](
	[OP_ID] [int] IDENTITY(1,1) NOT NULL,
	[BillID] [int] NULL,
	[Dish] [nvarchar](max) NULL,
	[Rate] [decimal](18, 2) NULL,
	[Quantity] [decimal](18, 2) NULL,
	[Amount] [decimal](18, 2) NULL,
	[VATPer] [decimal](18, 2) NULL,
	[VATAmount] [decimal](18, 3) NULL,
	[STPer] [decimal](18, 2) NULL,
	[STAmount] [decimal](18, 2) NULL,
	[SCPer] [decimal](18, 2) NULL,
	[SCAmount] [decimal](18, 2) NULL,
	[DiscountPer] [decimal](18, 4) NULL,
	[DiscountAmount] [decimal](18, 3) NULL,
	[TotalAmount] [decimal](18, 2) NULL,
	[TableNo] [nchar](30) NULL,
	[Category] [nchar](200) NULL,
	[RCost] [decimal](18, 2) NULL,
	[DishNameArabic] [nvarchar](250) NULL,
 CONSTRAINT [PK_TempRestaurantPOS_OrderedProductBillKOT] PRIMARY KEY CLUSTERED 
(
	[OP_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[tblOrder](
	[OD_ID] [int] IDENTITY(1,1) NOT NULL,
	[ODNo] [int] NOT NULL,
	[BillNo] [nchar](30) NULL,
 CONSTRAINT [PK_tblOrder] PRIMARY KEY CLUSTERED 
(
	[OD_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[CateringInvoice](
	[PO_ID] [int] NOT NULL,
	[PONumber] [nchar](50) NOT NULL,
	[Date] [datetime] NULL,
	[Supplier_ID] [int] NULL,
	[Terms] [nvarchar](max) NULL,
	[SubTotal] [decimal](18, 2) NULL,
	[VATPer] [decimal](18, 2) NULL,
	[VATAmount] [decimal](18, 2) NULL,
	[GrandTotal] [decimal](18, 2) NULL,
	[TaxType] [nchar](20) NULL,
	[scuReceiptNo] [nchar](50) NULL,
	[totalTaxableAmount] [decimal](18, 2) NULL,
	[totalTaxAmount] [decimal](18, 2) NULL,
	[internalData] [nchar](100) NULL,
	[signature] [nchar](100) NULL,
	[scdcId] [nchar](100) NULL,
	[scuReceiptDate] [datetime] NULL,
	[invoiceVerificationUrl] [nvarchar](max) NULL,
	[zeroRated] [decimal](18, 2) NULL,
	[nonVatable] [decimal](18, 2) NULL,
	[Note3] [nvarchar](50) NULL,
 CONSTRAINT [PK_CateringInvoice] PRIMARY KEY CLUSTERED 
(
	[PO_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[CateringInvoice_Join](
	[POJ_ID] [int] IDENTITY(1,1) NOT NULL,
	[PurchaseOrderID] [int] NOT NULL,
	[ProductID] [int] NOT NULL,
	[Qty] [decimal](18, 2) NOT NULL,
	[PricePerUnit] [decimal](18, 2) NULL,
	[Amount] [decimal](18, 2) NULL,
	[Note] [nvarchar](50) NULL,
 CONSTRAINT [PK_CateringInvoice_Join] PRIMARY KEY CLUSTERED 
(
	[POJ_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


-- ------------------------------------------------------------------------------------------------
-- MODULE: Sales Payments And Vouchers
-- Payments, gift cards, vouchers, deleted-invoice audit trail, promotions
-- ------------------------------------------------------------------------------------------------

CREATE TABLE [dbo].[Payment](
	[T_ID] [int] NOT NULL,
	[TransactionID] [nchar](20) NULL,
	[Date] [datetime] NOT NULL,
	[PaymentMode] [nchar](30) NULL,
	[SupplierID] [int] NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[Remarks] [nvarchar](250) NULL,
	[PaymentModeDetails] [nvarchar](250) NULL,
 CONSTRAINT [PK_Payment] PRIMARY KEY CLUSTERED 
(
	[T_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[GiftCard](
	[ID] [nchar](20) NOT NULL,
	[Amount] [decimal](18, 2) NULL,
	[CustomerName] [nchar](150) NULL,
	[EntryDate] [datetime] NULL,
 CONSTRAINT [PK_GiftCard] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Voucher](
	[ID] [int] NOT NULL,
	[VoucherNo] [nchar](30) NOT NULL,
	[Name] [nchar](150) NULL,
	[Date] [datetime] NOT NULL,
	[Details] [nvarchar](max) NULL,
	[PaymentMode] [nchar](150) NOT NULL,
	[GrandTotal] [decimal](18, 2) NOT NULL,
 CONSTRAINT [PK_Voucher] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[Voucher_OtherDetails](
	[VD_ID] [int] IDENTITY(1,1) NOT NULL,
	[VoucherID] [int] NOT NULL,
	[Particulars] [nvarchar](250) NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[Note] [nvarchar](max) NULL,
 CONSTRAINT [PK_Voucher_OtherDetails] PRIMARY KEY CLUSTERED 
(
	[VD_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[NotesMaster](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Notes] [nvarchar](250) NOT NULL,
 CONSTRAINT [PK_NotesMaster] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[DeletedInvoices](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[BillNo] [nchar](15) NULL,
	[BillDate] [datetime] NULL,
	[GrandTotal] [decimal](18, 2) NULL,
	[Operator] [nchar](100) NULL,
	[PaymentMode] [nchar](100) NULL,
	[Reason] [nchar](200) NULL,
	[DeletedDate] [datetime] NULL,
	[BillType] [nchar](20) NULL,
	[Canceled_Deleted] [nchar](20) NULL,
 CONSTRAINT [PK_DeletedInvoices] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[DeletedInvoices_Join](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[BillNo] [nchar](15) NULL,
	[ItemName] [nvarchar](max) NULL,
	[Qty] [decimal](18, 2) NULL,
	[TotalAmount] [decimal](18, 2) NULL,
 CONSTRAINT [PK_DeletedInvoices_Join] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[Promotion](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Dish] [nvarchar](250) NULL,
	[Rate] [decimal](18, 2) NULL,
	[PDay] [nchar](30) NULL,
	[TimeFrom] [datetime] NULL,
	[TimeTo] [datetime] NULL,
	[Active] [nchar](10) NULL,
 CONSTRAINT [PK_Promotion] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


-- ------------------------------------------------------------------------------------------------
-- MODULE: Customers Credit And Loyalty
-- Credit customer ledgers, loyalty/membership programs
-- ------------------------------------------------------------------------------------------------

CREATE TABLE [dbo].[CreditCustomer](
	[CC_ID] [int] NOT NULL,
	[CreditCustomerID] [nchar](30) NOT NULL,
	[Name] [nchar](200) NULL,
	[ContactNo] [nchar](50) NULL,
	[Address] [nvarchar](max) NULL,
	[OpeningBalance] [decimal](18, 2) NULL,
	[OpeningBalanceType] [nchar](10) NULL,
	[RegistrationDate] [datetime] NULL,
	[Active] [nchar](10) NULL,
	[EmailID] [nchar](100) NULL,
 CONSTRAINT [PK_CreditCustomer] PRIMARY KEY CLUSTERED 
(
	[CC_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[CreditCustomerLedger](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Date] [datetime] NULL,
	[LedgerNo] [nchar](50) NULL,
	[Label] [nchar](200) NULL,
	[Debit] [decimal](18, 2) NULL,
	[Credit] [decimal](18, 2) NULL,
	[CreditCustomer_ID] [int] NULL,
 CONSTRAINT [PK_CreditCustomerLedger] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[CreditCustomerPayment](
	[T_ID] [int] NOT NULL,
	[TransactionID] [nchar](20) NULL,
	[Date] [datetime] NOT NULL,
	[PaymentMode] [nchar](30) NULL,
	[CreditCustomer_ID] [int] NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[Remarks] [nvarchar](250) NULL,
	[PaymentModeDetails] [nvarchar](250) NULL,
 CONSTRAINT [PK_CreditCustomerPayment] PRIMARY KEY CLUSTERED 
(
	[T_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[HDCustomer](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CustomerName] [nchar](150) NULL,
	[Address] [nvarchar](max) NULL,
	[ContactNo] [nchar](50) NULL,
 CONSTRAINT [PK_HDCustomer] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[LoyaltyMember](
	[MemberID] [int] NOT NULL,
	[Name] [nchar](200) NULL,
	[CardNo] [nchar](50) NULL,
	[ContactNo] [nchar](50) NULL,
	[Address] [nvarchar](max) NULL,
	[RegistrationDate] [datetime] NULL,
	[Active] [nchar](10) NULL,
 CONSTRAINT [PK_LoyaltyMember] PRIMARY KEY CLUSTERED 
(
	[MemberID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[LoyaltyMemberLedgerBook](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Date] [datetime] NOT NULL,
	[LedgerNo] [nchar](50) NOT NULL,
	[Label] [nchar](200) NOT NULL,
	[PointsEarned] [decimal](18, 2) NOT NULL,
	[PointsRedeem] [decimal](18, 2) NOT NULL,
	[MemberID] [int] NULL,
 CONSTRAINT [PK_LoyaltyMemberBook] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Member](
	[MemberID] [int] NOT NULL,
	[Name] [nchar](200) NULL,
	[ContactNo] [nchar](50) NULL,
	[Address] [nvarchar](max) NULL,
	[RegistrationDate] [datetime] NULL,
	[Active] [nchar](10) NULL,
 CONSTRAINT [PK_Member] PRIMARY KEY CLUSTERED 
(
	[MemberID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[MemberLedger](
	[Id] [int] NOT NULL,
	[Date] [datetime] NULL,
	[LedgerNo] [nchar](50) NULL,
	[Label] [nchar](200) NULL,
	[Debit] [decimal](18, 2) NULL,
	[Credit] [decimal](18, 2) NULL,
	[MemberID] [int] NULL,
 CONSTRAINT [PK_MemberLedger] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


-- ------------------------------------------------------------------------------------------------
-- MODULE: Finance Banking And Ledgers
-- Bank accounts, journal, general ledger, funds, wallet
-- ------------------------------------------------------------------------------------------------

CREATE TABLE [dbo].[Bank](
	[BankName] [nvarchar](250) NOT NULL,
 CONSTRAINT [PK_Bank] PRIMARY KEY CLUSTERED 
(
	[BankName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[BankAccountLedger](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Date] [datetime] NULL,
	[AccNo] [nchar](50) NULL,
	[LedgerNo] [nchar](200) NULL,
	[Label] [nchar](200) NULL,
	[Debit] [decimal](18, 2) NULL,
	[Credit] [decimal](18, 2) NULL,
 CONSTRAINT [PK_BankAccountLedger] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[BankAccountRegistration](
	[AccountNo] [nchar](50) NOT NULL,
	[AccountName] [nchar](200) NULL,
	[AccountType] [nchar](100) NULL,
	[OpeningDate] [datetime] NULL,
	[BalanceAmount] [decimal](18, 2) NULL,
	[Active] [nchar](10) NULL,
	[BranchID] [int] NULL,
	[Id] [int] NULL,
 CONSTRAINT [PK_BankAccountRegistration] PRIMARY KEY CLUSTERED 
(
	[AccountNo] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[BankBranch](
	[Id] [int] NOT NULL,
	[BranchName] [nvarchar](250) NULL,
	[Address] [nvarchar](250) NULL,
	[ContactNo] [nchar](100) NULL,
	[SwiftCode] [nchar](50) NULL,
	[IFSCCode] [nchar](50) NULL,
	[BankName] [nvarchar](250) NOT NULL,
 CONSTRAINT [PK_BankBranch] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Journal](
	[ID] [int] NOT NULL,
	[DebitAccount] [nchar](200) NULL,
	[CreditAccount] [nchar](200) NULL,
	[Date] [datetime] NULL,
	[Amount] [decimal](18, 2) NULL,
	[Remarks] [nvarchar](max) NULL,
 CONSTRAINT [PK_Journal] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[LedgerBook](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Date] [datetime] NULL,
	[Name] [nchar](200) NULL,
	[LedgerNo] [nchar](200) NULL,
	[Label] [nchar](200) NULL,
	[AccLedger] [nchar](200) NULL,
	[Debit] [decimal](18, 2) NULL,
	[Credit] [decimal](18, 2) NULL,
	[PartyID] [nchar](50) NULL,
 CONSTRAINT [PK_LedgerBook] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[FundDeposit](
	[Id] [int] NOT NULL,
	[DepositerName] [nchar](150) NULL,
	[Amount] [decimal](18, 2) NULL,
	[Date] [datetime] NULL,
	[AccNo] [nchar](50) NULL,
	[Notes] [nchar](200) NULL,
 CONSTRAINT [PK_FundDeposit] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[FundTransfer](
	[Id] [int] NOT NULL,
	[AccountTransFrom] [nchar](50) NULL,
	[AccountTransTo] [nchar](50) NULL,
	[Amount] [decimal](18, 2) NULL,
	[Date] [datetime] NULL,
	[Operator] [nchar](150) NULL,
	[Notes] [nchar](200) NULL,
 CONSTRAINT [PK_FundTransfer] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Wallet](
	[WalletType] [nchar](200) NOT NULL,
 CONSTRAINT [PK_Wallet] PRIMARY KEY CLUSTERED 
(
	[WalletType] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


-- ------------------------------------------------------------------------------------------------
-- MODULE: Expenses
-- Non-inventory cash-out tracking against the till
-- ------------------------------------------------------------------------------------------------

CREATE TABLE [dbo].[Expense](
	[ExpenseName] [nvarchar](250) NOT NULL,
	[ExpenseType] [nchar](200) NOT NULL,
 CONSTRAINT [PK_Expense_1] PRIMARY KEY CLUSTERED 
(
	[ExpenseName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[ExpenseType](
	[Type] [nchar](200) NOT NULL,
 CONSTRAINT [PK_ExpenseType] PRIMARY KEY CLUSTERED 
(
	[Type] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


-- ------------------------------------------------------------------------------------------------
-- MODULE: Held Orders
-- Parked/held bills
-- ------------------------------------------------------------------------------------------------

CREATE TABLE [dbo].[HoldBill](
	[HoldID] [nchar](30) NOT NULL,
	[Date] [datetime] NULL,
	[UserID] [nchar](100) NULL,
	[BillType] [nchar](20) NULL,
	[TaxType] [nchar](20) NULL,
	[TableNo] [nchar](50) NULL,
	[CName] [nchar](150) NULL,
	[Address] [nvarchar](250) NULL,
	[CNo] [nchar](50) NULL,
 CONSTRAINT [PK_HoldBill] PRIMARY KEY CLUSTERED 
(
	[HoldID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[HoldItems](
	[OP_ID] [int] IDENTITY(1,1) NOT NULL,
	[BillID] [nchar](30) NULL,
	[Dish] [nvarchar](max) NULL,
	[Rate] [decimal](18, 2) NULL,
	[Quantity] [decimal](18, 2) NULL,
	[Amount] [decimal](18, 2) NULL,
	[VATPer] [decimal](18, 2) NULL,
	[VATAmount] [decimal](18, 2) NULL,
	[STPer] [decimal](18, 2) NULL,
	[STAmount] [decimal](18, 2) NULL,
	[SCPer] [decimal](18, 2) NULL,
	[SCAmount] [decimal](18, 2) NULL,
	[DiscountPer] [decimal](18, 4) NULL,
	[DiscountAmount] [decimal](18, 2) NULL,
	[TotalAmount] [decimal](18, 2) NULL,
	[Notes] [nvarchar](max) NULL,
	[Category] [nchar](200) NULL,
	[ItemStatus] [nchar](20) NULL,
	[DishNameArabic] [nvarchar](max) NULL,
 CONSTRAINT [PK_HoldItems] PRIMARY KEY CLUSTERED 
(
	[OP_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


-- ------------------------------------------------------------------------------------------------
-- MODULE: System And Logs
-- Application audit log
-- ------------------------------------------------------------------------------------------------

CREATE TABLE [dbo].[Logs](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserID] [nchar](100) NOT NULL,
	[Operation] [nvarchar](250) NOT NULL,
	[Date] [datetime] NOT NULL,
 CONSTRAINT [PK_Logs] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


-- ------------------------------------------------------------------------------------------------
-- FOREIGN KEY CONSTRAINTS
-- ------------------------------------------------------------------------------------------------

ALTER TABLE [dbo].[BankAccountLedger]  WITH CHECK ADD  CONSTRAINT [FK_BankAccountLedger_BankAccountRegistration] FOREIGN KEY([AccNo])
REFERENCES [dbo].[BankAccountRegistration] ([AccountNo])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[BankAccountRegistration]  WITH CHECK ADD  CONSTRAINT [FK_BankAccountRegistration_BankBranch] FOREIGN KEY([BranchID])
REFERENCES [dbo].[BankBranch] ([Id])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[BankBranch]  WITH CHECK ADD  CONSTRAINT [FK_BankBranch_Bank] FOREIGN KEY([BankName])
REFERENCES [dbo].[Bank] ([BankName])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[Category]  WITH CHECK ADD  CONSTRAINT [FK_Category_Kitchen] FOREIGN KEY([Kitchen])
REFERENCES [dbo].[Kitchen] ([Kitchenname])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[ClockIN]  WITH CHECK ADD  CONSTRAINT [FK_ClockIN_Registration] FOREIGN KEY([UserID])
REFERENCES [dbo].[Registration] ([UserID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[ClockOUT]  WITH CHECK ADD  CONSTRAINT [FK_ClockOUT_ClockIN] FOREIGN KEY([ClockInID])
REFERENCES [dbo].[ClockIN] ([ID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[CreditCustomerLedger]  WITH CHECK ADD  CONSTRAINT [FK_CreditCustomerLedger_CreditCustomer] FOREIGN KEY([CreditCustomer_ID])
REFERENCES [dbo].[CreditCustomer] ([CC_ID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[CreditCustomerPayment]  WITH CHECK ADD  CONSTRAINT [FK_CreditCustomerPayment_CreditCustomer] FOREIGN KEY([CreditCustomer_ID])
REFERENCES [dbo].[CreditCustomer] ([CC_ID])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[Dish]  WITH CHECK ADD  CONSTRAINT [FK_Dish_Category] FOREIGN KEY([Category])
REFERENCES [dbo].[Category] ([CategoryName])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[Expense]  WITH CHECK ADD  CONSTRAINT [FK_Expense_ExpenseType] FOREIGN KEY([ExpenseType])
REFERENCES [dbo].[ExpenseType] ([Type])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[FundDeposit]  WITH CHECK ADD  CONSTRAINT [FK_FundDeposit_BankAccountRegistration] FOREIGN KEY([AccNo])
REFERENCES [dbo].[BankAccountRegistration] ([AccountNo])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[Logs]  WITH CHECK ADD  CONSTRAINT [FK_Logs_Registration] FOREIGN KEY([UserID])
REFERENCES [dbo].[Registration] ([UserID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[LoyaltyMemberLedgerBook]  WITH CHECK ADD  CONSTRAINT [FK_LoyaltyMemberLedgerBook_LoyaltyMember] FOREIGN KEY([MemberID])
REFERENCES [dbo].[LoyaltyMember] ([MemberID])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[MemberLedger]  WITH CHECK ADD  CONSTRAINT [FK_MemberLedger_Member] FOREIGN KEY([MemberID])
REFERENCES [dbo].[Member] ([MemberID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[Modifiers]  WITH CHECK ADD  CONSTRAINT [FK_Modifiers_Dish] FOREIGN KEY([Item])
REFERENCES [dbo].[Dish] ([DishName])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[Payment]  WITH CHECK ADD  CONSTRAINT [FK_Payment_Supplier] FOREIGN KEY([SupplierID])
REFERENCES [dbo].[Supplier] ([ID])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[Payment_Withdraw]  WITH CHECK ADD  CONSTRAINT [FK_Payment_Withdraw_BankAccountRegistration] FOREIGN KEY([AccountFrom])
REFERENCES [dbo].[BankAccountRegistration] ([AccountNo])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[Payroll]  WITH CHECK ADD  CONSTRAINT [FK_Payroll_Registration] FOREIGN KEY([UserID])
REFERENCES [dbo].[Registration] ([UserID])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[Payroll_MB]  WITH CHECK ADD  CONSTRAINT [FK_Payroll_MB_Registration] FOREIGN KEY([UserID])
REFERENCES [dbo].[Registration] ([UserID])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[PizzaMaster]  WITH CHECK ADD  CONSTRAINT [FK_PizzaMaster_PizzaSize] FOREIGN KEY([PizzaSize])
REFERENCES [dbo].[PizzaSize] ([Size])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[PizzaModifier]  WITH CHECK ADD  CONSTRAINT [FK_PizzaModifier_PizzaMaster] FOREIGN KEY([PizzaID])
REFERENCES [dbo].[PizzaMaster] ([Pizza_ID])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[PizzaTopping]  WITH CHECK ADD  CONSTRAINT [FK_PizzaTopping_PizzaSize] FOREIGN KEY([PizzaSize])
REFERENCES [dbo].[PizzaSize] ([Size])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[Product]  WITH CHECK ADD  CONSTRAINT [FK_Product_Product] FOREIGN KEY([Unit])
REFERENCES [dbo].[UnitMaster] ([Unit])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[Product]  WITH CHECK ADD  CONSTRAINT [FK_Product_RMCategory] FOREIGN KEY([Category])
REFERENCES [dbo].[RMCategory] ([CategoryName])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[Product_OpeningStock]  WITH CHECK ADD  CONSTRAINT [FK_Product_OpeningStock_Product1] FOREIGN KEY([ProductID])
REFERENCES [dbo].[Product] ([PID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[Product_OpeningStock]  WITH CHECK ADD  CONSTRAINT [FK_Product_OpeningStock_Warehouse] FOREIGN KEY([Warehouse])
REFERENCES [dbo].[Warehouse] ([WarehouseName])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[Product_OpeningStockC]  WITH CHECK ADD  CONSTRAINT [FK_Product_OpeningStock_Product1C] FOREIGN KEY([ProductID])
REFERENCES [dbo].[ProductC] ([PID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[Product_OpeningStockC]  WITH CHECK ADD  CONSTRAINT [FK_Product_OpeningStock_WarehouseC] FOREIGN KEY([Warehouse])
REFERENCES [dbo].[Warehouse] ([WarehouseName])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[ProductC]  WITH CHECK ADD  CONSTRAINT [FK_Product_ProductC] FOREIGN KEY([Unit])
REFERENCES [dbo].[UnitMaster] ([Unit])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[ProductC]  WITH CHECK ADD  CONSTRAINT [FK_Product_RMCategoryC] FOREIGN KEY([Category])
REFERENCES [dbo].[RMCategory] ([CategoryName])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[Promotion]  WITH CHECK ADD  CONSTRAINT [FK_Promotion_Dish] FOREIGN KEY([Dish])
REFERENCES [dbo].[Dish] ([DishName])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[Purchase]  WITH CHECK ADD  CONSTRAINT [FK_Purchase_Supplier] FOREIGN KEY([Supplier_ID])
REFERENCES [dbo].[Supplier] ([ID])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[Purchase_Join]  WITH CHECK ADD  CONSTRAINT [FK_Purchase_Join_Product] FOREIGN KEY([ProductID])
REFERENCES [dbo].[Product] ([PID])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[Purchase_Join]  WITH CHECK ADD  CONSTRAINT [FK_Purchase_Join_Purchase] FOREIGN KEY([PurchaseID])
REFERENCES [dbo].[Purchase] ([ST_ID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[Purchase_Join]  WITH CHECK ADD  CONSTRAINT [FK_Purchase_Join_Warehouse] FOREIGN KEY([Warehouse])
REFERENCES [dbo].[Warehouse] ([WarehouseName])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[PurchaseOrder]  WITH CHECK ADD  CONSTRAINT [FK_PurchaseOrder_Supplier] FOREIGN KEY([Supplier_ID])
REFERENCES [dbo].[Supplier] ([ID])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[PurchaseOrder_Join]  WITH CHECK ADD  CONSTRAINT [FK_PurchaseOrder_Join_Product] FOREIGN KEY([ProductID])
REFERENCES [dbo].[Product] ([PID])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[PurchaseOrder_Join]  WITH CHECK ADD  CONSTRAINT [FK_PurchaseOrder_Join_PurchaseOrder] FOREIGN KEY([PurchaseOrderID])
REFERENCES [dbo].[PurchaseOrder] ([PO_ID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[PurchaseOrder2_Join]  WITH CHECK ADD  CONSTRAINT [FK_PurchaseOrder2_Join_Product] FOREIGN KEY([ProductID])
REFERENCES [dbo].[ProductC] ([PID])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[PurchaseOrder2_Join]  WITH CHECK ADD  CONSTRAINT [FK_PurchaseOrder2_Join_PurchaseOrder] FOREIGN KEY([PurchaseOrderID])
REFERENCES [dbo].[PurchaseOrder2] ([PO_ID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[PurchaseOrder3]  WITH CHECK ADD  CONSTRAINT [FK_PurchaseOrder3_Supplier] FOREIGN KEY([Supplier_ID])
REFERENCES [dbo].[Supplier] ([ID])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[PurchaseOrder3_Join]  WITH CHECK ADD  CONSTRAINT [FK_PurchaseOrder3_Join_Product] FOREIGN KEY([ProductID])
REFERENCES [dbo].[Product] ([PID])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[PurchaseOrder3_Join]  WITH CHECK ADD  CONSTRAINT [FK_PurchaseOrder3_Join_PurchaseOrder] FOREIGN KEY([PurchaseOrderID])
REFERENCES [dbo].[PurchaseOrder3] ([PO_ID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[Recipe]  WITH CHECK ADD  CONSTRAINT [FK_Recipe_Dish] FOREIGN KEY([Dish])
REFERENCES [dbo].[Dish] ([DishName])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[Recipe_Join]  WITH CHECK ADD  CONSTRAINT [FK_Recipe_Join_Product] FOREIGN KEY([ProductID])
REFERENCES [dbo].[Product] ([PID])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[Recipe_Join]  WITH CHECK ADD  CONSTRAINT [FK_Recipe_Join_Recipe] FOREIGN KEY([RecipeID])
REFERENCES [dbo].[Recipe] ([R_ID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[RestaurantPOS_BillingInfoEB]  WITH CHECK ADD  CONSTRAINT [FK_RestaurantPOS_BillingInfoEB_Registration] FOREIGN KEY([Operator])
REFERENCES [dbo].[Registration] ([UserID])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[RestaurantPOS_BillingInfoHD]  WITH CHECK ADD  CONSTRAINT [FK_RestaurantPOS_BillingInfoHD_Registration] FOREIGN KEY([Operator])
REFERENCES [dbo].[Registration] ([UserID])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[RestaurantPOS_BillingInfoKOT]  WITH CHECK ADD  CONSTRAINT [FK_RestaurantPOS_BillingInfoKOT_Registration] FOREIGN KEY([Operator])
REFERENCES [dbo].[Registration] ([UserID])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[RestaurantPOS_BillingInfoTA]  WITH CHECK ADD  CONSTRAINT [FK_RestaurantPOS_BillingInfoTA_Registration] FOREIGN KEY([Operator])
REFERENCES [dbo].[Registration] ([UserID])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[RestaurantPOS_OrderedProductBillEB]  WITH CHECK ADD  CONSTRAINT [FK_RestaurantPOS_OrderedProductBillEB_RestaurantPOS_BillingInfoEB] FOREIGN KEY([BillID])
REFERENCES [dbo].[RestaurantPOS_BillingInfoEB] ([Id])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[RestaurantPOS_OrderedProductBillHD]  WITH CHECK ADD  CONSTRAINT [FK_RestaurantPOS_OrderedProductBillHD_RestaurantPOS_BillingInfoHD] FOREIGN KEY([BillID])
REFERENCES [dbo].[RestaurantPOS_BillingInfoHD] ([Id])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[RestaurantPOS_OrderedProductBillKOT]  WITH CHECK ADD  CONSTRAINT [FK_RestaurantPOS_OrderedProductBillKOT_R_Table] FOREIGN KEY([TableNo])
REFERENCES [dbo].[R_Table] ([TableNo])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[RestaurantPOS_OrderedProductBillKOT]  WITH CHECK ADD  CONSTRAINT [FK_RestaurantPOS_OrderedProductBillKOT_TempRestaurantPOS_OrderInfoKOT] FOREIGN KEY([BillID])
REFERENCES [dbo].[RestaurantPOS_BillingInfoKOT] ([Id])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[RestaurantPOS_OrderedProductBillTA]  WITH CHECK ADD  CONSTRAINT [FK_RestaurantPOS_OrderedProductBillTA_RestaurantPOS_BillingInfoTA] FOREIGN KEY([BillID])
REFERENCES [dbo].[RestaurantPOS_BillingInfoTA] ([Id])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[RestaurantPOS_OrderedProductKOT]  WITH CHECK ADD  CONSTRAINT [FK_RestaurantPOS_OrderedProductKOT_RestaurantPOS_OrderInfoKOT] FOREIGN KEY([TicketID])
REFERENCES [dbo].[RestaurantPOS_OrderInfoKOT] ([ID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[RestaurantPOS_OrderInfoKOT]  WITH CHECK ADD  CONSTRAINT [FK_RestaurantPOS_OrderInfoKOT_R_Table] FOREIGN KEY([TableNo])
REFERENCES [dbo].[R_Table] ([TableNo])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[RestaurantPOS_OrderInfoKOT]  WITH CHECK ADD  CONSTRAINT [FK_RestaurantPOS_OrderInfoKOT_Registration] FOREIGN KEY([Operator])
REFERENCES [dbo].[Registration] ([UserID])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[RM_Used_Join]  WITH CHECK ADD  CONSTRAINT [FK_RM_Used_Join_Product] FOREIGN KEY([ProductID])
REFERENCES [dbo].[Product] ([PID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[RM_Used_Join]  WITH CHECK ADD  CONSTRAINT [FK_RM_Used_Join_RM_Used] FOREIGN KEY([RawMaterialID])
REFERENCES [dbo].[RM_Used] ([RM_ID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[Stock_Store_Join]  WITH CHECK ADD  CONSTRAINT [FK_Stock_Store_Join_Dish] FOREIGN KEY([Dish])
REFERENCES [dbo].[Dish] ([DishName])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[Stock_Store_Join]  WITH CHECK ADD  CONSTRAINT [FK_Stock_Store_Join_Stock_Store] FOREIGN KEY([StockID])
REFERENCES [dbo].[Stock_Store] ([St_ID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[StockAdjustment_MI]  WITH CHECK ADD  CONSTRAINT [FK_StockAdjustment_MI_Dish] FOREIGN KEY([Dish])
REFERENCES [dbo].[Dish] ([DishName])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[StockAdjustment_Store]  WITH CHECK ADD  CONSTRAINT [FK_StockAdjustment_Kitchen_Product] FOREIGN KEY([ProductID])
REFERENCES [dbo].[Product] ([PID])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[StockAdjustment_Warehouse]  WITH CHECK ADD  CONSTRAINT [FK_StockAdjustment_Warehouse_Product] FOREIGN KEY([ProductID])
REFERENCES [dbo].[Product] ([PID])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[StockAdjustment_Warehouse]  WITH CHECK ADD  CONSTRAINT [FK_StockAdjustment_Warehouse_Warehouse] FOREIGN KEY([Warehouse])
REFERENCES [dbo].[Warehouse] ([WarehouseName])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[StockTransfer]  WITH CHECK ADD  CONSTRAINT [FK_StockTransfer_Kitchen] FOREIGN KEY([Kitchen])
REFERENCES [dbo].[Kitchen] ([Kitchenname])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[StockTransfer_Join]  WITH CHECK ADD  CONSTRAINT [FK_StockTransfer_Join_StockTransfer] FOREIGN KEY([StockTransferID])
REFERENCES [dbo].[StockTransfer] ([ST_ID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[StockTransfer_Join]  WITH CHECK ADD  CONSTRAINT [FK_StockTransfer_Product] FOREIGN KEY([ProductID])
REFERENCES [dbo].[Product] ([PID])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[StockTransfer_Join]  WITH CHECK ADD  CONSTRAINT [FK_StockTransfer_Warehouse] FOREIGN KEY([Warehouse])
REFERENCES [dbo].[Warehouse] ([WarehouseName])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[TableLayout]  WITH CHECK ADD  CONSTRAINT [FK_TableLayout_R_Table] FOREIGN KEY([TableNo])
REFERENCES [dbo].[R_Table] ([TableNo])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[TableMapping]  WITH CHECK ADD  CONSTRAINT [FK_TableMapping_R_Table] FOREIGN KEY([TableNo])
REFERENCES [dbo].[R_Table] ([TableNo])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[TableMapping]  WITH CHECK ADD  CONSTRAINT [FK_TableMapping_Registration] FOREIGN KEY([UserID])
REFERENCES [dbo].[Registration] ([UserID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[TableReservation]  WITH CHECK ADD  CONSTRAINT [FK_TableReservation_R_Table] FOREIGN KEY([TableNo])
REFERENCES [dbo].[R_Table] ([TableNo])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[Temp_Stock]  WITH CHECK ADD  CONSTRAINT [FK_Temp_Stock_Product] FOREIGN KEY([ProductID])
REFERENCES [dbo].[Product] ([PID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[Temp_Stock]  WITH CHECK ADD  CONSTRAINT [FK_Temp_Stock_Warehouse] FOREIGN KEY([Warehouse])
REFERENCES [dbo].[Warehouse] ([WarehouseName])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[Temp_Stock_RM]  WITH CHECK ADD  CONSTRAINT [FK_Temp_Stock_RM_Product] FOREIGN KEY([ProductID])
REFERENCES [dbo].[Product] ([PID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[Temp_Stock_Store]  WITH CHECK ADD  CONSTRAINT [FK_TempStock_Store_Dish] FOREIGN KEY([Dish])
REFERENCES [dbo].[Dish] ([DishName])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[TempRestaurantPOS_BillingInfoKOT]  WITH CHECK ADD  CONSTRAINT [FK_TempRestaurantPOS_BillingInfoKOT_Registration] FOREIGN KEY([Operator])
REFERENCES [dbo].[Registration] ([UserID])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[TempRestaurantPOS_OrderedProductBillKOT]  WITH CHECK ADD  CONSTRAINT [FK_TempRestaurantPOS_OrderedProductBillKOT_R_Table] FOREIGN KEY([TableNo])
REFERENCES [dbo].[R_Table] ([TableNo])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[TempRestaurantPOS_OrderedProductBillKOT]  WITH CHECK ADD  CONSTRAINT [FK_TempRestaurantPOS_OrderedProductBillKOT_TempRestaurantPOS_OrderInfoKOT] FOREIGN KEY([BillID])
REFERENCES [dbo].[TempRestaurantPOS_BillingInfoKOT] ([Id])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[UserRights]  WITH CHECK ADD  CONSTRAINT [FK_UserRights_Registration] FOREIGN KEY([UserID])
REFERENCES [dbo].[Registration] ([UserID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[Voucher_OtherDetails]  WITH CHECK ADD  CONSTRAINT [FK_Voucher_OtherDetails_Expense] FOREIGN KEY([Particulars])
REFERENCES [dbo].[Expense] ([ExpenseName])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[Voucher_OtherDetails]  WITH CHECK ADD  CONSTRAINT [FK_Voucher_OtherDetails_Voucher] FOREIGN KEY([VoucherID])
REFERENCES [dbo].[Voucher] ([ID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[Warehouse]  WITH CHECK ADD  CONSTRAINT [FK_Warehouse_WarehouseType] FOREIGN KEY([WarehouseType])
REFERENCES [dbo].[WarehouseType] ([Type])
ON UPDATE CASCADE
GO

ALTER TABLE [dbo].[WorkPeriodEnd]  WITH CHECK ADD  CONSTRAINT [FK_WorkPeriodEnd_WorkPeriodStart] FOREIGN KEY([Id])
REFERENCES [dbo].[WorkPeriodStart] ([ID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
