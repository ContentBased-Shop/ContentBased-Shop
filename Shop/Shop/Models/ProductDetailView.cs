using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Shop.Models
{
    public class ProductDetailView
    {
        // Thông tin cơ bản từ HangHoa
        public string MaHangHoa { get; set; }
        public string TenHangHoa { get; set; }
        public string MaDanhMuc { get; set; }
        public string TenDanhMuc  { get; set; }
        public string MaThuongHieu { get; set; }
        public string TenThuongHieu { get; set; }

        public string HinhAnh { get; set; }
        public string MoTa { get; set; }
        public string MoTaDai { get; set; }
        public DateTime? NgayTao { get; set; }


        // Dữ liệu từ bảng BienTheHangHoa
        public List<BienTheHangHoa> BienThes { get; set; }

        // Dữ liệu từ bảng DanhGia
        public int SoLuongDanhGia { get; set; }
        public double DanhGiaTrungBinh { get; set; }

        // Thêm thông tin từ bảng MoTaChiTietHangHoa
        public string TieuDe { get; set; }
        public string NoiDung { get; set; }
        public DateTime? NgayCapNhat { get; set; }

    }

}