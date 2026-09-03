using System.Windows;

namespace Clidapos.Wpf.Views
{
    public partial class VoidReasonPopup : Window
    {
        public string Reason { get; private set; } = "";

        public VoidReasonPopup(int lineCount, decimal total, string currencySymbol)
        {
            InitializeComponent();
            SummaryText.Text = $"This will clear {lineCount} line(s) worth {currencySymbol} {total:N2} from the cart. This cannot be undone.";
            ReasonInput.Focus();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            var reason = ReasonInput.Text.Trim();
            if (reason.Length == 0)
            {
                ErrorText.Text = "A reason is required to void this sale.";
                return;
            }

            Reason = reason;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
