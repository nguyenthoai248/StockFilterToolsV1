using System.Collections.Generic;
using System.Text.Json;

namespace StockFilterToolsV1.Utils
{
    public static class JsonHelper
    {
        public static List<string> ParseSymbols(string json)
        {
            var doc = JsonDocument.Parse(json);
            var list = new List<string>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.TryGetProperty("symbol", out var symbol))
                    list.Add(symbol.GetString());
            }
            return list;
        }
    }
}
