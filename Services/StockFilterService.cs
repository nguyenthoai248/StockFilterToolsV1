using Microsoft.Data.Sqlite;
using Newtonsoft.Json.Linq;
using StockFilterToolsV1.Models;
using System.IO;
using System.Text.Json;

namespace StockFilterToolsV1.Services
{
    class StockFilterService
    {
        public async Task<List<string>> Filter1And2Async(int selectedFilter, decimal? revenueThreshold, decimal? epsThreshold, decimal? profitThreshold, bool applyCond4)
        {
            var result = new List<string>();

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string solutionDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));

            string dbPath = Path.Combine(solutionDir, "Data", "financial_data.db");
            string connStr = $"Data Source={dbPath}";
            var allStockJsons = new Dictionary<string, string>();

            // Step 1: Load from DB
            var connection = new SqliteConnection(connStr);
            connection.Open();
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Symbol, ResponseJson FROM IncomeStatements";
            using var reader = cmd.ExecuteReader();
            while (await reader.ReadAsync())
            {
                string symbol = reader["Symbol"].ToString();
                string json = reader["ResponseJson"].ToString();

                if (!string.IsNullOrWhiteSpace(symbol) && !string.IsNullOrWhiteSpace(json))
                    allStockJsons[symbol] = json;
            }

            // Step 2: Parse + Filter
            var filteredSymbols = new List<string>();

            Parallel.ForEach(allStockJsons, kvp =>
            {
                try
                {
                    var IncomeStatementData = JsonSerializer.Deserialize<IncomeStatementModel>(kvp.Value);
                    var revenueGrowthRate = GetNetRevenueGrowthRate(IncomeStatementData);
                    var epsGrowthRate = GetEpsGrowthRate(IncomeStatementData);
                    var NetProfitGrowthRate = CalculateNetProfitGrowthRate(IncomeStatementData);
                    var isGrossProfitValid = IsGrossProfitMarginGrowthRateValid(IncomeStatementData); 

                    bool dk1 = revenueGrowthRate > revenueThreshold;
                    bool dk2 = epsGrowthRate > epsThreshold;
                    bool dk3 = NetProfitGrowthRate > profitThreshold;
                    bool dk4 = isGrossProfitValid;

                    if(selectedFilter == 1) 
                    {
                        if ((dk1 || dk2 || dk3) && dk4)
                        {
                            lock (filteredSymbols)
                                filteredSymbols.Add(kvp.Key);
                        }
                    } else if(selectedFilter == 2) 
                    {
                        if (dk1 && dk2 && dk3 && dk4)
                        {
                            lock (filteredSymbols)
                                filteredSymbols.Add(kvp.Key);
                        }
                    }
                }
                catch
                {
                    // Handle or ignore bad data
                }
            });

            return filteredSymbols;
        }

        public async Task<List<string>> Filter3Async(decimal? capitalOccupationRate, decimal? projectImplementSpeed)
        {
            var result = new List<string>();

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string solutionDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));

            string dbPath = Path.Combine(solutionDir, "Data", "financial_data.db");
            string connStr = $"Data Source={dbPath}";
            var allStockJsons = new Dictionary<string, string>();
            string listOrgJson = string.Empty;

            // Step 1: Load from DB
            var connection = new SqliteConnection(connStr);
            connection.Open();
            await using var cmd1 = connection.CreateCommand();
            cmd1.CommandText = "SELECT Symbol, ResponseJson FROM BalanceSheets";
            using var reader1 = cmd1.ExecuteReader();
            while (await reader1.ReadAsync())
            {
                string symbol = reader1["Symbol"].ToString();
                string json = reader1["ResponseJson"].ToString();

                if (!string.IsNullOrWhiteSpace(symbol) && !string.IsNullOrWhiteSpace(json))
                    allStockJsons[symbol] = json;
            }

            await using var cmd2 = connection.CreateCommand();
            cmd2.CommandText = "SELECT Symbol, ResponseJson FROM Organizations ORDER BY Symbol";
            using var reader2 = cmd2.ExecuteReader();
            while (await reader2.ReadAsync())
            {
                string symbol = reader2["Symbol"].ToString();
                string json = reader2["ResponseJson"].ToString();

                if (!string.IsNullOrWhiteSpace(symbol) && !string.IsNullOrWhiteSpace(json))
                    listOrgJson = json;
            }

            // Step 2: Parse + Filter
            var filteredSymbols = new List<string>();

            Parallel.ForEach(allStockJsons, kvp =>
            {
                try
                {
                    var BalanceSheets = JsonSerializer.Deserialize<BalanceSheetModel>(kvp.Value);
                    var listOrg = ParseList(listOrgJson);
                    var CapitalOccupationRate = CalculateCapitalOccupationRate(BalanceSheets);
                    var ProjectImplementSpeed = CalculateProjectImplementSpeed(BalanceSheets);

                    bool dk1 = CapitalOccupationRate > capitalOccupationRate;
                    bool dk2 = ProjectImplementSpeed > projectImplementSpeed;

                    var org = listOrg.Find(o => o.Symbol == kvp.Key);

                    if (dk1 || dk2 && org.IcbCode.StartsWith("86"))
                    {
                        lock (filteredSymbols)
                            filteredSymbols.Add(kvp.Key);
                    }
                }
                catch
                {
                    // Handle or ignore bad data
                }
            });

            return filteredSymbols;
        }

        private decimal GetNetRevenueGrowthRate(Models.IncomeStatementModel incomeStatementObj)
        {
            var thisQData = incomeStatementObj.items[0].quarterly[0];
            var thisQLastYearData = incomeStatementObj.items[0].quarterly[4];
            var result = (thisQData.isa3 - thisQLastYearData.isa3) / thisQLastYearData.isa3;
            var formattedResult = (result * 100) ?? 0;
            return (decimal)formattedResult;
        }

        private decimal GetEpsGrowthRate(Models.IncomeStatementModel incomeStatementObj)
        {
            var thisQData = incomeStatementObj.items[0].quarterly[0];
            var thisQLastYearData = incomeStatementObj.items[0].quarterly[4];
            var result = (thisQData.isa23 - thisQLastYearData.isa23) / thisQLastYearData.isa23;
            var formattedResult = (result * 100) ?? 0;
            return (decimal)formattedResult;
        }

        private decimal CalculateNetProfitGrowthRate(Models.IncomeStatementModel incomeStatementObj)
        {
            var thisQData = incomeStatementObj.items[0].quarterly[0];
            var thisQLastYearData = incomeStatementObj.items[0].quarterly[4];
            var result = (thisQData.isa20 - thisQLastYearData.isa20) / thisQData.isa20;
            var formattedResult = (result * 100) ?? 0;
            return (decimal)formattedResult;
        }
        private bool IsGrossProfitMarginGrowthRateValid(Models.IncomeStatementModel incomeStatementObj)
        {
            var thisQData = incomeStatementObj.items[0].quarterly[0];
            var thisQLastYearData = incomeStatementObj.items[0].quarterly[4];
            var thisQGrossProfit = thisQData.isa5 / thisQData.isa3;
            var thisQLastYearGrossProfit = thisQLastYearData.isa5 / thisQLastYearData.isa3;
            return thisQGrossProfit > thisQLastYearGrossProfit;
        }

        private decimal CalculateCapitalOccupationRate(Models.BalanceSheetModel balanceSheetObj)
        {
            var quarterlySize = balanceSheetObj.items[0].quarterly.Count;
            if (balanceSheetObj.items.Count <= 0 || 1 >= quarterlySize) return 0;
            var thisQData = balanceSheetObj.items[0].quarterly[0];
            var prevQData = balanceSheetObj.items[0].quarterly[1];
            var thisQAdvanceReceipt = thisQData.bsa58 + thisQData.bsa63 + thisQData.bsa69 + thisQData.bsa73;
            var prevQAdvanceReceipt = prevQData.bsa58 + prevQData.bsa63 + prevQData.bsa69 + prevQData.bsa73;
            var result = (thisQAdvanceReceipt - prevQAdvanceReceipt) / prevQAdvanceReceipt;
            var formattedResult = (result * 100) ?? 0;
            return (decimal)formattedResult;
        }

        private decimal CalculateProjectImplementSpeed(Models.BalanceSheetModel balanceSheetObj)
        {
            var quarterlySize = balanceSheetObj.items[0].quarterly.Count;
            if (balanceSheetObj.items.Count <= 0 || 1 >= quarterlySize) return 0;
            var thisQData = balanceSheetObj.items[0].quarterly[0];
            var prevQData = balanceSheetObj.items[0].quarterly[1];
            var thisQProjectProgress = thisQData.bsa15 + thisQData.bsa163;
            var prevQProjectProgress = prevQData.bsa15 + prevQData.bsa163;
            var result = (thisQProjectProgress - prevQProjectProgress) / prevQProjectProgress;
            var formattedResult = (result * 100) ?? 0;
            return (decimal)formattedResult;
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
    }
}
