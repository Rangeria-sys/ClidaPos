using System.Windows;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class LoyaltyLedgerDetailPopup : Window
    {
        private readonly LoyaltyService _loyaltyService = new();
        private readonly LogService _logService = new();
        private readonly int _memberId;
        private readonly string _memberName;

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

            await _loyaltyService.AddPointsEarnedAsync(_memberId, LabelInput.Text.Trim(), points);
            await _logService.LogAsync(CurrentSession.UserId,
                $"Recorded {points:N2} points earned for Loyalty Member '{_memberName}' ({LabelInput.Text.Trim()})");

            LabelInput.Clear();
            PointsInput.Clear();
            await LoadHistory();
        }

        private async void RecordRedeemed_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGetValidatedInput(out var points)) return;

            await _loyaltyService.AddPointsRedeemedAsync(_memberId, LabelInput.Text.Trim(), points);
            await _logService.LogAsync(CurrentSession.UserId,
                $"Recorded {points:N2} points redeemed for Loyalty Member '{_memberName}' ({LabelInput.Text.Trim()})");

            LabelInput.Clear();
            PointsInput.Clear();
            await LoadHistory();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}