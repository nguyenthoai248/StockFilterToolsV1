using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StockFilterToolsV1.Models;
using StockFilterToolsV1.Services;
using StockFilterToolsV1.Views;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using DataRow = StockFilterToolsV1.Models.DataRow;

namespace StockFilterToolsV1.ViewModels
{
    partial class MainViewModel : ObservableObject
    {
        private readonly SyncService _syncService;
        private readonly DatabaseService _databaseService;
        private readonly StockFilterService _stockFilterService;
        [ObservableProperty] private ObservableCollection<DataGridColumn> stockDataColumns = new();
        [ObservableProperty] private ObservableCollection<DataRow> stockDataRows = new();
        [ObservableProperty] private string companyName = string.Empty;
        [ObservableProperty] private string comStockCode = string.Empty;
        [ObservableProperty] private string stockCode = string.Empty;
        [ObservableProperty] private bool isGridVisible;
        [ObservableProperty] private bool isLoading;

        //Style
        private Style rowHeaderCellStyle = new Style(typeof(TextBlock));
        private Style dataCellStyle = new Style(typeof(TextBlock));

        private string noData = "#";
        public ICommand FetchCommand { get; }
        public ICommand SyncDataCommand { get; }
        public ICommand FilterDataCommand { get; }

        public MainViewModel()
        {
            FetchCommand = new AsyncRelayCommand<string>(async param =>
            {
                IsLoading = true;
                IsGridVisible = false;
                StockDataColumns.Clear();
                string symbol = param;
                if (!string.IsNullOrWhiteSpace(symbol))
                    await LoadDataAsync(symbol);
            },
            param => !string.IsNullOrWhiteSpace(param));
            _syncService = new SyncService();
            _databaseService = new DatabaseService();
            _stockFilterService = new StockFilterService();
            SyncDataCommand = new RelayCommand(SyncData);
            FilterDataCommand = new RelayCommand(BtnFilter_Click);

            // Mặc định lấy dữ liệu cho mã cổ phiếu MWG
            FetchCommand.Execute("MWG");

            //Style
            rowHeaderCellStyle.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));
            rowHeaderCellStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.Black));
            rowHeaderCellStyle.Setters.Add(new Setter(TextBlock.FontSizeProperty, 18.0));
            rowHeaderCellStyle.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(0, 0, 10, 0)));

            dataCellStyle.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));
            dataCellStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.Black));
            dataCellStyle.Setters.Add(new Setter(TextBlock.FontSizeProperty, 16.0));
            dataCellStyle.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(0, 0, 10, 0)));
        }

        private async void SyncData(object obj)
        {
            MessageBox.Show("Đang cập nhật dữ liệu mới. Vui lòng không tắt phần mềm cho đến khi nhận được thông báo cập nhật xong!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            await _syncService.SyncAllData();
            MessageBox.Show("Đã cập nhật xong dữ liệu mới nhất!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void BtnFilter_Click(object obj)
        {
            var filterWindow = new FilterWindow();
            if (filterWindow.ShowDialog() == true)
            {
                var condition = filterWindow.mFilterCondition;
                var selectedFilter = condition.SelectedFilter;
                var result = new List<string>();
                if (selectedFilter == 1 || selectedFilter == 2) 
                {
                    result = await _stockFilterService.Filter1And2Async(selectedFilter, condition.DoanhSoThuan, condition.EPS, condition.LoiNhuanSauThue, condition.ApplyDK4);
                } else if (selectedFilter == 3) 
                {
                    result = await _stockFilterService.Filter3Async(condition.TocDoChiemDungVon, condition.TocDoTrienKhaiDA);
                }

                Debug.WriteLine("Ket qua loc: ");
                for (int i = 0; i < result.Count; i++) 
                {
                    Debug.Write(result[i] + ", ");
                }
            }
        }

        private async Task LoadDataAsync(string symbol)
        {
            try
            {
                var orgInfoTask = _databaseService.GetOrganizationTable();
                var incomeStatementTask = _databaseService.GetIncomeStatementsBySymbol(symbol.ToUpper());
                var balanceSheetTask = _databaseService.GetBalanceSheetBySymbol(symbol.ToUpper());

                // Chờ cả hai hoàn tất

                // Truy xuất kết quả
                var orgInfo = await orgInfoTask;

                string orgInfoJson = "";
                string incomeStatementJson = "";
                string balanceSheetJson = "";
                var CompanyTypeCode = string.Empty;
                var IcbCode = string.Empty;

                // Lấy kết quả
                if (orgInfo.Rows.Count > 0 && orgInfo.Rows[0]["ResponseJson"] != DBNull.Value)
                {
                    orgInfoJson = orgInfo.Rows[0]["ResponseJson"]?.ToString() ?? string.Empty;
                    var parsedList = ParseList(orgInfoJson);
                    if (parsedList != null)
                    {
                        CompanyName = parsedList.Find(o => o.Symbol == symbol.ToUpper())?.Name ?? string.Empty;
                        CompanyTypeCode = parsedList.Find(o => o.Symbol == symbol.ToUpper())?.ComTypeCode ?? string.Empty;
                        IcbCode = parsedList.Find(o => o.Symbol == symbol.ToUpper())?.IcbCode ?? string.Empty;
                    }
                    else
                    {
                        CompanyName = string.Empty;
                        CompanyTypeCode = string.Empty;
                        IcbCode = string.Empty;
                        return;
                    }

                    ComStockCode = symbol.ToUpper();
                    if (!IsStockCodeExist(symbol.ToUpper(), parsedList))
                    {
                        MessageBox.Show("Không tìm thấy thông tin mã cổ phiếu trong cơ sở dữ liệu.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        IsLoading = false;
                        return;
                    }
                } 
                else
                {
                    MessageBox.Show("Không tìm thấy thông tin mã cổ phiếu trong cơ sở dữ liệu.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    IsLoading = false;
                    return;
                }

                var incomeStatementResponse = await incomeStatementTask;
                var balanceSheetResponse = await balanceSheetTask;

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
                if (incomeStatementObj.items.Count <= 0 || balanceSheetObj.items.Count <= 0) return;
                quarterDataSize = incomeStatementObj.items[0].quarterly.Count < balanceSheetObj.items[0].quarterly.Count 
                    ? incomeStatementObj.items[0].quarterly.Count : balanceSheetObj.items[0].quarterly.Count;

                if (quarterDataSize > 12)
                    quarterDataSize = 12;
                if (quarterDataSize <= 0)
                    return;


                for (int i = 0; i < quarterDataSize; i++)
                {
                    quarters.Add("Q" + incomeStatementObj.items[0].quarterly[i].quarterReport + " " + incomeStatementObj.items[0].quarterly[i].yearReport);
                }

                ShowDataGrid(quarterDataSize, quarters, incomeStatementObj, balanceSheetObj);
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
                    var icbCode = org["icbCode"];
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

                    if (icbCode != null)
                    {
                        temOrg.IcbCode = icbCode.ToString();
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
        private void ShowDataGrid(int quarterDataSize, List<string> quarters, IncomeStatementModel incomeStatementObj, BalanceSheetModel? balanceSheetObj)
        {
            // Tạo cột đầu tiên
            StockDataColumns.Add(new DataGridTextColumn
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
                                        "Tài sản dở dang dài hạn",
                                        "NIM(%)",
                                        "Lợi nhuận sau thuế",
                                        "Thu nhập lãi thuần",
                                        "Lãi thuần từ hoạt động dịch vụ",
                                        "Thu nhập khác, ròng",
                                        "Tỷ lệ CASA",
                                        "Tài sản ngắn hạn",
                                        "Người mua trả tiền trước ngắn hạn",
                                        "Doanh thu chưa thực hiện ngắn hạn",
                                        "Người mua trả tiền trước dài hạn",
                                        "Doanh thu chưa thực hiện dài hạn"
                                    };
            StockDataRows.Clear();

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
                        case 0: // Doanh số thuần
                            dataRow.QuarterlyValues.Add(quarterIncomeStatementData.isa3?.ToString("#,##0") ?? noData);
                            break;
                        case 1: // Lãi gộp
                            dataRow.QuarterlyValues.Add(quarterIncomeStatementData.isa5?.ToString("#,##0") ?? noData);
                            break;
                        case 2: // Lãi/(lỗ) thuần sau thuế
                            dataRow.QuarterlyValues.Add(quarterIncomeStatementData.isa20?.ToString("#,##0") ?? noData);
                            break;
                        case 3: // Lãi cơ bản trên cổ phiếu(EPS)
                            dataRow.QuarterlyValues.Add(quarterIncomeStatementData.isa23?.ToString("#,##0") ?? noData);
                            break;
                        case 4: // Hàng tồn kho, ròng
                            dataRow.QuarterlyValues.Add(quarterBalanceSheetData.bsa15?.ToString("#,##0") ?? noData);
                            break;
                        case 5: // Các khoản phải thu
                            dataRow.QuarterlyValues.Add(quarterBalanceSheetData.bsa8?.ToString("#,##0") ?? noData);
                            break;
                        case 6: // Tài sản dở dang dài hạn
                            dataRow.QuarterlyValues.Add(quarterBalanceSheetData.bsa163?.ToString("#,##0") ?? noData);
                            break;
                        case 7: // NIM(%)
                            dataRow.QuarterlyValues.Add(noData);
                            break;
                        case 8: // Lợi nhuận sau thuế
                            dataRow.QuarterlyValues.Add(quarterIncomeStatementData.isa20?.ToString("#,##0") ?? noData);
                            break;
                        case 9: // Thu nhập lãi thuần
                            dataRow.QuarterlyValues.Add(quarterIncomeStatementData.isb27?.ToString("#,##0") ?? noData);
                            break;
                        case 10: // Lãi thuần từ hoạt động dịch vụ
                            dataRow.QuarterlyValues.Add(quarterIncomeStatementData.isb30?.ToString("#,##0") ?? noData);
                            break;
                        case 11: // Thu nhập khác, ròng
                            dataRow.QuarterlyValues.Add(quarterIncomeStatementData.isa14?.ToString("#,##0") ?? noData);
                            break;
                        case 12: // Tỷ lệ CASA
                            dataRow.QuarterlyValues.Add(noData);
                            break;
                        case 13: // Tài sản ngắn hạn
                            dataRow.QuarterlyValues.Add(quarterBalanceSheetData.bsa1?.ToString("#,##0") ?? noData);
                            break;
                        case 14: // Người mua trả tiền trước ngắn hạn
                            dataRow.QuarterlyValues.Add(quarterBalanceSheetData.bsa58?.ToString("#,##0") ?? noData);
                            break;
                        case 15: // Doanh thu chưa thực hiện ngắn hạn
                            dataRow.QuarterlyValues.Add(quarterBalanceSheetData.bsa63?.ToString("#,##0") ?? noData);
                            break;
                        case 16: // Người mua trả tiền trước dài hạn
                            dataRow.QuarterlyValues.Add(quarterBalanceSheetData.bsa69?.ToString("#,##0") ?? noData);
                            break;
                        case 17: // Doanh thu chưa thực hiện dài hạn
                            dataRow.QuarterlyValues.Add(quarterBalanceSheetData.bsa73?.ToString("#,##0") ?? noData);
                            break;
                    }
                }

                StockDataRows.Add(dataRow);
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

            StockDataRows.Add(emptyRow);

            var ManuFactEvaluateCriteria = new[]
            {
                            "Tốc độ tăng trưởng Doanh số thuần",
                            "Tốc độ tăng trưởng Lãi( lỗ) thuần sau  thuế",
                            "Tốc độ tăng trưởng EPS",
                            "Tốc độ tăng trưởng Hàng tồn kho",
                            "Tốc độ tăng khoản phải thu",
                            "Tỷ suất LNST/ Doanh thu thuần",
                            "Biên lãi gộp",
                            "Tiến độ dự án",
                            "Thu trước tiền của khách hàng",
                            "Tốc độ chiếm dụng vốn của khách hàng",
                            "Tốc độ triển khai dự án",
                            "Tốc độ tăng trưởng tài sản dở dang dài hạn",
                            "Tỷ trọng khoản phải thu",
                            "Tốc độ tăng trưởng LNST",
                            "Thu nhập ngoài lãi",
                            "Tốc độ tăng thu nhập lãi thuần",
                            "Tốc độ tăng thu nhập ngoài lãi"
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
                        case 7:
                            dataRow.QuarterlyValues.Add(CalculateProjectProgress(balanceSheetObj, j));
                            break;
                        case 8:
                            dataRow.QuarterlyValues.Add(CalculateAdvanceReceipts(balanceSheetObj, j));
                            break;
                        case 9:
                            dataRow.QuarterlyValues.Add(CalculateCapitalOccupationRate(balanceSheetObj, j));
                            break;
                        case 10:
                            dataRow.QuarterlyValues.Add(CalculateProjectImplementSpeed(balanceSheetObj, j));
                            break;
                        case 11:
                            dataRow.QuarterlyValues.Add(CalculateLongTermCipGrowth(balanceSheetObj, j));
                            break;
                        case 12:
                            dataRow.QuarterlyValues.Add(CalculateReceivablesRatio(balanceSheetObj, j));
                            break;
                        case 13:
                            dataRow.QuarterlyValues.Add(CalculateNetProfitGrowthRate(incomeStatementObj, j));
                            break;
                        case 14:
                            dataRow.QuarterlyValues.Add(CalculateNonInterestIncome(incomeStatementObj, j));
                            break;
                        case 15:
                            dataRow.QuarterlyValues.Add(CalculateNetInterestIncomeGrowth(incomeStatementObj, j));
                            break;
                        case 16:
                            dataRow.QuarterlyValues.Add(CalculateNonInterestIncomeGrowth(incomeStatementObj, j));
                            break;
                    }
                }
                StockDataRows.Add(dataRow);
            }

            for (int i = 0; i < quarterDataSize; i++)
            {
                StockDataColumns.Add(new DataGridTextColumn
                {
                    Header = quarters[i],
                    Binding = new Binding($"QuarterlyValues[{i}]"),
                    Width = DataGridLength.Auto,
                    ElementStyle = dataCellStyle,
                });
            }

            IsGridVisible = StockDataColumns.Any();
        }

        private string GetNetRevenueGrowthRate(Models.IncomeStatementModel incomeStatementObj, int j)
        {
            var quarterlySize = incomeStatementObj.items[0].quarterly.Count;
            if (incomeStatementObj.items.Count <= 0 || j + 4 >= quarterlySize) return "#";
            var thisQData = incomeStatementObj.items[0].quarterly[j];
            var thisQLastYearData = incomeStatementObj.items[0].quarterly[j + 4];
            var result = (thisQData.isa3 - thisQLastYearData.isa3) / thisQLastYearData.isa3;
            var formattedResult = (result * 100)?.ToString("F2") + "%";
            return formattedResult;
        }

        private string GetNetProfitGrowthRate(Models.IncomeStatementModel incomeStatementObj, int j)
        {
            var quarterlySize = incomeStatementObj.items[0].quarterly.Count;
            if (incomeStatementObj.items.Count <= 0 || j + 4 >= quarterlySize) return "#";
            var thisQData = incomeStatementObj.items[0].quarterly[j];
            var thisQLastYearData = incomeStatementObj.items[0].quarterly[j + 4];
            var result = (thisQData.isa20 - thisQLastYearData.isa20) / thisQLastYearData.isa20;
            var formattedResult = (result * 100)?.ToString("F2") + "%";
            return formattedResult;
        }

        private string GetEpsGrowthRate(Models.IncomeStatementModel incomeStatementObj, int j)
        {
            var quarterlySize = incomeStatementObj.items[0].quarterly.Count;
            if (incomeStatementObj.items.Count <= 0 || j + 4 >= quarterlySize) return "#";
            var thisQData = incomeStatementObj.items[0].quarterly[j];
            var thisQLastYearData = incomeStatementObj.items[0].quarterly[j + 4];
            var result = (thisQData.isa23 - thisQLastYearData.isa23) / thisQLastYearData.isa23;
            var formattedResult = (result * 100)?.ToString("F2") + "%";
            return formattedResult;
        }

        private string GetInventoryGrowthRate(Models.BalanceSheetModel balanceSheetObj, int j)
        {
            var quarterlySize = balanceSheetObj.items[0].quarterly.Count;
            if (balanceSheetObj.items.Count <= 0 || j + 1 >= quarterlySize) return "#";
            var thisQData = balanceSheetObj.items[0].quarterly[j];
            var prevQData = balanceSheetObj.items[0].quarterly[j + 1];
            var result = (thisQData.bsa15 - prevQData.bsa15) / prevQData.bsa15;
            var formattedResult = (result * 100)?.ToString("F2") + "%";
            return formattedResult;
        }

        private string GetAccReceivableGrowthRate(Models.BalanceSheetModel balanceSheetObj, int j)
        {
            var quarterlySize = balanceSheetObj.items[0].quarterly.Count;
            if (balanceSheetObj.items.Count <= 0 || j + 1 >= quarterlySize) return "#";
            var thisQData = balanceSheetObj.items[0].quarterly[j];
            var prevQData = balanceSheetObj.items[0].quarterly[j + 1];
            var result = (thisQData.bsa8 - prevQData.bsa8) / prevQData.bsa8;
            var formattedResult = (result * 100)?.ToString("F2") + "%";
            return formattedResult;
        }

        private string GetNetProfitMargin(Models.IncomeStatementModel incomeStatementObj, int j)
        {
            var quarterlySize = incomeStatementObj.items[0].quarterly.Count;
            if (incomeStatementObj.items.Count <= 0) return "#";
            var thisQData = incomeStatementObj.items[0].quarterly[j];
            var result = thisQData.isa20 / thisQData.isa3;
            var formattedResult = (result * 100)?.ToString("F2") + "%";
            return formattedResult;
        }

        private string GetGrossProfitMargin(Models.IncomeStatementModel incomeStatementObj, int j)
        {
            var quarterlySize = incomeStatementObj.items[0].quarterly.Count;
            if (incomeStatementObj.items.Count <= 0) return "#";
            var thisQData = incomeStatementObj.items[0].quarterly[j];
            var result = thisQData.isa5 / thisQData.isa3;
            var formattedResult = (result * 100)?.ToString("F2") + "%";
            return formattedResult;
        }

        private string CalculateProjectProgress(Models.BalanceSheetModel balanceSheetObj, int j)
        {
            if (balanceSheetObj.items.Count <= 0) return "#";
            var thisQData = balanceSheetObj.items[0].quarterly[j];
            var result = thisQData.bsa15 + thisQData.bsa163;
            var formattedResult = (result * 100)?.ToString("F2") + "%";
            return formattedResult;
        }

        private string CalculateAdvanceReceipts(Models.BalanceSheetModel balanceSheetObj, int j)
        {
            if (balanceSheetObj.items.Count <= 0) return "#";
            var thisQData = balanceSheetObj.items[0].quarterly[j];
            var result = thisQData.bsa58 + thisQData.bsa63 + thisQData.bsa69 + thisQData.bsa73;
            var formattedResult = (result * 100)?.ToString("F2") + "%";
            return formattedResult;
        }

        private string CalculateCapitalOccupationRate(Models.BalanceSheetModel balanceSheetObj, int j)
        {
            var quarterlySize = balanceSheetObj.items[0].quarterly.Count;
            if (balanceSheetObj.items.Count <= 0 || j + 1 >= quarterlySize) return "#";
            var thisQData = balanceSheetObj.items[0].quarterly[j];
            var prevQData = balanceSheetObj.items[0].quarterly[j + 1];
            var thisQAdvanceReceipt = thisQData.bsa58 + thisQData.bsa63 + thisQData.bsa69 + thisQData.bsa73;
            var prevQAdvanceReceipt = prevQData.bsa58 + prevQData.bsa63 + prevQData.bsa69 + prevQData.bsa73;
            var result = (thisQAdvanceReceipt - prevQAdvanceReceipt) / prevQAdvanceReceipt;
            var formattedResult = (result * 100)?.ToString("F2") + "%";
            return formattedResult;
        }
        
        private string CalculateProjectImplementSpeed(Models.BalanceSheetModel balanceSheetObj, int j)
        {
            var quarterlySize = balanceSheetObj.items[0].quarterly.Count;
            if (balanceSheetObj.items.Count <= 0 || j + 1 >= quarterlySize) return "#";
            var thisQData = balanceSheetObj.items[0].quarterly[j];
            var prevQData = balanceSheetObj.items[0].quarterly[j + 1];
            var thisQProjectProgress = thisQData.bsa15 + thisQData.bsa163;
            var prevQProjectProgress = prevQData.bsa15 + prevQData.bsa163;
            var result = (thisQProjectProgress - prevQProjectProgress) / prevQProjectProgress;
            var formattedResult = (result * 100)?.ToString("F2") + "%";
            return formattedResult;
        }

        private string CalculateReceivablesRatio(Models.BalanceSheetModel balanceSheetObj, int j)
        {
            if (balanceSheetObj.items.Count <= 0) return "#";
            var thisQData = balanceSheetObj.items[0].quarterly[j];
            var result = thisQData.bsa8 / thisQData.bsa1;
            var formattedResult = (result * 100)?.ToString("F2") + "%";
            return formattedResult;
        }

        private string CalculateLongTermCipGrowth(Models.BalanceSheetModel balanceSheetObj, int j)
        {
            var quarterlySize = balanceSheetObj.items[0].quarterly.Count;
            if (balanceSheetObj.items.Count <= 0 || j + 1 >= quarterlySize) return "#";
            var thisQData = balanceSheetObj.items[0].quarterly[j];
            var prevQData = balanceSheetObj.items[0].quarterly[j + 1];
            var result = (thisQData.bsa163 - prevQData.bsa163) / prevQData.bsa163;
            var formattedResult = (result * 100)?.ToString("F2") + "%";
            return formattedResult;
        }
        
        private string CalculateNetProfitGrowthRate(Models.IncomeStatementModel incomeStatementObj, int j)
        {
            var quarterlySize = incomeStatementObj.items[0].quarterly.Count;
            if (incomeStatementObj.items.Count <= 0 || j + 4 >= quarterlySize) return "#";
            var thisQData = incomeStatementObj.items[0].quarterly[j];
            var thisQLastYearData = incomeStatementObj.items[0].quarterly[j + 4];
            var result = (thisQData.isa20 - thisQLastYearData.isa20) / thisQData.isa20;
            var formattedResult = (result * 100)?.ToString("F2") + "%";
            return formattedResult;
        }
        private string CalculateNonInterestIncome(Models.IncomeStatementModel incomeStatementObj, int j)
        {
            if (incomeStatementObj.items.Count <= 0) return "#";
            var thisQData = incomeStatementObj.items[0].quarterly[j];
            var result = thisQData.isb30 + thisQData.isa14;
            var formattedResult = (result * 100)?.ToString("F2") + "%";
            return formattedResult;
        }

        private string CalculateNetInterestIncomeGrowth(Models.IncomeStatementModel incomeStatementObj, int j)
        {
            var quarterlySize = incomeStatementObj.items[0].quarterly.Count;
            if (incomeStatementObj.items.Count <= 0 || j + 4 >= quarterlySize) return "#";
            var thisQData = incomeStatementObj.items[0].quarterly[j];
            var thisQLastYearData = incomeStatementObj.items[0].quarterly[j + 4];
            var result = (thisQData.isb27 - thisQLastYearData.isb27) / thisQLastYearData.isb27;
            var formattedResult = (result * 100)?.ToString("F2") + "%";
            return formattedResult;
        }

        private string CalculateNonInterestIncomeGrowth(Models.IncomeStatementModel incomeStatementObj, int j)
        {
            var quarterlySize = incomeStatementObj.items[0].quarterly.Count;
            if (incomeStatementObj.items.Count <= 0 || j + 4 >= quarterlySize) return "#";
            var thisQData = incomeStatementObj.items[0].quarterly[j];
            var thisQLastYearData = incomeStatementObj.items[0].quarterly[j + 4];
            var thisQNII = thisQData.isb30 + thisQData.isa14;
            var thisQLastYearNII = thisQLastYearData.isb30 + thisQLastYearData.isa14;
            var result = (thisQNII - thisQLastYearNII) / thisQLastYearNII;
            var formattedResult = (result * 100)?.ToString("F2") + "%";
            return formattedResult;
        }

        private bool IsStockCodeExist(string stockCode, List<Organization> listOrg)
        {
            var rows = listOrg.FirstOrDefault(o => o.Symbol == stockCode);
            return rows != null;
        }
    }
}
