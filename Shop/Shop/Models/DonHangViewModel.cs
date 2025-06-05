using System;
using System.Collections.Generic;

namespace Shop.Models
{
    public class DonHangViewModel
    {
        public string MaDonHang { get; set; }
        public string MaKhachHang { get; set; }
        public int? MaVoucher { get; set; }
        public double TongTien { get; set; }
        public string TrangThaiThanhToan { get; set; }
        public string TrangThaiDonHang { get; set; }
        public DateTime NgayTao { get; set; }
        public List<ChiTietDonHangViewModel> ChiTietDonHangs { get; set; }
        public GiaoHangViewModel GiaoHang { get; set; }
    }

    public class ChiTietDonHangViewModel
    {
        public string MaChiTietDonHang { get; set; }
        public string MaDonHang { get; set; }
        public string MaBienThe { get; set; }
        public int SoLuong { get; set; }
        public double DonGia { get; set; }
        public string TenHangHoa { get; set; }
        public string HinhAnh { get; set; }
        public string MauSac { get; set; }
        public string DungLuong { get; set; }
        public string CPU { get; set; }
        public string RAM { get; set; }
        public string KichThuocManHinh { get; set; }
        public string LoaiBoNho { get; set; }
    }

    public class GiaoHangViewModel
    {
        public string MaGiaoHang { get; set; }
        public string MaDonHang { get; set; }
        public string MaDiaChi { get; set; }
        public string MaVanDon { get; set; }
        public string DonViVanChuyen { get; set; }
        public string TrangThaiGiaoHang { get; set; }
        public string NgayGuiHang { get; set; }
        public string NgayNhanHang { get; set; }

        public string DiaChiDayDu { get; set; }
        public string TenNguoiNhan { get; set; }
        public string SoDienThoai { get; set; }
    }
} 