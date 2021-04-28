using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AmrodWCIntegration.Clients.Amrod;
using AmrodWCIntegration.Clients.Wordpress;
using AmrodWCIntegration.Config;
using AmrodWCIntegration.Utils;

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
        private string currentRequest;

        public WcAmrodSync(AmrodClient amrodClient, WoocommerceClient woocommerceClient, WordPressClient wordPressClient, IOptions<WcOptions> optionsAccessor)
        {
            _wcOptions = optionsAccessor.Value;
            _amrodClient = amrodClient;
            _woocommerceClient = woocommerceClient;
            _wordPressClient = wordPressClient;
        }

        public void RunTask(Func<Task> task, string requestId, CancellationToken ct = default)
        {
            currentRequest = requestId;
            _ = Task.Run(task, ct);
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
                                    var newImage = new Media
                                    {
                                        title = arImage.ImageUrlXL.Substring(arImage.ImageUrlXL.LastIndexOf('/') + 1), //arImage.Name.Equals("default") ? arProductDetails.ProductCode : arImage.Name,
                                        alt_text = arImage.ImageUrlXL.Substring(arImage.ImageUrlXL.LastIndexOf('/') + 1),
                                        source_url = arImage.ImageUrlXL,
                                    };
                                    newImage = await _wordPressClient.AddImageMedia(newImage);
                                    newProduct.images.Add(new ProductImage
                                    {
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

        public async Task ImportCategoryProducts(Models.Amrod.AmrodCategory amrodCategory, ProductCategory wcCategory, CancellationToken ct = default)
        {
            var newProduct = new Product
            {
                type = "variable",
                status = "publish",
                featured = false,
                catalog_visibility = "visible",
                images = new List<ProductImage>()
            };

            try
            {
                // Prepare: Fetch all the WC data we need to work with for lookups and compare
                ProgressTracker.Add(currentRequest, $"{DateTimeOffset.Now} :: ImportCategoryProducts :: Fetching WC data...");
                var wcProductAttributesTask = _woocommerceClient.GetProductAttributes();
                var wcProductsTask = _woocommerceClient.GetProducts();

                await Task.WhenAll(wcProductAttributesTask, wcProductsTask);

                var wcProductAttributes = await wcProductAttributesTask;
                var wcProducts = await wcProductsTask;
                var wcAttributeTerms = new List<ProductAttributeTerm>();

                foreach (var attr in wcProductAttributes)
                {
                    wcAttributeTerms.AddRange(await _woocommerceClient.GetProductAttributeTerms(attr.id.Value));
                }

                // Prepare: Fetch Amrod products
                ProgressTracker.Add(currentRequest, $"{DateTimeOffset.Now} :: ImportCategoryProducts :: Fetching AR products...");
                var categoryProducts = await _amrodClient.GetCategoryProducts(amrodCategory.CategoryId);

                // Prepare: Initialize all our counters used to show progress
                int productsCount = categoryProducts.Count();
                int productsImportProgress = 1;
                int variationsCount = 1;
                int variationImportProgress = 1;
                int imagesCount = 1;
                int imagesImportProgress = 1;

                // Working: Check if items already existing, and if they do add them to the new category if not already added
                var existingProductIds = await AddExistingProductsToNewCateogory(amrodCategory, wcCategory, categoryProducts, wcProducts);
                productsImportProgress += existingProductIds.Count;

                // Working: Loop all Amrod products not already in WC
                foreach (var product in categoryProducts.Where(x => !existingProductIds.Contains(x.ProductId)))
                {
                    // Prepare: Fetch Amrod product Full details
                    ProgressTracker.Add(currentRequest, $"{DateTimeOffset.Now} :: ImportCategoryProducts :: Fetching AR product details: {product.ProductCode} ({productsImportProgress}/{productsCount})...");
                    var arProductDetails = await _amrodClient.GetProductDetails(product.ProductId);

                    if (SkipChildVariationsOfExisting(arProductDetails, categoryProducts, wcProducts))
                    {
                        ProgressTracker.Add(currentRequest, $"{DateTimeOffset.Now} :: ImportCategoryProducts :: Skipping item: {product.ProductCode} ({productsImportProgress}/{productsCount})...");
                        productsImportProgress++;
                        continue;
                    }

                    imagesCount = arProductDetails.Images.Count();
                    imagesImportProgress = 1;

                    // Working: Import all non-variable products (Products with no options [Colors]/[Sizes])
                    if (IsSimpleProduct(arProductDetails))
                    {
                        if (!wcProducts.Any(x => x.sku == arProductDetails.ProductCode))
                        {
                            ProgressTracker.Add(currentRequest, $"{DateTimeOffset.Now} :: ImportCategoryProducts :: AR Product not in WC, proceeding with import ({productsImportProgress}/{productsCount})...");

                            newProduct = new Product
                            {
                                type = "simple",
                                status = "publish",
                                featured = false,
                                catalog_visibility = "visible",
                                images = new List<ProductImage>(),
                                sku = arProductDetails.ProductCode,
                                slug = arProductDetails.ProductCode,
                                name = arProductDetails.ProductName,
                                description = System.Web.HttpUtility.HtmlDecode(arProductDetails.ProductDescription),
                                short_description = System.Web.HttpUtility.HtmlDecode(arProductDetails.ProductDescription),
                                price = GetItemPrice(arProductDetails.Price),
                                regular_price = GetItemPrice(arProductDetails.Price),
                                manage_stock = true,
                                stock_quantity = arProductDetails.StockLevel?.Levels?.First()?.InStock
                            };

                            // Images
                            foreach (var arImage in arProductDetails.Images)
                            {
                                ProgressTracker.Add(currentRequest, $"{DateTimeOffset.Now} :: ImportCategoryProducts :: Adding product image: {arImage.Name} ({imagesImportProgress}/{imagesCount})...");
                                string imageUrl = "";
                                if (!string.IsNullOrEmpty(arImage.ImageUrlXL))
                                {
                                    imageUrl = arImage.ImageUrlXL;
                                }
                                else if (!string.IsNullOrEmpty(arImage.ImageUrl3x))
                                {
                                    imageUrl = arImage.ImageUrl3x;
                                }
                                else if (!string.IsNullOrEmpty(arImage.ImageUrl2x))
                                {
                                    imageUrl = arImage.ImageUrl2x;
                                }
                                else if (!string.IsNullOrEmpty(arImage.ImageUrl))
                                {
                                    imageUrl = arImage.ImageUrl;
                                }

                                var newImage = new Media
                                {
                                    title = imageUrl.Substring(imageUrl.LastIndexOf('/') + 1),
                                    alt_text = imageUrl.Substring(imageUrl.LastIndexOf('/') + 1),
                                    source_url = imageUrl,
                                };
                                newImage = await _wordPressClient.AddImageMedia(newImage);
                                if (newImage != null)
                                {
                                    newProduct.images.Add(new ProductImage
                                    {
                                        id = newImage.id,
                                        name = newImage.title,
                                        alt = newImage.alt_text,
                                        src = newImage.source_url
                                    });
                                }
                                imagesImportProgress++;
                            }

                            var prodCategories = new List<ProductCategoryLine> {
                                new ProductCategoryLine
                                {
                                    id = wcCategory?.id,
                                    name = wcCategory?.name,
                                    slug = wcCategory?.slug
                                }
                            };
                            newProduct.categories = prodCategories;

                            ProgressTracker.Add(currentRequest, $"{DateTimeOffset.Now} :: ImportCategoryProducts :: Saving new Product: {newProduct.sku}...");
                            newProduct = await _woocommerceClient.CreateNewProduct(newProduct);
                            ProgressTracker.Add(currentRequest, $"{DateTimeOffset.Now} :: ImportCategoryProducts :: Product {newProduct.sku} succesfully imported ({productsImportProgress}/{productsCount})...");
                        }
                    }
                    else
                    {
                        if (!wcProducts.Any(x => x.sku == arProductDetails.ProductCode))
                        {
                            ProgressTracker.Add(currentRequest, $"{DateTimeOffset.Now} :: ImportCategoryProducts :: AR Product not in WC, proceeding with import ({productsImportProgress}/{productsCount})...");

                            newProduct = new Product
                            {
                                type = "variable",
                                status = "publish",
                                featured = false,
                                catalog_visibility = "visible",
                                images = new List<ProductImage>(),
                                sku = arProductDetails.ProductCode,
                                slug = arProductDetails.ProductCode,
                                name = arProductDetails.ProductName,
                                description = System.Web.HttpUtility.HtmlDecode(arProductDetails.ProductDescription),
                                short_description = System.Web.HttpUtility.HtmlDecode(arProductDetails.ProductDescription),
                                price = GetItemPrice(arProductDetails.Price),
                                regular_price = GetItemPrice(arProductDetails.Price)
                            };

                            // Images
                            foreach (var arImage in arProductDetails.Images)
                            {
                                ProgressTracker.Add(currentRequest, $"{DateTimeOffset.Now} :: ImportCategoryProducts :: Adding product image: {arImage.Name} ({imagesImportProgress}/{imagesCount})...");
                                string imageUrl = "";
                                if (!string.IsNullOrEmpty(arImage.ImageUrlXL))
                                {
                                    imageUrl = arImage.ImageUrlXL;
                                }
                                else if (!string.IsNullOrEmpty(arImage.ImageUrl3x))
                                {
                                    imageUrl = arImage.ImageUrl3x;
                                }
                                else if (!string.IsNullOrEmpty(arImage.ImageUrl2x))
                                {
                                    imageUrl = arImage.ImageUrl2x;
                                }
                                else if (!string.IsNullOrEmpty(arImage.ImageUrl))
                                {
                                    imageUrl = arImage.ImageUrl;
                                }

                                var newImage = new Media
                                {
                                    title = imageUrl.Substring(imageUrl.LastIndexOf('/') + 1),
                                    alt_text = imageUrl.Substring(imageUrl.LastIndexOf('/') + 1),
                                    source_url = imageUrl,
                                };
                                newImage = await _wordPressClient.AddImageMedia(newImage);
                                if (newImage != null)
                                {
                                    newProduct.images.Add(new ProductImage
                                    {
                                        id = newImage.id,
                                        name = newImage.title,
                                        alt = newImage.alt_text,
                                        src = newImage.source_url
                                    });
                                }
                                imagesImportProgress++;
                            }

                            var prodCategories = new List<ProductCategoryLine> {
                                new ProductCategoryLine
                                {
                                    id = wcCategory?.id,
                                    name = wcCategory?.name,
                                    slug = wcCategory?.slug
                                }
                            };
                            newProduct.categories = prodCategories;

                            ProgressTracker.Add(currentRequest, $"{DateTimeOffset.Now} :: ImportCategoryProducts :: Saving new Product: {newProduct.sku}...");
                            newProduct = await _woocommerceClient.CreateNewProduct(newProduct);

                            if (arProductDetails.StockLevel != null && arProductDetails.StockLevel.Levels != null)
                            {
                                variationsCount = arProductDetails.StockLevel.Levels.Count();
                                variationImportProgress = 1;
                                bool hasColors = arProductDetails.StockLevel.Levels.Select(x => x.ColourName).Distinct().Count() > 1;
                                bool hasSizes = arProductDetails.StockLevel.Levels.Select(x => x.SizeCode).Distinct().Count() > 1;

                                ProgressTracker.Add(currentRequest, $"{DateTimeOffset.Now} :: ImportCategoryProducts :: Product has variations...");
                                if (hasColors)
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
                                }

                                if (hasSizes)
                                {
                                    var sizeAttributes = new ProductAttributeLine
                                    {
                                        id = wcProductAttributes.First(x => x.name == "Size").id,
                                        name = "Size",
                                        visible = true,
                                        variation = true,
                                        options = arProductDetails.StockLevel.Levels.Select(x => x.SizeCode).Distinct().ToList()
                                    };
                                    newProduct.attributes.Add(sizeAttributes);
                                }

                                newProduct = await _woocommerceClient.UpdateProduct(newProduct);

                                foreach (var productVariation in arProductDetails.StockLevel.Levels)
                                {
                                    List<VariationAttribute> productVariationAttributes = new List<VariationAttribute>();
                                    if (hasColors)
                                    {
                                        productVariationAttributes.Add(new VariationAttribute
                                        {
                                            id = wcProductAttributes.First(x => x.name == "Color").id,
                                            name = "Color",
                                            option = productVariation.ColourName
                                        }
                                        );
                                    }
                                    if (hasSizes)
                                    {
                                        productVariationAttributes.Add(new VariationAttribute
                                        {
                                            id = wcProductAttributes.First(x => x.name == "Size").id,
                                            name = "Size",
                                            option = productVariation.SizeCode
                                        }
                                        );
                                    }

                                    var newProductVariation = new Variation
                                    {
                                        sku = productVariation.ItemCode == newProduct.sku ? null : productVariation.ItemCode,
                                        attributes = productVariationAttributes,
                                        stock_quantity = productVariation.InStock,
                                        manage_stock = true,
                                        regular_price = GetItemPrice(arProductDetails.VarientPrices?.FirstOrDefault(x => x.SizeCode == productVariationAttributes.First(x => x.name == "Size").option)?.Price ?? arProductDetails.Price)
                                    };

                                    ProgressTracker.Add(currentRequest, $"{DateTimeOffset.Now} :: ImportCategoryProducts :: Adding product variation ({variationImportProgress}/{variationsCount})...");
                                    newProductVariation = await _woocommerceClient.CreateNewProductVariation(newProductVariation, newProduct.id.Value);
                                    newProduct.variations.Add((int)newProductVariation.id.Value);
                                    variationImportProgress++;
                                }
                                ProgressTracker.Add(currentRequest, $"{DateTimeOffset.Now} :: ImportCategoryProducts :: Saving product variations...");
                                await _woocommerceClient.UpdateProduct(newProduct);
                            }
                            ProgressTracker.Add(currentRequest, $"{DateTimeOffset.Now} :: ImportCategoryProducts :: Product {newProduct.sku} succesfully imported ({productsImportProgress}/{productsCount})...");
                            wcProducts.Add(newProduct);
                        }
                    }
                    productsImportProgress++;
                }
                ProgressTracker.Add(currentRequest, $"{DateTimeOffset.Now} :: done");
            }
            catch (Exception ex)
            {
                ProgressTracker.Add(currentRequest, $"{DateTimeOffset.Now} :: An Error occured: {ex.Message}{ex.InnerException}");
                try
                {
                    if (newProduct.images != null && newProduct.images.Count > 0)
                    {
                        foreach (var image in newProduct.images)
                        {
                            await _wordPressClient.DeleteImageMedia((int)image.id);
                        }
                    }
                    if (newProduct.id != null)
                        await _woocommerceClient.DeleteProduct(newProduct);
                }
                catch (Exception innerEx)
                {
                    ProgressTracker.Add(currentRequest, $"{DateTimeOffset.Now} :: An Error occured during rollback: {innerEx.Message}{Environment.NewLine}{innerEx.InnerException}");
                }
            }
        }

        async Task<List<int>> AddExistingProductsToNewCateogory(Models.Amrod.AmrodCategory amrodCategory, ProductCategory wcCategory, IEnumerable<Models.Amrod.AmrodProduct> arProducts, List<Product> wcProducts)
        {
            List<int> existingProductIds = new List<int>();
            int productsCount = arProducts.Count();
            int productsImportProgress = 1;
            foreach (var product in arProducts)
            {
                if (wcProducts.Any(x => x.sku == product.ProductCode))
                {
                    ProgressTracker.Add(currentRequest, $"{DateTimeOffset.Now} :: ImportCategoryProducts :: AR Product already in WC, checking categories ({productsImportProgress}/{productsCount})...");

                    var existingProduct = wcProducts.First(x => x.sku == product.ProductCode);
                    if (!existingProduct.categories.Any(x => x.name == wcCategory.name))
                    {
                        ProgressTracker.Add(currentRequest, $"{DateTimeOffset.Now} :: ImportCategoryProducts :: Adding new category to product: {wcCategory.name}...");
                        var prodCategory = new ProductCategoryLine
                        {
                            id = wcCategory?.id,
                            name = wcCategory?.name,
                            slug = wcCategory?.slug
                        };
                        existingProduct.categories.Add(prodCategory);
                        existingProduct = await _woocommerceClient.UpdateProduct(existingProduct);
                    }
                    existingProductIds.Add(product.ProductId);
                }
                productsImportProgress++;
            }
            return existingProductIds;
        }

        decimal GetItemPrice(decimal? cost)
        {
            var itemCost = cost ?? 0.00m;
            if (_wcOptions.AddVat)
                itemCost *= 1.15m;

            var itemPrice = itemCost / (1 - _wcOptions.ProfitMargin);
            if (_wcOptions.AddRounding)
                itemPrice = Math.Ceiling(itemPrice) - 0.05m;

            return Math.Round(itemPrice,2, MidpointRounding.AwayFromZero);
        }

        bool IsSimpleProduct(Models.Amrod.AmrodProductDetail arProductDetails)
        {
            if (arProductDetails.StockLevel == null || arProductDetails.StockLevel.Levels == null || arProductDetails.StockLevel.Levels.Count() <= 1)
                return true;

            return false;
        }

        bool SkipChildVariationsOfExisting(Models.Amrod.AmrodProductDetail arProductDetails, IEnumerable<Models.Amrod.AmrodProduct> categoryProducts, List<Product> wcProducts)
        {
            var parentCodeParts = arProductDetails.StockLevel?.Levels?.FirstOrDefault()?.ItemBaseCode?.Split('-');
            var productCoreParts = arProductDetails.ProductCode.Split('-');

            // Check if any existing WC products exist for the Base product of the variation
            if (wcProducts.Any(w => w.sku == arProductDetails.StockLevel?.Levels?.FirstOrDefault()?.ItemBaseCode))
                return true;

            if (arProductDetails.StockLevel?.Levels?.FirstOrDefault()?.ItemBaseCode == arProductDetails.ProductCode)
                return false;

            if (parentCodeParts.Length > 1 && categoryProducts.Any(x => x.ProductCode == arProductDetails.StockLevel?.Levels?.FirstOrDefault()?.ItemBaseCode) && parentCodeParts[0] == productCoreParts[0] && parentCodeParts[1] == productCoreParts[1])
                return true;
            else
                return false;
        }
    }
}