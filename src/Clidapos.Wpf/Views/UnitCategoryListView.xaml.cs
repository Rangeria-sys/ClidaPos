using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    public partial class UnitCategoryListView : Window
    {
        private readonly UnitService _unitService = new();
        private List<string> _all = new();

        public UnitCategoryListView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _all = await _unitService.GetAllAsync();
            ApplyToGrid(_all);
        }

        private void ApplyToGrid(List<string> names)
        {
            UnitGrid.ItemsSource = names
                .Select((name, index) => new NumberedRow { Number = index + 1, Name = name })
                .ToList();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = SearchBox.Text.Trim().ToLower();
            ApplyToGrid(string.IsNullOrEmpty(q)
                ? _all
                : _all.Where(c => c.ToLower().Contains(q)).ToList());
        }

        private void UnitGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (UnitGrid.SelectedItem is NumberedRow row)
            {
                var popup = new UnitCategoryPopup(row.Name);
                popup.Show();
                Close();
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}