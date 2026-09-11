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

    /// <summary>One cashier's activity within a shared period - Operator on
    /// each sale identifies who rang it up, regardless of the period itself
    /// being store-wide rather than per-cashier.</summary>
    public class CashierBreakdownRow
    {
        public string CashierName { get; set; } = "";
        public int BillCount { get; set; }
        public decimal Takings { get; set; }
        public int VoidCount { get; set; }
    }

    public class ShiftSummary
    {
        public int PeriodId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public bool IsOpen => EndedAt == null;
        public string? CashierName { get; set; }

        public int BillCount { get; set; }
        public decimal ItemCount { get; set; }

        public decimal GrandTotal { get; set; }
        public decimal TaxableTotal { get; set; }
        public decimal VatTotal { get; set; }
        public decimal DiscountsTotal { get; set; }

        public int VoidedSalesCount { get; set; }
        public decimal VoidedSalesTotal { get; set; }

        public decimal CashTotal { get; set; }
        public decimal MpesaTotal { get; set; }
        public decimal CardTotal { get; set; }
        public decimal OtherTotal { get; set; }

        public decimal AverageSale => BillCount == 0 ? 0 : Math.Round(GrandTotal / BillCount, 2);

        // Cash drawer reconciliation - null until the cashier has actually
        // entered a count for that side (opening or closing).
        public decimal? OpeningCash { get; set; }
        public decimal? ClosingCash { get; set; }

        /// <summary>What should be in the drawer if nothing went missing -
        /// the opening float plus every cash sale recorded during the period.
        /// Null until an opening count has actually been entered.</summary>
        public decimal? ExpectedCash => OpeningCash == null ? null : OpeningCash.Value + CashTotal;

        /// <summary>Positive = drawer has more than expected (over), negative =
        /// less than expected (short). Null until both counts exist.</summary>
        public decimal? CashVariance => (ClosingCash == null || ExpectedCash == null)
            ? null
            : ClosingCash.Value - ExpectedCash.Value;

        public List<TopItem> TopItems { get; set; } = new();
        public List<CashierBreakdownRow> CashierBreakdown { get; set; } = new();
    }
}