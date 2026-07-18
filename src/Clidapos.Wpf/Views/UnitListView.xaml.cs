using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Entities;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class UnitListView : Window
    {
        private readonly Registration _currentUser;
        private readonly UnitService _unitService = new();
        private List<string> _all = new();

        public UnitListView(Registration currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _all = await _unitService.GetAllAsync();
            UnitGrid.ItemsSource = _all;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text.Trim().ToLower();
            UnitGrid.ItemsSource = string.IsNullOrEmpty(q)
                ? _all
                : _all.Where(u => u.ToLower().Contains(q)).ToList();
        }

        private void UnitGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (UnitGrid.SelectedItem is string name)
            {
                new CategoryUnitView(_currentUser, editUnit: name).Show();
                Close();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            new CategoryUnitView(_currentUser).Show();
            Close();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    }
}