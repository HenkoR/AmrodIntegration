using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AmrodWCIntegration.Models.Amrod
{
    public class AmrodProductAttribute
    {
        public string SizeCode { get; set; }
        public AmrodAttribute[] Attributes { get; set; }
    }
}
