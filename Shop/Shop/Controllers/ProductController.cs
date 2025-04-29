using System;
using System.Collections.Generic;
using System.Linq;
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
        public ActionResult ProductDetail()
        {
            return View();
        }
        public ActionResult ProductDienThoaiTabLet()
        {
            return View();
        }
        public ActionResult ProductLapTopPC()
        {
            return View();
        }
        public ActionResult Gaming()
        {
            return View();
        }
        public ActionResult AnotherProduct()
        {
            return View();
        }
        public ActionResult PhuKien()
        {
            return View();
        }
        public ActionResult ProductWishList()
        {
            var products = (from p in data.HangHoas
                            orderby p.TenHangHoa
                            select p).ToList(); // lấy hết, không lọc IsInStock

            return View(products);
        }
    }
}
