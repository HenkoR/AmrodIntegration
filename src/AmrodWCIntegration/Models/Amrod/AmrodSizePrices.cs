using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AmrodWCIntegration.Models.Amrod
{
    public class AmrodSizePrices
    {
        public string ColourCode { get; set; }
        public string SizeCode { get; set; }
        public decimal? Price { get; set; }
        public decimal? PriceWithCurrency { get; set; }
    }
}
