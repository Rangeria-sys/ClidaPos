using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Data
{
    public class ClidaposDbContext : DbContext
    {
        public DbSet<Registration> Registrations => Set<Registration>();
        public DbSet<WorkPeriodStart> WorkPeriodStarts => Set<WorkPeriodStart>();
        public DbSet<WorkPeriodEnd> WorkPeriodEnds => Set<WorkPeriodEnd>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductOpeningStock> ProductOpeningStocks => Set<ProductOpeningStock>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<Purchase> Purchases => Set<Purchase>();
        public DbSet<PurchaseJoin> PurchaseJoins => Set<PurchaseJoin>();
        public DbSet<UnitMaster> UnitMasters => Set<UnitMaster>();
        public DbSet<RMCategory> RMCategories => Set<RMCategory>();
        public DbSet<Warehouse> Warehouses => Set<Warehouse>();
        public DbSet<WarehouseType> WarehouseTypes => Set<WarehouseType>();
        public DbSet<SaleBill> SaleBills => Set<SaleBill>();
        public DbSet<SaleItem> SaleItems => Set<SaleItem>();
        public DbSet<DeletedInvoice> DeletedInvoices => Set<DeletedInvoice>();
        public DbSet<DeletedInvoiceJoin> DeletedInvoiceJoins => Set<DeletedInvoiceJoin>();
        public DbSet<ExpenseType> ExpenseTypes => Set<ExpenseType>();
        public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
        public DbSet<Expense> Expenses => Set<Expense>();
        public DbSet<LogEntry> Logs => Set<LogEntry>();
        public DbSet<Hotel> Hotels => Set<Hotel>();
        public DbSet<EmployeeRegistration> EmployeeRegistrations => Set<EmployeeRegistration>();
        public DbSet<SupplierLedgerEntry> SupplierLedgerEntries => Set<SupplierLedgerEntry>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<CustomerLedgerEntry> CustomerLedgerEntries => Set<CustomerLedgerEntry>();
        public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
        public DbSet<Bank> Banks => Set<Bank>();
        public DbSet<BankBranch> BankBranches => Set<BankBranch>();
        public DbSet<BankAccountRegistration> BankAccountRegistrations => Set<BankAccountRegistration>();
        public DbSet<BankAccountLedger> BankAccountLedgers => Set<BankAccountLedger>();
        public DbSet<LoyaltyMember> LoyaltyMembers => Set<LoyaltyMember>();
        public DbSet<LoyaltyMemberLedgerBook> LoyaltyMemberLedgerBooks => Set<LoyaltyMemberLedgerBook>();
        public DbSet<LoyaltySetting> LoyaltySettings => Set<LoyaltySetting>();
        public DbSet<Voucher> Vouchers => Set<Voucher>();
        public DbSet<VoucherOtherDetail> VoucherOtherDetails => Set<VoucherOtherDetail>();
        public DbSet<Promotion> Promotions => Set<Promotion>();
        public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
        public DbSet<LedgerBookEntry> LedgerBookEntries => Set<LedgerBookEntry>();

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            var connectionString = config.GetConnectionString("ClidaDB");
            options.UseSqlServer(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Registration>(b =>
            {
                b.ToTable("Registration", "dbo");
                b.HasKey(x => x.UserID);
                b.Property(x => x.UserID).HasColumnType("nchar(100)");
                b.Property(x => x.UserType).HasColumnType("nchar(30)");
                b.Property(x => x.Password).HasColumnType("nchar(50)");
                b.Property(x => x.Name).HasColumnType("nchar(150)");
                b.Property(x => x.JoiningDate).HasColumnType("datetime");
                b.Property(x => x.Active).HasColumnType("nchar(10)");
                b.Property(x => x.ContactNo).HasColumnType("nchar(50)");
                b.Property(x => x.EmailID).HasColumnType("nchar(150)");
                b.Property(x => x.SSN).HasColumnType("nchar(50)");
                b.Property(x => x.PayrollType).HasColumnType("nchar(30)");
                b.Property(x => x.CardNo).HasColumnType("nchar(50)");
                b.Property(x => x.AutoLogout).HasColumnType("nchar(10)");
            });

            modelBuilder.Entity<WorkPeriodStart>(b =>
            {
                b.ToTable("WorkPeriodStart", "dbo");
                b.HasKey(x => x.ID);
                b.Property(x => x.WPStart).HasColumnType("datetime");
                b.Property(x => x.Status).HasColumnType("nchar(20)");
            });

            modelBuilder.Entity<WorkPeriodEnd>(b =>
            {
                b.ToTable("WorkPeriodEnd", "dbo");
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
                b.Property(x => x.WPEnd).HasColumnType("datetime");
            });

            modelBuilder.Entity<Product>(b =>
            {
                b.ToTable("Product", "dbo");
                b.HasKey(x => x.PID);
                b.Property(x => x.PID).ValueGeneratedNever();
                b.Property(x => x.ProductCode).HasColumnType("nchar(30)");
                b.Property(x => x.ProductName).HasColumnType("nchar(200)");
                b.Property(x => x.Category).HasColumnType("nchar(150)");
                b.Property(x => x.Description).HasColumnType("nvarchar(max)");
                b.Property(x => x.Unit).HasColumnType("nchar(50)");
                b.Property(x => x.Price).HasColumnType("decimal(18,2)");
                b.Property(x => x.P_Supplier).HasColumnType("nchar(150)");
            });

            modelBuilder.Entity<ProductOpeningStock>(b =>
            {
                b.ToTable("Product_OpeningStock", "dbo");
                b.HasKey(x => x.PS_ID);
                b.Property(x => x.Warehouse).HasColumnType("nchar(250)");
                b.Property(x => x.Qty).HasColumnType("decimal(18,2)");
                b.Property(x => x.HasExpiryDate).HasColumnType("nchar(10)");
                b.Property(x => x.ExpiryDate).HasColumnType("nchar(50)");
            });

            modelBuilder.Entity<Supplier>(b =>
            {
                b.ToTable("Supplier", "dbo");
                b.HasKey(x => x.ID);
                b.Property(x => x.ID).ValueGeneratedNever();
                b.Property(x => x.SupplierID).HasColumnType("nchar(30)");
                b.Property(x => x.Name).HasColumnType("nchar(200)");
            });

            modelBuilder.Entity<Purchase>(b =>
            {
                b.ToTable("Purchase", "dbo");
                b.HasKey(x => x.ST_ID);
                b.Property(x => x.ST_ID).ValueGeneratedNever();
                b.Property(x => x.InvoiceNo).HasColumnType("nchar(30)");
                b.Property(x => x.PurchaseType).HasColumnType("nchar(20)");
                b.Property(x => x.SubTotal).HasColumnType("decimal(18,2)");
                b.Property(x => x.DiscountPer).HasColumnType("decimal(18,2)");
                b.Property(x => x.Discount).HasColumnType("decimal(18,2)");
                b.Property(x => x.PreviousDue).HasColumnType("decimal(18,2)");
                b.Property(x => x.FreightCharges).HasColumnType("decimal(18,2)");
                b.Property(x => x.OtherCharges).HasColumnType("decimal(18,2)");
                b.Property(x => x.Total).HasColumnType("decimal(18,2)");
                b.Property(x => x.RoundOff).HasColumnType("decimal(18,2)");
                b.Property(x => x.GrandTotal).HasColumnType("decimal(18,2)");
                b.Property(x => x.TotalPayment).HasColumnType("decimal(18,2)");
                b.Property(x => x.PaymentDue).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<PurchaseJoin>(b =>
            {
                b.ToTable("Purchase_Join", "dbo");
                b.HasKey(x => x.SP_ID);
                b.Property(x => x.SP_ID).ValueGeneratedOnAdd();
                b.Property(x => x.Qty).HasColumnType("decimal(18,2)");
                b.Property(x => x.Price).HasColumnType("decimal(18,2)");
                b.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
                b.Property(x => x.Warehouse).HasColumnType("nchar(250)");
            });

            modelBuilder.Entity<UnitMaster>(b =>
            {
                b.ToTable("UnitMaster", "dbo");
                b.HasKey(x => x.Unit);
                b.Property(x => x.Unit).HasColumnType("nchar(50)");
            });

            modelBuilder.Entity<RMCategory>(b =>
            {
                b.ToTable("RMCategory", "dbo");
                b.HasKey(x => x.CategoryName);
                b.Property(x => x.CategoryName).HasColumnType("nchar(150)");
            });

            modelBuilder.Entity<Warehouse>(b =>
            {
                b.ToTable("Warehouse", "dbo");
                b.HasKey(x => x.WarehouseName);
                b.Property(x => x.WarehouseName).HasColumnType("nchar(250)");
                b.Property(x => x.Address).HasColumnType("nvarchar(250)");
                b.Property(x => x.WarehouseType).HasColumnType("nchar(200)");
                b.Property(x => x.City).HasColumnType("nchar(200)");
            });

            modelBuilder.Entity<WarehouseType>(b =>
            {
                b.ToTable("WarehouseType", "dbo");
                b.HasKey(x => x.Type);
                b.Property(x => x.Type).HasColumnType("nchar(200)");
            });

            // ---------- SALES (counter / takeaway channel) ----------
            modelBuilder.Entity<SaleBill>(b =>
            {
                b.ToTable("RestaurantPOS_BillingInfoTA", "dbo");
                b.HasKey(x => x.Id);
                b.Property(x => x.Id).ValueGeneratedNever();
                b.Property(x => x.BillNo).HasColumnType("nchar(15)");
                b.Property(x => x.BillDate).HasColumnType("datetime");
                b.Property(x => x.SubTotal).HasColumnType("decimal(18,2)");
                b.Property(x => x.TADiscountPer).HasColumnType("decimal(18,4)");
                b.Property(x => x.TADiscountAmt).HasColumnType("decimal(18,2)");
                b.Property(x => x.GrandTotal).HasColumnType("decimal(18,2)");
                b.Property(x => x.Cash).HasColumnType("decimal(18,2)");
                b.Property(x => x.Change).HasColumnType("decimal(18,2)");
                b.Property(x => x.Operator).HasColumnType("nchar(100)");
                b.Property(x => x.PaymentMode).HasColumnType("nchar(50)");
                b.Property(x => x.CustomerName).HasColumnType("nchar(150)");
                b.Property(x => x.PhoneNo).HasColumnType("nchar(100)");
                b.Property(x => x.TA_Status).HasColumnType("nchar(30)");
                b.Property(x => x.TaxType).HasColumnType("nchar(20)");
                b.Property(x => x.Card).HasColumnType("decimal(18,2)");
                b.Property(x => x.TotalTaxableAmount).HasColumnName("totalTaxableAmount").HasColumnType("decimal(18,2)");
                b.Property(x => x.TotalTaxAmount).HasColumnName("totalTaxAmount").HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<SaleItem>(b =>
            {
                b.ToTable("RestaurantPOS_OrderedProductBillTA", "dbo");
                b.HasKey(x => x.OP_ID);
                b.Property(x => x.OP_ID).ValueGeneratedOnAdd();
                b.Property(x => x.Dish).HasColumnType("nvarchar(max)");
                b.Property(x => x.Rate).HasColumnType("decimal(18,2)");
                b.Property(x => x.Quantity).HasColumnType("decimal(18,2)");
                b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
                b.Property(x => x.VATPer).HasColumnType("decimal(18,2)");
                b.Property(x => x.VATAmount).HasColumnType("decimal(18,3)");
                b.Property(x => x.DiscountPer).HasColumnType("decimal(18,4)");
                b.Property(x => x.DiscountAmount).HasColumnType("decimal(18,3)");
                b.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
                b.Property(x => x.Category).HasColumnType("nchar(200)");
                b.Property(x => x.ItemStatus).HasColumnType("nchar(30)");
            });

            // ---------- VOID / REFUND AUDIT TRAIL ----------
            modelBuilder.Entity<DeletedInvoice>(b =>
            {
                b.ToTable("DeletedInvoices", "dbo");
                b.HasKey(x => x.Id);
                b.Property(x => x.BillNo).HasColumnType("nchar(15)");
                b.Property(x => x.GrandTotal).HasColumnType("decimal(18,2)");
                b.Property(x => x.Operator).HasColumnType("nchar(100)");
                b.Property(x => x.PaymentMode).HasColumnType("nchar(100)");
                b.Property(x => x.Reason).HasColumnType("nchar(200)");
                b.Property(x => x.BillType).HasColumnType("nchar(20)");
                b.Property(x => x.Canceled_Deleted).HasColumnType("nchar(20)");
            });

            modelBuilder.Entity<DeletedInvoiceJoin>(b =>
            {
                b.ToTable("DeletedInvoices_Join", "dbo");
                b.HasKey(x => x.Id);
                b.Property(x => x.BillNo).HasColumnType("nchar(15)");
                b.Property(x => x.ItemName).HasColumnType("nvarchar(max)");
                b.Property(x => x.Qty).HasColumnType("decimal(18,2)");
                b.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            });

            // ---------- EXPENSE TYPE (Category Setup) ----------
            modelBuilder.Entity<ExpenseType>(b =>
            {
                b.ToTable("ExpenseType", "dbo");
                b.HasKey(x => x.Type);
                b.Property(x => x.Type).HasColumnType("nchar(200)");
            });

            // ---------- STOCK ADJUSTMENT ----------
            modelBuilder.Entity<StockAdjustment>(b =>
            {
                b.ToTable("StockAdjustment_Warehouse", "dbo");
                b.HasKey(x => x.SA_ID);
                b.Property(x => x.Warehouse).HasColumnType("nchar(250)");
                b.Property(x => x.AdjustmentType).HasColumnType("nchar(20)");
                b.Property(x => x.Qty).HasColumnType("decimal(18,2)");
                b.Property(x => x.Reason).HasColumnType("nchar(200)");
            });

            // ---------- EXPENSE (master list of named expense items) ----------
            modelBuilder.Entity<Expense>(b =>
            {
                b.ToTable("Expense", "dbo");
                b.HasKey(x => x.ExpenseName);
                b.Property(x => x.ExpenseName).HasColumnType("nvarchar(250)");
                b.Property(x => x.ExpenseType).HasColumnType("nchar(200)");
            });

            // ---------- SYSTEM LOGS ----------
            modelBuilder.Entity<LogEntry>(b =>
            {
                b.ToTable("Logs", "dbo");
                b.HasKey(x => x.Id);
                b.Property(x => x.UserID).HasColumnType("nchar(100)");
                b.Property(x => x.Operation).HasColumnType("nvarchar(250)");
                b.Property(x => x.Date).HasColumnType("datetime");
            });

            // ---------- HOTEL (business profile - singleton, one row) ----------
            modelBuilder.Entity<Hotel>(b =>
            {
                b.ToTable("Hotel", "dbo");
                b.HasKey(x => x.Id);
                b.Property(x => x.HotelName).HasColumnType("nchar(150)");
                b.Property(x => x.AddressLine1).HasColumnType("nvarchar(250)");
                b.Property(x => x.AddressLine2).HasColumnType("nvarchar(250)");
                b.Property(x => x.AddressLine3).HasColumnType("nvarchar(250)");
                b.Property(x => x.ContactNo).HasColumnType("nchar(100)");
                b.Property(x => x.EmailID).HasColumnType("nchar(150)");
                b.Property(x => x.TIN).HasColumnType("nchar(30)");
                b.Property(x => x.STNo).HasColumnType("nchar(30)");
                b.Property(x => x.CIN).HasColumnType("nchar(30)");
                b.Property(x => x.BaseCurrency).HasColumnType("nchar(200)");
                b.Property(x => x.CurrencyCode).HasColumnType("nchar(10)");
                b.Property(x => x.TicketFooterMessage).HasColumnType("nvarchar(250)");
                b.Property(x => x.ShowLogo).HasColumnType("nchar(20)");
                b.Property(x => x.CapitalAccount).HasColumnType("decimal(18,2)");
                b.Property(x => x.Logo).HasColumnType("image");
            });

            // ---------- EMPLOYEE REGISTRATION (real HR/personal details) ----------
            modelBuilder.Entity<EmployeeRegistration>(b =>
            {
                b.ToTable("EmployeeRegistration", "dbo");
                b.HasKey(x => x.EmpId);
                b.Property(x => x.EmployeeID).HasColumnType("nchar(15)");
                b.Property(x => x.EmployeeName).HasColumnType("nchar(150)");
                b.Property(x => x.Address).HasColumnType("nvarchar(250)");
                b.Property(x => x.City).HasColumnType("nchar(150)");
                b.Property(x => x.ContactNo).HasColumnType("nchar(30)");
                b.Property(x => x.Email).HasColumnType("nchar(150)");
                b.Property(x => x.DateOfJoining).HasColumnType("datetime");
                b.Property(x => x.Active).HasColumnType("nchar(20)");
                b.Property(x => x.Photo).HasColumnType("image");
            });

            // ---------- SUPPLIER LEDGER (linked to real Supplier via SupplierID) ----------
            modelBuilder.Entity<SupplierLedgerEntry>(b =>
            {
                b.ToTable("SupplierLedgerBook", "dbo");
                b.HasKey(x => x.Id);
                b.Property(x => x.Date).HasColumnType("datetime");
                b.Property(x => x.Name).HasColumnType("nchar(200)");
                b.Property(x => x.LedgerNo).HasColumnType("nchar(50)");
                b.Property(x => x.Label).HasColumnType("nchar(200)");
                b.Property(x => x.Debit).HasColumnType("decimal(18,2)");
                b.Property(x => x.Credit).HasColumnType("decimal(18,2)");
                b.Property(x => x.PartyID).HasColumnType("nchar(20)");
            });

            // ---------- CUSTOMER (real customer master) ----------
            modelBuilder.Entity<Customer>(b =>
            {
                b.ToTable("Customer", "dbo");
                b.HasKey(x => x.ID);
                b.Property(x => x.ID).ValueGeneratedNever();
                b.Property(x => x.CustomerID).HasColumnType("nchar(30)");
                b.Property(x => x.Name).HasColumnType("nchar(200)");
                b.Property(x => x.ContactNo).HasColumnType("nchar(50)");
                b.Property(x => x.Email).HasColumnType("nchar(150)");
            });

            // ---------- CUSTOMER LEDGER (linked to real Customer via int CreditCustomer_ID) ----------
            modelBuilder.Entity<CustomerLedgerEntry>(b =>
            {
                b.ToTable("CreditCustomerLedger", "dbo");
                b.HasKey(x => x.Id);
                b.Property(x => x.Date).HasColumnType("datetime");
                b.Property(x => x.LedgerNo).HasColumnType("nchar(50)");
                b.Property(x => x.Label).HasColumnType("nchar(200)");
                b.Property(x => x.Debit).HasColumnType("decimal(18,2)");
                b.Property(x => x.Credit).HasColumnType("decimal(18,2)");
            });

            // ---------- PAYROLL RUN (real, Kenya-appropriate, linked to EmployeeRegistration) ----------
            modelBuilder.Entity<PayrollRun>(b =>
            {
                b.ToTable("PayrollRun", "dbo");
                b.HasKey(x => x.Id);
                b.Property(x => x.PaymentDate).HasColumnType("datetime");
                b.Property(x => x.PayMonth).HasColumnType("nchar(20)");
                b.Property(x => x.GrossSalary).HasColumnType("decimal(18,2)");
                b.Property(x => x.NSSFPer).HasColumnType("decimal(18,4)");
                b.Property(x => x.NSSF).HasColumnType("decimal(18,2)");
                b.Property(x => x.SHAPer).HasColumnType("decimal(18,4)");
                b.Property(x => x.SHA).HasColumnType("decimal(18,2)");
                b.Property(x => x.HousingLevyPer).HasColumnType("decimal(18,4)");
                b.Property(x => x.HousingLevy).HasColumnType("decimal(18,2)");
                b.Property(x => x.PAYEPer).HasColumnType("decimal(18,4)");
                b.Property(x => x.PAYE).HasColumnType("decimal(18,2)");
                b.Property(x => x.NetPay).HasColumnType("decimal(18,2)");
                b.Property(x => x.PaymentMode).HasColumnType("nchar(50)");
                b.Property(x => x.Remarks).HasColumnType("nchar(250)");
            });

            // ---------- FINANCE & BANKING (real, linked Bank -> Branch -> Account -> Ledger) ----------
            modelBuilder.Entity<Bank>(b =>
            {
                b.ToTable("Bank", "dbo");
                b.HasKey(x => x.BankName);
                b.Property(x => x.BankName).HasColumnType("nvarchar(250)");
            });

            modelBuilder.Entity<BankBranch>(b =>
            {
                b.ToTable("BankBranch", "dbo");
                b.HasKey(x => x.Id);
                b.Property(x => x.BranchName).HasColumnType("nvarchar(250)");
                b.Property(x => x.Address).HasColumnType("nvarchar(250)");
                b.Property(x => x.ContactNo).HasColumnType("nchar(100)");
                b.Property(x => x.SwiftCode).HasColumnType("nchar(50)");
                b.Property(x => x.IFSCCode).HasColumnType("nchar(50)");
                b.Property(x => x.BankName).HasColumnType("nvarchar(250)");
            });

            modelBuilder.Entity<BankAccountRegistration>(b =>
            {
                b.ToTable("BankAccountRegistration", "dbo");
                b.HasKey(x => x.AccountNo);
                b.Property(x => x.AccountNo).HasColumnType("nchar(50)");
                b.Property(x => x.AccountName).HasColumnType("nchar(200)");
                b.Property(x => x.AccountType).HasColumnType("nchar(100)");
                b.Property(x => x.OpeningDate).HasColumnType("datetime");
                b.Property(x => x.BalanceAmount).HasColumnType("decimal(18,2)");
                b.Property(x => x.Active).HasColumnType("nchar(10)");
            });

            modelBuilder.Entity<BankAccountLedger>(b =>
            {
                b.ToTable("BankAccountLedger", "dbo");
                b.HasKey(x => x.Id);
                b.Property(x => x.Date).HasColumnType("datetime");
                b.Property(x => x.AccNo).HasColumnType("nchar(50)");
                b.Property(x => x.LedgerNo).HasColumnType("nchar(200)");
                b.Property(x => x.Label).HasColumnType("nchar(200)");
                b.Property(x => x.Debit).HasColumnType("decimal(18,2)");
                b.Property(x => x.Credit).HasColumnType("decimal(18,2)");
            });

            // ---------- LOYALTY & MEMBERSHIP (real points-based program) ----------
            modelBuilder.Entity<LoyaltyMember>(b =>
            {
                b.ToTable("LoyaltyMember", "dbo");
                b.HasKey(x => x.MemberID);
                b.Property(x => x.MemberID).ValueGeneratedNever();
                b.Property(x => x.Name).HasColumnType("nchar(200)");
                b.Property(x => x.CardNo).HasColumnType("nchar(50)");
                b.Property(x => x.ContactNo).HasColumnType("nchar(50)");
                b.Property(x => x.Address).HasColumnType("nvarchar(max)");
                b.Property(x => x.RegistrationDate).HasColumnType("datetime");
                b.Property(x => x.Active).HasColumnType("nchar(10)");
            });

            modelBuilder.Entity<LoyaltyMemberLedgerBook>(b =>
            {
                b.ToTable("LoyaltyMemberLedgerBook", "dbo");
                b.HasKey(x => x.Id);
                b.Property(x => x.Date).HasColumnType("datetime");
                b.Property(x => x.LedgerNo).HasColumnType("nchar(50)");
                b.Property(x => x.Label).HasColumnType("nchar(200)");
                b.Property(x => x.PointsEarned).HasColumnType("decimal(18,2)");
                b.Property(x => x.PointsRedeem).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<LoyaltySetting>(b =>
            {
                b.ToTable("LoyaltySetting", "dbo");
                b.HasKey(x => x.LoyaltyName);
                b.Property(x => x.LoyaltyName).HasColumnType("nchar(150)");
                b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
                b.Property(x => x.Points).HasColumnType("decimal(18,2)");
            });

            // ---------- VOUCHERS & PROMOTIONS ----------
            modelBuilder.Entity<Voucher>(b =>
            {
                b.ToTable("Voucher", "dbo");
                b.HasKey(x => x.ID);
                b.Property(x => x.VoucherNo).HasColumnType("nchar(30)");
                b.Property(x => x.Name).HasColumnType("nchar(150)");
                b.Property(x => x.Date).HasColumnType("datetime");
                b.Property(x => x.Details).HasColumnType("nvarchar(max)");
                b.Property(x => x.PaymentMode).HasColumnType("nchar(150)");
                b.Property(x => x.GrandTotal).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<VoucherOtherDetail>(b =>
            {
                b.ToTable("Voucher_OtherDetails", "dbo");
                b.HasKey(x => x.VD_ID);
                b.Property(x => x.Particulars).HasColumnType("nvarchar(250)");
                b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
                b.Property(x => x.Note).HasColumnType("nvarchar(max)");
            });

            modelBuilder.Entity<Promotion>(b =>
            {
                b.ToTable("Promotion", "dbo");
                b.HasKey(x => x.Id);
                b.Property(x => x.Dish).HasColumnType("nvarchar(250)");
                b.Property(x => x.Rate).HasColumnType("decimal(18,2)");
                b.Property(x => x.PDay).HasColumnType("nchar(30)");
                b.Property(x => x.TimeFrom).HasColumnType("datetime");
                b.Property(x => x.TimeTo).HasColumnType("datetime");
                b.Property(x => x.Active).HasColumnType("nchar(10)");
            });

            // ---------- ACCOUNTING: manual double-entry Journal + LedgerBook ----------
            modelBuilder.Entity<JournalEntry>(b =>
            {
                b.ToTable("Journal", "dbo");
                b.HasKey(x => x.ID);
                b.Property(x => x.DebitAccount).HasColumnType("nchar(200)");
                b.Property(x => x.CreditAccount).HasColumnType("nchar(200)");
                b.Property(x => x.Date).HasColumnType("datetime");
                b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
                b.Property(x => x.Remarks).HasColumnType("nvarchar(max)");
            });

            modelBuilder.Entity<LedgerBookEntry>(b =>
            {
                b.ToTable("LedgerBook", "dbo");
                b.HasKey(x => x.Id);
                b.Property(x => x.Date).HasColumnType("datetime");
                b.Property(x => x.Name).HasColumnType("nchar(200)");
                b.Property(x => x.LedgerNo).HasColumnType("nchar(200)");
                b.Property(x => x.Label).HasColumnType("nchar(200)");
                b.Property(x => x.AccLedger).HasColumnType("nchar(200)");
                b.Property(x => x.Debit).HasColumnType("decimal(18,2)");
                b.Property(x => x.Credit).HasColumnType("decimal(18,2)");
                b.Property(x => x.PartyID).HasColumnType("nchar(50)");
            });
        }
    }
}