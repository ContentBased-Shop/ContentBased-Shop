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
using System.Net.Mail;
using System.Net;
using System.Web.Script.Serialization;

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
        // [GET]: Login
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl; // Lưu returnUrl vào ViewBag để sử dụng trong view
            return View();
        }
        //[POST]: Login
        [HttpPost]
        public ActionResult Login(string username, string password, string returnUrl, string tempCart = null)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin";
                return View();
            }

            // Tìm người dùng với tên đăng nhập
            var user = data.KhachHangs.FirstOrDefault(u =>
                u.TenDangNhap == username );

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
                        Session["AccountName"] = user.TenDangNhap;
                        Session["Password"] = user.MatKhauHash;
                        
                        // Merge giỏ hàng tạm từ sessionStorage (nếu có)
                        if (!string.IsNullOrEmpty(tempCart))
                        {
                            MergeCart(user.MaKhachHang, tempCart);
                        }
                        
                        // Nếu có returnUrl thì chuyển hướng đến đó
                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        
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
                        Session["AccountName"] = user.TenDangNhap;
                        Session["Password"] = user.MatKhauHash;
                        
                        // Merge giỏ hàng tạm từ sessionStorage (nếu có)
                        if (!string.IsNullOrEmpty(tempCart))
                        {
                            MergeCart(user.MaKhachHang, tempCart);
                        }
                        
                        // Nếu có returnUrl thì chuyển hướng đến đó
                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        
                        // Chuyển hướng đến trang Dashboard
                        return RedirectToAction("Index", "Home");
                    }
                }
            }
            TempData["Error"] = "Tên đăng nhập hoặc mật khẩu không đúng";
            ViewBag.ReturnUrl = returnUrl; // Lưu lại returnUrl để nếu đăng nhập không thành công
            ViewBag.TempCart = tempCart; // Lưu lại giỏ hàng tạm
            return View();
        }
        #endregion
        
        #region Cart
        // Tạo giỏ hàng mới nếu khách hàng chưa có
        private GioHang GetOrCreateCart(string maKhachHang)
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
        
        // Merge giỏ hàng từ localStorage vào giỏ hàng trong CSDL
        private void MergeCart(string maKhachHang, string tempCartJson)
        {
            try
            {
                // Chuyển đổi chuỗi JSON thành danh sách đối tượng giỏ hàng
                var serializer = new JavaScriptSerializer();
                var tempCartItems = serializer.Deserialize<List<CartItemModel>>(tempCartJson);
                
                if (tempCartItems == null || tempCartItems.Count == 0)
                    return;
                
                // Lấy hoặc tạo giỏ hàng cho khách hàng
                var gioHang = GetOrCreateCart(maKhachHang);
                
                // Lấy danh sách sản phẩm hiện có trong giỏ hàng CSDL
                var existingItems = data.ChiTietGioHangs.Where(ct => ct.GioHang.MaKhachHang == maKhachHang).ToList();
                
                foreach (var item in tempCartItems)
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
                
                // Cập nhật thời gian của giỏ hàng
                gioHang.NgayCapNhat = DateTime.Now;
                
                // Lưu thay đổi vào CSDL
                data.SubmitChanges();
            }
            catch (Exception ex)
            {
                // Log exception
                System.Diagnostics.Debug.WriteLine($"Error merging cart: {ex.Message}");
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
                return View();
            }
            
            // Nếu chưa đăng nhập, chuyển hướng đến trang đăng nhập với returnUrl là PreCart
            return RedirectToAction("Login", new { returnUrl = Url.Action("PreCart", "InnerPage") });
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
                        int number = rand.Next(0, 100000); // 0 -> 99999
                        code = "DH" + DateTime.Now.ToString("yyMMdd") + number.ToString("D5"); // Ví dụ: DH2305250001
                    } while (data.DonHangs.Any(hd => hd.MaDonHang == code));
                    return code;
                }
                
                // Tạo mã thanh toán mới
                string GenerateUniquePaymentId()
                {
                    Random rand = new Random();
                    string code;
                    do
                    {
                        int number = rand.Next(0, 100000); // 0 -> 99999
                        code = "TT" + DateTime.Now.ToString("yyMMdd") + number.ToString("D5"); // Ví dụ: TT2305250001
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
                        int number = rand.Next(0, 100000); // 0 -> 99999
                        code = "GH" + DateTime.Now.ToString("yyMMdd") + number.ToString("D5"); // Ví dụ: GH2305250001
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
                
                // Cập nhật tổng tiền đơn hàng
                donHang.TongTien = Convert.ToSingle(tongTien);
                
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
                    MaDonHang = donHang.MaDonHang,
                    MaDiaChi = model.maDiaChi,
                    MaVanDon = GenerateUniqueTrackingId(),
                    TrangThaiGiaoHang = "ChuanBiHang"
                };
                
                data.GiaoHangs.InsertOnSubmit(giaoHang);
                
                // Lưu thay đổi vào CSDL
                data.SubmitChanges();
                
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
            
            // Lấy danh sách sản phẩm trong đơn hàng
            var chiTietDonHang = data.ChiTietDonHangs
                .Where(ct => ct.MaDonHang == id)
                .Select(ct => new
                {
                    ct.MaBienThe,
                    ct.SoLuong,
                    ct.DonGia,
                    BienThe = data.BienTheHangHoas.FirstOrDefault(bt => bt.MaBienThe == ct.MaBienThe),
                    HangHoa = data.HangHoas.FirstOrDefault(hh => hh.MaHangHoa == data.BienTheHangHoas.FirstOrDefault(bt => bt.MaBienThe == ct.MaBienThe).MaHangHoa)
                })
                .ToList();
            
            ViewBag.DonHang = donHang;
            ViewBag.ThanhToan = thanhToan;
            ViewBag.GiaoHang = giaoHang;
            ViewBag.DiaChi = diaChi;
            ViewBag.ChiTietDonHang = chiTietDonHang;
            
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


            // Lưu thông tin người dùng vào Session
            Session["UserID"] = newUser.MaKhachHang;
            Session["UserName"] = newUser.HoTen;
            Session["AccountName"] = newUser.TenDangNhap;
            Session["Password"] = newUser.MatKhauHash;
            
            // Merge giỏ hàng tạm từ sessionStorage (nếu có)
            if (!string.IsNullOrEmpty(tempCart))
            {
                MergeCart(newUser.MaKhachHang, tempCart);
            }
            
            // Chuyển hướng đến trang Dashboard
            return RedirectToAction("Index", "Home");
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

            const string subject = "Mã OTP đăng ký tài khoản";
            string body = $@"Quý khách  thân mến,

            Có vẻ như Quý khách đang đăng nhập vào tài khoản Swootechsmart bằng một thiết bị mới. Mã xác thực OTP của Quý khách là {otp}.

            Mã xác thực này sẽ hết hiệu lực trong 2 phút.

            Để đảm bảo an toàn, vui lòng không chia sẻ mã này cho bất cứ ai.

            Cảm ơn Quý khách đã lựa chọn Swootechsmart.

            Trân trọng,";

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
                    Body = body
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
    }
    
    // Model cho sản phẩm trong đơn hàng
    public class OrderItemModel
    {
        public string maBienThe { get; set; }
        public int soLuong { get; set; }
    }
}
