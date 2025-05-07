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

            // Truy vấn dữ liệu khách hàng từ database
            var khachHang = data.KhachHangs
                .Where(kh => kh.MaKhachHang == maKhachHang)
                .FirstOrDefault();

            // Kiểm tra nếu không tìm thấy khách hàng
            if (khachHang == null)
            {
                return RedirectToAction("Login", "InnerPage"); // Nếu không tìm thấy, chuyển hướng đến đăng nhập
            }

            // Trả lại view với đối tượng KhachHang
            return View(khachHang); // Truyền khách hàng vào view
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
