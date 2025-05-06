using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Shop.Models
{
    public class HangHoaViewModel
    {
        public string TenHangHoa { get; set; }
        public string HinhAnh { get; set; }
        public string MoTa { get; set; }
        public DateTime NgayTao { get; set; }

        public double GiaGoc { get; set; }
        public double GiaKhuyenMai { get; set; }
        public int SoLuongTonKho { get; set; }

        public int SoLuongDanhGia { get; set; }
        public float DanhGiaTrungBinh { get; set; }
    }


}