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

        private decimal _price;
        public decimal Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(); OnPropertyChanged(nameof(Amount)); }
        }

        private decimal _qty;
        public decimal Qty
        {
            get => _qty;
            set { _qty = value; OnPropertyChanged(); OnPropertyChanged(nameof(Amount)); }
        }

        public decimal Amount => Price * Qty;
    }
}