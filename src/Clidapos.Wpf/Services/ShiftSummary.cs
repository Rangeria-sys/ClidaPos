using System;
using System.Collections.Generic;

namespace Clidapos.Wpf.Services
{
    public class TopItem
    {
        public string Name { get; set; } = "";
        public decimal Qty { get; set; }
        public decimal Value { get; set; }
    }

    public class ShiftSummary
    {
        public int PeriodId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public bool IsOpen => EndedAt == null;

        public int BillCount { get; set; }
        public decimal ItemCount { get; set; }

        public decimal GrandTotal { get; set; }
        public decimal TaxableTotal { get; set; }
        public decimal VatTotal { get; set; }

        public decimal CashTotal { get; set; }
        public decimal MpesaTotal { get; set; }
        public decimal CardTotal { get; set; }
        public decimal OtherTotal { get; set; }

        public decimal AverageSale => BillCount == 0 ? 0 : Math.Round(GrandTotal / BillCount, 2);

        public List<TopItem> TopItems { get; set; } = new();
    }
}