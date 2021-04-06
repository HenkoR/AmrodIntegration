using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AmrodWCIntegration.Models.Amrod
{
    public class AmrodCategoryProductsResponse
    {
        public IEnumerable<AmrodProduct> Products { get; set; }
    }
}
