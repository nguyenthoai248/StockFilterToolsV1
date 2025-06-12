using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockFilterToolsV1.Models
{
    public class FilterCondition
    {
        public decimal? DoanhSoThuan { get; set; }
        public decimal? EPS { get; set; }
        public decimal? LoiNhuanSauThue { get; set; }
        public bool ApplyDK4 { get; set; }
    }
}
