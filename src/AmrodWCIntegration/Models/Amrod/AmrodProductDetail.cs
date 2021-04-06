using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AmrodWCIntegration.Models.Amrod
{
    public class AmrodProductDetail
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string ProductDescription { get; set; }
        public int Behavior { get; set; }
        public int Promotion { get; set; }
        public string PromotionStyle { get; set; }
        public string BrandingRibbonStyle { get; set; }
        public string BrandingRibbonImage { get; set; }
        public string Gender { get; set; }
        public string GenderOther { get; set; }
        public int? GenderOtherId { get; set; }
        public decimal? Price { get; set; }
        public decimal? PriceWithCurrency { get; set; }
        public AmrodStockLevel StockLevel { get; set; }
        public IEnumerable<AmrodProductImages> Images { get; set; }
        public IEnumerable<AmrodSizePrices> VarientPrices { get; set; }
        public bool IsEssential { get; set; }
        public string BrandingGuideJpgUrl { get; set; }
        public string BrandingGuidePdfUrl { get; set; }
        public AmrodProductAttribute[] Attributes { get; set; }
    }
}
