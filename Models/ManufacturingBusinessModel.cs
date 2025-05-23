using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockFilterToolsV1.Models
{
    class ManufacturingBusinessModel
    {
        public string NetRevenue { get; set; }
        public string GrossProfit { get; set; }
        public string NetProfitAfterTax { get; set; }
        public string Eps { get; set; }
        public string NetInventory { get; set; }
        public string Receivables { get; set; }
        public string LongTermWorkInProgressAssets { get; set; }
    }
}
