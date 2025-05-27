using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using Com.CloudRail.SI.Types;
using Shop.Models;

namespace Shop
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801
  
    public class MvcApplication : System.Web.HttpApplication
    {
        SHOPDataContext data = new SHOPDataContext("Data Source=MSI;Initial Catalog=CuaHang2;Persist Security Info=True;Use" +
              "r ID=sa;Password=123;Encrypt=True;TrustServerCertificate=True");
        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {
            HttpCookie authCookie = Context.Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie != null && !string.IsNullOrEmpty(authCookie.Value))
            {
                FormsAuthenticationTicket ticket = FormsAuthentication.Decrypt(authCookie.Value);
                if (ticket != null && !ticket.Expired)
                {
                    var identity = new FormsIdentity(ticket);
                    string[] roles = ticket.UserData.Split(','); // nếu cần phân quyền
                    var principal = new GenericPrincipal(identity, roles);
                    Context.User = principal;
                }
            }
        }


        protected void Application_AcquireRequestState(object sender, EventArgs e)
        {
            if (HttpContext.Current?.User?.Identity?.IsAuthenticated == true &&
                HttpContext.Current.Session["UserID"] == null)
            {
                string username = HttpContext.Current.User.Identity.Name;

                // Gọi CSDL để lấy thông tin người dùng
               
                    var user = data.KhachHangs.FirstOrDefault(u => u.TenDangNhap == username);
                    if (user != null)
                    {
                        HttpContext.Current.Session["UserID"] = user.MaKhachHang;
                        HttpContext.Current.Session["UserName"] = user.HoTen;
                        HttpContext.Current.Session["AccountName"] = user.TenDangNhap;
                        HttpContext.Current.Session["Password"] = user.MatKhauHash;

                       
                    }
                
            }
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
        protected void Application_Error()
        {
            Exception exception = Server.GetLastError();
            Response.Clear();

            HttpException httpException = exception as HttpException;
            RouteData routeData = new RouteData();
            routeData.Values.Add("controller", "Error");

            if (httpException == null)
            {
                routeData.Values.Add("action", "General");
            }
            else
            {
                switch (httpException.GetHttpCode())
                {
                    case 403:
                        routeData.Values.Add("action", "AccessDenied");
                        break;
                    case 404:
                        routeData.Values.Add("action", "NotFound");
                        break;
                    default:
                        routeData.Values.Add("action", "General");
                        break;
                }
            }

            Server.ClearError();
            IController errorController = new Shop.Controllers.ErrorController();
            errorController.Execute(new RequestContext(new HttpContextWrapper(Context), routeData));
        }

    }
}