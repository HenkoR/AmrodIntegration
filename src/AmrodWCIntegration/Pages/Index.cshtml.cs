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

using AmrodWCIntegration.Clients;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

using WooCommerceNET.WooCommerce.v3;

namespace AmrodWCIntegration.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly WoocommerceClient woocommerce;
        public List<ProductCategory> Categories;
        public IndexModel(ILogger<IndexModel> logger, WoocommerceClient woocommerceClient)
        {
            _logger = logger;
            woocommerce = woocommerceClient;
        }

        public async Task OnGet()
        {
            Categories = await woocommerce.GetCategories();

        }
    }
}
