using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AmrodWCIntegration.Models.Amrod
{
    public class AmrodProduct
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public int Behavior { get; set; }
        public int Promotion { get; set; }
        //public string PromotionStyle { get; set; }
        //public string BrandingRibbonStyle { get; set; }
        public string BrandingRibbonImage { get; set; }
        public decimal Price { get; set; }
        public decimal PriceWithCurrency { get; set; }
        //public int Sizes { get; set; }
        public string ImageUrl { get; set; }
        public string ImageUrl2x { get; set; }
        public string ImageUrl3x { get; set; }
        public string ImageUrlXL { get; set; }
        public bool IsEssential { get; set; }
    }
}
