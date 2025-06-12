using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockFilterToolsV1.Models
{
    class FilterDataModel
    {
        public decimal RevenueGrowth { get; set; }         // DK1
        public decimal EPS { get; set; }                   // DK2
        public decimal ProfitAfterTax { get; set; }        // DK3
        public decimal GrossMargin { get; set; }           // Biên lãi gộp hiện tại
        public decimal GrossMarginSameQuarterLastYear { get; set; } // Biên lãi gộp cùng kỳ
    }
}
