using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Shop.Models
{
    public class ProductDetailPageView
    {
        public ProductDetailView Product { get; set; }
        public List<ProductDetailView> RelatedProducts { get; set; }

        public BienTheHangHoaViewModel SelectedBienThe { get; set; }
        public List<BienTheHangHoaViewModel> ListBienThes { get; set; }

        public List<KhuyenMaiTangKem> ListKhuyenMais { get; set; }

        public List<BienTheHangHoa> ListBienTheTatCaHangHoa { get; set; }
    }

}