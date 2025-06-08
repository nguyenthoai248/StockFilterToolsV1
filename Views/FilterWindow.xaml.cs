using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace StockFilterToolsV1.Views
{
    /// <summary>
    /// Interaction logic for FilterWindow.xaml
    /// </summary>
    public partial class FilterWindow : Window
    {
        public FilterWindow()
        {
            InitializeComponent();
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            //Condition = new FilterCondition
            //{
            //    DoanhSoThuan = decimal.TryParse(txtDoanhSoThuan.Text, out var d1) ? d1 : (decimal?)null,
            //    EPS = decimal.TryParse(txtEPS.Text, out var d2) ? d2 : (decimal?)null,
            //    LoiNhuanSauThue = decimal.TryParse(txtLoiNhuan.Text, out var d3) ? d3 : (decimal?)null,
            //    ApplyDK4 = chkDK4.IsChecked == true
            //};

            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
