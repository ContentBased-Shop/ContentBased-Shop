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
        SHOPDataContext data = new SHOPDataContext("Data Source=MSI;Initial Catalog=CuaHang2;Persist Security Info=True;Use" +
                "r ID=sa;Password=123;Encrypt=True;TrustServerCertificate=True");
        // GET: /Home/
        #region TRANG-CHU
        public ActionResult Index()
        {
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
                                   hh.MaHangHoa,
                                   hh.TenHangHoa,
                                   hh.MaDanhMuc,
                                   hh.MaThuongHieu,
                                   hh.HinhAnh,
                                   hh.MoTa,
                                   hh.NgayTao
                               } into g

                               select new HangHoaViewModel
                               {
                                   MaHangHoa = g.Key.MaHangHoa,
                                   TenHangHoa = g.Key.TenHangHoa,
                                   MaDanhMuc = g.Key.MaDanhMuc,
                                   MaThuongHieu = g.Key.MaThuongHieu,
                                   HinhAnh = g.Key.HinhAnh,
                                   MoTa = g.Key.MoTa,
                                   NgayTao = g.Key.NgayTao.Value,

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
        #endregion
    }
}
