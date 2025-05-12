using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Shop.Models
{
    public class BienTheHangHoaViewModel
    {
        public string MaBienThe { get; set; }
        public string MaHangHoa { get; set; }

        public string MauSac { get; set; }
        public string DungLuong { get; set; }
        public string CPU { get; set; }
        public string RAM { get; set; }
        public string KichThuocManHinh { get; set; }
        public string LoaiBoNho { get; set; }

        public double GiaBan { get; set; }
        public double GiaKhuyenMai { get; set; }
        public int SoLuongTonKho { get; set; }

        public List<string> UrlAnh { get; set; } = new List<string>();  // Danh sách ảnh theo biến thể
    }

}