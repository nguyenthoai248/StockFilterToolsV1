namespace StockFilterToolsV1.Models
{
    public class FilterCondition
    {
        // Bộ lọc 1 & 2
        public decimal? DoanhSoThuan { get; set; }         // DK1
        public decimal? EPS { get; set; }                  // DK2
        public decimal? LoiNhuanSauThue { get; set; }      // DK3
        public bool ApplyDK4 { get; set; }                 // DK4

        // Bộ lọc 3 (BĐS)
        public decimal? TocDoChiemDungVon { get; set; }    // DK1 (Bộ lọc 3)
        public decimal? TocDoTrienKhaiDA { get; set; }     // DK2 (Bộ lọc 3)

        // Bộ lọc 4
        public decimal? TangTruongTSDangDaiHan { get; set; }   // DK1 (Bộ lọc 4)
        public decimal? TangTruongHTK { get; set; }            // DK2 (Bộ lọc 4)

        // Tùy chọn: để biết đang dùng bộ lọc nào
        public int SelectedFilter { get; set; } // 1, 2, 3 hoặc 4
    }
}
