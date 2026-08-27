using System;
using System.Windows;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class WalletTypeListView : Window
    {
        private readonly WalletTypeService _walletTypeService = new();
        private readonly LogService _logService = new();

        public WalletTypeListView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            TypeGrid.ItemsSource = await _walletTypeService.GetAllAsync();
        }

        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            if (string.IsNullOrWhiteSpace(NewTypeInput.Text))
            {
                ErrorText.Text = "Enter a wallet type name.";
                return;
            }

            try
            {
                await _walletTypeService.AddAsync(NewTypeInput.Text);
                await _logService.LogAsync(CurrentSession.UserId, $"Added Wallet Type '{NewTypeInput.Text.Trim()}'");
                NewTypeInput.Text = "";
                await LoadData();
            }
            catch (Exception ex)
            {
                ErrorText.Text = ex.InnerException?.Message ?? ex.Message;
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = "";

            if (TypeGrid.SelectedItem is not WalletType selected)
            {
                ErrorText.Text = "Select a wallet type to delete.";
                return;
            }

            var confirm = MessageBox.Show($"Remove wallet type '{selected.Type.Trim()}'?", "Confirm Remove",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            await _walletTypeService.DeleteAsync(selected.Type);
            await _logService.LogAsync(CurrentSession.UserId, $"Deleted Wallet Type '{selected.Type.Trim()}'");
            await LoadData();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}