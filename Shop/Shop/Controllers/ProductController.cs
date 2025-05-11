using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
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
        public ActionResult ProductSearch(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return View("ProductSearch", new List<HangHoaViewModel>());
            }

            var hangHoaFiltered = (from hh in data.HangHoas
                                   where hh.TenHangHoa.ToLower().Contains(keyword.ToLower())

                                   join bt in data.BienTheHangHoas on hh.MaHangHoa equals bt.MaHangHoa into btGroup
                                   from bienThe in btGroup.DefaultIfEmpty()

                                   join dg in data.DanhGias on hh.MaHangHoa equals dg.MaHangHoa into dgGroup
                                   from danhGia in dgGroup.DefaultIfEmpty()

                                   group new { bienThe, danhGia } by new
                                   {
                                       hh.MaHangHoa,
                                       hh.MoTaDai,
                                       hh.TenHangHoa,
                                       hh.HinhAnh,
                                       hh.MoTa,
                                       hh.NgayTao
                                   } into g

                                   select new HangHoaViewModel
                                   {
                                       MaHangHoa = g.Key.MaHangHoa,
                                       MoTaDai = g.Key.MoTaDai,
                                       TenHangHoa = g.Key.TenHangHoa,
                                       HinhAnh = g.Key.HinhAnh,
                                       MoTa = g.Key.MoTa,
                                       NgayTao = g.Key.NgayTao.Value,
                                       MaBienThe = g.Where(x => x.bienThe != null)
                                                 .Select(x => x.bienThe.MaBienThe)
                                                 .FirstOrDefault(),
                                       GiaGoc = g.Where(x => x.bienThe != null)
                                                  .Select(x => x.bienThe.GiaGoc)
                                                  .FirstOrDefault() ?? 0,
                                       GiaKhuyenMai = g.Where(x => x.bienThe != null)
                                                    .Select(x => x.bienThe.GiaKhuyenMai)
                                                    .FirstOrDefault() ?? 0,
                                       SoLuongTonKho = g.Where(x => x.bienThe != null)
                                                     .Select(x => x.bienThe.SoLuongTonKho)
                                                     .FirstOrDefault() ?? 0,
                                       SoLuongDanhGia = g.Count(x => x.danhGia != null),
                                       DanhGiaTrungBinh = g.Any(x => x.danhGia != null)
                                                   ? g.Average(x => (float?)x.danhGia.SoSao) ?? 0
                                                   : 0
                                   }).ToList();

            return View("ProductSearch", hangHoaFiltered);
        }


        #endregion
        #region ProductDetail
        public ActionResult ProductDetail(string id, string mabienthe)
        {
            // Hiện thị danh sách đã thêm vào ds yêu thích
            string maKhachHang = Session["UserID"] as string;
            List<string> dsYeuThich = new List<string>();

            if (!string.IsNullOrEmpty(maKhachHang))
            {
                dsYeuThich = data.YeuThiches
                                 .Where(y => y.MaKhachHang == maKhachHang)
                                 .Select(y => y.MaHangHoa)
                                 .ToList();
            }

            ViewBag.DanhSachYeuThich = dsYeuThich;
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
                    MaBienThe = x.BienThes.FirstOrDefault()?.MaBienThe,
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

            var selectedBienThe = (from bt in data.BienTheHangHoas
                                   where bt.MaBienThe == mabienthe && bt.MaHangHoa == id
                                   join ha in data.HinhAnhHangHoas
                                   on bt.MaBienThe equals ha.MaBienThe into hinhAnhGroup
                                   select new BienTheHangHoaViewModel
                                   {
                                       MaBienThe = bt.MaBienThe,
                                       MaHangHoa = bt.MaHangHoa,
                                       MauSac = bt.MauSac,
                                       DungLuong = bt.DungLuong,
                                       CPU = bt.CPU,
                                       RAM = bt.RAM,
                                       KichThuocManHinh = bt.KichThuocManHinh,
                                       LoaiBoNho = bt.LoaiBoNho,
                                       GiaGoc = bt.GiaGoc ?? 0,
                                       GiaKhuyenMai = bt.GiaKhuyenMai ?? 0,
                                       SoLuongTonKho = bt.SoLuongTonKho ?? 0,
                                       UrlAnh = hinhAnhGroup.Select(x => x.UrlAnh).ToList()
                                   }).FirstOrDefault();

            // lisst bien the

            var listBienThes = (from bt in data.BienTheHangHoas
                                where bt.MaHangHoa == id
                                join ha in data.HinhAnhHangHoas
                                on bt.MaBienThe equals ha.MaBienThe into hinhAnhGroup
                                select new BienTheHangHoaViewModel
                                {
                                    MaBienThe = bt.MaBienThe,
                                    MaHangHoa = bt.MaHangHoa,
                                    MauSac = bt.MauSac,
                                    DungLuong = bt.DungLuong,
                                    CPU = bt.CPU,
                                    RAM = bt.RAM,
                                    KichThuocManHinh = bt.KichThuocManHinh,
                                    LoaiBoNho = bt.LoaiBoNho,
                                    GiaGoc = bt.GiaGoc ?? 0,
                                    GiaKhuyenMai = bt.GiaKhuyenMai ?? 0,
                                    SoLuongTonKho = bt.SoLuongTonKho ?? 0,
                                    UrlAnh = hinhAnhGroup.Select(x => x.UrlAnh).ToList()
                                }).ToList();



            var product = productList.FirstOrDefault(p => p.MaHangHoa == id);
            if (product == null) return HttpNotFound();
            var ListKhuyenMais = data.KhuyenMaiTangKems
                    .ToList();
            var ListBienTheTatCaHangHoa = data.BienTheHangHoas
                   .ToList();
            var viewModel = new ProductDetailPageView
            {
                ListBienTheTatCaHangHoa= ListBienTheTatCaHangHoa,
                ListKhuyenMais = ListKhuyenMais,
                ListBienThes = listBienThes,
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
            string maKhachHang = Session["UserID"] as string;
            if (string.IsNullOrEmpty(maKhachHang))
            {
                return RedirectToAction("Login", "InnerPage"); // Chưa đăng nhập
            }

            var hangHoaYeuThich = (from yt in data.YeuThiches
                                   where yt.MaKhachHang == maKhachHang
                                   select yt.MaHangHoa).ToList();

            var hangHoaFull = (from hh in data.HangHoas
                               where hangHoaYeuThich.Contains(hh.MaHangHoa)

                               join bt in data.BienTheHangHoas
                               on hh.MaHangHoa equals bt.MaHangHoa into btGroup
                               from bienThe in btGroup.DefaultIfEmpty()

                               join dg in data.DanhGias
                               on hh.MaHangHoa equals dg.MaHangHoa into dgGroup
                               from danhGia in dgGroup.DefaultIfEmpty()

                               group new { bienThe, danhGia } by new
                               {
                                   hh.MaHangHoa,
                                   hh.MoTaDai,
                                   hh.TenHangHoa,
                                   hh.HinhAnh,
                                   hh.MoTa,
                                   hh.NgayTao
                               } into g

                               select new HangHoaViewModel
                               {
                                   MaHangHoa = g.Key.MaHangHoa,
                                   MoTaDai = g.Key.MoTaDai,
                                   TenHangHoa = g.Key.TenHangHoa,
                                   HinhAnh = g.Key.HinhAnh,
                                   MoTa = g.Key.MoTa,
                                   NgayTao = g.Key.NgayTao ?? DateTime.Now,

                                   GiaGoc = g.Where(x => x.bienThe != null)
                                             .Select(x => x.bienThe.GiaGoc)
                                             .FirstOrDefault() ?? 0,

                                   GiaKhuyenMai = g.Where(x => x.bienThe != null)
                                                   .Select(x => x.bienThe.GiaKhuyenMai)
                                                   .FirstOrDefault() ?? 0,

                                   SoLuongTonKho = g.Where(x => x.bienThe != null)
                                                    .Select(x => x.bienThe.SoLuongTonKho)
                                                    .FirstOrDefault() ?? 0,

                                   SoLuongDanhGia = g.Count(x => x.danhGia != null),

                                   DanhGiaTrungBinh = g.Any(x => x.danhGia != null)
                                                    ? g.Average(x => (float?)x.danhGia.SoSao) ?? 0
                                                    : 0
                               }).ToList();
            return View(hangHoaFull);
        }

        [HttpPost]
        public JsonResult AddWishList(string maHangHoa)
        {
            var maKhachHang = Session["UserID"] as string;
            if (string.IsNullOrEmpty(maKhachHang))
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để thực hiện thao tác này." });
            }
            var yeuThich = data.YeuThiches
                                .FirstOrDefault(y => y.MaKhachHang == maKhachHang && y.MaHangHoa == maHangHoa);

            if (yeuThich != null)
            {
                // Nếu đã có thì xóa
                data.YeuThiches.DeleteOnSubmit(yeuThich);
                data.SubmitChanges();
                return Json(new { success = true, removed = true, message = "Đã xóa khỏi danh sách yêu thích!" });
            }
            else
            {
                // Nếu chưa có thì thêm
                var newYeuThich = new YeuThich
                {
                    MaKhachHang = maKhachHang,
                    MaHangHoa = maHangHoa
                };
                data.YeuThiches.InsertOnSubmit(newYeuThich);
                data.SubmitChanges();
                return Json(new { success = true, removed = false, message = "Đã thêm vào danh sách yêu thích!" });
            }
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
                                   MaBienThe = g.Where(x => x.bienThe != null)
                                             .Select(x => x.bienThe.MaBienThe)
                                             .FirstOrDefault(),
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

        [HttpGet]
        public ActionResult AddRecentProduct(string maHangHoa, string maBienThe)
        {
            var list = HttpRuntime.Cache["RecentProducts"] as List<string> ?? new List<string>();

            if (list.Contains(maHangHoa))
                list.Remove(maHangHoa);

            list.Insert(0, maHangHoa);

            if (list.Count > 10)
                list = list.Take(10).ToList();

            HttpRuntime.Cache["RecentProducts"] = list;

            // Redirect đến trang chi tiết sản phẩm
            return RedirectToAction("ProductDetail", "Product", new { id = maHangHoa, maBienThe = maBienThe });
        }

        [ChildActionOnly]                     // <-- thêm thuộc tính này (tùy chọn)
        public ActionResult RecentlyViewedPartial()
        {
            var dsMaHangHoa = HttpRuntime.Cache["RecentProducts"] as List<string> ?? new List<string>();

            var recentProducts =
                (from hh in data.HangHoas
                 join bt in data.BienTheHangHoas on hh.MaHangHoa equals bt.MaHangHoa into btGroup
                 from bienThe in btGroup.DefaultIfEmpty()
                 where dsMaHangHoa.Contains(hh.MaHangHoa)
                 group new { hh, bienThe } by new
                 {
                     hh.MaHangHoa,
                     hh.TenHangHoa,
                     hh.HinhAnh,
                     hh.MoTa
                 } into g
                 select new ProductRecentViewModel
                 {
                     MaHangHoa = g.Key.MaHangHoa,
                     TenHangHoa = g.Key.TenHangHoa,
                     HinhAnh = g.Key.HinhAnh,
                     MoTa = g.Key.MoTa,
                     MaBienThe = g.Select(x => x.bienThe.MaBienThe).FirstOrDefault(),
                     GiaGoc = g.Select(x => x.bienThe.GiaGoc ?? 0).FirstOrDefault(),
                     GiaKhuyenMai = g.Select(x => x.bienThe.GiaKhuyenMai ?? 0).FirstOrDefault(),
                     SoLuongTonKho = g.Select(x => x.bienThe.SoLuongTonKho ?? 0).FirstOrDefault()
                 }).ToList();

            // sắp xếp lại đúng thứ tự người dùng vừa xem
            var ordered = dsMaHangHoa
                          .Select(ma => recentProducts.FirstOrDefault(p => p.MaHangHoa == ma))
                          .Where(p => p != null)
                          .ToList();

            return PartialView("_RecentlyViewedPartial", ordered);   // model = List<ProductRecentViewModel>
        }

    }
}
