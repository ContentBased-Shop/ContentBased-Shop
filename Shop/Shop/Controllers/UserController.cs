using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Shop.Models;
using Shop.Helpers;
using System.Web.Providers.Entities;
namespace Shop.Controllers
{
    public class UserController : Controller
    {
        SHOPDataContext data = new SHOPDataContext("Data Source=MSI;Initial Catalog=CuaHang2;Persist Security Info=True;Use" +
                 "r ID=sa;Password=123;Encrypt=True;TrustServerCertificate=True");
        //
        // GET: /User/
        public ActionResult Index()
        {
            if (Session["UserID"] == null)
                return RedirectToAction("Login", "InnerPage");

            string maKhachHang = Session["UserID"].ToString();

            var khachHang = data.KhachHangs
                .FirstOrDefault(kh => kh.MaKhachHang == maKhachHang);

            if (khachHang == null)
                return RedirectToAction("Login", "InnerPage");

            var diaChis = data.DiaChiKhachHangs
                .Where(dc => dc.MaKhachHang == maKhachHang)
                .ToList();

            var viewModel = new UserViewModel
            {
                KhachHang = khachHang,
                ListDiaChiKhachHangs = diaChis
            };

            return View(viewModel);
        }
        // POST: CapNhatDiaChiMacDinh
        [HttpPost]
        public ActionResult CapNhatDiaChiMacDinh(string maDiaChi)
        {
            if (Session["UserID"] == null)
                return new HttpStatusCodeResult(401); // Unauthorized

            string maKhachHang = Session["UserID"].ToString();

            // Tìm tất cả địa chỉ của khách hàng
            var diaChis = data.DiaChiKhachHangs
                .Where(dc => dc.MaKhachHang == maKhachHang)
                .ToList();

            foreach (var dc in diaChis)
                dc.LaMacDinh = false;

            var diaChiMacDinh = diaChis.FirstOrDefault(dc => dc.MaDiaChi == maDiaChi);
            if (diaChiMacDinh != null)
            {
                diaChiMacDinh.LaMacDinh = true;
                data.SubmitChanges(); 
                return new HttpStatusCodeResult(200);
            }

            return new HttpStatusCodeResult(400); // Bad request nếu không tìm thấy
        }
        [HttpPost]
        public JsonResult LuuDiaChiMoi(DiaChiKhachHang diaChi)
        {
            // Kiểm tra nếu người dùng chưa đăng nhập
            if (Session["UserID"] == null)
            {
                return Json(new { success = false, message = "Bạn chưa đăng nhập." });
            }

            try
            {
                // Lấy ID khách hàng từ session
                string maKhachHang = Session["UserID"].ToString();
                diaChi.MaKhachHang = maKhachHang;
                diaChi.LaMacDinh = false;

                // Tạo mã địa chỉ ngẫu nhiên
                string maDiaChi;
                Random rand = new Random();
                do
                {
                    maDiaChi = "DC" + rand.Next(10000, 99999).ToString();
                } while (data.DiaChiKhachHangs.Any(d => d.MaDiaChi == maDiaChi)); // Kiểm tra mã đã tồn tại trong DB

                diaChi.MaDiaChi = maDiaChi;

                // Thêm địa chỉ vào cơ sở dữ liệu
                data.DiaChiKhachHangs.InsertOnSubmit(diaChi);
                data.SubmitChanges();

                // Trả về kết quả sau khi lưu thành công
                return Json(new
                {
                    success = true,
                    diaChi.MaDiaChi,
                    diaChi.TenNguoiNhan,
                    diaChi.SoDienThoai,
                    diaChi.DiaChiDayDu
                });
            }
            catch (Exception ex)
            {
                // Nếu có lỗi xảy ra, trả về thông báo lỗi
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
           
        [HttpDelete]
        public JsonResult XoaDiaChi(string id)
        {
            try
            {
                // Tìm địa chỉ theo mã địa chỉ
                var diaChi = data.DiaChiKhachHangs.FirstOrDefault(d => d.MaDiaChi == id);

                // Kiểm tra nếu địa chỉ tồn tại
                if (diaChi != null)
                {
                    // Xóa địa chỉ khỏi cơ sở dữ liệu
                    data.DiaChiKhachHangs.DeleteOnSubmit(diaChi);
                    data.SubmitChanges();
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "Địa chỉ không tồn tại." });
                }
            }
            catch (Exception ex)
            {
                // Nếu có lỗi xảy ra, trả về thông báo lỗi
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
        [HttpGet]
        public ActionResult Edit(string id)
        {
            var diaChi = data.DiaChiKhachHangs.FirstOrDefault(d => d.MaDiaChi == id);
            if (diaChi == null)
            {
                return HttpNotFound(); // hoặc RedirectToAction nếu muốn chuyển trang
            }

            return View(diaChi);
        }

        [HttpGet]
        public JsonResult GetDonHang()
        {
            if (Session["UserID"] == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập" }, JsonRequestBehavior.AllowGet);

            string maKhachHang = Session["UserID"].ToString();

            var donHangs = data.DonHangs
                .Where(dh => dh.MaKhachHang == maKhachHang)
                .OrderByDescending(dh => dh.NgayTao)
                .Select(dh => new DonHangViewModel
                {
                    MaDonHang = dh.MaDonHang,
                    MaKhachHang = dh.MaKhachHang,
                    MaVoucher = dh.MaVoucher,
                    TongTien = (double)dh.TongTien,
                    TrangThaiThanhToan = dh.TrangThaiThanhToan,
                    TrangThaiDonHang = dh.TrangThaiDonHang,
                    NgayTao = (DateTime)dh.NgayTao,
                    ChiTietDonHangs = data.ChiTietDonHangs
                        .Where(ct => ct.MaDonHang == dh.MaDonHang)
                        .Select(ct => new ChiTietDonHangViewModel
                        {
                            MaChiTietDonHang = ct.MaChiTietDonHang,
                            MaDonHang = ct.MaDonHang,
                            MaBienThe = ct.MaBienThe,
                            SoLuong = (int)ct.SoLuong,
                            DonGia = (double)ct.DonGia,
                            TenHangHoa = data.HangHoas
                                .Where(hh => hh.MaHangHoa == data.BienTheHangHoas
                                    .Where(bth => bth.MaBienThe == ct.MaBienThe)
                                    .Select(bth => bth.MaHangHoa)
                                    .FirstOrDefault())
                                .Select(hh => hh.TenHangHoa)
                                .FirstOrDefault(),
                            HinhAnh = data.HangHoas
                                .Where(hh => hh.MaHangHoa == data.BienTheHangHoas
                                    .Where(bth => bth.MaBienThe == ct.MaBienThe)
                                    .Select(bth => bth.MaHangHoa)
                                    .FirstOrDefault())
                                .Select(hh => hh.HinhAnh)
                                .FirstOrDefault(),
                            MauSac = data.BienTheHangHoas
                                .Where(bth => bth.MaBienThe == ct.MaBienThe)
                                .Select(bth => bth.MauSac)
                                .FirstOrDefault(),
                            DungLuong = data.BienTheHangHoas
                                .Where(bth => bth.MaBienThe == ct.MaBienThe)
                                .Select(bth => bth.DungLuong)
                                .FirstOrDefault(),
                            CPU = data.BienTheHangHoas
                                .Where(bth => bth.MaBienThe == ct.MaBienThe)
                                .Select(bth => bth.CPU)
                                .FirstOrDefault(),
                            RAM = data.BienTheHangHoas
                                .Where(bth => bth.MaBienThe == ct.MaBienThe)
                                .Select(bth => bth.RAM)
                                .FirstOrDefault(),
                            KichThuocManHinh = data.BienTheHangHoas
                                .Where(bth => bth.MaBienThe == ct.MaBienThe)
                                .Select(bth => bth.KichThuocManHinh)
                                .FirstOrDefault(),
                            LoaiBoNho = data.BienTheHangHoas
                                .Where(bth => bth.MaBienThe == ct.MaBienThe)
                                .Select(bth => bth.LoaiBoNho)
                                .FirstOrDefault()
                        }).ToList(),
                    GiaoHang = data.GiaoHangs
                        .Where(gh => gh.MaDonHang == dh.MaDonHang)
                        .Select(gh => new GiaoHangViewModel
                        {
                            MaGiaoHang = gh.MaGiaoHang,
                            MaDonHang = gh.MaDonHang,
                            MaDiaChi = gh.MaDiaChi,
                            MaVanDon = gh.MaVanDon,
                            DonViVanChuyen = gh.DonViVanChuyen,
                            TrangThaiGiaoHang = gh.TrangThaiGiaoHang,
                            NgayGuiHang = gh.NgayGuiHang,
                            NgayNhanHang = gh.NgayNhanHang,
                            DiaChiDayDu = data.DiaChiKhachHangs
                                .Where(dc => dc.MaDiaChi == gh.MaDiaChi)
                                .Select(dc => dc.DiaChiDayDu)
                                .FirstOrDefault(),
                            TenNguoiNhan = data.DiaChiKhachHangs
                                .Where(dc => dc.MaDiaChi == gh.MaDiaChi)
                                .Select(dc => dc.TenNguoiNhan)
                                .FirstOrDefault(),
                            SoDienThoai = data.DiaChiKhachHangs
                                .Where(dc => dc.MaDiaChi == gh.MaDiaChi)
                                .Select(dc => dc.SoDienThoai)
                                .FirstOrDefault()
                        }).FirstOrDefault()
                }).ToList();

            return Json(new { success = true, data = donHangs }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult HuyDonHang(string maDonHang)
        {
            if (Session["UserID"] == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập" });

            try
            {
                var donHang = data.DonHangs.FirstOrDefault(dh => dh.MaDonHang == maDonHang);
                if (donHang == null)
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });

                if (donHang.TrangThaiDonHang != "DangXuLy" || donHang.TrangThaiThanhToan != "ChoThanhToan")
                    return Json(new { success = false, message = "Không thể hủy đơn hàng này" });

                donHang.TrangThaiDonHang = "DaHuy";
                donHang.TrangThaiThanhToan = "DaHuy";
                data.SubmitChanges();

                return Json(new { success = true, message = "Hủy đơn hàng thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // POST: /User/ChangePassword
        public ActionResult ChangePassword(string password, string password_new)
        {
            string username = Session["AccountName"]?.ToString();
            string sessionPassword = Session["password"]?.ToString();
            password = SecurityHelper.EncryptPassword(password, "mysecretkey");
            if (username == null || sessionPassword == null)
            {
                // Nếu không có session => về login
                return RedirectToAction("Login", "InnerPage");
            }
            if (password == sessionPassword)
            {
                var user = data.KhachHangs.FirstOrDefault(u =>
                    u.TenDangNhap == username );

                if (user != null)
                {
                    user.MatKhauHash = SecurityHelper.EncryptPassword(password_new, "mysecretkey");
                    data.SubmitChanges();

                    // Cập nhật session password mới luôn
                    Session["password"] = SecurityHelper.EncryptPassword(password_new, "mysecretkey"); ;
                    TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy tài khoản.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Mật khẩu cũ không đúng.";
            }

            return RedirectToAction("Index", "User");
        }

        // POST: /User/XacNhanDoiThongTin
        [HttpPost]
        public ActionResult XacNhanDoiThongTin(string HoTen, string Email, string SoDienThoai)
        {
            var userId = Session["UserID"]?.ToString();

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var khachHang = data.KhachHangs.FirstOrDefault(k => k.MaKhachHang == userId);
            if (khachHang != null)
            {
                khachHang.HoTen = HoTen;
                khachHang.Email = Email;
                khachHang.SoDienThoai = SoDienThoai;

                data.SubmitChanges();
            }

            return RedirectToAction("Index", "User"); // quay lại trang thông tin
        }

    }
}
