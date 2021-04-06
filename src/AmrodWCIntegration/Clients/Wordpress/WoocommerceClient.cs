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

using AmrodWCIntegration.Config;

using Microsoft.Extensions.Options;

using WooCommerceNET;
using WooCommerceNET.WooCommerce.v3;
using WooCommerceNET.WooCommerce.v3.Extension;

namespace AmrodWCIntegration.Clients.Wordpress
{
    public class WoocommerceClient
    {
        readonly WordPressRestApi restAPI;
        private readonly WcOptions wcOptions;

        public WoocommerceClient(IOptions<WcOptions> optionsAccessor)
        {
            wcOptions = optionsAccessor.Value;
            restAPI = new WordPressRestApi(wcOptions.ApiUri, wcOptions.ApiKey, wcOptions.ApiSecret);
            restAPI.WCAuthWithJWT = true;
        }

        public async Task<List<ProductCategory>> GetCategories()
        {
            WCObject wc = new WCObject(restAPI);
            return await wc.Category.GetAll(new Dictionary<string, string>() { { "per_page", "100" } });
        }

        public async Task<List<Product>> GetProducts()
        {
            WCObject wc = new WCObject(restAPI);
            return await wc.Product.GetAll();
        }

        public async Task<List<ProductAttribute>> GetProductAttributes()
        {
            WCObject wc = new WCObject(restAPI);
            return await wc.Attribute.GetAll(new Dictionary<string, string>() { { "per_page", "50" } });
        }

        public async Task<List<ProductAttributeTerm>> GetProductAttributeTerms(uint attrId)
        {
            WCObject wc = new WCObject(restAPI);
            return await wc.Attribute.Terms.GetAll(attrId, new Dictionary<string, string>() { { "per_page", "50" } });
        }

        public async Task<ProductCategory> CreateNewCategory(ProductCategory category)
        {
            WCObject wc = new WCObject(restAPI);
            return await wc.Category.Add(category);
        }

        public async Task<Product> CreateNewProduct(Product product)
        {
            WCObject wc = new WCObject(restAPI);
            return await wc.Product.Add(product);
        }

        public async Task<Product> UpdateProduct(Product product)
        {
            WCObject wc = new WCObject(restAPI);
            return await wc.Product.Update((int)product.id.Value, product);
        }

        public async Task<Variation> CreateNewProductVariation(Variation variation, uint productId)
        {
            WCObject wc = new WCObject(restAPI);
            return await wc.Product.Variations.Add(variation, (int)productId);
        }

        public async Task<ProductAttributeTerm> CreateNewProductAttributeTerm(ProductAttributeTerm attributeTerm, uint parentId)
        {
            WCObject wc = new WCObject(restAPI);
            return await wc.Attribute.Terms.Add(attributeTerm, (int)parentId);
        }
    }
}
