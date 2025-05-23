namespace StockFilterToolsV1.Utils
{
    public static class Config
    {
        public static string ListOrganizationUrl = "https://fiin-core.ssi.com.vn/Master/GetListOrganization?language=vi";
        public static string BalanceSheetUrl = "https://fiin-fundamental.ssi.com.vn/FinancialStatement/GetBalanceSheet?Frequency=quarterly&numberOfPeriod=12&OrganCode={0}";
        public static string IncomeStatementUrl = "https://fiin-fundamental.ssi.com.vn/FinancialStatement/GetIncomeStatement?Frequency=quarterly&numberOfPeriod=12&OrganCode={0}";
    }
}
