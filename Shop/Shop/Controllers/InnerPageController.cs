﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Shop.Models;
using Shop.Helpers;
using System.Web.Providers.Entities;
using System.Net.Mail;
using System.Net;
using System.Web.Script.Serialization;
using System.Web.Security;

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
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl; // Lưu returnUrl vào ViewBag để sử dụng trong view
            return View();
        }
        //[POST]: Login
        [ValidateInput(false)]
        [HttpPost]
        public ActionResult Login(string username, string password, string returnUrl, bool rememberMe = false, string tempCart = null)
            {

            ViewBag.ReturnUrl = returnUrl;
            ViewBag.TempCart = tempCart;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin";
                return View();
            }

            var user = data.KhachHangs.FirstOrDefault(u => u.TenDangNhap == username);

            if (user == null)
            {
                TempData["Error"] = "Tên đăng nhập hoặc mật khẩu không đúng";
                return View();
            }

            bool isPasswordValid = false;

            try
            {
                string decryptedPassword = SecurityHelper.DecryptPassword(user.MatKhauHash, "mysecretkey");
                isPasswordValid = decryptedPassword == password;
            }
            catch
            {
                // Trường hợp password cũ không mã hoá
                isPasswordValid = user.MatKhauHash == password;
            }

            if (!isPasswordValid)
            {
                TempData["Error"] = "Tên đăng nhập hoặc mật khẩu không đúng";
                return View();
            }

            // Lưu session cơ bản
            Session["UserID"] = user.MaKhachHang;
            Session["UserName"] = user.HoTen;
            Session["AccountName"] = user.TenDangNhap;
            Session["Password"] = user.MatKhauHash;

            // Nếu là mật khẩu tạm thời, chuyển đến trang đổi mật khẩu
            if (user.ExpiryTime.HasValue && user.ExpiryTime.Value > DateTime.Now)
            {
                Session["RequirePasswordChange"] = true;
                return RedirectToAction("ChangePassword", "InnerPage");
            }

            // Tạo cookie xác thực nếu đăng nhập thành công
            var ticket = new FormsAuthenticationTicket(
                1,
                username,
                DateTime.Now,
                DateTime.Now.AddMinutes(60),
                rememberMe,
                username,
                FormsAuthentication.FormsCookiePath
            );

            string encryptedTicket = FormsAuthentication.Encrypt(ticket);
            var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
            if (rememberMe)
            {
                authCookie.Expires = ticket.Expiration;
            }
                Response.Cookies.Add(authCookie);

            try
            {
                GetOrCreateCart(user.MaKhachHang);

                if (!string.IsNullOrEmpty(tempCart))
                {
                    MergeCart(user.MaKhachHang, tempCart);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling cart during login: {ex.Message}");
            }

            // Chuyển hướng đến returnUrl nếu có
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        #endregion

        #region Cart
        // Tạo giỏ hàng mới nếu khách hàng chưa có
        private GioHang GetOrCreateCart(string maKhachHang)
        {
            try
            {
                var gioHang = data.GioHangs.FirstOrDefault(g => g.MaKhachHang == maKhachHang);
                
                if (gioHang == null)
                {
                    gioHang = new GioHang
                    {
                        MaKhachHang = maKhachHang,
                        NgayCapNhat = DateTime.Now
                    };
                    
                    data.GioHangs.InsertOnSubmit(gioHang);
                    data.SubmitChanges();
                }
                
                return gioHang;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetOrCreateCart: {ex.Message}");
                throw;
            }
        }
        
        // Merge giỏ hàng từ localStorage vào giỏ hàng trong CSDL
        private void MergeCart(string maKhachHang, string tempCartJson)
        {
            try
            {
                if (string.IsNullOrEmpty(tempCartJson))
                {
                    System.Diagnostics.Debug.WriteLine("tempCartJson is null or empty");
                    return;
                }

                // Chuyển đổi chuỗi JSON thành danh sách đối tượng giỏ hàng
                var serializer = new JavaScriptSerializer();
                List<CartItemModel> tempCartItems;
                
                try 
                {
                    tempCartItems = serializer.Deserialize<List<CartItemModel>>(tempCartJson);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error deserializing cart JSON: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Invalid JSON: {tempCartJson}");
                    return;
                }
                
                if (tempCartItems == null || tempCartItems.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No items in temp cart");
                    return;
                }
                
                // Lấy hoặc tạo giỏ hàng cho khách hàng
                var gioHang = GetOrCreateCart(maKhachHang);
                
                // Lấy danh sách sản phẩm hiện có trong giỏ hàng CSDL
                var existingItems = data.ChiTietGioHangs.Where(ct => ct.GioHang.MaKhachHang == maKhachHang).ToList();
                
                foreach (var item in tempCartItems)
                {
                    try
                    {
                        // Kiểm tra sản phẩm đã tồn tại trong giỏ hàng chưa
                        var existingItem = existingItems.FirstOrDefault(e => e.MaBienThe == item.maBienThe);
                        
                        if (existingItem != null)
                        {
                            // Cộng dồn số lượng
                            existingItem.SoLuong += item.soLuong;
                        }
                        else
                        {
                            // Thêm mới vào giỏ hàng
                            var newItem = new ChiTietGioHang
                            {
                                MaGioHang = gioHang.MaGioHang,
                                MaBienThe = item.maBienThe,
                                SoLuong = item.soLuong,
                                NgayThem = DateTime.Now
                            };
                            
                            data.ChiTietGioHangs.InsertOnSubmit(newItem);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing cart item {item.maBienThe}: {ex.Message}");
                        continue; // Tiếp tục với item tiếp theo nếu có lỗi
                    }
                }
                
                // Cập nhật thời gian của giỏ hàng
                gioHang.NgayCapNhat = DateTime.Now;
                
                // Lưu thay đổi vào CSDL
                data.SubmitChanges();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error merging cart: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw; // Ném lại exception để controller có thể xử lý
            }
        }
        
        // Kiểm tra người dùng đã đăng nhập chưa
        private bool IsLoggedIn()
        {
            return Session["UserID"] != null;
        }
        
        // Trang giỏ hàng
        public ActionResult PreCart()
        {
            // Nếu người dùng đã đăng nhập, cho phép xem giỏ hàng
            if (IsLoggedIn())
            {
                string maKhachHang = Session["UserID"].ToString();
                try
                {
                    // Kiểm tra và tạo giỏ hàng nếu chưa có
                    GetOrCreateCart(maKhachHang);
                    return View();
                }
                catch (Exception ex)
                {
                    // Log lỗi
                    System.Diagnostics.Debug.WriteLine($"Error in PreCart: {ex.Message}");
                    // Chuyển hướng đến trang lỗi
                    return RedirectToAction("Error", "Home");
                }
            }
            
            // Nếu chưa đăng nhập, chuyển hướng đến trang đăng nhập với returnUrl là PreCart
            return RedirectToAction("Login", "InnerPage", new { returnUrl = Url.Action("PreCart", "InnerPage") });
        }
        
        // Lấy giỏ hàng từ CSDL
        [HttpGet]
        public ActionResult GetCartItems()
        {
            if (!IsLoggedIn())
            {
                return Json(new { success = false, message = "Bạn chưa đăng nhập." }, JsonRequestBehavior.AllowGet);
            }
            
            string maKhachHang = Session["UserID"].ToString();
            
            try
            {
                var cartItems = (from ct in data.ChiTietGioHangs
                                join gh in data.GioHangs on ct.MaGioHang equals gh.MaGioHang
                                join bt in data.BienTheHangHoas on ct.MaBienThe equals bt.MaBienThe
                                join hh in data.HangHoas on bt.MaHangHoa equals hh.MaHangHoa
                                join ha in data.HinhAnhHangHoas on bt.MaBienThe equals ha.MaBienThe into haGroup
                                where gh.MaKhachHang == maKhachHang
                                select new CartItemModel
                                {
                                    maChiTietGioHang = ct.MaChiTietGioHang,
                                    maHangHoa = bt.MaHangHoa,
                                    maBienThe = bt.MaBienThe,
                                    tenHangHoa = hh.TenHangHoa,
                                    mauSac = bt.MauSac,
                                    dungLuong = bt.DungLuong,
                                    hinhAnh = haGroup.Select(ha => ha.UrlAnh).FirstOrDefault() ?? hh.HinhAnh,
                                    giaBan = (decimal)(bt.GiaBan ?? 0),
                                    giaKhuyenMai = (decimal)(bt.GiaKhuyenMai ?? 0),
                                    soLuong = (int)ct.SoLuong,
                                    soLuongTonKho = bt.SoLuongTonKho ?? 0,
                                    ngayThem = ct.NgayThem
                                }).ToList();
                
                return Json(new { success = true, cartItems }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        
        // Cập nhật giỏ hàng
        [HttpPost]
        public ActionResult UpdateCartItem(CartItemUpdateModel model)
        {
            if (!IsLoggedIn())
            {
                return Json(new { success = false, message = "Bạn chưa đăng nhập." });
            }
            
            string maKhachHang = Session["UserID"].ToString();
            
            try
            {
                // Lấy hoặc tạo giỏ hàng cho khách hàng
                var gioHang = GetOrCreateCart(maKhachHang);
                
                switch (model.action.ToLower())
                {
                    case "add":
                    case "update":
                        // Kiểm tra xem sản phẩm đã có trong giỏ hàng chưa
                        var existingItem = data.ChiTietGioHangs
                            .FirstOrDefault(ct => ct.GioHang.MaKhachHang == maKhachHang && ct.MaBienThe == model.maBienThe);
                        
                        if (existingItem != null)
                        {
                            // Cập nhật số lượng
                            if (model.action.ToLower() == "add")
                                existingItem.SoLuong += model.soLuong;
                            else
                                existingItem.SoLuong = model.soLuong;
                        }
                        else
                        {
                            // Thêm mới vào giỏ hàng
                            var newItem = new ChiTietGioHang
                            {
                                MaGioHang = gioHang.MaGioHang,
                                MaBienThe = model.maBienThe,
                                SoLuong = model.soLuong,
                                NgayThem = DateTime.Now
                            };
                            
                            data.ChiTietGioHangs.InsertOnSubmit(newItem);
                        }
                        break;
                        
                    case "remove":
                        // Xóa sản phẩm khỏi giỏ hàng
                        var itemToRemove = data.ChiTietGioHangs
                            .FirstOrDefault(ct => ct.GioHang.MaKhachHang == maKhachHang && ct.MaBienThe == model.maBienThe);
                        
                        if (itemToRemove != null)
                        {
                            data.ChiTietGioHangs.DeleteOnSubmit(itemToRemove);
                        }
                        break;
                }
                
                // Cập nhật thời gian của giỏ hàng
                gioHang.NgayCapNhat = DateTime.Now;
                
                // Lưu thay đổi vào CSDL
                data.SubmitChanges();
                
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        
        [HttpPost]
        public ActionResult MergeCart(string tempCart)
        {
            if (!IsLoggedIn())
            {
                return Json(new { success = false, message = "Bạn chưa đăng nhập." });
            }
            
            string maKhachHang = Session["UserID"].ToString();
            
            try
            {
                MergeCart(maKhachHang, tempCart);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        #endregion
        
        #region Checkout
        // Trang thanh toán
        public ActionResult Checkout()
        {
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", new { returnUrl = Url.Action("PreCart", "InnerPage") });
            }
            
            return View();
        }
        
        // Lấy danh sách địa chỉ của người dùng
        [HttpGet]
        public ActionResult GetUserAddresses()
        {
            if (Session["UserID"] == null)
            {
                return Json(new { success = false, message = "Bạn chưa đăng nhập." }, JsonRequestBehavior.AllowGet);
            }
            
            string maKhachHang = Session["UserID"].ToString();
            
            try
            {
                var addresses = data.DiaChiKhachHangs
                    .Where(d => d.MaKhachHang == maKhachHang)
                    .Select(d => new AddressModel
                    {
                        maDiaChi = d.MaDiaChi,
                        tenNguoiNhan = d.TenNguoiNhan,
                        soDienThoai = d.SoDienThoai,
                        diaChiDayDu = d.DiaChiDayDu,
                        laMacDinh = (bool)d.LaMacDinh
                    })
                    .ToList();
                
                return Json(new { success = true, addresses }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        
        // Thêm địa chỉ mới cho người dùng
        [HttpPost]
        public ActionResult AddUserAddress(AddressModel model)
        {
            if (Session["UserID"] == null)
            {
                return Json(new { success = false, message = "Bạn chưa đăng nhập." });
            }
            
            string maKhachHang = Session["UserID"].ToString();
            
            try
            {
                // Tạo mã địa chỉ mới
                string GenerateUniqueAddressId()
                {
                    Random rand = new Random();
                    string code;
                    do
                    {
                        int number = rand.Next(0, 100000); // 0 -> 99999
                        code = "DC" + number.ToString("D5"); // Ví dụ: DC04212
                    } while (data.DiaChiKhachHangs.Any(dc => dc.MaDiaChi == code));
                    return code;
                }
                
                // Nếu đặt làm địa chỉ mặc định, cập nhật các địa chỉ khác
                if (model.laMacDinh)
                {
                    var existingDefaultAddresses = data.DiaChiKhachHangs
                        .Where(d => d.MaKhachHang == maKhachHang && d.LaMacDinh == true);
                    
                    foreach (var addr in existingDefaultAddresses)
                    {
                        addr.LaMacDinh = false;
                    }
                }
                
                // Tạo địa chỉ mới
                var newAddress = new DiaChiKhachHang
                {
                    MaDiaChi = GenerateUniqueAddressId(),
                    MaKhachHang = maKhachHang,
                    TenNguoiNhan = model.tenNguoiNhan,
                    SoDienThoai = model.soDienThoai,
                    DiaChiDayDu = model.diaChiDayDu,
                    LaMacDinh = model.laMacDinh
                };
                
                data.DiaChiKhachHangs.InsertOnSubmit(newAddress);
                data.SubmitChanges();
                
                return Json(new { success = true, maDiaChi = newAddress.MaDiaChi });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        
        // Tạo đơn hàng
        [HttpPost]
        public ActionResult CreateOrder(OrderModel model)
        {
            if (Session["UserID"] == null)
            {
                return Json(new { success = false, message = "Bạn chưa đăng nhập." });
            }
            
            string maKhachHang = Session["UserID"].ToString();
            
            try
            {
                // Kiểm tra địa chỉ
                var address = data.DiaChiKhachHangs
                    .FirstOrDefault(d => d.MaDiaChi == model.maDiaChi && d.MaKhachHang == maKhachHang);
                
                if (address == null)
                {
                    return Json(new { success = false, message = "Địa chỉ giao hàng không hợp lệ." });
                }
                
                // Kiểm tra danh sách sản phẩm
                if (model.sanPham == null || model.sanPham.Count == 0)
                {
                    return Json(new { success = false, message = "Không có sản phẩm nào để đặt hàng." });
                }
                
                // Kiểm tra tồn kho
                foreach (var item in model.sanPham)
                {
                    var bienThe = data.BienTheHangHoas.FirstOrDefault(b => b.MaBienThe == item.maBienThe);
                    if (bienThe == null)
                    {
                        return Json(new { success = false, message = $"Sản phẩm không tồn tại: {item.maBienThe}" });
                    }
                    
                    if (bienThe.SoLuongTonKho < item.soLuong)
                    {
                        var hangHoa = data.HangHoas.FirstOrDefault(h => h.MaHangHoa == bienThe.MaHangHoa);
                        return Json(new { 
                            success = false, 
                            message = $"Sản phẩm '{hangHoa?.TenHangHoa}' chỉ còn {bienThe.SoLuongTonKho} sản phẩm."
                        });
                    }
                }
                
                // Tạo mã đơn hàng mới
                string GenerateUniqueOrderId()
                {
                    Random rand = new Random();
                    string code;
                    do
                    {
                        // Thêm timestamp để đảm bảo tính duy nhất
                        string timestamp = DateTime.Now.ToString("yyMMddHHmmss");
                        int number = rand.Next(0, 1000); // Giảm phạm vi random xuống 3 chữ số
                        code = "DH" + timestamp + number.ToString("D3"); // Ví dụ: DH230525123456001
                    } while (data.DonHangs.Any(dh => dh.MaDonHang == code));
                    return code;
                }
                
                // Tạo mã thanh toán mới
                string GenerateUniquePaymentId()
                {
                    Random rand = new Random();
                    string code;
                    do
                    {
                        // Thêm timestamp để đảm bảo tính duy nhất
                        string timestamp = DateTime.Now.ToString("yyMMddHHmmss");
                        int number = rand.Next(0, 1000); // Giảm phạm vi random xuống 3 chữ số
                        code = "TT" + timestamp + number.ToString("D3"); // Ví dụ: TT230525123456001
                    } while (data.ThanhToans.Any(tt => tt.MaThanhToan == code));
                    return code;
                }
                
                // Tạo mã giao hàng mới
                string GenerateUniqueShippingId()
                {
                    Random rand = new Random();
                    string code;
                    do
                    {
                        // Thêm timestamp để đảm bảo tính duy nhất
                        string timestamp = DateTime.Now.ToString("yyMMddHHmmss");
                        int number = rand.Next(0, 1000); // Giảm phạm vi random xuống 3 chữ số
                        code = "GH" + timestamp + number.ToString("D3"); // Ví dụ: GH230525123456001
                    } while (data.GiaoHangs.Any(gh => gh.MaGiaoHang == code));
                    return code;
                }
                
                // Tạo mã chi tiết đơn hàng mới
                string GenerateUniqueOrderDetailId()
                {
                    Random rand = new Random();
                    string code;
                    do
                    {
                        int number = rand.Next(0, 100000); // 0 -> 99999
                        code = "CT" + DateTime.Now.ToString("yyMMdd") + number.ToString("D5"); // Ví dụ: CT2305250001
                    } while (data.ChiTietDonHangs.Any(ct => ct.MaChiTietDonHang == code));
                    return code;
                }
                
                // Tạo mã vận đơn mới
                string GenerateUniqueTrackingId()
                {
                    Random rand = new Random();
                    string code;
                    int number = rand.Next(100000000, 999999999); // 9 chữ số
                    code = "VD" + number.ToString();
                    return code;
                }
                
                // Tạo đơn hàng mới
                var donHang = new DonHang
                {
                    MaDonHang = GenerateUniqueOrderId(),
                    MaKhachHang = maKhachHang,
                    TongTien = 0, // Sẽ được cập nhật sau
                    TrangThaiThanhToan = "ChoThanhToan",
                    TrangThaiDonHang = "DangXuLy",
                    NgayTao = DateTime.Now
                };
                
                data.DonHangs.InsertOnSubmit(donHang);
                
                // Tính tổng tiền
                decimal tongTien = 0;
                
                // Thêm chi tiết đơn hàng
                foreach (var item in model.sanPham)
                {
                    var bienThe = data.BienTheHangHoas.FirstOrDefault(b => b.MaBienThe == item.maBienThe);
                    
                    // Lấy giá sản phẩm
                    decimal donGia = Convert.ToDecimal(bienThe.GiaKhuyenMai ?? bienThe.GiaBan ?? 0);
                    decimal thanhTien = donGia * item.soLuong;
                    tongTien += thanhTien;
                    
                    // Thêm chi tiết đơn hàng
                    var chiTietDonHang = new ChiTietDonHang
                    {
                        MaChiTietDonHang = GenerateUniqueOrderDetailId(),
                        MaDonHang = donHang.MaDonHang,
                        MaBienThe = item.maBienThe,
                        SoLuong = item.soLuong,
                        DonGia = Convert.ToSingle(donGia)
                    };
                    
                    data.ChiTietDonHangs.InsertOnSubmit(chiTietDonHang);
                    
                    // Cập nhật số lượng tồn kho
                    bienThe.SoLuongTonKho -= item.soLuong;
                    
                    // Xóa sản phẩm khỏi giỏ hàng (nếu có)
                    var gioHang = data.GioHangs.FirstOrDefault(g => g.MaKhachHang == maKhachHang);
                    if (gioHang != null)
                    {
                        var chiTietGioHang = data.ChiTietGioHangs
                            .FirstOrDefault(ct => ct.MaGioHang == gioHang.MaGioHang && ct.MaBienThe == item.maBienThe);
                        
                        if (chiTietGioHang != null)
                        {
                            data.ChiTietGioHangs.DeleteOnSubmit(chiTietGioHang);
                        }
                    }
                }
                
                // Tính thuế (8% tổng tiền hàng)
                decimal thue = tongTien * 0.08m;
                
                // Cộng thuế vào tổng tiền đơn hàng (không tính phí vận chuyển)
                decimal tongTienSauThue = tongTien + thue;
                
                // Cập nhật tổng tiền đơn hàng (đã bao gồm thuế)
                donHang.TongTien = Convert.ToSingle(tongTienSauThue);
                
                // Xử lý voucher nếu có
                if (!string.IsNullOrWhiteSpace(model.maVoucherCode))
                {
                    var validationResult = ValidateVoucher(maKhachHang, model.maVoucherCode, Convert.ToSingle(tongTienSauThue));
                    
                    if (validationResult.success)
                    {
                        // Trừ tiền giảm giá từ voucher
                        float discountValue = validationResult.discountValue;
                        donHang.TongTien -= Convert.ToSingle(discountValue);
                        
                        // Lấy voucher từ CSDL
                        var voucher = data.Vouchers.FirstOrDefault(v => v.MaVoucherCode == model.maVoucherCode);
                        if (voucher != null)
                        {
                            // Cập nhật mã voucher vào đơn hàng
                            donHang.MaVoucher = voucher.MaVoucher;
                            
                            // Cập nhật số lượng đã sử dụng
                            voucher.SoLuongDaDung += 1;
                            
                            // Kiểm tra và cập nhật trạng thái phân phối voucher
                            var phanPhoi = data.PhanPhoiVouchers
                                .FirstOrDefault(pv => pv.MaVoucher == voucher.MaVoucher && pv.MaKhachHang == maKhachHang);
                            
                            if (phanPhoi != null)
                            {
                                // Đã có trong bảng phân phối, cập nhật trạng thái
                                phanPhoi.DaSuDung = true;
                                phanPhoi.NgaySuDung = DateTime.Now;
                            }
                            else
                            {
                                // Chưa có trong bảng phân phối, thêm mới
                                var newPhanPhoi = new PhanPhoiVoucher
                                {
                                    MaVoucher = voucher.MaVoucher,
                                    MaKhachHang = maKhachHang,
                                    DaSuDung = true,
                                    NgaySuDung = DateTime.Now
                                };
                                
                                data.PhanPhoiVouchers.InsertOnSubmit(newPhanPhoi);
                            }
                        }
                    }
                }
                
                // Tạo thanh toán
                var thanhToan = new ThanhToan
                {
                    MaThanhToan = GenerateUniquePaymentId(),
                    MaDonHang = donHang.MaDonHang,
                    PhuongThucThanhToan = model.phuongThucThanhToan,
                    TrangThai = "ChoXuLy"
                };
                
                if (model.phuongThucThanhToan == "COD")
                {
                    thanhToan.TrangThai = "ChoXuLy";
                }
                
                data.ThanhToans.InsertOnSubmit(thanhToan);
                
                // Tạo giao hàng
                var giaoHang = new GiaoHang
                {
                    MaGiaoHang = GenerateUniqueShippingId(),
                    DonViVanChuyen = "J&T Express",
                    MaDonHang = donHang.MaDonHang,
                    MaDiaChi = model.maDiaChi,
                    MaVanDon = GenerateUniqueTrackingId(),
                    TrangThaiGiaoHang = "ChuanBiHang"
                };
                
                data.GiaoHangs.InsertOnSubmit(giaoHang);
                
                // Lưu thay đổi vào CSDL
                data.SubmitChanges();
                
                // Sau khi tạo đơn hàng thành công và trước khi return
                SendOrderConfirmationEmail(donHang.MaDonHang);
                
                return Json(new { 
                    success = true, 
                    message = "Đặt hàng thành công", 
                    maDonHang = donHang.MaDonHang,
                    redirectUrl = Url.Action("OrderSuccess", "InnerPage", new { id = donHang.MaDonHang })
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        
        // Trang thông báo đặt hàng thành công
        public ActionResult OrderSuccess(string id)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login");
            }
            
            string maKhachHang = Session["UserID"].ToString();
            
            // Lấy thông tin đơn hàng
            var donHang = data.DonHangs.FirstOrDefault(dh => dh.MaDonHang == id && dh.MaKhachHang == maKhachHang);
            
            if (donHang == null)
            {
                return RedirectToAction("Index", "Home");
            }
            
            // Lấy thông tin thanh toán
            var thanhToan = data.ThanhToans.FirstOrDefault(tt => tt.MaDonHang == id);
            
            // Lấy thông tin giao hàng
            var giaoHang = data.GiaoHangs.FirstOrDefault(gh => gh.MaDonHang == id);
            
            // Lấy địa chỉ giao hàng
            var diaChi = giaoHang != null ? data.DiaChiKhachHangs.FirstOrDefault(dc => dc.MaDiaChi == giaoHang.MaDiaChi) : null;
            
            // Lấy thông tin voucher nếu có
            VoucherDetailModel voucherModel = null;
            if (donHang.MaVoucher.HasValue)
            {
                var voucher = data.Vouchers.FirstOrDefault(v => v.MaVoucher == donHang.MaVoucher);
                if (voucher != null)
                {
                    // Lấy chi tiết đơn hàng cơ bản
                    var chiTietDonHangCoSo = data.ChiTietDonHangs
                        .Where(ct => ct.MaDonHang == id)
                        .ToList();
                        
                    // Tính tổng đơn hàng trước khi giảm giá từ chi tiết đơn hàng
                    float tongTienTruocGiamGia = 0;
                    foreach (var ct in chiTietDonHangCoSo)
                    {
                        if (ct.SoLuong.HasValue && ct.DonGia.HasValue)
                        {
                            tongTienTruocGiamGia += (float)(ct.SoLuong.Value * ct.DonGia.Value);
                        }
                    }
                    
                    // Tính số tiền được giảm (chênh lệch giữa tổng đơn hàng trước và sau khi áp dụng voucher)
                    float soTienGiam = tongTienTruocGiamGia - (float)donHang.TongTien;
                    
                    voucherModel = new VoucherDetailModel
                    {
                        MaVoucher = voucher.MaVoucher,
                        MaVoucherCode = voucher.MaVoucherCode,
                        TenVoucher = voucher.TenVoucher,
                        LoaiGiamGia = voucher.LoaiGiamGia,
                        GiaTriGiamGia = Convert.ToSingle(voucher.GiaTriGiamGia),
                        MoTa = voucher.MoTa,
                        SoTienGiam = soTienGiam
                    };
                }
            }
            
            // Lấy chi tiết đơn hàng cơ bản (cho phần hiển thị)
            var chiTietDonHangView = data.ChiTietDonHangs
                .Where(ct => ct.MaDonHang == id)
                .ToList();
                
            var chiTietDonHang = new List<OrderSuccessItemModel>();
            
            // Biến đổi thành OrderSuccessItemModel
            foreach (var ct in chiTietDonHangView)
            {
                var bienThe = data.BienTheHangHoas.FirstOrDefault(bt => bt.MaBienThe == ct.MaBienThe);
                string tenHangHoa = "Sản phẩm";
                string mauSac = null;
                string dungLuong = null;
                
                if (bienThe != null)
                {
                    var hangHoa = data.HangHoas.FirstOrDefault(hh => hh.MaHangHoa == bienThe.MaHangHoa);
                    if (hangHoa != null)
                    {
                        tenHangHoa = hangHoa.TenHangHoa;
                    }
                    
                    mauSac = bienThe.MauSac;
                    dungLuong = bienThe.DungLuong;
                }
                
                chiTietDonHang.Add(new OrderSuccessItemModel
                {
                    MaBienThe = ct.MaBienThe,
                    SoLuong = ct.SoLuong.HasValue ? ct.SoLuong.Value : 0,
                    DonGia = ct.DonGia.HasValue ? (decimal)ct.DonGia.Value : 0m,
                    TenSanPham = tenHangHoa,
                    MauSac = mauSac,
                    DungLuong = dungLuong
                });
            }
            
            ViewBag.DonHang = donHang;
            ViewBag.ThanhToan = thanhToan;
            ViewBag.GiaoHang = giaoHang;
            ViewBag.DiaChi = diaChi;
            ViewBag.ChiTietDonHang = chiTietDonHang;
            ViewBag.Voucher = voucherModel;
            
            return View();
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
        [HttpPost]
        public JsonResult CapNhatDaDoc(string maThongBao)
        {
            // Lấy thông báo từ DB
            var thongBao = data.ThongBaos.FirstOrDefault(tb => tb.MaThongBao == maThongBao);
            if (thongBao != null)
            {
                thongBao.DaDoc = true;
                data.SubmitChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
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
        public ActionResult Register(string name, string username, string email, string password, string phonenumber, string tempCart = null)
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
                SoDienThoai = phonenumber,
            };


           data.KhachHangs.InsertOnSubmit(newUser);
           data.SubmitChanges();

            string GenerateUniqueMaThongBaog()
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
            var thongBaoMoi = new ThongBao
            {
                MaThongBao = GenerateUniqueMaThongBaog(),
                MaKhachHang = newUser.MaKhachHang,
                TieuDe = "Chào mừng đến với SwooTechSmart!",
                NoiDung = "Mã giảm giá đặc biệt dành riêng cho bạn: WELCOME10 (giảm 10% cho đơn hàng đầu tiên)",
                DaDoc = false,
                NgayGui = DateTime.Now
            };
            data.ThongBaos.InsertOnSubmit(thongBaoMoi); // LINQ thông qua DbSet
            data.SubmitChanges(); // lưu vào DB


            // Lưu thông tin người dùng vào Session
            Session["UserID"] = newUser.MaKhachHang;
            Session["UserName"] = newUser.HoTen;
            Session["AccountName"] = newUser.TenDangNhap;
            Session["Password"] = newUser.MatKhauHash;
            
            // Kiểm tra và tạo giỏ hàng nếu chưa có
            GetOrCreateCart(newUser.MaKhachHang);
            
            // Merge giỏ hàng tạm từ sessionStorage (nếu có)
            if (!string.IsNullOrEmpty(tempCart))
            {
                MergeCart(newUser.MaKhachHang, tempCart);
            }
            
            // Chuyển hướng đến trang Dashboard


            TempData["ShowRegisterSuccess"] = true;
            TempData["SendWelcomeEmail"] = email;
            TempData["UserName"] = newUser.HoTen;
            return RedirectToAction("Register"); // Quay lại Register để hiện Swal + gửi email



        }
        [HttpGet]
        public ActionResult SendOtp(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Email không hợp lệ" }, JsonRequestBehavior.AllowGet);
            }

            var otp = new Random().Next(100000, 999999).ToString();
            var fromAddress = new MailAddress("phamnguyenvu287@gmail.com", "Swoo Techsmart");
            var toAddress = new MailAddress(email);
            const string fromPassword = "sryh smuc npaf tuvq"; // bỏ \r\n thừa
            const string subject = "Mã xác thực OTP - Đăng ký tài khoản Swootechsmart";
                        string body = $@"
                        <!DOCTYPE html>
                        <html lang='vi'>
                        <head>
                            <meta charset='UTF-8'>
                            <style>
                                body {{
                                    font-family: Arial, sans-serif;
                                    background-color: #f4f4f4;
                                    margin: 0;
                                    padding: 0;
                                }}
                                .container {{
                                    max-width: 600px;
                                    margin: 40px auto;
                                    background-color: #ffffff;
                                    padding: 30px;
                                    border-radius: 8px;
                                    box-shadow: 0 2px 10px rgba(0,0,0,0.1);
                                }}
                                .header {{
                                    text-align: center;
                                    font-size: 20px;
                                    font-weight: bold;
                                    color: #333;
                                    margin-bottom: 20px;
                                }}
                                .otp {{
                                    font-size: 32px;
                                    font-weight: bold;
                                    color: #007bff;
                                    text-align: center;
                                    margin: 20px 0;
                                    letter-spacing: 4px;
                                }}
                                .note {{
                                    color: #555;
                                    font-size: 14px;
                                    line-height: 1.6;
                                }}
                                .footer {{
                                    margin-top: 30px;
                                    text-align: center;
                                    font-size: 13px;
                                    color: #999;
                                }}
                            </style>
                        </head>
                        <body>
                            <div class='container'>
                                <div class='header'>Mã OTP xác thực tài khoản</div>

                                <p class='note'>
                                    Kính gửi Quý khách,<br><br>
                                    Quý khách đang thực hiện đăng ký tài khoản tại <strong>Swootechsmart</strong>.<br>
                                    Vui lòng sử dụng mã OTP bên dưới để hoàn tất quá trình xác thực:
                                </p>

                                <div class='otp'>{otp}</div>

                                <p class='note'>
                                    Mã OTP có hiệu lực trong <strong>2 phút</strong> kể từ khi nhận được email này.<br>
                                    <strong>Lưu ý:</strong> Vui lòng không chia sẻ mã này với bất kỳ ai.
                                </p>

                                <div class='footer'>
                                    Cảm ơn Quý khách đã lựa chọn Swootechsmart!<br>
                                    Trân trọng,<br>
                                    Đội ngũ Swootechsmart
                                </div>
                            </div>
                        </body>
                        </html>";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };

            try
            {
                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                })
                {
                    smtp.Send(message);
                }

                // Không lưu Session
                return Json(new { success = true, otp = otp }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi gửi email: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        #endregion
        public ActionResult Logout()
        {
            // Xóa Session
            Session.Clear();
            Session.Abandon();

            // Xóa FormsAuthentication Cookie
            FormsAuthentication.SignOut();

            // Xóa ASPXAUTH cookie nếu còn
            if (Request.Cookies[FormsAuthentication.FormsCookieName] != null)
            {
                var cookie = new HttpCookie(FormsAuthentication.FormsCookieName)
                {
                    Expires = DateTime.Now.AddDays(-1),
                    Value = ""
                };
                Response.Cookies.Add(cookie);
            }

            // Gọi view Logout sẽ thực hiện xóa sessionStorage bên client
            return View("Logout");
        }





        #region Forgot Password
        [HttpPost]
        public ActionResult ForgotPassword(string email, string username)
        {
            try
            {
                // Kiểm tra email và username có tồn tại không
                var user = data.KhachHangs.FirstOrDefault(kh => 
                    kh.Email == email && kh.TenDangNhap == username);

                if (user == null)
                {
                    return Json(new { success = false, message = "Email hoặc tên đăng nhập không chính xác." });
                }

                // Tạo mật khẩu tạm 12 ký tự
                string tempPassword = GenerateTempPassword(12);
                
                // Mã hóa mật khẩu tạm
                string encryptedTempPassword = SecurityHelper.EncryptPassword(tempPassword, "mysecretkey");

                // Cập nhật mật khẩu và thời gian hết hạn
                user.MatKhauHash = encryptedTempPassword;
                user.ExpiryTime = DateTime.Now.AddMinutes(10);
                data.SubmitChanges();

                // Gửi email chứa mật khẩu tạm
                SendTempPasswordEmail(user.Email, user.HoTen, tempPassword);

                return Json(new { success = true, message = "Mật khẩu tạm đã được gửi đến email của bạn." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        private string GenerateTempPassword(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private void SendTempPasswordEmail(string toEmail, string toName, string tempPassword)
        {
            var fromAddress = new MailAddress("managertask34@gmail.com", "SWOO Admin");
            var toAddress = new MailAddress(toEmail, toName);
            const string subject = "Mật khẩu tạm - SWOO";

            string body = string.Format(@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #28a745;'>Mật khẩu tạm của bạn</h2>
                    <p>Xin chào {0},</p>
                    <p>Bạn đã yêu cầu đặt lại mật khẩu. Dưới đây là mật khẩu tạm của bạn:</p>
                    
                    <div style='margin: 20px 0; padding: 15px; background-color: #f8f9fa; border-radius: 5px;'>
                        <p style='font-size: 18px; font-weight: bold; text-align: center;'>{1}</p>
                    </div>

                    <p><strong>Lưu ý quan trọng:</strong></p>
                    <ul>
                        <li>Mật khẩu này sẽ hết hạn sau 10 phút</li>
                        <li>Khi đăng nhập với mật khẩu tạm, bạn sẽ được yêu cầu đổi sang mật khẩu mới</li>
                        <li>Vui lòng không chia sẻ mật khẩu này với bất kỳ ai</li>
                    </ul>

                    <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #dee2e6;'>
                        <p style='color: #666;'>Trân trọng,<br>SWOO Team</p>
                    </div>
                </div>",
                toName,
                tempPassword
            );

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, "veaq dwhq oico jlzc")
            };

            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            })
            {
                smtp.Send(message);
            }
        }

        [HttpGet]
        public ActionResult ChangePassword()
        {
            // Kiểm tra xem người dùng đã đăng nhập chưa
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "InnerPage");
            }

            // Kiểm tra xem có phải đang sử dụng mật khẩu tạm không
            string maKhachHang = Session["UserID"].ToString();
            var user = data.KhachHangs.FirstOrDefault(kh => kh.MaKhachHang == maKhachHang);

            if (user == null)
            {
                return RedirectToAction("Login", "InnerPage");
            }

            // Nếu không phải mật khẩu tạm và không có yêu cầu đổi mật khẩu
            if (!user.ExpiryTime.HasValue && Session["RequirePasswordChange"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        public ActionResult ChangePassword(string currentPassword, string newPassword)
        {
            if (Session["UserID"] == null)
            {
                return Json(new { success = false, message = "Bạn chưa đăng nhập." });
            }

            string maKhachHang = Session["UserID"].ToString();
            var user = data.KhachHangs.FirstOrDefault(kh => kh.MaKhachHang == maKhachHang);

            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin người dùng." });
            }

            try
            {
                // Kiểm tra mật khẩu hiện tại
                string decryptedCurrentPassword = SecurityHelper.DecryptPassword(user.MatKhauHash, "mysecretkey");
                
                if (decryptedCurrentPassword != currentPassword)
                {
                    return Json(new { success = false, message = "Mật khẩu hiện tại không chính xác." });
                }

                // Kiểm tra mật khẩu mới có đủ mạnh không
                if (!IsStrongPassword(newPassword))
                {
                    return Json(new { success = false, message = "Mật khẩu mới phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt." });
                }

                // Mã hóa và cập nhật mật khẩu mới
                string encryptedNewPassword = SecurityHelper.EncryptPassword(newPassword, "mysecretkey");
                user.MatKhauHash = encryptedNewPassword;
                user.ExpiryTime = null; // Xóa thời gian hết hạn
                data.SubmitChanges();

                return Json(new { success = true, message = "Đổi mật khẩu thành công." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        private bool IsStrongPassword(string password)
        {
            // Kiểm tra độ dài tối thiểu
            if (password.Length < 8) return false;

            // Kiểm tra có chữ hoa
            if (!password.Any(char.IsUpper)) return false;

            // Kiểm tra có chữ thường
            if (!password.Any(char.IsLower)) return false;

            // Kiểm tra có số
            if (!password.Any(char.IsDigit)) return false;

            // Kiểm tra có ký tự đặc biệt
            if (!password.Any(c => !char.IsLetterOrDigit(c))) return false;

            return true;
        }
        #endregion

        public ActionResult ForgotPassword()
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
        public ActionResult TuyenDung()
        {
            return View();
        }

      
        public ActionResult LoadThongBao()
        {
            var maKhachHang = Session["UserID"] as string;

            if (string.IsNullOrEmpty(maKhachHang))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized); // hoặc return PartialView với thông báo trống
            }
            var thongBaos = data.ThongBaos
                 .Where(tb => tb.MaKhachHang == maKhachHang)
                 .OrderByDescending(tb => tb.NgayGui)
                 .Take(10)
                 .ToList(); // Không dùng .Select()


            return PartialView("_ThongBaoPartial", thongBaos);
        }
        public JsonResult GetSoLuongThongBao()
        {
            string maKhachHang = Session["UserID"]?.ToString(); // hoặc HttpContext.User.Identity.Name nếu dùng Identity
            if (string.IsNullOrEmpty(maKhachHang))
                return Json(0, JsonRequestBehavior.AllowGet);

          
            int count = data.ThongBaos.Count(tb => tb.MaKhachHang == maKhachHang && tb.DaDoc == false);

            return Json(count, JsonRequestBehavior.AllowGet);
        }

        #region Voucher
        // Model voucher cho response API
        public class VoucherModel
        {
            public int maVoucher { get; set; }
            public string maVoucherCode { get; set; }
            public string tenVoucher { get; set; }
            public string loaiGiamGia { get; set; }
            public float giaTriGiamGia { get; set; }
            public float donHangToiThieu { get; set; }
            public string moTa { get; set; }
            public bool daApDung { get; set; }
            public float soTienGiam { get; set; }
        }
        
        // Model VoucherDetailModel cho hiển thị chi tiết
        public class VoucherDetailModel
        {
            public int MaVoucher { get; set; }
            public string MaVoucherCode { get; set; }
            public string TenVoucher { get; set; }
            public string LoaiGiamGia { get; set; }
            public float GiaTriGiamGia { get; set; }
            public float SoTienGiam { get; set; }
            public string MoTa { get; set; }
        }
        
        // Model request kiểm tra voucher
        public class VoucherCheckModel
        {
            public string maVoucherCode { get; set; }
            public float tongTien { get; set; }
        }
        
        // API kiểm tra voucher
        [HttpPost]
        public ActionResult CheckVoucher(VoucherCheckModel model)
        {
            if (!IsLoggedIn())
            {
                return Json(new { success = false, message = "Bạn chưa đăng nhập." });
            }
            
            string maKhachHang = Session["UserID"].ToString();
            
            if (string.IsNullOrWhiteSpace(model.maVoucherCode))
            {
                return Json(new { success = false, message = "Mã voucher không hợp lệ." });
            }
            
            try
            {
                // Chuyển đổi rõ ràng từ float sang float (nếu cần)
                float tongTien = Convert.ToSingle(model.tongTien);
                
                // Kiểm tra và áp dụng voucher
                var validationResult = ValidateVoucher(maKhachHang, model.maVoucherCode, tongTien);
                
                if (!validationResult.success)
                {
                    return Json(new { success = false, message = validationResult.message });
                }
                
                var voucher = validationResult.voucher;
                float discountValue = validationResult.discountValue;
                
                return Json(new { 
                    success = true, 
                    message = "Áp dụng voucher thành công.", 
                    voucher = new VoucherModel {
                        maVoucher = voucher.MaVoucher,
                        maVoucherCode = voucher.MaVoucherCode,
                        tenVoucher = voucher.TenVoucher,
                        loaiGiamGia = voucher.LoaiGiamGia,
                        giaTriGiamGia = Convert.ToSingle(voucher.GiaTriGiamGia),
                        donHangToiThieu = Convert.ToSingle(voucher.DonHangToiThieu),
                        moTa = voucher.MoTa,
                        daApDung = true,
                        soTienGiam = discountValue
                    } 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }
        
        // Hàm kiểm tra voucher
        private dynamic ValidateVoucher(string maKhachHang, string maVoucherCode, float tongTien)
        {
            // Tìm voucher trong CSDL
            var voucher = data.Vouchers.FirstOrDefault(v => 
                v.MaVoucherCode == maVoucherCode && 
                v.TrangThai == "HoatDong");
            
            if (voucher == null)
            {
                return new { success = false, message = "Mã voucher không tồn tại hoặc đã hết hạn." };
            }
            
            // Kiểm tra thời gian hiệu lực
            DateTime now = DateTime.Now;
            if (now < voucher.NgayBatDau || now > voucher.NgayKetThuc)
            {
                return new { success = false, message = "Mã voucher đã hết hạn hoặc chưa đến thời gian sử dụng." };
            }
            
            // Kiểm tra giá trị đơn hàng tối thiểu
            if (tongTien < voucher.DonHangToiThieu)
            {
                return new { 
                    success = false, 
                    message = $"Đơn hàng tối thiểu để sử dụng voucher là {voucher.DonHangToiThieu:N0} đ." 
                };
            }
            
            // Kiểm tra số lượng voucher còn lại (nếu là voucher công khai)
            if ((bool)voucher.IsPublic && voucher.SoLuong <= voucher.SoLuongDaDung)
            {
                return new { success = false, message = "Mã voucher đã hết số lượng sử dụng." };
            }
            
            // Kiểm tra xem người dùng đã sử dụng voucher này chưa
            var userVoucher = data.PhanPhoiVouchers
                .FirstOrDefault(pv => pv.MaVoucher == voucher.MaVoucher && pv.MaKhachHang == maKhachHang);
            
            // Nếu là voucher riêng (không công khai)
            if (!(bool)voucher.IsPublic)
            {
                // Voucher riêng: người dùng phải được phân phối và chưa sử dụng
                if (userVoucher == null)
                {
                    return new { success = false, message = "Bạn không thể sử dụng mã voucher này." };
                }
                
                if ((bool)userVoucher.DaSuDung)
                {
                    return new { success = false, message = "Bạn đã sử dụng mã voucher này rồi." };
                }
            }
            // Nếu là voucher công khai
            else
            {
                // Voucher công khai: mỗi người dùng chỉ dùng 1 lần
                if (userVoucher != null && (bool) userVoucher.DaSuDung)
                {
                    return new { success = false, message = "Bạn đã sử dụng mã voucher này rồi." };
                }
            }
            
            // Tính giá trị giảm giá
            float discountValue = 0;
            if (voucher.LoaiGiamGia == "TienMat")
            {
                // Giảm trực tiếp theo giá trị tiền mặt
                discountValue = Convert.ToSingle(voucher.GiaTriGiamGia);
            }
            else if (voucher.LoaiGiamGia == "PhanTram")
            {
                // Giảm theo phần trăm của tổng tiền (đã bao gồm thuế)
                discountValue = tongTien * (Convert.ToSingle(voucher.GiaTriGiamGia) / 100);
            }
            
            // Đảm bảo số tiền giảm không vượt quá tổng tiền
            if (discountValue > tongTien)
            {
                discountValue = tongTien;
            }
            
            return new { success = true, voucher, discountValue };
        }
        
        // Hàm áp dụng voucher khi đặt hàng
        private void ApplyVoucher(string maKhachHang, string maVoucherCode, string maDonHang)
        {
            if (string.IsNullOrWhiteSpace(maVoucherCode)) return;
            
            var voucher = data.Vouchers.FirstOrDefault(v => v.MaVoucherCode == maVoucherCode);
            if (voucher == null) return;
            
            // Cập nhật số lượng đã sử dụng
            voucher.SoLuongDaDung += 1;
            
            // Kiểm tra và cập nhật trạng thái phân phối voucher
            var phanPhoi = data.PhanPhoiVouchers
                .FirstOrDefault(pv => pv.MaVoucher == voucher.MaVoucher && pv.MaKhachHang == maKhachHang);
            
            if (phanPhoi != null)
            {
                // Đã có trong bảng phân phối, cập nhật trạng thái
                phanPhoi.DaSuDung = true;
                phanPhoi.NgaySuDung = DateTime.Now;
            }
            else
            {
                // Chưa có trong bảng phân phối, thêm mới
                var newPhanPhoi = new PhanPhoiVoucher
                {
                    MaVoucher = voucher.MaVoucher,
                    MaKhachHang = maKhachHang,
                    DaSuDung = true,
                    NgaySuDung = DateTime.Now
                };
                
                data.PhanPhoiVouchers.InsertOnSubmit(newPhanPhoi);
            }
            
            // Cập nhật mã voucher vào đơn hàng
            var donHang = data.DonHangs.FirstOrDefault(dh => dh.MaDonHang == maDonHang);
            if (donHang != null)
            {
                donHang.MaVoucher = voucher.MaVoucher;
            }
            
            // Lưu thay đổi
            data.SubmitChanges();
        }
        #endregion

        #region Email
        private readonly string _emailAddress = "managertask34@gmail.com";
        private readonly string _emailPassword = "veaq dwhq oico jlzc";
        private readonly string _emailDisplayName = "PrimeTech Admin";

        private void SendOrderConfirmationEmail(string maDonHang)
        {
            try
            {
                // Lấy thông tin đơn hàng
                var donHang = data.DonHangs.FirstOrDefault(dh => dh.MaDonHang == maDonHang);
                if (donHang == null) return;

                // Lấy thông tin khách hàng
                var khachHang = data.KhachHangs.FirstOrDefault(kh => kh.MaKhachHang == donHang.MaKhachHang);
                if (khachHang == null) return;

                // Lấy chi tiết đơn hàng
                var chiTietDonHang = data.ChiTietDonHangs
                    .Where(ct => ct.MaDonHang == maDonHang)
                    .ToList();

                // Tạo bảng sản phẩm
                var productTable = new StringBuilder();
                productTable.AppendLine("<table style='width:100%; border-collapse: collapse;'>");
                productTable.AppendLine("<tr style='background-color: #f8f9fa;'>");
                productTable.AppendLine("<th style='padding: 10px; border: 1px solid #dee2e6;'>Sản phẩm</th>");
                productTable.AppendLine("<th style='padding: 10px; border: 1px solid #dee2e6;'>Số lượng</th>");
                productTable.AppendLine("<th style='padding: 10px; border: 1px solid #dee2e6;'>Đơn giá</th>");
                productTable.AppendLine("<th style='padding: 10px; border: 1px solid #dee2e6;'>Thành tiền</th>");
                productTable.AppendLine("</tr>");

                foreach (var item in chiTietDonHang)
                {
                    var bienThe = data.BienTheHangHoas.FirstOrDefault(bt => bt.MaBienThe == item.MaBienThe);
                    var hangHoa = bienThe != null ? data.HangHoas.FirstOrDefault(hh => hh.MaHangHoa == bienThe.MaHangHoa) : null;
                    var tenSanPham = hangHoa?.TenHangHoa ?? "Sản phẩm";
                    var thanhTien = item.SoLuong * item.DonGia;

                    productTable.AppendLine("<tr>");
                    productTable.AppendLine($"<td style='padding: 10px; border: 1px solid #dee2e6;'>{tenSanPham}</td>");
                    productTable.AppendLine($"<td style='padding: 10px; border: 1px solid #dee2e6; text-align: center;'>{item.SoLuong}</td>");
                    productTable.AppendLine($"<td style='padding: 10px; border: 1px solid #dee2e6; text-align: right;'>{item.DonGia:N0} ₫</td>");
                    productTable.AppendLine($"<td style='padding: 10px; border: 1px solid #dee2e6; text-align: right;'>{thanhTien:N0} ₫</td>");
                    productTable.AppendLine("</tr>");
                }

                productTable.AppendLine("</table>");

                // Tính toán tổng tiền và thuế
                var tongTienHang = chiTietDonHang.Sum(ct => ct.SoLuong * ct.DonGia);
                var thue = (decimal)tongTienHang * 0.08m;
                var tongThanhToan = (decimal)tongTienHang + thue;

                // Tạo nội dung email
                var fromAddress = new MailAddress(_emailAddress, _emailDisplayName);
                var toAddress = new MailAddress(khachHang.Email, khachHang.HoTen);
                const string subject = "Xác nhận đơn hàng thành công - PrimeTech";

                string body = string.Format(@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <h2 style='color: #28a745;'>Xác nhận đơn hàng thành công</h2>
                        <p>Xin chào {0},</p>
                        <p>Cảm ơn bạn đã đặt hàng tại PrimeTech. Dưới đây là thông tin chi tiết đơn hàng của bạn:</p>
                        
                        <div style='margin: 20px 0;'>
                            <p><strong>Mã đơn hàng:</strong> {1}</p>
                            <p><strong>Ngày đặt hàng:</strong> {2}</p>
                            <p><strong>Phương thức thanh toán:</strong> {3}</p>
                        </div>

                        <h3 style='color: #333;'>Chi tiết đơn hàng:</h3>
                        {4}

                        <div style='margin: 20px 0; text-align: right;'>
                            <p><strong>Tổng tiền hàng:</strong> {5:N0} ₫</p>
                            <p><strong>Thuế (8%):</strong> {6:N0} ₫</p>
                            <p style='font-size: 18px; color: #28a745;'><strong>Tổng thanh toán:</strong> {7:N0} ₫</p>
                        </div>

                        <p>Chúng tôi sẽ xử lý đơn hàng của bạn trong thời gian sớm nhất.</p>
                        <p>Nếu bạn có bất kỳ thắc mắc nào, vui lòng liên hệ với chúng tôi qua email hoặc số điện thoại hỗ trợ.</p>
                        
                        <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #dee2e6;'>
                            <p style='color: #666;'>Trân trọng,<br>PrimeTech Team</p>
                        </div>
                    </div>",
                    khachHang.HoTen,
                    donHang.MaDonHang,
                    donHang.NgayTao.Value.ToString("dd/MM/yyyy HH:mm"),
                    donHang.TrangThaiThanhToan == "DaThanhToan" ? "Đã thanh toán" : "Thanh toán khi nhận hàng",
                    productTable,
                    tongTienHang,
                    thue,
                    tongThanhToan
                );

                // Cấu hình SMTP
                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, _emailPassword)
                };

                // Tạo và gửi email
                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                })
                {
                    smtp.Send(message);
                }
            }
            catch (Exception ex)
            {
                // Log lỗi gửi email
                System.Diagnostics.Debug.WriteLine($"Lỗi gửi email xác nhận đơn hàng: {ex.Message}");
            }
        }
        #endregion
        private static readonly List<FAQItem> faqData = new List<FAQItem>
{
    new FAQItem
    {
        Id = "1",
        Category = "order",
        Question = "Làm thế nào để đặt hàng trên website?",
        Answer = "Để đặt hàng, bạn chỉ cần: 1) Chọn sản phẩm và thêm vào giỏ hàng, 2) Điền thông tin giao hàng, 3) Chọn phương thức thanh toán, 4) Xác nhận đơn hàng. Chúng tôi sẽ gửi email xác nhận ngay sau khi đặt hàng thành công."
    },
    new FAQItem
    {
        Id = "2",
        Category = "order",
        Question = "Tôi có thể hủy hoặc thay đổi đơn hàng không?",
        Answer = "Bạn có thể hủy hoặc thay đổi đơn hàng trong vòng 30 phút sau khi đặt hàng. Sau thời gian này, đơn hàng sẽ được chuyển vào quy trình xử lý và không thể thay đổi."
    },
    new FAQItem
    {
        Id = "3",
        Category = "payment",
        Question = "Các phương thức thanh toán nào được hỗ trợ?",
        Answer = "Chúng tôi hỗ trợ thanh toán qua: Thẻ tín dụng/ghi nợ (Visa, Mastercard), Ví điện tử (MoMo, ZaloPay, VNPay), Chuyển khoản ngân hàng, và Thanh toán khi nhận hàng (COD)."
    },
    new FAQItem
    {
        Id = "4",
        Category = "payment",
        Question = "Thông tin thanh toán của tôi có an toàn không?",
        Answer = "Tuyệt đối an toàn! Chúng tôi sử dụng mã hóa SSL 256-bit và tuân thủ tiêu chuẩn bảo mật PCI DSS. Thông tin thẻ của bạn không được lưu trữ trên hệ thống của chúng tôi."
    },
    new FAQItem
    {
        Id = "5",
        Category = "shipping",
        Question = "Thời gian giao hàng là bao lâu?",
        Answer = "Thời gian giao hàng phụ thuộc vào địa điểm: Nội thành Hà Nội/TP.HCM: 1-2 ngày, Các tỉnh thành khác: 2-5 ngày, Vùng sâu vùng xa: 5-7 ngày. Đơn hàng đặt trước 14h sẽ được xử lý trong ngày."
    },
    new FAQItem
    {
        Id = "6",
        Category = "shipping",
        Question = "Phí vận chuyển được tính như thế nào?",
        Answer = "Phí vận chuyển được tính theo khu vực và trọng lượng đơn hàng. Miễn phí vận chuyển cho đơn hàng từ 500.000đ trở lên trong nội thành và từ 1.000.000đ cho các tỉnh khác."
    },
    new FAQItem
    {
        Id = "7",
        Category = "return",
        Question = "Chính sách đổi trả như thế nào?",
        Answer = "Bạn có thể đổi trả sản phẩm trong vòng 30 ngày kể từ ngày nhận hàng. Sản phẩm phải còn nguyên vẹn, chưa sử dụng và có đầy đủ bao bì, phụ kiện đi kèm."
    },
    new FAQItem
    {
        Id = "8",
        Category = "return",
        Question = "Ai chịu phí vận chuyển khi đổi trả?",
        Answer = "Nếu lỗi từ phía shop (sản phẩm lỗi, giao sai hàng), chúng tôi sẽ chịu phí vận chuyển. Nếu khách hàng đổi ý, khách hàng sẽ chịu phí vận chuyển đổi trả."
    },
    new FAQItem
    {
        Id = "9",
        Category = "account",
        Question = "Làm thế nào để tạo tài khoản?",
        Answer = "Bạn có thể tạo tài khoản bằng cách click vào 'Đăng ký' ở góc trên cùng, điền thông tin cá nhân và xác thực email. Hoặc đăng ký nhanh qua Facebook/Google."
    },
    new FAQItem
    {
        Id = "10",
        Category = "account",
        Question = "Tôi quên mật khẩu, phải làm sao?",
        Answer = "Click vào 'Quên mật khẩu' ở trang đăng nhập, nhập email đã đăng ký. Chúng tôi sẽ gửi link đặt lại mật khẩu đến email của bạn trong vòng 5 phút."
    },
    new FAQItem
    {
        Id = "11",
        Category = "product",
        Question = "Làm sao để kiểm tra tình trạng kho hàng?",
        Answer = "Tình trạng kho hàng được hiển thị ngay trên trang sản phẩm. Nếu sản phẩm hết hàng, bạn có thể đăng ký nhận thông báo khi hàng về."
    },
    new FAQItem
    {
        Id = "12",
        Category = "product",
        Question = "Sản phẩm có được bảo hành không?",
        Answer = "Tất cả sản phẩm đều có chế độ bảo hành theo quy định của nhà sản xuất. Thời gian bảo hành từ 6 tháng đến 2 năm tùy loại sản phẩm, được ghi rõ trong mô tả sản phẩm."
    }
};

        public ActionResult Faq(string search = "", string category = "all")
        {
            var filtered = faqData.Where(f =>
                (category == "all" || f.Category == category) &&
                (string.IsNullOrEmpty(search) || f.Question.ToLower().Contains(search.ToLower()) || f.Answer.ToLower().Contains(search.ToLower()))
            ).ToList();

            ViewBag.SearchTerm = search;
            ViewBag.SelectedCategory = category;
            ViewBag.AllCategories = new List<string> { "all", "order", "payment", "shipping", "return", "account", "product" };

            return View(filtered);
        }
    }
   
    public class FAQItem
    {
        public string Id { get; set; }
        public string Category { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }
    }

    // Models for cart items
    public class CartItemModel
    {
        public int maChiTietGioHang { get; set; }
        public string maHangHoa { get; set; }
        public string maBienThe { get; set; }
        public string tenHangHoa { get; set; }
        public string mauSac { get; set; }
        public string dungLuong { get; set; }
        public string hinhAnh { get; set; }
        public decimal giaBan { get; set; }
        public decimal giaKhuyenMai { get; set; }
        public int soLuong { get; set; }
        public int soLuongTonKho { get; set; }
        public DateTime? ngayThem { get; set; }
    }
    
    public class CartItemUpdateModel
    {
        public string maBienThe { get; set; }
        public int soLuong { get; set; }
        public string action { get; set; } // "add", "update", or "remove"
    }
    
    // Model cho địa chỉ giao hàng
    public class AddressModel
    {
        public string maDiaChi { get; set; }
        public string tenNguoiNhan { get; set; }
        public string soDienThoai { get; set; }
        public string diaChiDayDu { get; set; }
        public bool laMacDinh { get; set; }
    }
    
    // Model cho đơn hàng
    public class OrderModel
    {
        public string maDiaChi { get; set; }
        public List<OrderItemModel> sanPham { get; set; }
        public string phuongThucThanhToan { get; set; }
        public string ghiChu { get; set; }
        public string maVoucherCode { get; set; }  // Thêm trường mã voucher
        public double tongTien { get; set; }  // Thêm trường tổng tiền
    }
    
    // Model cho sản phẩm trong đơn hàng
    public class OrderItemModel
    {
        public string maBienThe { get; set; }
        public int soLuong { get; set; }
    }
    
    public class OrderSuccessItemModel
    {
        public string MaBienThe { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public string TenSanPham { get; set; }
        public string MauSac { get; set; }
        public string DungLuong { get; set; }
    }
}
