using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Newtonsoft.Json;

using WooCommerceNET;

namespace AmrodWCIntegration.Clients
{
    public class WordPressRestApi : RestAPI
    {
        public WordPressRestApi(string url, string key, string secret, bool authorizedHeader = true,
               Func<string, string> jsonSerializeFilter = null,
               Func<string, string> jsonDeserializeFilter = null,
               Action<HttpWebRequest> requestFilter = null) : base(url, key, secret, authorizedHeader, jsonSerializeFilter, jsonDeserializeFilter, requestFilter)
        {
        }

        public override T DeserializeJSon<T>(string jsonString)
        {
            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        public override string SerializeJSon<T>(T t)
        {
            return JsonConvert.SerializeObject(t);
        }
    }
}
