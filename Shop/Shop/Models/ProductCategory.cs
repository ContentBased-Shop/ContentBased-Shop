using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Shop.Models
{
    public class ProductCategory
    {
        // Danh sách biến thể
        public List<BienTheHangHoaViewModel> BienTheHangHoas { get; set; }
        public List<HangHoa> HangHoas { get; set; }
        public List<ThuongHieu> ThuongHieus { get; set; }
        public List<DanhGia> ListDanhGia { get; set; }
        public List<BienTheHangHoa> ListBienTheGoc { get; set; }
        public List<HangHoa> ListHangHoaGoc { get; set; }
    }
}