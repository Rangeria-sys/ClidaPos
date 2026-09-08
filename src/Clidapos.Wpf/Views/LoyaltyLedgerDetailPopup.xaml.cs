using System;
using System.Media;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class LoyaltyLedgerDetailPopup : Window
    {
        private readonly LoyaltyService _loyaltyService = new();
        private readonly LogService _logService = new();
        private readonly int _memberId;
        private readonly string _memberName;

        /// <summary>Fired after points are successfully earned or redeemed, so the
        /// screen that opened this popup (the Loyalty list) can refresh its
        /// balances immediately instead of only showing them from when it first
        /// loaded.</summary>
        public event EventHandler? BalanceChanged;

        public LoyaltyLedgerDetailPopup(int memberId, string memberName)
        {
            InitializeComponent();
            _memberId = memberId;
            _memberName = memberName;
            MemberNameText.Text = memberName;

            Loaded += async (s, e) => await LoadHistory();
        }

        private async System.Threading.Tasks.Task LoadHistory()
        {
            var entries = await _loyaltyService.GetLedgerForMemberAsync(_memberId);
            HistoryGrid.ItemsSource = entries;

            var balance = 0m;
            foreach (var entry in entries)
                balance += entry.PointsEarned - entry.PointsRedeem;

            BalanceText.Text = balance.ToString("N2");
        }

        private bool TryGetValidatedInput(out decimal points)
        {
            points = 0;
            ErrorText.Text = "";

            if (string.IsNullOrWhiteSpace(LabelInput.Text))
            {
                ErrorText.Text = "Enter a description first.";
                return false;
            }

            if (!decimal.TryParse(PointsInput.Text, out points) || points <= 0)
            {
                ErrorText.Text = "Enter a valid number of points greater than zero.";
                return false;
            }

            return true;
        }

        private async void RecordEarned_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetValidatedInput(out var points)) return;

            var confirm = MessageBox.Show(
                $"Are you sure you want to add {points:N2} points earned for '{_memberName}'?\n\n{LabelInput.Text.Trim()}",
                "Confirm Points Earned", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            await _loyaltyService.AddPointsEarnedAsync(_memberId, LabelInput.Text.Trim(), points);
            await _logService.LogAsync(CurrentSession.UserId,
                $"Recorded {points:N2} points earned for Loyalty Member '{_memberName}' ({LabelInput.Text.Trim()})");

            LabelInput.Clear();
            PointsInput.Clear();
            await LoadHistory();
            BalanceChanged?.Invoke(this, EventArgs.Empty);
        }

        private async void RecordRedeemed_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetValidatedInput(out var points)) return;

            var confirm = MessageBox.Show(
                $"Are you sure '{_memberName}' is redeeming {points:N2} points?\n\n{LabelInput.Text.Trim()}",
                "Confirm Points Redeemed", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            await _loyaltyService.AddPointsRedeemedAsync(_memberId, LabelInput.Text.Trim(), points);
            await _logService.LogAsync(CurrentSession.UserId,
                $"Recorded {points:N2} points redeemed for Loyalty Member '{_memberName}' ({LabelInput.Text.Trim()})");

            LabelInput.Clear();
            PointsInput.Clear();
            await LoadHistory();
            BalanceChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static readonly Regex NumericPattern = new(@"^[0-9]*\.?[0-9]*$");

        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox textBox)
            {
                e.Handled = true;
                return;
            }

            var proposedText = textBox.Text
                .Remove(textBox.SelectionStart, textBox.SelectionLength)
                .Insert(textBox.SelectionStart, e.Text);

            e.Handled = !NumericPattern.IsMatch(proposedText);
        }

        private void Backdrop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender)
            {
                SystemSounds.Exclamation.Play();
            }
        }
    }
}
