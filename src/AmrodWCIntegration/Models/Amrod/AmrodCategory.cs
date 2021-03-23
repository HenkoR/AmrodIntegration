using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AmrodWCIntegration.Models.Amrod
{
    public class AmrodCategory
    {
        public int CategoryId { get; set; }
        public IEnumerable<AmrodCategory> SubCategories { get; set; }
        public string CategoryName { get; set; }
        public int RelativePosition { get; set; }
        public int Behaviour { get; set; }
        public int ProductCount { get; set; }
        public string CategoryImageUrl { get; set; }
        public string CategoryImageUrl2x { get; set; }
        public string CategoryImageUrl3x { get; set; }
        public int FilterValueId { get; set; }
    }
}
