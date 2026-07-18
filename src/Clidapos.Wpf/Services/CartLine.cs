using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Clidapos.Wpf.Services
{
    /// <summary>One line in the on-screen cart. Notifies so the grid totals update live.</summary>
    public class CartLine : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string ProductCode { get; set; } = "";
        public string Category { get; set; } = "";

        private decimal _rate;
        public decimal Rate
        {
            get => _rate;
            set { _rate = value; OnPropertyChanged(); OnPropertyChanged(nameof(Amount)); }
        }

        private decimal _quantity;
        public decimal Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(); OnPropertyChanged(nameof(Amount)); }
        }

        public decimal Amount => Rate * Quantity;
    }
}