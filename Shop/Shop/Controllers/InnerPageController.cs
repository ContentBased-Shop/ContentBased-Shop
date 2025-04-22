using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Shop.Models;
using Shop.Helpers;
namespace Shop.Controllers
{
    public class InnerPageController : Controller
    {

        SHOPDataContext data = new SHOPDataContext("Data Source=ACERNITRO5;Initial Catalog=CuaHang2;Persist Security Info=True;Use" +
                 "r ID=sa;Password=123;Encrypt=True;TrustServerCertificate=True");
        public ActionResult Index()
        {
            return View();
        }
        #region Login
        public ActionResult Login()
        {
            return View();
        }
       
        [HttpPost]
        public ActionResult Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin";
                return View("Index");
            }

            // Tìm người dùng với tên đăng nhập
            var user = data.NhanViens.FirstOrDefault(u =>
                u.TenDangNhap == username &&
                u.TrangThai == "HoatDong");

            if (user != null)
            {
                try
                {
                    // Giải mã mật khẩu và so sánh
                    string decryptedPassword = SecurityHelper.DecryptPassword(user.MatKhau, "mysecretkey");

                    if (decryptedPassword == password)
                    {
                        // Lưu thông tin người dùng vào Session
                        Session["UserID"] = user.MaNhanVien;
                        Session["UserName"] = user.HoTen;
                        Session["Role"] = user.VaiTro;

                        // Chuyển hướng đến trang Dashboard
                        return RedirectToAction("Dashboard");
                    }
                }
                catch (Exception)
                {
                    // Xử lý lỗi giải mã (có thể là mật khẩu đã được lưu dạng text trước đó)
                    if (user.MatKhau == password)
                    {
                        // Lưu thông tin người dùng vào Session
                        Session["UserID"] = user.MaNhanVien;
                        Session["UserName"] = user.HoTen;
                        Session["Role"] = user.VaiTro;

                        // Chuyển hướng đến trang Dashboard
                        return RedirectToAction("Dashboard");
                    }
                }
            }

            ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng";
            return View("Index");
        }
        #endregion
        #region Login
        public ActionResult Register()
        {
            return View();
        }
        #endregion
        public ActionResult ForgotPassword()
        {
            return View();
        }
        public ActionResult PreCart()
        {
            return View();
        }

        public ActionResult Checkout()
        {
            return View();
        }
        public ActionResult Checkout_ShoppingCart()
        {
            return View();
        }
        public ActionResult Contact()
        {
            return View();
        }
        public ActionResult About()
        {
            return View();
        }
        public ActionResult Promotion()
        {
            return View();
        }
    }
}
