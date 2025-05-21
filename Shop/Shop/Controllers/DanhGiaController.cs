using System;
using System.Linq;
using System.Web.Mvc;
using Shop.Models;

namespace Shop.Controllers
{
    public class DanhGiaController : Controller
    {
        SHOPDataContext data = new SHOPDataContext("Data Source=MSI;Initial Catalog=CuaHang2;Persist Security Info=True;Use");

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

                // Kiểm tra xem khách hàng đã đánh giá sản phẩm này chưa
                var danhGiaCu = data.DanhGias.FirstOrDefault(dg => 
                    dg.MaKhachHang == maKhachHang && 
                    dg.MaHangHoa == maHangHoa);

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
        public JsonResult KiemTraDanhGia(string maBienThe)
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

                // Kiểm tra đánh giá trước đó
                var danhGia = data.DanhGias.FirstOrDefault(dg => 
                    dg.MaKhachHang == maKhachHang && 
                    dg.MaHangHoa == maHangHoa);

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
    }
} 