using Microsoft.Data.Sqlite;
using StockFilterToolsV1.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace StockFilterToolsV1.Services
{
    class StockFilterService
    {
        public async Task<List<string>> LoadAndFilterStockDataAsync(decimal? revenueThreshold, decimal? epsThreshold, decimal? profitThreshold, bool applyCond4)
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

                    if ((dk1 || dk2 || dk3) && dk4)
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
    }
}
