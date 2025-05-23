using System;
using System.Linq;
using System.Web.Mvc;
using Shop.Models;

namespace Shop.Controllers
{
    public class DanhGiaController : Controller
    {
        SHOPDataContext data = new SHOPDataContext("Data Source=ACERNITRO5;Initial Catalog=CuaHang2;Persist Security Info=True;User ID=sa;Password=123;Encrypt=True;TrustServerCertificate=True");

        [HttpPost]
        public JsonResult LuuDanhGia(string maDonHang, string maBienThe, int soSao, string binhLuan)
        {
            if (Session["UserID"] == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập" });

            try
            {
                // Kiểm tra số sao hợp lệ
                if (soSao < 1 || soSao > 5)
                    return Json(new { success = false, message = "Số sao không hợp lệ" });

                string maKhachHang = Session["UserID"].ToString();

                // Lấy MaHangHoa từ MaBienThe
                var bienThe = data.BienTheHangHoas.FirstOrDefault(bth => bth.MaBienThe == maBienThe);
                if (bienThe == null)
                    return Json(new { success = false, message = "Không tìm thấy thông tin sản phẩm" });

                string maHangHoa = bienThe.MaHangHoa;

                // Kiểm tra xem khách hàng đã đánh giá sản phẩm này trong đơn hàng này chưa
                var danhGiaCu = data.DanhGias.FirstOrDefault(dg => 
                    dg.MaKhachHang == maKhachHang && 
                    dg.MaHangHoa == maHangHoa &&
                    dg.MaDonHang == maDonHang);

                if (danhGiaCu != null)
                {
                    // Cập nhật đánh giá cũ
                    danhGiaCu.SoSao = soSao;
                    danhGiaCu.BinhLuan = binhLuan;
                }
                else
                {
                    // Thêm đánh giá mới
                    var danhGiaMoi = new DanhGia
                    {
                        MaKhachHang = maKhachHang,
                        MaHangHoa = maHangHoa,
                        MaDonHang = maDonHang,
                        SoSao = soSao,
                        BinhLuan = binhLuan,
                        NgayTao = DateTime.Now
                    };
                    data.DanhGias.InsertOnSubmit(danhGiaMoi);
                }

                // Cập nhật điểm trung bình của sản phẩm
                var danhGiaTrungBinh = data.DanhGias
                    .Where(dg => dg.MaHangHoa == maHangHoa)
                    .Average(dg => dg.SoSao);

                var hangHoa = data.HangHoas.FirstOrDefault(hh => hh.MaHangHoa == maHangHoa);
                if (hangHoa != null)
                {
                    hangHoa.DanhGiaTrungBinh = (float)danhGiaTrungBinh;
                }

                // Cập nhật điểm Collaborative Filtering
                var collaborativeFiltering = data.CollaborativeFilterings
                    .FirstOrDefault(cf => cf.MaKhachHang == maKhachHang && cf.MaHangHoa == maHangHoa);

                if (collaborativeFiltering != null)
                {
                    // Cập nhật điểm cũ
                    collaborativeFiltering.DiemSo = (float)soSao;
                }
                else
                {
                    // Thêm điểm mới
                    var cfMoi = new CollaborativeFiltering
                    {
                        MaKhachHang = maKhachHang,
                        MaHangHoa = maHangHoa,
                        DiemSo = (float)soSao
                    };
                    data.CollaborativeFilterings.InsertOnSubmit(cfMoi);
                }

                data.SubmitChanges();

                return Json(new { success = true, message = "Cảm ơn bạn đã đánh giá sản phẩm!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpGet]
        public JsonResult KiemTraDanhGia(string maBienThe, string maDonHang)
        {
            if (Session["UserID"] == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập" }, JsonRequestBehavior.AllowGet);

            try
            {
                string maKhachHang = Session["UserID"].ToString();

                // Lấy MaHangHoa từ MaBienThe
                var bienThe = data.BienTheHangHoas.FirstOrDefault(bth => bth.MaBienThe == maBienThe);
                if (bienThe == null)
                    return Json(new { success = false, message = "Không tìm thấy thông tin sản phẩm" }, JsonRequestBehavior.AllowGet);

                string maHangHoa = bienThe.MaHangHoa;

                // Kiểm tra đánh giá trước đó cho đơn hàng cụ thể
                var danhGia = data.DanhGias.FirstOrDefault(dg => 
                    dg.MaKhachHang == maKhachHang && 
                    dg.MaHangHoa == maHangHoa &&
                    dg.MaDonHang == maDonHang);

                if (danhGia != null)
                {
                    return Json(new { 
                        success = true, 
                        daDanhGia = true,
                        soSao = danhGia.SoSao,
                        binhLuan = danhGia.BinhLuan,
                        ngayTao = danhGia.NgayTao
                    }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { 
                    success = true, 
                    daDanhGia = false 
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetSanPhamChuaDanhGia(string maDonHang)
        {
            if (Session["UserID"] == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập" }, JsonRequestBehavior.AllowGet);

            try
            {
                string maKhachHang = Session["UserID"].ToString();

                // Lấy danh sách sản phẩm trong đơn hàng
                var chiTietDonHang = data.ChiTietDonHangs
                    .Where(ct => ct.MaDonHang == maDonHang)
                    .Select(ct => new {
                        maBienThe = ct.MaBienThe,
                        tenHangHoa = ct.BienTheHangHoa.HangHoa.TenHangHoa,
                        maHangHoa = ct.BienTheHangHoa.MaHangHoa
                    })
                    .ToList();

                // Lấy danh sách sản phẩm đã đánh giá
                var daDanhGia = data.DanhGias
                    .Where(dg => dg.MaDonHang == maDonHang)
                    .Select(dg => dg.MaHangHoa)
                    .ToList();

                // Lọc ra các sản phẩm chưa đánh giá
                var chuaDanhGia = chiTietDonHang
                    .Where(ct => !daDanhGia.Contains(ct.maHangHoa))
                    .Select(ct => new {
                        maBienThe = ct.maBienThe,
                        tenHangHoa = ct.tenHangHoa
                    })
                    .ToList();

                return Json(new { 
                    success = true, 
                    data = chuaDanhGia 
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
} 