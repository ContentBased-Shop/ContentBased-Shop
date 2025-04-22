using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Shop.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/
        #region TRANG-CHU
        public ActionResult Index()
        {
            return View();
        }
        #endregion
    }
}
