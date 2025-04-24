using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Shop.Models;
using Shop.Helpers;
using System.Web.Providers.Entities;
namespace Shop.Controllers
{
    public class InnerPageController : Controller
    {

        SHOPDataContext data = new SHOPDataContext("Data Source=MSI;Initial Catalog=CuaHang2;Persist Security Info=True;Use" +
                 "r ID=sa;Password=123;Encrypt=True;TrustServerCertificate=True");
        public ActionResult Index()
        {
            return View();
        }
        #region Login
        // [GET]: Login
        public ActionResult Login()
        {
            return View();
        }
        //[POST]: Login
        [HttpPost]
        public ActionResult Login(string username, string password)
            {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin";
                return View("Index");
            }

            // Tìm người dùng với tên đăng nhập
            var user = data.KhachHangs.FirstOrDefault(u =>
                u.TenDangNhap == username &&
                u.TrangThai == "HoatDong");

            if (user != null)
            {
                try
                {
                    // Giải mã mật khẩu và so sánh
                    string decryptedPassword = SecurityHelper.DecryptPassword(user.MatKhauHash, "mysecretkey");
                    if (decryptedPassword == password)
                    {
                        // Lưu thông tin người dùng vào Session
                        Session["UserID"] = user.MaKhachHang;
                        Session["UserName"] = user.HoTen;
                        // Chuyển hướng đến trang Home
                        return RedirectToAction("Index", "Home");
                    }
                   
                }
                catch (Exception)
                {
                    // Xử lý lỗi giải mã (có thể là mật khẩu đã được lưu dạng text trước đó)
                    if (user.MatKhauHash == password)
                    {
                        // Lưu thông tin người dùng vào Session
                        Session["UserID"] = user.MaKhachHang;
                        Session["UserName"] = user.HoTen;

                        // Chuyển hướng đến trang Dashboard
                        return RedirectToAction("Index", "Home");
                    }
                }
            }
            TempData["Error"] = "Tên đăng nhập hoặc mật khẩu không đúng";
            return RedirectToAction("Login");
        }
        #endregion
        #region Register
        [HttpGet]
        public JsonResult CheckPhoneNumber(string phonenumber)
        {
            // Kiểm tra xem số điện thoại có tồn tại trong cơ sở dữ liệu không
            bool exists = data.KhachHangs.Any(u => u.SoDienThoai == phonenumber); // Sử dụng đúng tên bảng và cột của bạn
            return Json(new { exists = exists }, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public JsonResult CheckEmail(string email)
        {
            email = email?.Trim().ToLower(); // chuẩn hóa
            bool exists = data.KhachHangs.Any(kh => kh.Email.ToLower() == email);
            return Json(new { exists = exists }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult CheckUsername(string username)
        {
            // LINQ query kiểm tra xem username có tồn tại chưa
            bool exists = data.KhachHangs.Any(u => u.TenDangNhap == username);
            return Json(new { exists = exists }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Register()
        {
            return View();
        }
        // [POST]: Register
        [HttpPost]
        public ActionResult Register(string name, string username, string email, string password, string phonenumber)
        {
            // Kiểm tra trống
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ thông tin.";
                return RedirectToAction("Register");
            }

            // Kiểm tra username đã tồn tại chưa
            var existingUser = data.KhachHangs.FirstOrDefault(u => u.TenDangNhap == username);
            if (existingUser != null)
            {
                TempData["Error"] = "Tên đăng nhập đã được sử dụng.";
                return RedirectToAction("Register");
            }
            var existingEmail = data.KhachHangs.FirstOrDefault(u => u.Email == email);
            if (existingUser != null)
            {
                TempData["Error"] = "Tên Email đã được sử dụng.";
                return RedirectToAction("Register");
            }

            // Mã hóa mật khẩu (ví dụ dùng AES hoặc Hash)
            string key = "mysecretkey"; // nên lưu ở cấu hình
            string encryptedPassword = SecurityHelper.EncryptPassword(password, key);
            string GenerateUniqueMaKhachHang()
            {
                Random rand = new Random();
                string code;
                do
                {
                    int number = rand.Next(0, 100000); // 0 -> 99999
                    code = "KH" + number.ToString("D5"); // Ví dụ: KH04212
                } while (data.KhachHangs.Any(kh => kh.MaKhachHang == code)); // Kiểm tra trùng
                return code;
            }
            // Tạo người dùng mới
            var newUser = new KhachHang
            {
                MaKhachHang = GenerateUniqueMaKhachHang(), // Gán mã trước khi insert
                HoTen = name,
                TenDangNhap = username,
                Email = email,
                MatKhauHash = encryptedPassword,
                DiaChi = "SG",
                SoDienThoai = phonenumber,
                TrangThai = "HoatDong",
                NgayTao = DateTime.Now,
            };
           data.KhachHangs.InsertOnSubmit(newUser);
           data.SubmitChanges();


            // Lưu thông tin người dùng vào Session
            Session["UserID"] = newUser.MaKhachHang;
            Session["UserName"] = newUser.HoTen;

            // Chuyển hướng đến trang Dashboard
            return RedirectToAction("Index", "Home");
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
