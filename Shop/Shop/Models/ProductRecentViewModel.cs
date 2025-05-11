using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Shop.Models
{
    public class ProductRecentViewModel
    {
        public string MaHangHoa { get; set; }
        public string TenHangHoa { get; set; }
        public string HinhAnh { get; set; }
        public string MoTa { get; set; }
   
        public string MaBienThe { get; set; }
        public double GiaGoc { get; set; }
        public double GiaKhuyenMai { get; set; }
        public int SoLuongTonKho { get; set; }
    }

}
