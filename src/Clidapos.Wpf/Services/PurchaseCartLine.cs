using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Clidapos.Wpf.Services
{
    /// <summary>One line in the on-screen purchase grid. Notifies so totals update live.</summary>
    public class PurchaseCartLine : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string ProductCode { get; set; } = "";

        /// <summary>What you paid last time, shown for reference only - never used to auto-fill Price.</summary>
        public decimal? LastKnownPrice { get; set; }

        private string _expiryDate = "";
        /// <summary>Free-text "yyyy-MM-dd" or blank - optional, only relevant for products that expire.</summary>
        public string ExpiryDate
        {
            get => _expiryDate;
            set { _expiryDate = value; OnPropertyChanged(); }
        }

        private decimal _price;
        public decimal Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(); OnPropertyChanged(nameof(Amount)); OnPropertyChanged(nameof(NeedsPrice)); }
        }

        /// <summary>True while Cost Price is still blank/zero - drives the yellow "needs filling" highlight.</summary>
        public bool NeedsPrice => Price <= 0;

        private decimal _qty;
        public decimal Qty
        {
            get => _qty;
            set { _qty = value; OnPropertyChanged(); OnPropertyChanged(nameof(Amount)); }
        }

        public decimal Amount => Price * Qty;
    }
}