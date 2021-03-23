using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AmrodWCIntegration.Config
{
    public class AmrodOptions
    {
        public string ApiUri { get; set; }
        public string ApiToken { get; set; }
        public string ClientCode { get; set; }
        public string TokenType { get; set; }
    }
}
