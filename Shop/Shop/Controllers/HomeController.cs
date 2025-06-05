using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Shop.Models;

namespace Shop.Controllers
{
    public class SanPhamGoiY
    {
        public string MaHangHoa { get; set; }
        public string TenHangHoa { get; set; }
        public string HinhAnh { get; set; }
        public string MoTa { get; set; }
        public decimal GiaBan { get; set; }
        public decimal? GiaKhuyenMai { get; set; }
        public string MaBienThe { get; set; }
        public int SoLuongTonKho { get; set; }
        public double? DiemDuDoan { get; set; }
        public double? DiemTuongDong { get; set; }
    }

    public class HomeController : Controller
    {
        SHOPDataContext data = new SHOPDataContext("Data Source=ACERNITRO5;Initial Catalog=CuaHang2;Persist Security Info=True;Use" +
                "r ID=sa;Password=123;Encrypt=True;TrustServerCertificate=True");
        // GET: /Home/
        #region TRANG-CHU
        public ActionResult Index()
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
            var danhMucs = data.DanhMucs
                     .ToList();

            ViewBag.DanhMucList = danhMucs;
            // Tách truy vấn thành các phần nhỏ hơn
            var hangHoaQuery = data.HangHoas
                .Select(hh => new
                {
                    hh.MaDanhMuc,
                    hh.MaHangHoa,
                    hh.MoTaDai,
                    hh.TenHangHoa,
                    hh.HinhAnh,
                    hh.MoTa,
                    hh.NgayTao
                }).ToList();

            var bienTheQuery = data.BienTheHangHoas
                .Select(bt => new
                {
                    bt.MaHangHoa,
                    bt.MaBienThe,
                    bt.GiaBan,
                    bt.GiaKhuyenMai,
                    bt.SoLuongTonKho
                }).ToList();

            var danhGiaQuery = data.DanhGias
                .Select(dg => new
                {
                    dg.MaHangHoa,
                    dg.MaDanhGia,
                    dg.SoSao
                }).ToList();

            var hangHoaFull = hangHoaQuery
                .Select(hh => new HangHoaViewModel
                {
                    MaDanhMuc = hh.MaDanhMuc,
                    MaHangHoa = hh.MaHangHoa,
                    MoTaDai = hh.MoTaDai,
                    TenHangHoa = hh.TenHangHoa,
                    HinhAnh = hh.HinhAnh,
                    MoTa = hh.MoTa,
                    NgayTao = hh.NgayTao.Value,
                    MaBienThe = bienTheQuery
                        .Where(bt => bt.MaHangHoa == hh.MaHangHoa)
                        .Select(bt => bt.MaBienThe)
                        .FirstOrDefault(),
                    GiaBan = bienTheQuery
                        .Where(bt => bt.MaHangHoa == hh.MaHangHoa)
                        .Select(bt => bt.GiaBan)
                        .FirstOrDefault() ?? 0,
                    GiaKhuyenMai = bienTheQuery
                        .Where(bt => bt.MaHangHoa == hh.MaHangHoa)
                        .Select(bt => bt.GiaKhuyenMai)
                        .FirstOrDefault() ?? 0,
                    SoLuongTonKho = bienTheQuery
                        .Where(bt => bt.MaHangHoa == hh.MaHangHoa)
                        .Select(bt => bt.SoLuongTonKho)
                        .FirstOrDefault() ?? 0,
                    SoLuongDanhGia = danhGiaQuery
                        .Where(dg => dg.MaHangHoa == hh.MaHangHoa)
                        .Select(dg => dg.MaDanhGia)
                        .Distinct()
                        .Count(),
                    DanhGiaTrungBinh = danhGiaQuery
                        .Where(dg => dg.MaHangHoa == hh.MaHangHoa)
                        .Any() ? danhGiaQuery
                            .Where(dg => dg.MaHangHoa == hh.MaHangHoa)
                            .Average(dg => (float?)dg.SoSao) ?? 0
                        : 0
                }).ToList();

            return View(hangHoaFull);
        }
        #endregion

        public JsonResult GetRecommendedProducts(string maKhachHang)
        {
            try
            {
                var ketQuaGopY = new List<SanPhamGoiY>();
                
                // Lấy danh sách sản phẩm xem gần đây từ cache
                var dsMaHangHoaXemGanDay = HttpRuntime.Cache["RecentProducts"] as List<string> ?? new List<string>();
                
                // Trường hợp 1: Người chưa đăng nhập
                if (string.IsNullOrEmpty(maKhachHang))
                {
                    if (dsMaHangHoaXemGanDay.Any())
                    {
                        // Lấy điểm tương đồng của các sản phẩm đã xem với các sản phẩm khác
                        var sanPhamLienQuan = (from cbf in data.ContentBasedFilterings
                                              where dsMaHangHoaXemGanDay.Contains(cbf.MaHangHoa1) || 
                                                    dsMaHangHoaXemGanDay.Contains(cbf.MaHangHoa2)
                                              // Lấy sản phẩm khác (không phải sản phẩm đã xem)
                                              let maHangHoaXem = dsMaHangHoaXemGanDay.Contains(cbf.MaHangHoa1) ? cbf.MaHangHoa1 : cbf.MaHangHoa2
                                              let maHangHoaLienQuan = cbf.MaHangHoa1 == maHangHoaXem ? cbf.MaHangHoa2 : cbf.MaHangHoa1
                                              // Join với bảng Hàng Hóa và Biến Thể để lấy thông tin chi tiết
                                              join hh in data.HangHoas on maHangHoaLienQuan equals hh.MaHangHoa
                                              join bt in data.BienTheHangHoas on hh.MaHangHoa equals bt.MaHangHoa
                                              // Lấy thông tin đánh giá
                                              join dg in data.DanhGias on hh.MaHangHoa equals dg.MaHangHoa into dgGroup
                                              from danhGia in dgGroup.DefaultIfEmpty()
                                              group new { hh, bt, danhGia, cbf } by new
                                              {
                                                  hh.MaHangHoa,
                                                  hh.TenHangHoa,
                                                  hh.HinhAnh,
                                                  hh.MoTa,
                                                  bt.GiaBan,
                                                  bt.GiaKhuyenMai,
                                                  bt.MaBienThe,
                                                  bt.SoLuongTonKho,
                                                  cbf.DiemTuongDong
                                              } into g
                                              select new SanPhamGoiY
                                              {
                                                  MaHangHoa = g.Key.MaHangHoa,
                                                  TenHangHoa = g.Key.TenHangHoa,
                                                  HinhAnh = g.Key.HinhAnh,
                                                  MoTa = g.Key.MoTa,
                                                  GiaBan = (decimal)g.Key.GiaBan,
                                                  GiaKhuyenMai = (decimal)  g.Key.GiaKhuyenMai,
                                                  MaBienThe = g.Key.MaBienThe,
                                                  SoLuongTonKho = (int)g.Key.SoLuongTonKho,
                                                  DiemTuongDong = g.Key.DiemTuongDong,
                                                  DiemDuDoan = null
                                              })
                                              .OrderByDescending(x => x.DiemTuongDong)
                                              .Take(5)
                                              .ToList();

                        ketQuaGopY.AddRange(sanPhamLienQuan);
                    }
                }
                else
                {
                    // Lấy danh sách sản phẩm đã đánh giá của khách hàng
                    var danhGiaDaCo = data.CollaborativeFilterings
                        .Where(cf => cf.MaKhachHang == maKhachHang)
                        .Select(cf => new { cf.MaHangHoa, cf.DiemSo })
                        .ToList();

                    // Trường hợp 2: Người mới đăng nhập lần đầu (chưa có đánh giá)
                    if (!danhGiaDaCo.Any())
                    {
                        // Kết hợp sản phẩm yêu thích và sản phẩm xem gần đây
                        var dsMaHangHoaCanXet = new List<string>();
                        
                        // Thêm sản phẩm yêu thích
                        var sanPhamYeuThich = data.YeuThiches
                            .Where(yt => yt.MaKhachHang == maKhachHang)
                            .Select(yt => yt.MaHangHoa)
                            .ToList();
                        dsMaHangHoaCanXet.AddRange(sanPhamYeuThich);
                        
                        // Thêm sản phẩm xem gần đây
                        dsMaHangHoaCanXet.AddRange(dsMaHangHoaXemGanDay);
                        
                        if (dsMaHangHoaCanXet.Any())
                        {
                            // Lấy điểm tương đồng của các sản phẩm đã xem/yêu thích với các sản phẩm khác
                            var sanPhamLienQuan = (from cbf in data.ContentBasedFilterings
                                                  where dsMaHangHoaCanXet.Contains(cbf.MaHangHoa1) || 
                                                        dsMaHangHoaCanXet.Contains(cbf.MaHangHoa2)
                                                  // Lấy sản phẩm khác (không phải sản phẩm đã xem/yêu thích)
                                                  let maHangHoaXem = dsMaHangHoaCanXet.Contains(cbf.MaHangHoa1) ? cbf.MaHangHoa1 : cbf.MaHangHoa2
                                                  let maHangHoaLienQuan = cbf.MaHangHoa1 == maHangHoaXem ? cbf.MaHangHoa2 : cbf.MaHangHoa1
                                                  // Join với bảng Hàng Hóa và Biến Thể để lấy thông tin chi tiết
                                                  join hh in data.HangHoas on maHangHoaLienQuan equals hh.MaHangHoa
                                                  join bt in data.BienTheHangHoas on hh.MaHangHoa equals bt.MaHangHoa
                                                  // Lấy thông tin đánh giá
                                                  join dg in data.DanhGias on hh.MaHangHoa equals dg.MaHangHoa into dgGroup
                                                  from danhGia in dgGroup.DefaultIfEmpty()
                                                  group new { hh, bt, danhGia, cbf } by new
                                                  {
                                                      hh.MaHangHoa,
                                                      hh.TenHangHoa,
                                                      hh.HinhAnh,
                                                      hh.MoTa,
                                                      bt.GiaBan,
                                                      bt.GiaKhuyenMai,
                                                      bt.MaBienThe,
                                                      bt.SoLuongTonKho,
                                                      cbf.DiemTuongDong
                                                  } into g
                                                  select new SanPhamGoiY
                                                  {
                                                      MaHangHoa = g.Key.MaHangHoa,
                                                      TenHangHoa = g.Key.TenHangHoa,
                                                      HinhAnh = g.Key.HinhAnh,
                                                      MoTa = g.Key.MoTa,
                                                      GiaBan = (decimal)g.Key.GiaBan,
                                                      GiaKhuyenMai = (decimal)g.Key.GiaKhuyenMai,
                                                      MaBienThe = g.Key.MaBienThe,
                                                      SoLuongTonKho = (int)g.Key.SoLuongTonKho,
                                                      DiemTuongDong = g.Key.DiemTuongDong,
                                                      DiemDuDoan = null
                                                  })
                                                  .OrderByDescending(x => x.DiemTuongDong)
                                                  .Take(5)
                                                  .ToList();

                            ketQuaGopY.AddRange(sanPhamLienQuan);
                        }
                    }
                    // Trường hợp 3: Người đã mua hàng và có đánh giá
                    else
                    {
                        // Giữ nguyên logic cũ cho người đã mua hàng
                        var maHangHoaDaDanhGia = danhGiaDaCo.Select(dg => dg.MaHangHoa).ToList();
                        var danhGiaChuaCo = data.HangHoas
                            .Where(hh => !maHangHoaDaDanhGia.Contains(hh.MaHangHoa))
                            .Select(hh => hh.MaHangHoa)
                            .ToList();

                        foreach (var maHangHoaChuaDanhGia in danhGiaChuaCo)
                        {
                            double tongDiemTuongDong = 0;
                            double tongDiemDanhGia = 0;

                            foreach (var danhGia in danhGiaDaCo)
                            {
                                var diemTuongDong = data.ContentBasedFilterings
                                    .Where(cbf => (cbf.MaHangHoa1 == danhGia.MaHangHoa && cbf.MaHangHoa2 == maHangHoaChuaDanhGia) ||
                                                (cbf.MaHangHoa1 == maHangHoaChuaDanhGia && cbf.MaHangHoa2 == danhGia.MaHangHoa))
                                    .Select(cbf => cbf.DiemTuongDong)
                                    .FirstOrDefault();

                                if (diemTuongDong > 0)
                                {
                                    tongDiemTuongDong += (double)diemTuongDong;
                                    tongDiemDanhGia += (double)(danhGia.DiemSo * diemTuongDong);
                                }
                            }

                            if (tongDiemTuongDong > 0)
                            {
                                double diemDuDoan = tongDiemDanhGia / tongDiemTuongDong;
                                
                                var hangHoa = (from hh in data.HangHoas
                                             join bt in data.BienTheHangHoas on hh.MaHangHoa equals bt.MaHangHoa
                                             where hh.MaHangHoa == maHangHoaChuaDanhGia
                                             select new
                                             {
                                                 hh.MaHangHoa,
                                                 hh.TenHangHoa,
                                                 hh.HinhAnh,
                                                 hh.MoTa,
                                                 bt.GiaBan,
                                                 bt.GiaKhuyenMai,
                                                 bt.MaBienThe,
                                                 bt.SoLuongTonKho
                                             }).FirstOrDefault();
                                
                                if (hangHoa != null)
                                {
                                    ketQuaGopY.Add(new SanPhamGoiY
                                    {
                                        MaHangHoa = hangHoa.MaHangHoa,
                                        TenHangHoa = hangHoa.TenHangHoa,
                                        HinhAnh = hangHoa.HinhAnh,
                                        MoTa = hangHoa.MoTa,
                                        GiaBan = (decimal)hangHoa.GiaBan,
                                        GiaKhuyenMai = (decimal)hangHoa.GiaKhuyenMai,
                                        MaBienThe = hangHoa.MaBienThe,
                                        SoLuongTonKho = (int)hangHoa.SoLuongTonKho,
                                        DiemDuDoan = Math.Round(diemDuDoan, 2),
                                        DiemTuongDong = null
                                    });
                                }
                            }
                        }
                    }
                }

                // Sắp xếp kết quả theo điểm dự đoán/điểm tương đồng giảm dần và lấy 5 sản phẩm đầu tiên
                var ketQuaCuoiCung = ketQuaGopY
                    .OrderByDescending(x => x.DiemDuDoan ?? x.DiemTuongDong ?? 0)
                    .Take(5)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"Tổng số kết quả trước khi sắp xếp: {ketQuaGopY.Count}");
                System.Diagnostics.Debug.WriteLine($"Số kết quả cuối cùng: {ketQuaCuoiCung.Count}");

                return Json(new { success = true, data = ketQuaCuoiCung }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult GetRecentProductIds()
        {
            var list = HttpRuntime.Cache["RecentProducts"] as List<string> ?? new List<string>();
            return Json(list, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetRelatedProducts(string maHangHoa)
        {
            try
            {
                // Lấy danh sách các sản phẩm có điểm tương đồng cao với sản phẩm hiện tại
                var sanPhamLienQuan = (from cbf in data.ContentBasedFilterings
                                      where (cbf.MaHangHoa1 == maHangHoa || cbf.MaHangHoa2 == maHangHoa)
                                      // Lấy sản phẩm khác (không phải sản phẩm hiện tại)
                                      let maHangHoaLienQuan = cbf.MaHangHoa1 == maHangHoa ? cbf.MaHangHoa2 : cbf.MaHangHoa1
                                      // Join với bảng Hàng Hóa và Biến Thể để lấy thông tin chi tiết
                                      join hh in data.HangHoas on maHangHoaLienQuan equals hh.MaHangHoa
                                      join bt in data.BienTheHangHoas on hh.MaHangHoa equals bt.MaHangHoa
                                      // Lấy thông tin đánh giá
                                      join dg in data.DanhGias on hh.MaHangHoa equals dg.MaHangHoa into dgGroup
                                      from danhGia in dgGroup.DefaultIfEmpty()
                                      group new { hh, bt, danhGia, cbf } by new
                                      {
                                          hh.MaHangHoa,
                                          hh.TenHangHoa,
                                          hh.HinhAnh,
                                          hh.MoTa,
                                          bt.GiaBan,
                                          bt.GiaKhuyenMai,
                                          bt.MaBienThe,
                                          bt.SoLuongTonKho,
                                          cbf.DiemTuongDong
                                      } into g
                                      select new
                                      {
                                          MaHangHoa = g.Key.MaHangHoa,
                                          TenHangHoa = g.Key.TenHangHoa,
                                          HinhAnh = g.Key.HinhAnh,
                                          MoTa = g.Key.MoTa,
                                          GiaBan = g.Key.GiaBan,
                                          GiaKhuyenMai = g.Key.GiaKhuyenMai,
                                          MaBienThe = g.Key.MaBienThe,
                                          SoLuongTonKho = g.Key.SoLuongTonKho,
                                          DiemTuongDong = g.Key.DiemTuongDong,
                                          SoLuongDanhGia = g.Count(x => x.danhGia != null),
                                          DanhGiaTrungBinh = g.Any(x => x.danhGia != null) 
                                            ? g.Average(x => (float?)x.danhGia.SoSao) ?? 0 
                                            : 0
                                      })
                                      .OrderByDescending(x => x.DiemTuongDong) // Sắp xếp theo điểm tương đồng giảm dần
                                      .Take(4) // Lấy 4 sản phẩm liên quan
                                      .ToList();

                return Json(new { success = true, data = sanPhamLienQuan }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
