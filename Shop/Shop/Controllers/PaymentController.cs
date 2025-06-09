using PayPal.Api;
using Shop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace Shop.Controllers
{
    public class PaymentController : Controller
    {
    SHOPDataContext data = new SHOPDataContext("Data Source=MSI;Initial Catalog=CuaHang2;Persist Security Info=True;Use" +
                "r ID=sa;Password=123;Encrypt=True;TrustServerCertificate=True");
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

                // Tạo nội dung email
                var fromAddress = new MailAddress(_emailAddress, _emailDisplayName);
                var toAddress = new MailAddress(khachHang.Email, khachHang.HoTen);
                const string subject = "Xác nhận đơn hàng thành công - PrimeTech";

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
                var body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <h2 style='color: #28a745;'>Xác nhận đơn hàng thành công</h2>
                        <p>Xin chào {khachHang.HoTen},</p>
                        <p>Cảm ơn bạn đã đặt hàng tại PrimeTech. Dưới đây là thông tin chi tiết đơn hàng của bạn:</p>
                        
                        <div style='margin: 20px 0;'>
                            <p><strong>Mã đơn hàng:</strong> {donHang.MaDonHang}</p>
                            <p><strong>Ngày đặt hàng:</strong> {donHang.NgayTao:dd/MM/yyyy HH:mm}</p>
                            <p><strong>Phương thức thanh toán:</strong> Đã thanh toán qua PayPal</p>
                        </div>

                        <h3 style='color: #333;'>Chi tiết đơn hàng:</h3>
                        {productTable}

                        <div style='margin: 20px 0; text-align: right;'>
                            <p><strong>Tổng tiền hàng:</strong> {tongTienHang:N0} ₫</p>
                            <p><strong>Thuế (8%):</strong> {thue:N0} ₫</p>
                            <p style='font-size: 18px; color: #28a745;'><strong>Tổng thanh toán:</strong> {tongThanhToan:N0} ₫</p>
                        </div>

                        <p>Chúng tôi sẽ xử lý đơn hàng của bạn trong thời gian sớm nhất.</p>
                        <p>Nếu bạn có bất kỳ thắc mắc nào, vui lòng liên hệ với chúng tôi qua email hoặc số điện thoại hỗ trợ.</p>
                        
                        <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #dee2e6;'>
                            <p style='color: #666;'>Trân trọng,<br>PrimeTech Team</p>
                        </div>
                    </div>";

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
        // Hàm tạo mã đơn hàng, thanh toán, v.v.
        string GenerateUniqueOrderId()
        {
            Random rand = new Random();
            string code;
            do
            {
                int number = rand.Next(0, 100000);
                code = "DH" + DateTime.Now.ToString("yyMMdd") + number.ToString("D5");
            } while (data.DonHangs.Any(hd => hd.MaDonHang == code));
            return code;
        }

        string GenerateUniquePaymentId()
        {
            Random rand = new Random();
            string code;
            do
            {
                int number = rand.Next(0, 100000);
                code = "TT" + DateTime.Now.ToString("yyMMdd") + number.ToString("D5");
            } while (data.ThanhToans.Any(tt => tt.MaThanhToan == code));
            return code;
        }

        string GenerateUniqueShippingId()
        {
            Random rand = new Random();
            string code;
            do
            {
                int number = rand.Next(0, 100000);
                code = "GH" + DateTime.Now.ToString("yyMMdd") + number.ToString("D5");
            } while (data.GiaoHangs.Any(gh => gh.MaGiaoHang == code));
            return code;
        }

        string GenerateUniqueTrackingId()
        {
            Random rand = new Random();
            string code;
            int number = rand.Next(100000000, 999999999);
            code = "VD" + number.ToString();
            return code;
        }

        string GenerateUniqueOrderDetailId()
        {
            Random rand = new Random();
            string code;
            do
            {
                int number = rand.Next(0, 100000);
                code = "CTDH" + DateTime.Now.ToString("yyMMdd") + number.ToString("D5");
            } while (data.ChiTietDonHangs.Any(ct => ct.MaChiTietDonHang == code));
            return code;
        }

        #region Paypal
        // Khởi tạo thanh toán PayPal
        [HttpPost]
        public ActionResult PayWithPaypal(OrderModel model)
        {
            if (Session["UserID"] == null)
            {
                return Json(new { success = false, message = "Bạn chưa đăng nhập." });
            }

            try
            {
                // Lấy thông tin sản phẩm để tính tổng tiền
                double tongTien = 0;
                List<OrderItem> orderItems = new List<OrderItem>();

                foreach (var item in model.sanPham)
                {
                    var bienThe = data.BienTheHangHoas.FirstOrDefault(b => b.MaBienThe == item.maBienThe);
                    if (bienThe == null)
                    {
                        return Json(new { success = false, message = $"Sản phẩm không tồn tại: {item.maBienThe}" });
                    }

                    var hangHoa = data.HangHoas.FirstOrDefault(h => h.MaHangHoa == bienThe.MaHangHoa);
                    
                    double donGia = bienThe.GiaKhuyenMai ?? bienThe.GiaBan ?? 0;
                    tongTien += donGia * item.soLuong;
                    
                    orderItems.Add(new OrderItem {
                        Name = hangHoa?.TenHangHoa ?? "Sản phẩm",
                        Quantity = item.soLuong,
                        Price = donGia,
                        Sku = bienThe.MaBienThe
                    });
                }

                // Tính thuế và phí vận chuyển
                double thue = tongTien * 0.08;
                double phiVanChuyen = 0; // Đặt phí vận chuyển bằng 0
                double tongThanhToan = tongTien + thue + phiVanChuyen;

                // Xử lý voucher nếu có
                if (!string.IsNullOrWhiteSpace(model.maVoucherCode))
                {
                    var voucher = data.Vouchers.FirstOrDefault(v => v.MaVoucherCode == model.maVoucherCode);
                    if (voucher != null)
                    {
                        // Áp dụng giảm giá
                        double soTienGiam = 0;
                        if (voucher.LoaiGiamGia == "TienMat")
                        {
                            // Giảm tiền mặt
                            soTienGiam = (double)voucher.GiaTriGiamGia;
                        }
                        else if (voucher.LoaiGiamGia == "PhanTram")
                        {
                            // Giảm phần trăm (áp dụng trên tổng đã bao gồm thuế)
                            soTienGiam = tongThanhToan * ((double)voucher.GiaTriGiamGia / 100);
                        }

                        // Đảm bảo số tiền giảm không vượt quá tổng tiền
                        if (soTienGiam > tongThanhToan)
                        {
                            soTienGiam = tongThanhToan;
                        }

                        // Trừ tiền giảm giá
                        tongThanhToan -= soTienGiam;
                    }
                }

                // Chuyển đổi từ VND sang USD (tỷ giá 1 USD = 23000 VND)
                double usdExchangeRate = 26000;
                double tongTienUSD = Math.Round(tongThanhToan / usdExchangeRate, 2);

                // Lưu thông tin đơn hàng vào Session để sử dụng sau khi thanh toán
                Session["PayPalOrderModel"] = model;

                // Khởi tạo thanh toán PayPal
                var apiContext = PaypalConfiguration.GetAPIContext();

                // Thiết lập thông tin thanh toán
                var itemList = new ItemList()
                {
                    items = new List<Item>()
                };

                // Thêm các sản phẩm vào danh sách
                foreach (var orderItem in orderItems)
                {
                    // Tính giá bằng USD cho mỗi sản phẩm
                    double priceInUSD = Math.Round(orderItem.Price / usdExchangeRate, 2);
                    
                    itemList.items.Add(new Item()
                    {
                        name = orderItem.Name.Length > 120 ? orderItem.Name.Substring(0, 120) : orderItem.Name, // PayPal giới hạn 127 ký tự
                        currency = "USD",
                        price = priceInUSD.ToString("0.00"), // Định dạng theo yêu cầu của PayPal (2 số thập phân)
                        quantity = orderItem.Quantity.ToString(),
                        sku = orderItem.Sku.Length > 50 ? orderItem.Sku.Substring(0, 50) : orderItem.Sku // PayPal giới hạn 50 ký tự
                    });
                }

                // Thêm item giảm giá nếu có voucher
                if (!string.IsNullOrWhiteSpace(model.maVoucherCode))
                {
                    var voucher = data.Vouchers.FirstOrDefault(v => v.MaVoucherCode == model.maVoucherCode);
                    if (voucher != null)
                    {
                        double soTienGiam = 0;
                        if (voucher.LoaiGiamGia == "TienMat")
                        {
                            soTienGiam = (double)voucher.GiaTriGiamGia;
                        }
                        else if (voucher.LoaiGiamGia == "PhanTram")
                        {
                            // Tính lại tổng tiền đã bao gồm thuế
                            double tongTienTruocGiam = tongTien + thue + phiVanChuyen;
                            soTienGiam = tongTienTruocGiam * ((double)voucher.GiaTriGiamGia / 100);
                        }

                        if (soTienGiam > tongThanhToan)
                        {
                            soTienGiam = tongThanhToan;
                        }

                        if (soTienGiam > 0)
                        {
                            itemList.items.Add(new Item()
                            {
                                name = "Voucher giảm giá",
                                currency = "USD",
                                price = (-Math.Round(soTienGiam / usdExchangeRate, 2)).ToString("0.00"),
                                quantity = "1",
                                sku = "DISCOUNT"
                            });
                        }
                    }
                }

                // Thiết lập thông tin giao dịch
                var payer = new Payer() { payment_method = "paypal" };

                var redirUrls = new RedirectUrls()
                {
                    cancel_url = Url.Action("PaymentCancelled", "Payment", null, Request.Url.Scheme),
                    return_url = Url.Action("PaymentSuccess", "Payment", null, Request.Url.Scheme)
                };

                // Tính tổng tiền từ các sản phẩm để đảm bảo tổng chính xác
                double calculatedTotal = 0;
                foreach (var item in itemList.items)
                {
                    calculatedTotal += double.Parse(item.price) * int.Parse(item.quantity);
                }
                
                // Thêm thuế và phí vận chuyển vào USD
                double taxUSD = Math.Round(thue / usdExchangeRate, 2);
                double shippingUSD = Math.Round(phiVanChuyen / usdExchangeRate, 2);
                
                // Đảm bảo tổng chính xác sau khi làm tròn
                double finalTotal = calculatedTotal + taxUSD + shippingUSD;
                finalTotal = Math.Round(finalTotal, 2);

                // Chuẩn bị các thành phần của giao dịch
                var details = new Details()
                {
                    tax = taxUSD.ToString("0.00"),
                    shipping = shippingUSD.ToString("0.00"),
                    subtotal = calculatedTotal.ToString("0.00")
                };

                var amount = new Amount()
                {
                    currency = "USD",
                    total = finalTotal.ToString("0.00"), // Định dạng số thập phân theo chuẩn của PayPal
                    details = details
                };

                var transactionList = new List<Transaction>()
                {
                    new Transaction()
                    {
                        description = "Thanh toán đơn hàng",
                        invoice_number = DateTime.Now.Ticks.ToString(),
                        amount = amount,
                        item_list = itemList
                    }
                };

                var payment = new Payment()
                {
                    intent = "sale",
                    payer = payer,
                    transactions = transactionList,
                    redirect_urls = redirUrls
                };

                // Tạo thanh toán
                var createdPayment = payment.Create(apiContext);

                // Lấy URL để chuyển hướng đến trang thanh toán PayPal
                var links = createdPayment.links.GetEnumerator();
                string paypalRedirectUrl = null;

                while (links.MoveNext())
                {
                    var link = links.Current;
                    if (link.rel.ToLower().Trim().Equals("approval_url"))
                    {
                        paypalRedirectUrl = link.href;
                    }
                }

                // Lưu ID của thanh toán PayPal vào Session
                Session["PaymentId"] = createdPayment.id;

                return Json(new { success = true, redirectUrl = paypalRedirectUrl });
            }
            catch (Exception ex)
            {
                // Ghi log chi tiết hơn cho lỗi
                System.Diagnostics.Debug.WriteLine("PayPal Error: " + ex.ToString());
                
                // Xử lý trường hợp lỗi cụ thể
                return Json(new { success = false, message = "Lỗi khởi tạo thanh toán: " + ex.Message });
            }
        }

        // Xử lý khi thanh toán thành công
        public ActionResult PaymentSuccess()
        {
            // Lấy thông tin thanh toán từ URL
            var paymentId = Request.Params["paymentId"];
            var payerId = Request.Params["PayerID"];

            // Kiểm tra xem có ID thanh toán không
            if (string.IsNullOrEmpty(paymentId) || string.IsNullOrEmpty(payerId))
            {
                return RedirectToAction("Index", "Home");
            }

            try
            {
                // Lấy thông tin đơn hàng từ Session
                var orderModel = Session["PayPalOrderModel"] as OrderModel;
                if (orderModel == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                // Xác nhận thanh toán
                var apiContext = PaypalConfiguration.GetAPIContext();
                var paymentExecution = new PaymentExecution() { payer_id = payerId };
                var payment = new Payment() { id = paymentId };
                var executedPayment = payment.Execute(apiContext, paymentExecution);

                if (executedPayment.state.ToLower() == "approved")
                {
                    // Tạo đơn hàng
                    string maKhachHang = Session["UserID"].ToString();

                   
                    // Tạo đơn hàng mới
                    var donHang = new DonHang
                    {
                        MaDonHang = GenerateUniqueOrderId(),
                        MaKhachHang = maKhachHang,
                        TongTien = 0, // Sẽ được cập nhật sau
                        TrangThaiThanhToan = "DaThanhToan", // Đã thanh toán qua PayPal
                        TrangThaiDonHang = "DangXuLy",
                        NgayTao = DateTime.Now
                    };

                    data.DonHangs.InsertOnSubmit(donHang);

                    // Tính tổng tiền
                    decimal tongTien = 0;

                    // Lấy giỏ hàng của người dùng
                    var gioHang = data.GioHangs.FirstOrDefault(g => g.MaKhachHang == maKhachHang);

                    // Thêm chi tiết đơn hàng
                    foreach (var item in orderModel.sanPham)
                    {
                        var bienThe = data.BienTheHangHoas.FirstOrDefault(b => b.MaBienThe == item.maBienThe);
                        if (bienThe != null)
                        {
                            // Tạo chi tiết đơn hàng
                            var chiTietDonHang = new ChiTietDonHang
                            {
                                MaChiTietDonHang = GenerateUniqueOrderDetailId(),
                                MaDonHang = donHang.MaDonHang,
                                MaBienThe = item.maBienThe,
                                SoLuong = item.soLuong,
                                DonGia = (double)(bienThe.GiaKhuyenMai ?? bienThe.GiaBan ?? 0)
                            };

                            data.ChiTietDonHangs.InsertOnSubmit(chiTietDonHang);

                            // Cập nhật tồn kho
                            bienThe.SoLuongTonKho -= item.soLuong;

                            // Cập nhật tổng tiền
                            decimal donGia = (decimal)(bienThe.GiaKhuyenMai ?? bienThe.GiaBan ?? 0);
                            tongTien += donGia * item.soLuong;

                            // Xóa sản phẩm khỏi giỏ hàng trong database
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
                    }

                    // Tính thuế (phí vận chuyển = 0)
                    decimal thue = tongTien * 0.08m;
                    decimal phiVanChuyen = 0;
                    decimal tongThanhToan = tongTien + thue + phiVanChuyen;

                    // Xử lý voucher nếu có
                    if (!string.IsNullOrWhiteSpace(orderModel.maVoucherCode))
                    {
                        var voucher = data.Vouchers.FirstOrDefault(v => v.MaVoucherCode == orderModel.maVoucherCode);
                        if (voucher != null)
                        {
                            // Áp dụng giảm giá
                            decimal soTienGiam = 0;
                            if (voucher.LoaiGiamGia == "TienMat")
                            {
                                // Giảm tiền mặt
                                soTienGiam = (decimal)voucher.GiaTriGiamGia;
                            }
                            else if (voucher.LoaiGiamGia == "PhanTram")
                            {
                                // Giảm phần trăm (áp dụng trên tổng đã bao gồm thuế)
                                soTienGiam = tongThanhToan * ((decimal)voucher.GiaTriGiamGia / 100);
                            }

                            // Đảm bảo số tiền giảm không vượt quá tổng tiền
                            if (soTienGiam > tongThanhToan)
                            {
                                soTienGiam = tongThanhToan;
                            }

                            // Trừ tiền giảm giá
                            tongThanhToan -= soTienGiam;

                            // Cập nhật voucher vào đơn hàng
                            donHang.MaVoucher = voucher.MaVoucher;
                            voucher.SoLuongDaDung += 1;

                            // Kiểm tra và cập nhật trạng thái phân phối voucher
                            var phanPhoi = data.PhanPhoiVouchers
                                .FirstOrDefault(pv => pv.MaVoucher == voucher.MaVoucher && pv.MaKhachHang == maKhachHang);

                            if (phanPhoi != null)
                            {
                                phanPhoi.DaSuDung = true;
                                phanPhoi.NgaySuDung = DateTime.Now;
                            }
                            else
                            {
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

                    // Cập nhật tổng tiền đơn hàng
                    donHang.TongTien = (double)tongThanhToan;

                    // Tạo thanh toán
                    var thanhToan = new ThanhToan
                    {
                        MaThanhToan = GenerateUniquePaymentId(),
                        MaDonHang = donHang.MaDonHang,
                        PhuongThucThanhToan = "TheTinDungVNPAY", // PayPal được xem là thanh toán bằng thẻ tín dụng
                        MaGiaoDich = paymentId, // Lưu ID giao dịch PayPal
                        TrangThai = "ThanhCong", // Đã thanh toán thành công
                        NgayThanhToan = DateTime.Now
                    };

                    data.ThanhToans.InsertOnSubmit(thanhToan);

                    // Tạo giao hàng
                    var giaoHang = new GiaoHang
                    {
                        MaGiaoHang = GenerateUniqueShippingId(),
                        DonViVanChuyen = "J&T Express",
                        MaDonHang = donHang.MaDonHang,
                        MaDiaChi = orderModel.maDiaChi,
                        MaVanDon = GenerateUniqueTrackingId(),
                        TrangThaiGiaoHang = "ChuanBiHang"
                    };

                    data.GiaoHangs.InsertOnSubmit(giaoHang);

                    // Lưu thay đổi vào CSDL
                    data.SubmitChanges();

                    // Xóa thông tin thanh toán từ Session
                    Session.Remove("PayPalOrderModel");
                    Session.Remove("PaymentId");

                    // Đặt TempData để xóa giỏ hàng trên trang thành công
                    TempData["ClearCart"] = true;

                    // Sau khi tạo đơn hàng thành công và trước khi return
                    SendOrderConfirmationEmail(donHang.MaDonHang);

                    // Chuyển hướng đến trang thông báo thành công
                    return RedirectToAction("OrderSuccess", "InnerPage", new { id = donHang.MaDonHang });
                }
                else
                {
                    // Nếu thanh toán không thành công
                    return RedirectToAction("PaymentFailed");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Đã xảy ra lỗi: " + ex.Message;
                return View("PaymentFailed");
            }
        }

        // Xử lý khi hủy thanh toán
        public ActionResult PaymentCancelled()
        {
            // Xóa thông tin thanh toán từ Session
            Session.Remove("PayPalOrderModel");
            Session.Remove("PaymentId");

            return RedirectToAction("PreCart", "InnerPage");
        }

        // Xử lý khi thanh toán thất bại
        public ActionResult PaymentFailed()
        {
            // Xóa thông tin thanh toán từ Session
            Session.Remove("PayPalOrderModel");
            Session.Remove("PaymentId");

            ViewBag.Error = "Thanh toán không thành công. Vui lòng thử lại sau.";
            return View();
        }
        #endregion

        #region VNPay
        public ActionResult CreatePaymentUrl(OrderModel model)
        {
            if (Session["UserID"] == null)
            {
                return Json(new { success = false, message = "Bạn chưa đăng nhập." }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                // Kiểm tra dữ liệu đầu vào
                if (model == null || model.sanPham == null || !model.sanPham.Any())
                {
                    return Json(new { success = false, message = "Danh sách sản phẩm không hợp lệ." }, JsonRequestBehavior.AllowGet);
                }

                // Tính tổng tiền sản phẩm
                double tongTien = 0;
                foreach (var item in model.sanPham)
                {
                    var bienThe = data.BienTheHangHoas.FirstOrDefault(b => b.MaBienThe == item.maBienThe);
                    if (bienThe == null)
                    {
                        return Json(new { success = false, message = $"Sản phẩm không tồn tại: {item.maBienThe}" }, JsonRequestBehavior.AllowGet);
                    }

                    if (item.soLuong <= 0)
                    {
                        return Json(new { success = false, message = $"Số lượng không hợp lệ cho sản phẩm: {item.maBienThe}" }, JsonRequestBehavior.AllowGet);
                    }

                    double donGia = bienThe.GiaKhuyenMai ?? bienThe.GiaBan ?? 0;
                    tongTien += donGia * item.soLuong;
                }

                // Tính thuế và phí vận chuyển
                double thue = tongTien * 0.08;
                double phiVanChuyen = 0; // Có thể cấu hình động
                double tongThanhToan = tongTien + thue + phiVanChuyen;

                // Áp dụng voucher nếu có
                if (!string.IsNullOrWhiteSpace(model.maVoucherCode))
                {
                    var voucher = data.Vouchers.FirstOrDefault(v => v.MaVoucherCode == model.maVoucherCode && v.SoLuongDaDung < v.SoLuong && v.NgayKetThuc >= DateTime.Now);
                    if (voucher != null)
                    {
                        double soTienGiam = 0;
                        if (voucher.LoaiGiamGia == "TienMat")
                        {
                            soTienGiam = (double)voucher.GiaTriGiamGia;
                        }
                        else if (voucher.LoaiGiamGia == "PhanTram")
                        {
                            soTienGiam = tongThanhToan * ((double)voucher.GiaTriGiamGia / 100);
                        }

                        if (soTienGiam > tongThanhToan)
                        {
                            soTienGiam = tongThanhToan;
                        }

                        tongThanhToan -= soTienGiam;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Invalid or expired voucher: {model.maVoucherCode}");
                    }
                }

                // Lưu đơn hàng vào Session
                Session["VnPayOrderModel"] = model;

                // Tạo URL thanh toán VNPAY
                //string vnp_Returnurl = Url.Action("PaymentCallbackVnpay", "Payment", null, Request.Url.Scheme);
                //string vnp_Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
                //string vnp_TmnCode = "UTUJQSUQ";
                //string vnp_HashSecret = "4EWP3FZDFGBMGMRH5W9LPWCCXO1YXX57"; // Lưu ý: Lưu trong cấu hình thực tế

                string vnp_TmnCode = ConfigurationManager.AppSettings["Vnp_TmnCode"];
                string vnp_HashSecret = ConfigurationManager.AppSettings["Vnp_HashSecret"];
                string vnp_Url = ConfigurationManager.AppSettings["Vnp_Url"];
                string vnp_Returnurl = Url.Action("PaymentCallbackVnpay", "Payment", null, Request.Url.Scheme);
                VnPayLibrary vnpay = new VnPayLibrary();

                // Thêm các tham số bắt buộc theo tài liệu VNPay 2.1.0
                vnpay.AddRequestData("vnp_Version", "2.1.0");
                vnpay.AddRequestData("vnp_Command", "pay");
                vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
                vnpay.AddRequestData("vnp_Amount", ((long)(tongThanhToan * 100)).ToString());
                vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
                vnpay.AddRequestData("vnp_CurrCode", "VND");

                // Lấy địa chỉ IP
                string ipAddress = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                if (string.IsNullOrEmpty(ipAddress))
                {
                    ipAddress = Request.ServerVariables["REMOTE_ADDR"];
                }
                if (string.IsNullOrEmpty(ipAddress))
                {
                    ipAddress = "127.0.0.1"; // Giá trị mặc định nếu không lấy được IP
                }
                vnpay.AddRequestData("vnp_IpAddr", ipAddress);

                vnpay.AddRequestData("vnp_Locale", "vn");
                vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang #{DateTime.Now.Ticks}");
                vnpay.AddRequestData("vnp_OrderType", "other");
                vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
                vnpay.AddRequestData("vnp_TxnRef", DateTime.Now.Ticks.ToString());

                // Ghi log tham số để debug
                var requestDataLog = string.Join("&", vnpay.GetRequestData().Select(kvp => $"{kvp.Key}={kvp.Value}"));
                System.Diagnostics.Debug.WriteLine("VNPay Request Data: " + requestDataLog);

                // Tạo URL thanh toán
                string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);

                return Json(new { success = true, redirectUrl = paymentUrl }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VNPAY Error: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Lỗi tạo thanh toán VNPAY: {ex.Message}" }, JsonRequestBehavior.AllowGet);
            }
        }
    
        public ActionResult PaymentCallbackVnpay()
            {
                var vnpay = new VnPayLibrary();

                // Lấy tất cả tham số từ QueryString
                foreach (string key in Request.QueryString.AllKeys)
                {
                    if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase))
                    {
                        vnpay.AddResponseData(key, Request.QueryString[key]);
                    }
                }

                // Lấy địa chỉ IP
                string ipAddress = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                if (string.IsNullOrEmpty(ipAddress))
                {
                    ipAddress = Request.ServerVariables["REMOTE_ADDR"];
                }
                Console.WriteLine("IP Address: " + ipAddress); // Ghi log IP để debug

                // Kiểm tra chữ ký
                string vnp_HashSecret = "4EWP3FZDFGBMGMRH5W9LPWCCXO1YXX57"; // Thay bằng secret key từ VNPay
                bool checkSignature = vnpay.ValidateSignature(vnp_HashSecret);

                if (!checkSignature)
                {
                    Console.WriteLine("Signature validation failed.");
                    ViewBag.Message = "Sai chữ ký (có thể bị giả mạo dữ liệu)";
                    return View("PaymentFailed");
                }

                // Kiểm tra mã phản hồi
                string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                if (vnp_ResponseCode != "00")
                {
                    Console.WriteLine($"Payment error: ResponseCode = {vnp_ResponseCode}");
                    ViewBag.Message = $"Lỗi thanh toán: Mã {vnp_ResponseCode}";
                    return View("PaymentFailed");
                }

                try
                {
                    // Lấy thông tin đơn hàng từ Session
                    var orderModel = Session["VnPayOrderModel"] as OrderModel;
                    if (orderModel == null)
                    {
                        Console.WriteLine("OrderModel not found in Session.");
                        return RedirectToAction("Index", "Home");
                    }

                    string maKhachHang = Session["UserID"]?.ToString();
                    if (string.IsNullOrEmpty(maKhachHang))
                    {
                        Console.WriteLine("UserID not found in Session.");
                        return RedirectToAction("Index", "Home");
                    }

                    // Tạo mã duy nhất cho đơn hàng
                    string maDonHang = GenerateUniqueOrderId();
                    string maThanhToan = GenerateUniquePaymentId();
                    string maGiaoHang = GenerateUniqueShippingId();
                    string maVanDon = GenerateUniqueTrackingId();

                    // Tạo đối tượng đơn hàng
                    var donHang = new DonHang
                    {
                        MaDonHang = maDonHang,
                        MaKhachHang = maKhachHang,
                        TongTien = 0,
                        TrangThaiThanhToan = "DaThanhToan",
                        TrangThaiDonHang = "DangXuLy",
                        NgayTao = DateTime.Now
                    };

                    data.DonHangs.InsertOnSubmit(donHang);

                    decimal tongTien = 0;
                    var gioHang = data.GioHangs.FirstOrDefault(g => g.MaKhachHang == maKhachHang);

                    foreach (var item in orderModel.sanPham)
                    {
                        var bienThe = data.BienTheHangHoas.FirstOrDefault(b => b.MaBienThe == item.maBienThe);
                        if (bienThe == null)
                        {
                            Console.WriteLine($"Variant not found: MaBienThe = {item.maBienThe}");
                            continue; // Bỏ qua nếu không tìm thấy biến thể
                        }

                        var chiTietDonHang = new ChiTietDonHang
                        {
                            MaChiTietDonHang = GenerateUniqueOrderDetailId(),
                            MaDonHang = maDonHang,
                            MaBienThe = item.maBienThe,
                            SoLuong = item.soLuong,
                            DonGia = (double)(bienThe.GiaKhuyenMai ?? bienThe.GiaBan ?? 0)
                        };
                        data.ChiTietDonHangs.InsertOnSubmit(chiTietDonHang);

                        bienThe.SoLuongTonKho -= item.soLuong;
                        decimal donGia = (decimal)(bienThe.GiaKhuyenMai ?? bienThe.GiaBan ?? 0);
                        tongTien += donGia * item.soLuong;

                        // Xóa khỏi giỏ hàng
                        if (gioHang != null)
                        {
                            var ctgh = data.ChiTietGioHangs
                                .FirstOrDefault(ct => ct.MaGioHang == gioHang.MaGioHang && ct.MaBienThe == item.maBienThe);
                            if (ctgh != null)
                            {
                                data.ChiTietGioHangs.DeleteOnSubmit(ctgh);
                            }
                        }
                    }

                    // Tính thuế và tổng thanh toán
                    decimal thue = tongTien * 0.08m;
                    decimal tongThanhToan = tongTien + thue;

                    // Áp dụng voucher
                    if (!string.IsNullOrWhiteSpace(orderModel.maVoucherCode))
                    {
                        var voucher = data.Vouchers.FirstOrDefault(v => v.MaVoucherCode == orderModel.maVoucherCode);
                        if (voucher != null)
                        {
                            decimal soTienGiam = voucher.LoaiGiamGia == "TienMat"
                                ? (decimal)voucher.GiaTriGiamGia
                                : tongThanhToan * ((decimal)voucher.GiaTriGiamGia / 100);

                            if (soTienGiam > tongThanhToan) soTienGiam = tongThanhToan;
                            tongThanhToan -= soTienGiam;

                            donHang.MaVoucher = voucher.MaVoucher;
                            voucher.SoLuongDaDung += 1;

                            var phanPhoi = data.PhanPhoiVouchers
                                .FirstOrDefault(p => p.MaVoucher == voucher.MaVoucher && p.MaKhachHang == maKhachHang);

                            if (phanPhoi != null)
                            {
                                phanPhoi.DaSuDung = true;
                                phanPhoi.NgaySuDung = DateTime.Now;
                            }
                            else
                            {
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
                        else
                        {
                            Console.WriteLine($"Voucher not found: MaVoucherCode = {orderModel.maVoucherCode}");
                        }
                    }

                    donHang.TongTien = (double)tongThanhToan;

                    // Tạo thông tin thanh toán
                    var thanhToan = new ThanhToan
                    {
                        MaThanhToan = maThanhToan,
                        MaDonHang = maDonHang,
                        PhuongThucThanhToan = "TheTinDungPayPal",
                        MaGiaoDich = vnpay.GetResponseData("vnp_TransactionNo") ?? "N/A",
                        TrangThai = "ThanhCong",
                        NgayThanhToan = DateTime.Now
                    };

                    data.ThanhToans.InsertOnSubmit(thanhToan);

                    // Tạo thông tin giao hàng
                    var giaoHang = new GiaoHang
                    {
                        MaGiaoHang = maGiaoHang,
                        DonViVanChuyen = "J&T Express",
                        MaDonHang = maDonHang,
                        MaDiaChi = orderModel.maDiaChi,
                        MaVanDon = maVanDon,
                        TrangThaiGiaoHang = "ChuanBiHang"
                    };

                    data.GiaoHangs.InsertOnSubmit(giaoHang);

                    // Lưu tất cả thay đổi vào database
                    data.SubmitChanges();

                    // Xóa Session và đặt flag xóa giỏ hàng
                    Session.Remove("VnPayOrderModel");
                    TempData["ClearCart"] = true;

                    // Gửi email xác nhận
                    SendOrderConfirmationEmail(maDonHang);

                    return RedirectToAction("OrderSuccess", "InnerPage", new { id = maDonHang });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing order: {ex.Message}\nStackTrace: {ex.StackTrace}");
                    ViewBag.Error = $"Lỗi xử lý đơn hàng: {ex.Message}";
                    return View("PaymentFailed");
                }
            }

        #endregion
    }


    // Lớp hỗ trợ để quản lý thông tin sản phẩm thanh toán  
    public class OrderItem
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
        public double Price { get; set; }
        public string Sku { get; set; }
    }
} 