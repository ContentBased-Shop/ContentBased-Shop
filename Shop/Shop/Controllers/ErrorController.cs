using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Shop.Controllers
{
    public class ErrorController : Controller
    {
        public ActionResult NotFound()
        {
            Response.StatusCode = 404;
            return View();
        }

        public ActionResult AccessDenied()
        {
            Response.StatusCode = 403;
            return View();
        }

        public ActionResult General()
        {
            return View();
        }
    }


}
