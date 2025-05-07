using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Shop.Models
{
    public class DanhGiaView
    {
        public int SoSao { get; set; }
        public string BinhLuan { get; set; }
        public DateTime? NgayTao { get; set; }
        public string HoTenKhachHang { get; set; } // Thêm tên khách
    }

}