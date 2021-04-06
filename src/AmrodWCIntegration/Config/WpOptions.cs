// ==========================================================================
//  Amrod Woocommerce Integration
// ==========================================================================
//  Copyright (c) AmrodIntegration (Henko Rabie)
//  All rights reserved. Licensed under the GNU General Public License.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AmrodWCIntegration.Config
{
    public class WpOptions
    {
        public string BaseUri { get; set; }
        public string ApiUri { get; set; }
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public string OAuth_Token { get; set; }
        public string OAuth_Token_Secret { get; set; }
    }
}
