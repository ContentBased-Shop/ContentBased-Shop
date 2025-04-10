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

        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Login()
        {
            return View();
        }
        public ActionResult Register()
        {
            return View();
        }
        public ActionResult ForgotPassword()
        {
            return View();
        }
        public ActionResult PreCart()
        {
            return View();
        }
      
        public ActionResult Checkout()
        {
            return View();
        }
        public ActionResult Checkout_ShoppingCart()
        {
            return View();
        }
        public ActionResult Contact()
        {
            return View();
        }
    }
}
