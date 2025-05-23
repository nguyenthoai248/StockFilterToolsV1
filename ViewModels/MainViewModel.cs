using StockFilterToolsV1.Models;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace StockFilterToolsV1.ViewModels
{
    class MainViewModel : INotifyPropertyChanged
    {

        private ObservableCollection<DataGridColumn> _columns;
        public ObservableCollection<DataGridColumn> Columns
        {
            get => _columns;
            set { _columns = value; OnPropertyChanged(nameof(Columns)); }
        }

        private ObservableCollection<DataRow> _rows;
        public ObservableCollection<DataRow> Rows
        {
            get => _rows;
            set { _rows = value; OnPropertyChanged(nameof(Rows)); }
        }

        private string _stockCode;

        public string StockCode
        {
            get => _stockCode;
            set
            {
                _stockCode = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private bool _isGridVisible;
        public bool IsGridVisible
        {
            get => _isGridVisible;
            set
            {
                if (_isGridVisible != value)
                {
                    _isGridVisible = value;
                    OnPropertyChanged(nameof(IsGridVisible));
                }
            }
        }

        // ViewModels/MainViewModel.cs
        public ObservableCollection<DataGridColumn> IndustryColumns { get; set; }

        public ICommand FetchCommand { get; }

        public MainViewModel()
        {
            // Initialize non-nullable fields
            _columns = new ObservableCollection<DataGridColumn>();
            _rows = new ObservableCollection<DataRow>();
            IndustryColumns = new ObservableCollection<DataGridColumn>();

            FetchCommand = new RelayCommand(async param =>
            {
                string symbol = param as string;
                if (!string.IsNullOrWhiteSpace(symbol))
                    await LoadDataAsync(symbol);
            },
        param => !string.IsNullOrWhiteSpace(param as string));
        }

        public ObservableCollection<ManufacturingBusinessModel> manufacturingBusinessModels { get; set; } = new ObservableCollection<ManufacturingBusinessModel>();

        private async Task LoadDataAsync(string symbol)
        {
            IndustryColumns.Clear();
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
                client.DefaultRequestHeaders.Add("origin", "https://iboard.ssi.com.vn");
                client.DefaultRequestHeaders.Add("referer", "https://iboard.ssi.com.vn/");
                client.DefaultRequestHeaders.Add("x-fiin-user-token", "YOUR_TOKEN_HERE"); // Replace with your token
                                                                                          // Gọi 2 API song song
                string incomeStatementUrl = $"https://fiin-fundamental.ssi.com.vn/FinancialStatement/GetIncomeStatement?language=vi&OrganCode={symbol}&Skip=0&Frequency=quarterly&numberOfPeriod=12&latestYear=2025";
                string balanceSheetUrl = $"https://fiin-fundamental.ssi.com.vn/FinancialStatement/GetBalanceSheet?language=vi&OrganCode={symbol}&Skip=0&Frequency=quarterly&numberOfPeriod=12&latestYear=2025";

                try
                {
                    var incomeStatementApi = client.GetStringAsync(incomeStatementUrl);
                    var balanceSheetApi = client.GetStringAsync(balanceSheetUrl);

                    // Chờ cả hai hoàn tất
                    await Task.WhenAll(incomeStatementApi, balanceSheetApi);

                    // Lấy kết quả
                    string incomeStatementResponse = await incomeStatementApi;
                    string balanceSheetResponse = await balanceSheetApi;

                    // Parse JSON
                    var incomeStatementObj = JsonConvert.DeserializeObject<Models.IncomeStatementModel>(incomeStatementResponse);
                    var balanceSheetObj = JsonConvert.DeserializeObject<Models.BalanceSheetModel>(balanceSheetResponse);

                    var quarters = new List<string>();

                    var quarterDataSize = 0;
                    quarterDataSize = incomeStatementObj.items[0].quarterly.Count;
                    if (quarterDataSize > 12)
                        quarterDataSize = 12;
                    if (quarterDataSize <= 0)
                        return;


                    for (int i = 0; i < quarterDataSize; i++)
                    {
                        quarters.Add("Q" + incomeStatementObj.items[0].quarterly[i].quarterReport + " " + incomeStatementObj.items[0].quarterly[i].yearReport);
                    }

                    // Tạo cột đầu tiên
                    var rowHeaderCellStyle = new Style(typeof(TextBlock));
                    rowHeaderCellStyle.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));
                    rowHeaderCellStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.Black));
                    rowHeaderCellStyle.Setters.Add(new Setter(TextBlock.FontSizeProperty, 18.0));
                    rowHeaderCellStyle.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(0, 0, 10, 0)));

                    IndustryColumns.Add(new DataGridTextColumn
                    {
                        Header = "Tiêu chí CSDL",
                        Binding = new Binding("IndicatorName"),
                        ElementStyle = rowHeaderCellStyle,
                        Width = DataGridLength.Auto
                    });

                    var ManuFacIndicator = new[] {
                                        "Doanh số thuần",
                                        "Lãi gộp",
                                        "Lãi/(lỗ) thuần sau thuế",
                                        "Lãi cơ bản trên cổ phiếu(EPS)",
                                        "Hàng tồn kho, ròng",
                                        "Các khoản phải thu",
                                        "Tài sản dở dang dài hạn"
                                    };
                    Rows.Clear();
                    
                    for (int i = 0; i < ManuFacIndicator.Length; i++)
                    {
                        var dataRow = new DataRow
                        {
                            IndicatorName = ManuFacIndicator[i],
                            QuarterlyValues = new List<string>()
                        };

                        // Tạo dòng dữ liệu
                        for (int j = 0; j < quarterDataSize; j++)
                        {
                            var quarterIncomeStatementData = incomeStatementObj.items[0].quarterly[j];
                            var quarterBalanceSheetData = balanceSheetObj.items[0].quarterly[j];

                            switch (i)
                            {
                                case 0:
                                    dataRow.QuarterlyValues.Add(quarterIncomeStatementData.isa3.ToString("#,##0"));
                                    break;
                                case 1:
                                    dataRow.QuarterlyValues.Add(quarterIncomeStatementData.isa5.ToString("#,##0"));
                                    break;
                                case 2:
                                    dataRow.QuarterlyValues.Add(quarterIncomeStatementData.isa20.ToString("#,##0"));
                                    break;
                                case 3:
                                    dataRow.QuarterlyValues.Add(quarterIncomeStatementData.isa23.ToString("#,##0"));
                                    break;
                                case 4:
                                    dataRow.QuarterlyValues.Add(quarterBalanceSheetData.bsa15.ToString("#,##0"));
                                    break;
                                case 5:
                                    dataRow.QuarterlyValues.Add(quarterBalanceSheetData.bsa8.ToString("#,##0"));
                                    break;
                                case 6:
                                    dataRow.QuarterlyValues.Add(quarterBalanceSheetData.bsa163.ToString("#,##0"));
                                    break;
                            }
                        }

                        Rows.Add(dataRow);
                    }

                    Rows.Add(new DataRow
                    {
                        IndicatorName = "",
                        QuarterlyValues = new List<string>() 
                    });

                    var ManuFactEvaluateCriteria = new[]
                    {
                            "Tốc độ tăng trưởng Doanh số thuần",
                            "Tốc độ tăng trưởng Lãi( lỗ) thuần sau  thuế",
                            "Tốc độ tăng trưởng EPS",
                            "Tốc độ tăng trưởng Hàng tồn kho",
                            "Tốc độ tăng khoản phải thu",
                            "Tỷ suất LNST/ Doanh thu thuần",
                            "Biên lãi gộp"
                        };

                    for (int i = 0; i < ManuFactEvaluateCriteria.Length; i++)
                    {
                        var dataRow = new DataRow
                        {
                            IndicatorName = ManuFactEvaluateCriteria[i],
                            QuarterlyValues = new List<string>()
                        };

                        for (int j = 0; j < quarterDataSize; j++)
                        {
                            var quarterIncomeStatementData = incomeStatementObj.items[0].quarterly[j];
                            var quarterBalanceSheetData = balanceSheetObj.items[0].quarterly[j];
                            switch (i)
                            {
                                case 0:
                                    dataRow.QuarterlyValues.Add(GetNetRevenueGrowthRate(incomeStatementObj, j));
                                    break;
                                case 1:
                                    dataRow.QuarterlyValues.Add(GetNetProfitGrowthRate(incomeStatementObj, j));
                                    break;
                                case 2:
                                    dataRow.QuarterlyValues.Add(GetEpsGrowthRate(incomeStatementObj, j));
                                    break;
                                case 3:
                                    dataRow.QuarterlyValues.Add(GetInventoryGrowthRate(balanceSheetObj, j));
                                    break;
                                case 4:
                                    dataRow.QuarterlyValues.Add(GetAccReceivableGrowthRate(balanceSheetObj, j));
                                    break;
                                case 5:
                                    dataRow.QuarterlyValues.Add(GetNetProfitMargin(incomeStatementObj, j));
                                    break;
                                case 6:
                                    dataRow.QuarterlyValues.Add(GetGrossProfitMargin(incomeStatementObj, j));
                                    break;
                            }
                        }
                        Rows.Add(dataRow);
                    }

                    //Format cell
                    var dataCellStyle = new Style(typeof(TextBlock));
                    dataCellStyle.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));
                    dataCellStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.Black));
                    dataCellStyle.Setters.Add(new Setter(TextBlock.FontSizeProperty, 16.0));
                    dataCellStyle.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(0, 0, 10, 0)));

                    for (int i = 0; i < quarterDataSize; i++)
                    {
                        IndustryColumns.Add(new DataGridTextColumn
                        {
                            Header = quarters[i],
                            Binding = new Binding($"QuarterlyValues[{i}]"),
                            Width = DataGridLength.Auto,
                            ElementStyle = dataCellStyle,
                        });
                    }

                    IsGridVisible = IndustryColumns.Any();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }

        private string GetNetRevenueGrowthRate(Models.IncomeStatementModel incomeStatementObj, int j)
        {
            var thisQData = incomeStatementObj.items[0].quarterly[j];
            var thisQLastYearData = incomeStatementObj.items[0].quarterly[j + 4];
            var result = (thisQData.isa3 - thisQLastYearData.isa3) / thisQLastYearData.isa3;
            var formattedResult = (result * 100).ToString("F2") + "%";
            return formattedResult;
        }

        private string GetNetProfitGrowthRate(Models.IncomeStatementModel incomeStatementObj, int j)
        {
            var thisQData = incomeStatementObj.items[0].quarterly[j];
            var thisQLastYearData = incomeStatementObj.items[0].quarterly[j + 4];
            var result = (thisQData.isa20 - thisQLastYearData.isa20) / thisQLastYearData.isa20;
            var formattedResult = (result * 100).ToString("F2") + "%";
            return formattedResult;
        }

        private string GetEpsGrowthRate(Models.IncomeStatementModel incomeStatementObj, int j)
        {
            var thisQData = incomeStatementObj.items[0].quarterly[j];
            var thisQLastYearData = incomeStatementObj.items[0].quarterly[j + 4];
            var result = (thisQData.isa23 - thisQLastYearData.isa23) / thisQLastYearData.isa23;
            var formattedResult = (result * 100).ToString("F2") + "%";
            return formattedResult;
        }

        private string GetInventoryGrowthRate(Models.BalanceSheetModel balanceSheetObj, int j)
        {
            var thisQData = balanceSheetObj.items[0].quarterly[j];
            var prevQData = balanceSheetObj.items[0].quarterly[j + 1];
            var result = (thisQData.bsa15 - prevQData.bsa15) / prevQData.bsa15;
            var formattedResult = (result * 100).ToString("F2") + "%";
            return formattedResult;
        }

        private string GetAccReceivableGrowthRate(Models.BalanceSheetModel balanceSheetObj, int j)
        {
            var thisQData = balanceSheetObj.items[0].quarterly[j];
            var prevQData = balanceSheetObj.items[0].quarterly[j + 1];
            var result = (thisQData.bsa8 - prevQData.bsa8) / prevQData.bsa8;
            var formattedResult = (result * 100).ToString("F2") + "%";
            return formattedResult;
        }

        private string GetNetProfitMargin(Models.IncomeStatementModel incomeStatementObj, int j)
        {
            var thisQData = incomeStatementObj.items[0].quarterly[j];
            var result = thisQData.isa20 / thisQData.isa3;
            var formattedResult = (result * 100).ToString("F2") + "%";
            return formattedResult;
        }

        private string GetGrossProfitMargin(Models.IncomeStatementModel incomeStatementObj, int j)
        {
            var thisQData = incomeStatementObj.items[0].quarterly[j];
            var result = thisQData.isa5 / thisQData.isa3;
            var formattedResult = (result * 100).ToString("F2") + "%";
            return formattedResult;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
