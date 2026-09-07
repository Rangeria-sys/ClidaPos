using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clidapos.Wpf.Services;

namespace Clidapos.Wpf.Views
{
    /// <summary>Row-number + name pairing for display, since RMCategory has no numeric ID of its own - the number is just this list's position, not a database value.</summary>
    public class NumberedRow
    {
        public int Number { get; set; }
        public string Name { get; set; } = "";
    }

    public partial class ItemCategoryListView : Window
    {
        private readonly CategoryService _categoryService = new();
        private List<string> _all = new();

        public ItemCategoryListView()
        {
            InitializeComponent();
            Loaded += async (s, e) => await LoadData();
        }

        private async System.Threading.Tasks.Task LoadData()
        {
            _all = await _categoryService.GetAllAsync();
            ApplyToGrid(_all);
        }

        private void ApplyToGrid(List<string> names)
        {
            CategoryGrid.ItemsSource = names
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

        private void CategoryGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CategoryGrid.SelectedItem is NumberedRow row)
            {
                var popup = new ItemCategoryPopup(row.Name);
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