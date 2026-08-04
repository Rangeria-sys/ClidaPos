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
                b.Property(x => x.Active).HasColumnType("nchar(10)");
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
                // Id is NOT an identity column - it mirrors WorkPeriodStart.ID.
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
                b.Property(x => x.Date).HasColumnType("datetime");
                b.Property(x => x.Warehouse).HasColumnType("nchar(250)");
                b.Property(x => x.AdjustmentType).HasColumnType("nchar(20)");
                b.Property(x => x.Qty).HasColumnType("decimal(18,2)");
                b.Property(x => x.Reason).HasColumnType("nchar(200)");
            });
        }
    }
}