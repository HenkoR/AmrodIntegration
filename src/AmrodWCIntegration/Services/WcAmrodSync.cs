using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AmrodWCIntegration.Clients.Amrod;
using AmrodWCIntegration.Clients.Wordpress;

using WooCommerce.NET.WordPress.v2;

using WooCommerceNET.WooCommerce.v3;

namespace AmrodWCIntegration.Services
{
    public class WcAmrodSync
    {
        private readonly AmrodClient _amrodClient;
        private readonly WoocommerceClient _woocommerceClient;
        private readonly WordPressClient _wordPressClient;

        public WcAmrodSync(AmrodClient amrodClient, WoocommerceClient woocommerceClient, WordPressClient wordPressClient)
        {
            _amrodClient = amrodClient;
            _woocommerceClient = woocommerceClient;
            _wordPressClient = wordPressClient;
        }

        public async Task ImportCategoriesAsync(CancellationToken ct = default)
        {
            var amrodCategoriesTask = _amrodClient.GetCategoriesAsync();
            var wcCategoriesTask = _woocommerceClient.GetCategories();

            await Task.WhenAll(amrodCategoriesTask, wcCategoriesTask);

            var amrodCategories = await amrodCategoriesTask;
            var wcCategories = await wcCategoriesTask;

            foreach (var category in amrodCategories)
            {
                if (!wcCategories.Any(x => x.name == category.CategoryName))
                {
                    var newWcCat = new ProductCategory
                    {
                        name = category.CategoryName,
                    };
                    newWcCat = await _woocommerceClient.CreateNewCategory(newWcCat);
                    foreach (var subCat in category.SubCategories)
                    {
                        if (!wcCategories.Any(x => x.name == subCat.CategoryName))
                        {
                            var newWcSubCat = new ProductCategory
                            {
                                name = subCat.CategoryName,
                                parent = newWcCat.id,
                            };
                            newWcSubCat = await _woocommerceClient.CreateNewCategory(newWcSubCat);
                        }
                    }
                }
            }
        }
    }
}
