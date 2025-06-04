using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StockFilterToolsV1.Models;
using StockFilterToolsV1.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace StockFilterToolsV1.ViewModels
{
    partial class MainViewModel : ObservableObject
    {
        private readonly SyncService _syncService;
        private readonly DatabaseService _databaseService;
        [ObservableProperty] private ObservableCollection<DataGridColumn> industryColumns = new();
        [ObservableProperty] private ObservableCollection<DataRow> rows = new();
        [ObservableProperty] private string companyName = string.Empty;
        [ObservableProperty] private string comStockCode = string.Empty;
        [ObservableProperty] private string stockCode = string.Empty;
        [ObservableProperty] private bool isGridVisible;
        [ObservableProperty] private bool isLoading;

        public ICommand FetchCommand { get; }
        public ICommand SyncDataCommand { get; }

        public MainViewModel()
        {
            FetchCommand = new AsyncRelayCommand<string>(async param =>
            {
                IsLoading = true;
                IsGridVisible = false;
                IndustryColumns.Clear();
                string symbol = param;
                if (!string.IsNullOrWhiteSpace(symbol))
                    await LoadDataAsync(symbol);
            },
            param => !string.IsNullOrWhiteSpace(param));
            _syncService = new SyncService();
            _databaseService = new DatabaseService();
            SyncDataCommand = new RelayCommand(SyncData);
        }

        private async void SyncData(object obj)
        {
            await _syncService.SyncAllData();
        }
        private async Task LoadDataAsync(string symbol)
        {
            try
            {
                var orgInfoTask = _databaseService.GetOrganizationTable();
                var incomeStatementTask = _databaseService.GetIncomeStatementsBySymbol(symbol);
                var balanceSheetTask = _databaseService.GetBalanceSheetBySymbol(symbol);

                // Chờ cả hai hoàn tất
                await Task.WhenAll(orgInfoTask, incomeStatementTask, balanceSheetTask);

                // Truy xuất kết quả
                var orgInfo = await orgInfoTask;
                var incomeStatementResponse = await incomeStatementTask;
                var balanceSheetResponse = await balanceSheetTask;

                string orgInfoJson = "";
                string incomeStatementJson = "";
                string balanceSheetJson = "";

                // Lấy kết quả
                if (orgInfo.Rows.Count > 0 && orgInfo.Rows[0]["ResponseJson"] != DBNull.Value)
                {
                    orgInfoJson = orgInfo.Rows[0]["ResponseJson"]?.ToString() ?? string.Empty;
                    var parsedList = ParseList(orgInfoJson);
                    if (parsedList != null)
                    {
                        CompanyName = parsedList.Find(o => o.Symbol == symbol)?.Name ?? string.Empty;
                    }
                    else
                    {
                        CompanyName = string.Empty;
                    }

                    ComStockCode = symbol;
                }

                if (incomeStatementResponse.Rows.Count > 0 && incomeStatementResponse.Rows[0]["ResponseJson"] != DBNull.Value)
                {
                    incomeStatementJson = incomeStatementResponse.Rows[0]["ResponseJson"]?.ToString() ?? string.Empty;
                    balanceSheetJson = balanceSheetResponse.Rows[0]["ResponseJson"]?.ToString() ?? string.Empty;
                }

                // Parse JSON
                var incomeStatementObj = JsonConvert.DeserializeObject<Models.IncomeStatementModel>(incomeStatementJson);
                var balanceSheetObj = JsonConvert.DeserializeObject<Models.BalanceSheetModel>(balanceSheetJson);

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

                var emptyRow = new DataRow
                {
                    IndicatorName = "",
                    QuarterlyValues = new List<string>()
                };
                for (int i = 0; i < 12; i++)
                {
                    emptyRow.QuarterlyValues.Add("");
                }

                Rows.Add(emptyRow);

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
            finally
            {
                IsLoading = false;
            }
        }

        private List<Organization> ParseList(string json)
        {
            var result = new List<Organization>();
            var listOrgResponse = JObject.Parse(json);
            var listOrg = listOrgResponse["items"];
            if (listOrg != null)
            {
                foreach (var org in listOrg)
                {
                    var temOrg = new Organization();
                    var orgTicker = org["ticker"];
                    var orgCode = org["organCode"];
                    var orgName = org["organName"];
                    var comTypeCode = org["comTypeCode"];
                    if (orgTicker != null)
                    {
                        temOrg.Symbol = orgTicker.ToString();
                    }

                    if (orgCode != null)
                    {
                        temOrg.OrganCode = orgCode.ToString();
                    }

                    if (orgName != null)
                    {
                        temOrg.Name = orgName.ToString();
                    }

                    if (comTypeCode != null)
                    {
                        temOrg.ComTypeCode = comTypeCode.ToString();
                    }

                    result.Add(temOrg);
                }
            }
            return result;
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
    }
}
