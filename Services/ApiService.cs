using System.Net.Http;

namespace StockFilterToolsV1.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
            _httpClient.DefaultRequestHeaders.Add("origin", "https://iboard.ssi.com.vn");
            _httpClient.DefaultRequestHeaders.Add("referer", "https://iboard.ssi.com.vn/");
            _httpClient.DefaultRequestHeaders.Add("x-fiin-user-token", "YOUR_TOKEN_HERE");
        }

        public async Task<string> GetListOrganizationsAsync()
        {
            return await _httpClient.GetStringAsync(Utils.Config.ListOrganizationUrl);
        }

        public async Task<string> GetBalanceSheetAsync(string symbol)
        {
            return await _httpClient.GetStringAsync(string.Format(Utils.Config.BalanceSheetUrl, symbol));
        }

        public async Task<string> GetIncomeStatementAsync(string symbol)
        {
            return await _httpClient.GetStringAsync(string.Format(Utils.Config.IncomeStatementUrl, symbol));
        }
    }
}
