using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AmrodWCIntegration.Models.Amrod
{
    public class AmrodBaseResponse<T>
    {
        public int ResponseCode { get; set; }
        public string ResponseMessage { get; set; }
        public string ResponseDetail { get; set; }
        public T Body { get; set; }
    }
}
