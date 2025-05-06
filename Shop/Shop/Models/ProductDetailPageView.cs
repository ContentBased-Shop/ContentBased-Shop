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
    }

}