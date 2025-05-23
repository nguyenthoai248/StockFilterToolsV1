using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StockFilterToolsV1.Models;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading;
using System.Windows;

namespace StockFilterToolsV1.Services
{
    public class SyncService
    {
        private readonly ApiService _apiService;
        private readonly DatabaseService _dbService;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(10); // giới hạn 10 request song song
        private readonly Random _random = new Random();

        public SyncService()
        {
            _apiService = new ApiService();
            _dbService = new DatabaseService();
        }


        public async Task SyncAllData()
        {
            var orgJson = await _apiService.GetListOrganizationsAsync();
            _dbService.SaveJson("Organizations", "ALL", orgJson);
            var listOrgSymbol = ParseList(orgJson);

            if (listOrgSymbol == null || listOrgSymbol?.Count == 0)
            {
                Debug.WriteLine("Danh sách mã cổ phiếu trống.");
                return;
            }

            List<Task> tasks = new();
            //var vvsData = _dbService.GetBalanceSheetBySymbol("VVS");
            //Debug.WriteLine("ThoaiNV" + vvsData.Rows[0][1].ToString());
            foreach (var org in listOrgSymbol)
            {
                if (_dbService.IsSymbolRecentlyUpdated(org.Symbol))
                {
                    Console.WriteLine($"Skip {org.Symbol} (recently updated)");
                    continue;
                }

                tasks.Add(ProcessOrganizationAsync(org));

                // Thêm delay nhỏ tránh burst mạnh gây block IP
                await Task.Delay(_random.Next(800, 2000));
            }

            await Task.WhenAll(tasks);

            Console.WriteLine("Đã hoàn tất đồng bộ.");
        }

        private List<Organization> ParseList(string json)
        {
            var result = new List<Organization>();
            var listOrgResponse = JObject.Parse(json);
            var listOrg = listOrgResponse["items"];
            if (listOrg != null) {
                foreach (var org in listOrg)
                {
                    var temOrg = new Organization();
                    var orgTicker = org["ticker"];
                    var orgCode = org["organCode"];
                    if (orgTicker != null) {
                        temOrg.Symbol = orgTicker.ToString();
                    }

                    if (orgCode != null)
                    {
                        temOrg.OrganCode = orgCode.ToString();
                    }

                    result.Add(temOrg);
                }
            }
            return result;
        }

        private async Task ProcessOrganizationAsync(Organization organization)
        {
            await _semaphore.WaitAsync();
            try
            {
                Debug.WriteLine($"Đang xử lý: {organization.Symbol}");

                var balanceTask = _apiService.GetBalanceSheetAsync(organization.OrganCode);
                var incomeTask = _apiService.GetIncomeStatementAsync(organization.OrganCode);

                await Task.WhenAll(balanceTask, incomeTask);

                var balanceJson = balanceTask.Result;
                var incomeJson = incomeTask.Result;
                _dbService.SaveJson("BalanceSheets", organization.Symbol, balanceJson);
                _dbService.SaveJson("IncomeStatements", organization.Symbol, incomeJson);

                Debug.WriteLine($"✔ {organization.Symbol} xong.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Lỗi với {organization.Symbol}: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
