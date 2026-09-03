using System;
using System.Collections.Generic;

namespace Clidapos.Wpf.Entities
{
    public class HeldSale
    {
        public int Id { get; set; }
        public DateTime HeldDate { get; set; }
        public string? Operator { get; set; }
        public decimal DiscountPercent { get; set; }
        public string? CustomerName { get; set; }
        public string? Label { get; set; }
    }

    public class HeldSaleItem
    {
        public int Id { get; set; }
        public int HeldSaleId { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductCode { get; set; }
        public string? Category { get; set; }
        public decimal Rate { get; set; }
        public decimal Quantity { get; set; }
    }

    /// <summary>A held sale bundled with its line items - what the UI actually
    /// works with when browsing or resuming a held cart.</summary>
    public class HeldSaleWithItems
    {
        public HeldSale Sale { get; set; } = new();
        public List<HeldSaleItem> Items { get; set; } = new();
    }
}