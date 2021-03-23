using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AmrodWCIntegration.Models.Amrod
{
    public class AmrodCategoryTreeResponse
    {
        public IEnumerable<AmrodCategory> Categories { get; set; }
    }
}
