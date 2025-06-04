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
