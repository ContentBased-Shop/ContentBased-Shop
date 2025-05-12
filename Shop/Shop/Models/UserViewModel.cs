using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Shop.Models
{
    public class UserViewModel
    {
        public KhachHang KhachHang { get; set; }
        public List<DiaChiKhachHang> ListDiaChiKhachHangs { get; set; }
    }


}
