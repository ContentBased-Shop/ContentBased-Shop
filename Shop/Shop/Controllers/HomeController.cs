using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Shop.Models;

namespace Shop.Controllers
{
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
            var hangHoaFull = (from hh in data.HangHoas
                                   // Gộp với tất cả biến thể theo MaHangHoa
                               join bt in data.BienTheHangHoas
                               on hh.MaHangHoa equals bt.MaHangHoa into btGroup
                               from bienThe in btGroup.DefaultIfEmpty()

                                   // Gộp với đánh giá
                               join dg in data.DanhGias
                               on hh.MaHangHoa equals dg.MaHangHoa into dgGroup
                               from danhGia in dgGroup.DefaultIfEmpty()
                               group new { bienThe, danhGia } by new
                               {             
                                   hh.MaDanhMuc,
                                   hh.MaHangHoa,
                                   hh.MoTaDai,
                                   hh.TenHangHoa,
                                   hh.HinhAnh,
                                   hh.MoTa,
                                   hh.NgayTao
                               } into g

                               select new HangHoaViewModel
                               {
                                   MaDanhMuc = g.Key.MaDanhMuc,
                                   MaHangHoa = g.Key.MaHangHoa,
                                   MoTaDai = g.Key.MoTaDai,
                                   TenHangHoa = g.Key.TenHangHoa,
                                   HinhAnh = g.Key.HinhAnh,
                                   MoTa = g.Key.MoTa,
                                   NgayTao = g.Key.NgayTao.Value,
                                   MaBienThe = g.Where(x => x.bienThe != null)
                                             .Select(x => x.bienThe.MaBienThe)
                                             .FirstOrDefault(),
                                   GiaBan = g.Where(x => x.bienThe != null)
                                              .Select(x => x.bienThe.GiaBan)
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
        #endregion

        public JsonResult GetRecommendedProducts(string maKhachHang)
        {
            try
            {
                // Lấy danh sách sản phẩm đã đánh giá của khách hàng
                var danhGiaDaCo = data.CollaborativeFilterings
                    .Where(cf => cf.MaKhachHang == maKhachHang)
                    .Select(cf => new { cf.MaHangHoa, cf.DiemSo })
                    .ToList();

                // Lấy danh sách mã sản phẩm đã đánh giá
                var maHangHoaDaDanhGia = danhGiaDaCo.Select(dg => dg.MaHangHoa).ToList();

                // Lấy danh sách sản phẩm chưa đánh giá
                var danhGiaChuaCo = data.HangHoas
                    .Where(hh => !maHangHoaDaDanhGia.Contains(hh.MaHangHoa))
                    .Select(hh => hh.MaHangHoa)
                    .ToList();

                var ketQuaGopY = new List<dynamic>();

                foreach (var maHangHoaChuaDanhGia in danhGiaChuaCo)
                {
                    double tongDiemTuongDong = 0;
                    double tongDiemDanhGia = 0;

                    foreach (var danhGia in danhGiaDaCo)
                    {
                        // Lấy điểm tương đồng giữa sản phẩm đã đánh giá và sản phẩm chưa đánh giá
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
                        
                        // Lấy thông tin chi tiết sản phẩm
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
                            ketQuaGopY.Add(new
                            {
                                MaHangHoa = hangHoa.MaHangHoa,
                                TenHangHoa = hangHoa.TenHangHoa,
                                HinhAnh = hangHoa.HinhAnh,
                                MoTa = hangHoa.MoTa,
                                GiaBan = hangHoa.GiaBan,
                                GiaKhuyenMai = hangHoa.GiaKhuyenMai,
                                MaBienThe = hangHoa.MaBienThe,
                                SoLuongTonKho = hangHoa.SoLuongTonKho,
                                DiemDuDoan = Math.Round(diemDuDoan, 2)
                            });
                        }
                    }
                }

                // Sắp xếp kết quả theo điểm dự đoán giảm dần và lấy 3 sản phẩm đầu tiên
                var ketQuaCuoiCung = ketQuaGopY
                    .OrderByDescending(k => k.DiemDuDoan)
                    .Take(3)
                    .ToList();

                return Json(new { success = true, data = ketQuaCuoiCung }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
