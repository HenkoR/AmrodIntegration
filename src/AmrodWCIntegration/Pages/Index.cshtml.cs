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

using AmrodWCIntegration.Clients.Amrod;
using AmrodWCIntegration.Clients.Wordpress;
using AmrodWCIntegration.Models.Amrod;
using AmrodWCIntegration.Services;

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
        private readonly AmrodClient amrod;
        private readonly WcAmrodSync _wcAmrodSync;

        public List<ProductCategory> Categories;
        public IEnumerable<AmrodCategory> AmrodCategories;
        
        public IndexModel(ILogger<IndexModel> logger, WoocommerceClient woocommerceClient, AmrodClient amrodClient, WcAmrodSync wcAmrodSync)
        {
            _logger = logger;
            woocommerce = woocommerceClient;
            amrod = amrodClient;
            _wcAmrodSync = wcAmrodSync;
        }

        public async Task OnGet()
        {
            AmrodCategories = await amrod.GetAllCategoriesAsync();
            Categories = await woocommerce.GetCategories();
            //await _wcAmrodSync.ImportCategoriesAsync();
            //await _wcAmrodSync.ImportProductsAsync();
        }

        public async Task OnPostImportCategories()
        {
            await _wcAmrodSync.ImportCategoriesAsync();
            AmrodCategories = await amrod.GetAllCategoriesAsync();
            Categories = await woocommerce.GetCategories();
        }
    }
}
