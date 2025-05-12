using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Shop.Models
{
    public class ProductDetailView
    {
        // Thông tin cơ bản từ HangHoa
        public string MaBienThe { get; set; }
        public string MaHangHoa { get; set; }
        public string TenHangHoa { get; set; }
        public string MaDanhMuc { get; set; }
        public string TenDanhMuc  { get; set; }
        public string MaThuongHieu { get; set; }
        public string TenThuongHieu { get; set; }
        public string MoTaThuongHieu { get; set; }
        public string HinhAnh { get; set; }
        public string MoTa { get; set; }
        public string MoTaDai { get; set; }
        public DateTime? NgayTao { get; set; }


        // Dữ liệu từ bảng BienTheHangHoa
        public List<BienTheHangHoa> BienThes { get; set; }

        // Dữ liệu từ bảng DanhGia
        public int SoLuongDanhGia { get; set; }
        public double DanhGiaTrungBinh { get; set; }
        public List<DanhGiaView> DanhGiasChiTiet { get; set; }
        public Dictionary<int, int> ThongKeSoSao { get; set; } = new Dictionary<int, int>();

        // Thêm thông tin từ bảng MoTaChiTietHangHoa

        public string MoTaChiTiet { get; set; }
        public DateTime? NgayCapNhat { get; set; }
        public DateTime? NgayTaoMoTaChiTiet { get; set; }
        
    }

}