﻿// ==========================================================================
//  Amrod Woocommerce Integration
// ==========================================================================
//  Copyright (c) AmrodIntegration (Henko Rabie)
//  All rights reserved. Licensed under the GNU General Public License.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using AmrodWCIntegration.Config;

using Microsoft.Extensions.Options;

using WooCommerceNET;
using WooCommerceNET.WooCommerce.v3;
using WooCommerceNET.WooCommerce.v3.Extension;

namespace AmrodWCIntegration.Clients
{
    public class WoocommerceClient
    {
        readonly RestAPI restAPI;
        private readonly WcOptions wcOptions;

        public WoocommerceClient(IOptions<WcOptions> optionsAccessor)
        {
            wcOptions = optionsAccessor.Value;
            restAPI = new RestAPI(wcOptions.ApiUri, wcOptions.ApiKey, wcOptions.ApiSecret);
        }

        public async Task<List<ProductCategory>> GetCategories()
        {
            WCObject wc = new WCObject(restAPI);
            return await wc.Category.GetAll();
        }
    }
}
