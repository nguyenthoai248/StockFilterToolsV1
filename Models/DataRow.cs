
namespace StockFilterToolsV1.Models
{
    class DataRow
    {
        public string IndicatorName { get; set; }

        public List<string> QuarterlyValues { get; set; }

        public static implicit operator DataRow(System.Data.DataRow v)
        {
            throw new NotImplementedException();
        }
    }
}
