using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AmrodWCIntegration.Models.Amrod
{
    public class AmrodStockLevel
    {
        public IEnumerable<AmrodLevel> Levels { get; set; }
    }
}
