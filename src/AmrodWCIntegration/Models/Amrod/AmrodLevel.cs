using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AmrodWCIntegration.Models.Amrod
{
    public class AmrodLevel
    {
        public string ItemCode { get; set; }
        public string ItemBaseCode { get; set; }
        public string Updated { get; set; }
        public string Description { get; set; }
        public string ColourCode { get; set; }
        public string ColourName { get; set; }
        public string ColourHex { get; set; }
        public string SizeCode { get; set; }
        public int? InStock { get; set; }
        public int? Reserved { get; set; }
        public int? Main { get; set; }
        public int? Samples { get; set; }
    }
}
