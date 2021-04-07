using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AmrodWCIntegration.Clients.Amrod;
using AmrodWCIntegration.Clients.Wordpress;
using AmrodWCIntegration.Config;

using Microsoft.Extensions.Options;

using WooCommerce.NET.WordPress.v2;

using WooCommerceNET.WooCommerce.v3;

namespace AmrodWCIntegration.Services
{
    public class WcAmrodSync
    {
        private readonly AmrodClient _amrodClient;
        private readonly WoocommerceClient _woocommerceClient;
        private readonly WordPressClient _wordPressClient;
        private readonly WcOptions _wcOptions;

        public WcAmrodSync(AmrodClient amrodClient, WoocommerceClient woocommerceClient, WordPressClient wordPressClient, IOptions<WcOptions> optionsAccessor)
        {
            _wcOptions = optionsAccessor.Value;
            _amrodClient = amrodClient;
            _woocommerceClient = woocommerceClient;
            _wordPressClient = wordPressClient;
        }

        public async Task ImportCategoriesAsync(CancellationToken ct = default)
        {
            var amrodCategoriesTask = _amrodClient.GetTopLevelCategoriesAsync();
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

        public async Task ImportProductsAsync(CancellationToken ct = default)
        {
            try
            {
                var amrodCategoriesTask = _amrodClient.GetTopLevelCategoriesAsync();
                var wcCategoriesTask = _woocommerceClient.GetCategories();
                var wcProductAttributesTask = _woocommerceClient.GetProductAttributes();
                var wcProductsTask = _woocommerceClient.GetProducts();

                await Task.WhenAll(amrodCategoriesTask, wcCategoriesTask, wcProductAttributesTask, wcProductsTask);

                var amrodCategories = await amrodCategoriesTask;
                var wcCategories = await wcCategoriesTask;
                var wcProductAttributes = await wcProductAttributesTask;
                var wcProducts = await wcProductsTask;
                List<ProductAttributeTerm> wcAttributeTerms = new List<ProductAttributeTerm>();

                foreach (var attr in wcProductAttributes)
                {
                    wcAttributeTerms.AddRange(await _woocommerceClient.GetProductAttributeTerms(attr.id.Value));
                }

                foreach (var category in amrodCategories)
                {
                    foreach (var subCat in category.SubCategories)
                    {
                        var categoryProducts = await _amrodClient.GetCategoryProducts(subCat.CategoryId);

                        foreach (var product in categoryProducts)
                        {
                            var arProductDetails = await _amrodClient.GetProductDetails(product.ProductId);

                            if (!wcProducts.Any(x => x.sku == arProductDetails.ProductCode))
                            {
                                var newProduct = new Product
                                {
                                    type = "variable",
                                    status = "publish",
                                    featured = false,
                                    catalog_visibility = "visible",

                                    sku = arProductDetails.ProductCode,
                                    slug = arProductDetails.ProductCode,
                                    name = arProductDetails.ProductName,
                                    description = System.Web.HttpUtility.HtmlDecode(arProductDetails.ProductDescription),
                                    short_description = System.Web.HttpUtility.HtmlDecode(arProductDetails.ProductDescription),
                                    price = GetItemPrice(arProductDetails.Price),
                                    regular_price = GetItemPrice(arProductDetails.Price),
                                    images = new List<ProductImage>()
                                };

                                // Images
                                foreach (var arImage in arProductDetails.Images)
                                {
                                    var newImage = new Media {
                                        title = arImage.ImageUrlXL.Substring(arImage.ImageUrlXL.LastIndexOf('/') + 1), //arImage.Name.Equals("default") ? arProductDetails.ProductCode : arImage.Name,
                                        alt_text = arImage.ImageUrlXL.Substring(arImage.ImageUrlXL.LastIndexOf('/') + 1),
                                        source_url = arImage.ImageUrlXL,
                                    };
                                    newImage = await _wordPressClient.AddImageMedia(newImage);
                                    newProduct.images.Add(new ProductImage { 
                                        id = newImage.id,
                                        name = newImage.title,
                                        alt = newImage.alt_text,
                                        src = newImage.source_url 
                                    });
                                }

                                var arCategory = arProductDetails.Attributes?.Select(x => x.Attributes.First(att => att.AttributeName == "ALPCategory").AttributeValue);

                                if (arCategory.Any())
                                {
                                    var wcCategory = wcCategories.First(x => x.name == arCategory.First());
                                    var prodCategories = new List<ProductCategoryLine> {
                                        new ProductCategoryLine
                                        {
                                            id = wcCategory?.id,
                                            name = wcCategory?.name,
                                            slug = wcCategory?.slug
                                        }
                                    };
                                    newProduct.categories = prodCategories;
                                }

                                newProduct = await _woocommerceClient.CreateNewProduct(newProduct);

                                if (arProductDetails.StockLevel != null)
                                {
                                    var colorAttributes = new ProductAttributeLine
                                    {
                                        id = wcProductAttributes.First(x => x.name == "Color").id,
                                        name = "Color",
                                        visible = true,
                                        variation = true,
                                        options = arProductDetails.StockLevel.Levels.Select(x => x.ColourName).Distinct().ToList()
                                    };
                                    newProduct.attributes.Add(colorAttributes);

                                    var sizeAttributes = new ProductAttributeLine
                                    {
                                        id = wcProductAttributes.First(x => x.name == "Size").id,
                                        name = "Size",
                                        visible = true,
                                        variation = true,
                                        options = arProductDetails.StockLevel.Levels.Select(x => x.SizeCode).Distinct().ToList()
                                    };
                                    newProduct.attributes.Add(sizeAttributes);

                                    newProduct = await _woocommerceClient.UpdateProduct(newProduct);

                                    foreach (var productVariation in arProductDetails.StockLevel.Levels)
                                    {
                                        if (!wcAttributeTerms.Any(x => x.name == productVariation.ColourName))
                                        {
                                            var newTerm = new ProductAttributeTerm
                                            {
                                                name = productVariation.ColourName,
                                            };
                                            newTerm = await _woocommerceClient.CreateNewProductAttributeTerm(newTerm, wcProductAttributes.First(x => x.name == "Color").id.Value);
                                            wcAttributeTerms.Add(newTerm);
                                        }
                                        if (!wcAttributeTerms.Any(x => x.name == productVariation.SizeCode))
                                        {
                                            var newTerm = new ProductAttributeTerm
                                            {
                                                name = productVariation.SizeCode,
                                            };
                                            newTerm = await _woocommerceClient.CreateNewProductAttributeTerm(newTerm, wcProductAttributes.First(x => x.name == "Size").id.Value);
                                            wcAttributeTerms.Add(newTerm);
                                        }

                                        var att = new List<VariationAttribute>
                                        {
                                            new VariationAttribute
                                            {
                                                id = wcProductAttributes.First(x => x.name == "Color").id,
                                                name = "Color",
                                                option = productVariation.ColourName
                                            },
                                            new VariationAttribute
                                            {
                                                id = wcProductAttributes.First(x => x.name == "Size").id,
                                                name = "Size",
                                                option = productVariation.SizeCode
                                            }
                                        };

                                        var newProductVariation = new Variation
                                        {
                                            sku = productVariation.ItemCode,
                                            attributes = att,
                                            stock_quantity = productVariation.InStock,
                                            manage_stock = true,
                                            regular_price = GetItemPrice(arProductDetails.VarientPrices?.First(x => x.SizeCode == att.First(x => x.name == "Size").option).Price)
                                        };

                                        newProductVariation = await _woocommerceClient.CreateNewProductVariation(newProductVariation, newProduct.id.Value);
                                        newProduct.variations.Add((int)newProductVariation.id.Value);
                                    }
                                    await _woocommerceClient.UpdateProduct(newProduct);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        decimal GetItemPrice(decimal? cost)
        {
            var itemCost = cost ?? 0.00m;
            if (_wcOptions.AddVat)
                itemCost *= 1.15m;

            var itemPrice = itemCost / (1 - _wcOptions.ProfitMargin);
            if (_wcOptions.AddRounding)
                itemPrice = Math.Ceiling(itemPrice) - 0.05m;

            return itemPrice;
        }
    }
}