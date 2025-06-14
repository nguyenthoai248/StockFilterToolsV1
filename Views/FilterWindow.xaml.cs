using StockFilterToolsV1.Models;
using System.Windows;

namespace StockFilterToolsV1.Views
{
    /// <summary>
    /// Interaction logic for FilterWindow.xaml
    /// </summary>
    public partial class FilterWindow : Window
    {
        public FilterCondition mFilterCondition { get; set; }
        public FilterWindow()
        {
            InitializeComponent();
            rdoFilter1.IsChecked = true;
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            var condition = new FilterCondition();

            if (rdoFilter1.IsChecked == true || rdoFilter2.IsChecked == true)
            {
                condition.DoanhSoThuan = decimal.TryParse(txtDoanhSoThuan.Text, out var d1) ? d1 : (decimal?)null;
                condition.EPS = decimal.TryParse(txtEPS.Text, out var d2) ? d2 : (decimal?)null;
                condition.LoiNhuanSauThue = decimal.TryParse(txtLoiNhuan.Text, out var d3) ? d3 : (decimal?)null;
                condition.ApplyDK4 = chkDK4.IsChecked == true;
                condition.SelectedFilter = rdoFilter1.IsChecked == true ? 1 : 2;
            }
            else if (rdoFilter3.IsChecked == true)
            {
                condition.TocDoChiemDungVon = decimal.TryParse(txtChiemDungVon.Text, out var d4) ? d4 : (decimal?)null;
                condition.TocDoTrienKhaiDA = decimal.TryParse(txtTrienKhaiDuAn.Text, out var d5) ? d5 : (decimal?)null;
                condition.SelectedFilter = 3;
            }
            else if (rdoFilter4.IsChecked == true)
            {
                condition.TangTruongTSDangDaiHan = decimal.TryParse(txtTSDD.Text, out var d6) ? d6 : (decimal?)null;
                condition.TangTruongHTK = decimal.TryParse(txtTonKho.Text, out var d7) ? d7 : (decimal?)null;
                condition.SelectedFilter = 4;
            }

            mFilterCondition = condition;
            this.DialogResult = true;
            this.Close();
        }


        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Filter_Checked(object sender, RoutedEventArgs e)
        {
            // Ẩn tất cả panel
            panelFilter1.Visibility = Visibility.Collapsed;
            panelFilter2.Visibility = Visibility.Collapsed;
            panelFilter3.Visibility = Visibility.Collapsed;
            panelFilter4.Visibility = Visibility.Collapsed;
            panelFilter5.Visibility = Visibility.Collapsed;
            panelFilter6.Visibility = Visibility.Collapsed;
            panelFilter7.Visibility = Visibility.Collapsed;
            panelFilter8.Visibility = Visibility.Collapsed;

            // Hiển thị theo bộ lọc
            if (rdoFilter1.IsChecked == true || rdoFilter2.IsChecked == true)
            {
                panelFilter1.Visibility = Visibility.Visible;
                panelFilter2.Visibility = Visibility.Visible;
                panelFilter3.Visibility = Visibility.Visible;
                panelFilter4.Visibility = Visibility.Visible;
            }
            else if (rdoFilter3.IsChecked == true)
            {
                panelFilter5.Visibility = Visibility.Visible;
                panelFilter6.Visibility = Visibility.Visible;
            }
            else if (rdoFilter4.IsChecked == true)
            {
                panelFilter7.Visibility = Visibility.Visible;
                panelFilter8.Visibility = Visibility.Visible;
            }
        }
    }
}
