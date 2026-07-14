using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Clidapos.Wpf.Data;
using Clidapos.Wpf.Entities;

namespace Clidapos.Wpf.Services
{
    public class PurchaseService
    {
        private const string DefaultSupplierCode = "SUPP-DEFAULT";

        /// <summary>
        /// Ensures a hidden default supplier exists, so Purchase.Supplier_ID
        /// always has a valid row to point to without any UI for supplier picking.
        /// </summary>
        public async Task<int> EnsureDefaultSupplierAsync()
        {
            using var db = new ClidaposDbContext();

            var existing = await db.Suppliers
                .FirstOrDefaultAsync(s => s.SupplierID.Trim() == DefaultSupplierCode);

            if (existing != null)
                return existing.ID;

            var maxId = await db.Suppliers.Select(s => (int?)s.ID).MaxAsync() ?? 0;

            var newSupplier = new Supplier
            {
                ID = maxId + 1,
                SupplierID = DefaultSupplierCode,
                Name = "Unspecified Supplier"
            };
            db.Suppliers.Add(newSupplier);
            await db.SaveChangesAsync();

            return newSupplier.ID;
        }

        /// <summary>
        /// Records a buying-price entry for a product as a real Purchase + Purchase_Join
        /// transaction, so it feeds the same tables the Purchasing module will use later.
        /// </summary>
        public async Task RecordBuyingPriceAsync(int productId, decimal qty, decimal buyingPrice)
        {
            if (buyingPrice <= 0)
                return;

            var supplierId = await EnsureDefaultSupplierAsync();

            using var db = new ClidaposDbContext();

            var maxPurchaseId = await db.Purchases.Select(p => (int?)p.ST_ID).MaxAsync() ?? 0;
            var total = qty * buyingPrice;

            var purchase = new Purchase
            {
                ST_ID = maxPurchaseId + 1,
                InvoiceNo = $"INIT-{maxPurchaseId + 1}",
                Date = DateTime.Now,
                PurchaseType = "Initial",
                Supplier_ID = supplierId,
                SubTotal = total,
                DiscountPer = 0,
                Discount = 0,
                PreviousDue = 0,
                FreightCharges = 0,
                OtherCharges = 0,
                Total = total,
                RoundOff = 0,
                GrandTotal = total,
                TotalPayment = total,
                PaymentDue = 0
            };
            db.Purchases.Add(purchase);

            var maxJoinId = await db.PurchaseJoins.Select(j => (int?)j.SP_ID).MaxAsync() ?? 0;

            var line = new PurchaseJoin
            {
                SP_ID = maxJoinId + 1,
                PurchaseID = purchase.ST_ID,
                ProductID = productId,
                Qty = qty,
                Price = buyingPrice,
                TotalAmount = total,
                Warehouse = "Main Store"
            };
            db.PurchaseJoins.Add(line);

            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Gets the most recent buying price recorded for a product, if any.
        /// </summary>
        public async Task<decimal?> GetLatestBuyingPriceAsync(int productId)
        {
            using var db = new ClidaposDbContext();

            var latest = await db.PurchaseJoins
                .Where(j => j.ProductID == productId)
                .OrderByDescending(j => j.SP_ID)
                .FirstOrDefaultAsync();

            return latest?.Price;
        }
    }
}