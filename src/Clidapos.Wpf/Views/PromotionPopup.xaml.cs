using System;
using System.Globalization;
using System.Windows;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class PromotionPopup : Window
    {
        private readonly PromotionService _promotionService = new();
        private readonly LogService _logService = new();
        private Promotion? _editing;

        public PromotionPopup(Promotion? editPromotion = null)
        {
            InitializeComponent();

            if (editPromotion != null)
            {
                _editing = editPromotion;
                DishInput.Text = editPromotion.Dish?.Trim() ?? "";
                RateInput.Text = editPromotion.Rate?.ToString("0.##") ?? "";
                DayInput.Text = editPromotion.PDay?.Trim() ?? "";
                TimeFromInput.Text = editPromotion.TimeFrom?.ToString("HH:mm") ?? "";
                TimeToInput.Text = editPromotion.TimeTo?.ToString("HH:mm") ?? "";
                ActiveInput.Text = editPromotion.Active?.Trim() ?? "Y";
            }
            else
            {
                ActiveInput.Text = "Y";
            }
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            _editing = null;
            DishInput.Text = "";
            RateInput.Text = "";
            DayInput.Text = "";
            TimeFromInput.Text = "";
            TimeToInput.Text = "";
            ActiveInput.Text = "Y";
            ErrorText.Text = "";
            DishInput.Focus();
        }

        private bool TryParseFields(out decimal rate, out DateTime? timeFrom, out DateTime? timeTo)
        {
            rate = 0;
            timeFrom = null;
            timeTo = null;
            ErrorText.Text = "";

            if (string.IsNullOrWhiteSpace(DishInput.Text))
            {
                ErrorText.Text = "Item / Dish Name is required.";
                return false;
            }
            if (!decimal.TryParse(RateInput.Text, out rate) || rate <= 0)
            {
                ErrorText.Text = "Enter a valid Special Rate greater than zero.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(TimeFromInput.Text))
            {
                if (!TimeSpan.TryParseExact(TimeFromInput.Text.Trim(), "hh\\:mm", CultureInfo.InvariantCulture, out var fromSpan))
                {
                    ErrorText.Text = "Time From must be in HH:mm format, e.g. 18:00.";
                    return false;
                }
                timeFrom = DateTime.Today.Add(fromSpan);
            }

            if (!string.IsNullOrWhiteSpace(TimeToInput.Text))
            {
                if (!TimeSpan.TryParseExact(TimeToInput.Text.Trim(), "hh\\:mm", CultureInfo.InvariantCulture, out var toSpan))
                {
                    ErrorText.Text = "Time To must be in HH:mm format, e.g. 22:00.";
                    return false;
                }
                timeTo = DateTime.Today.Add(toSpan);
            }

            return true;
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!TryParseFields(out var rate, out var timeFrom, out var timeTo)) return;

            var newId = await _promotionService.GetNextIdAsync();

            var promotion = new Promotion
            {
                Id = newId,
                Dish = DishInput.Text.Trim(),
                Rate = rate,
                PDay = DayInput.Text.Trim(),
                TimeFrom = timeFrom,
                TimeTo = timeTo,
                Active = ActiveInput.Text.Trim()
            };

            try
            {
                await _promotionService.AddAsync(promotion);
                await _logService.LogAsync(CurrentSession.UserId, $"Added Promotion for '{promotion.Dish}' - Rate {rate:N2}");
                New_Click(sender, e);
                ErrorText.Text = "Saved.";
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                ErrorText.Text = detail;
            }
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            if (_editing == null)
            {
                ErrorText.Text = "Use Get Data, pick a promotion, then edit and Update.";
                return;
            }
            if (!TryParseFields(out var rate, out var timeFrom, out var timeTo)) return;

            try
            {
                _editing.Dish = DishInput.Text.Trim();
                _editing.Rate = rate;
                _editing.PDay = DayInput.Text.Trim();
                _editing.TimeFrom = timeFrom;
                _editing.TimeTo = timeTo;
                _editing.Active = ActiveInput.Text.Trim();

                await _promotionService.UpdateAsync(_editing);
                await _logService.LogAsync(CurrentSession.UserId, $"Updated Promotion for '{_editing.Dish}'");
                ErrorText.Text = "Updated.";
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                ErrorText.Text = detail;
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_editing == null)
            {
                ErrorText.Text = "Use Get Data, pick a promotion, then Delete.";
                return;
            }

            var confirm = MessageBox.Show($"Remove promotion for '{_editing.Dish?.Trim()}'?", "Confirm Remove",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            var deletedName = _editing.Dish?.Trim() ?? "";
            await _promotionService.DeleteAsync(_editing.Id);
            await _logService.LogAsync(CurrentSession.UserId, $"Deleted Promotion for '{deletedName}'");
            New_Click(sender, e);
            ErrorText.Text = "Removed.";
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}