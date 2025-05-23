using StockFilterToolsV1.ViewModels;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace StockFilterToolsV1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var vm = new MainViewModel();
            this.DataContext = vm;

            vm.IndustryColumns.CollectionChanged += IndustryColumns_CollectionChanged;
        }

        private void IndustryColumns_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            IndustryStockDataGrid.Columns.Clear();
            foreach (var col in ((ObservableCollection<DataGridColumn>)sender))
            {
                IndustryStockDataGrid.Columns.Add(col);
            }
        }
    }
}