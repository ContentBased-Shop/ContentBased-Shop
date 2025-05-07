using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using Shop.Models;

namespace Shop.Controllers
{
    public class ProductController : Controller
    {
        SHOPDataContext data = new SHOPDataContext("Data Source=MSI;Initial Catalog=CuaHang2;Persist Security Info=True;Use" +
               "r ID=sa;Password=123;Encrypt=True;TrustServerCertificate=True");
        //
        // GET: /Product/

        public ActionResult Index()
        {
            return View();
        }
        #region ProductDetail
        public ActionResult ProductDetail(string id, string mabienthe)
        {
            var productList = (from hh in data.HangHoas
                               join bienThe in data.BienTheHangHoas on hh.MaHangHoa equals bienThe.MaHangHoa into bienTheGroup
                               join dg in data.DanhGias on hh.MaHangHoa equals dg.MaHangHoa into danhGiaGroup
                               join mota in data.MoTaChiTietHangHoas on hh.MaHangHoa equals mota.MaHangHoa into motaGroup
                               join dm in data.DanhMucs on hh.MaDanhMuc equals dm.MaDanhMuc into dmGroup
                               join th in data.ThuongHieus on hh.MaThuongHieu equals th.MaThuongHieu into thGroup
                               select new
                               {
                                   HangHoa = hh,
                                   BienThes = bienTheGroup,
                                   DanhGias = danhGiaGroup,
                                   MoTaChiTiet = motaGroup.FirstOrDefault(),
                                   DanhMuc = dmGroup.FirstOrDefault(),
                                   ThuongHieu = thGroup.FirstOrDefault()
                                          })
                 .ToList()
                  .Select(x => new ProductDetailView
                {
                    MaHangHoa = x.HangHoa.MaHangHoa,
                    TenHangHoa = x.HangHoa.TenHangHoa,
                    MoTa = x.HangHoa.MoTa,
                    MoTaDai = x.HangHoa.MoTaDai,
                    HinhAnh = x.HangHoa.HinhAnh,
                    NgayTao = x.HangHoa.NgayTao ?? DateTime.MinValue,
                   
                    // Gán toàn bộ danh sách biến thể từ bienTheGroup
                    BienThes = x.BienThes.ToList(),  // Đã lấy từ bienTheGroup rồi, không cần query lại

                    // Đánh giá 
                    SoLuongDanhGia = x.DanhGias.Count(),
                    DanhGiaTrungBinh = x.DanhGias.Any() ? x.DanhGias.Average(d => d.SoSao).GetValueOrDefault() : 0,
                    DanhGiasChiTiet = x.DanhGias
                                .Join(data.KhachHangs, d => d.MaKhachHang, k => k.MaKhachHang,
                                    (d, k) => new DanhGiaView
                                    {
                                        SoSao = d.SoSao ?? 0,
                                        BinhLuan = d.BinhLuan,
                                        NgayTao = d.NgayTao,
                                        HoTenKhachHang = k.HoTen
                                    }).ToList(),

                    ThongKeSoSao = x.DanhGias
                    .GroupBy(d => d.SoSao ?? 0)
                    .ToDictionary(g => g.Key, g => g.Count()),

                    // Thêm mô tả chi tiết
                    MoTaChiTiet = x.MoTaChiTiet?.NoiDung ?? "",
                    NgayCapNhat = x.MoTaChiTiet?.NgayCapNhat ?? DateTime.MinValue,
                    NgayTaoMoTaChiTiet = x.MoTaChiTiet?.NgayTao ?? DateTime.MinValue,
                    // Thêm tên danh mục
                    TenDanhMuc = x.DanhMuc?.TenDanhMuc ?? "",

                    // Thương hiệu
                    TenThuongHieu = x.ThuongHieu?.TenThuongHieu ?? "",
                    MoTaThuongHieu = x.ThuongHieu?.MoTa ?? "",

                })
                .ToList();
            // Tìm biến thể theo mã biến thể nếu có truyền
            var selectedBienThe = data.BienTheHangHoas.FirstOrDefault(bt => bt.MaBienThe == mabienthe)
                                  ?? data.BienTheHangHoas.FirstOrDefault(); // fallback nếu không có mã
            var product = productList.FirstOrDefault(p => p.MaHangHoa == id);
            if (product == null) return HttpNotFound();

            var viewModel = new ProductDetailPageView
            {
                SelectedBienThe = selectedBienThe,
                Product = product,
                RelatedProducts = productList.Where(p => p.MaHangHoa != id).ToList()
            };
            return View(viewModel);
        }

        #endregion
        #region ProductDienThoaiTabLet
        public ActionResult ProductDienThoaiTabLet()
        {
            return View();
        }
        #endregion
        #region ProductLapTopPC
        public ActionResult ProductLapTopPC()
        {
            return View();
        }
        #endregion
        #region Gaming
        public ActionResult Gaming()
        {
            return View();
        }
        #endregion
        #region AnotherProduct
        public ActionResult AnotherProduct()
        {
            return View();
        }
        #endregion
        #region PhuKien
        public ActionResult PhuKien()
        {
            return View();
        }
        #endregion
        public ActionResult ProductWishList()
        {
            var products = (from p in data.HangHoas
                            orderby p.TenHangHoa
                            select p).ToList(); // lấy hết, không lọc IsInStock

            return View(products);
        }

        public JsonResult SearchSuggest(string keyword)
        {
            var hangHoaFull = (from hh in data.HangHoas
                               join bt in data.BienTheHangHoas
                               on hh.MaHangHoa equals bt.MaHangHoa into btGroup
                               from bienThe in btGroup.DefaultIfEmpty()
                               where hh.TenHangHoa.Contains(keyword)  // Lọc theo từ khóa
                               group new { bienThe } by new
                               {
                                   hh.MaHangHoa,       // Mã hàng hóa
                                   hh.TenHangHoa,      // Tên hàng hóa
                                   hh.HinhAnh          // Hình ảnh
                               } into g

                               select new HangHoaViewModel
                               {
                                   MaHangHoa = g.Key.MaHangHoa,        // Mã hàng hóa
                                   TenHangHoa = g.Key.TenHangHoa,      // Tên hàng hóa
                                   HinhAnh = g.Key.HinhAnh,            // Hình ảnh
                                   GiaGoc = g.Where(x => x.bienThe != null)
                                              .Select(x => x.bienThe.GiaGoc)
                                              .FirstOrDefault() ?? 0,    // Giá gốc
                                   GiaKhuyenMai = g.Where(x => x.bienThe != null)
                                                    .Select(x => x.bienThe.GiaKhuyenMai)
                                                    .FirstOrDefault() ?? 0,   // Giá khuyến mãi
                               }).ToList();

            return Json(hangHoaFull, JsonRequestBehavior.AllowGet);
        }
    }
}
