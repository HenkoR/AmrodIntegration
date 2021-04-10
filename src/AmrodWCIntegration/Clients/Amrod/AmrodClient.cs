using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using AmrodWCIntegration.Config;
using AmrodWCIntegration.Models.Amrod;

using Microsoft.Extensions.Options;

namespace AmrodWCIntegration.Clients.Amrod
{
    public class AmrodClient
    {
        public HttpClient Client { get; }
        private readonly AmrodOptions amrodOptions;

        public AmrodClient(IOptions<AmrodOptions> optionsAccessor, HttpClient client)
        {
            amrodOptions = optionsAccessor.Value;
            client.BaseAddress = new Uri(amrodOptions.ApiUri);
            client.DefaultRequestHeaders.Add("X-AMROD-IMPERSONATE", amrodOptions.ClientCode);
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var authTokenValue = $"Amrod type=\"{amrodOptions.TokenType}\", token=\"{amrodOptions.ApiToken}\"";

            client.DefaultRequestHeaders.Add("Authorization", authTokenValue);

            Client = client;
        }
        
        internal async Task<List<AmrodCategory>> GetAllCategoriesAsync()
        {
            var response = await Client.PostAsync("Catalogue/getCategoryTree", null);

            response.EnsureSuccessStatusCode();

            using var responseStream = await response.Content.ReadAsStreamAsync();

            var result = await JsonSerializer.DeserializeAsync<AmrodBaseResponse<AmrodCategoryTreeResponse>>(responseStream);

            var categories = result?.Body?.Categories.ToList();
            foreach (var cat in result.Body.Categories)
            {
                categories.AddRange(cat.SubCategories);
            }

            return categories;
        }

        internal async Task<IEnumerable<AmrodCategory>> GetTopLevelCategoriesAsync()
        {
            var response = await Client.PostAsync("Catalogue/getCategoryTree", null);

            response.EnsureSuccessStatusCode();

            using var responseStream = await response.Content.ReadAsStreamAsync();

            var result = await JsonSerializer.DeserializeAsync<AmrodBaseResponse<AmrodCategoryTreeResponse>>(responseStream);
            return result?.Body?.Categories;
        }

        internal async Task<AmrodCategory> GetCategoryAsync(int categoryId)
        {
            var requestBody = new StringContent(
                JsonSerializer.Serialize(new { categoryId = categoryId, IncludeClearanceItems = false }),
                Encoding.UTF8,
                "application/json");
            var response = await Client.PostAsync("Catalogue/getCategoryTree", requestBody);

            response.EnsureSuccessStatusCode();

            using var responseStream = await response.Content.ReadAsStreamAsync();

            var result = await JsonSerializer.DeserializeAsync<AmrodBaseResponse<AmrodCategoryTreeResponse>>(responseStream);
            return result?.Body?.Categories?.First();
        }

        internal async Task<IEnumerable<AmrodProduct>> GetCategoryProducts(int categoryId)
        {
            var requestBody = new StringContent(
                JsonSerializer.Serialize(new { categoryId = categoryId, IncludeClearanceItems = false }),
                Encoding.UTF8,
                "application/json");

            var response = await Client.PostAsync("Catalogue/getCategoryProducts", requestBody);

            response.EnsureSuccessStatusCode();

            using var responseStream = await response.Content.ReadAsStreamAsync();

            var result = await JsonSerializer.DeserializeAsync<AmrodBaseResponse<AmrodCategoryProductsResponse>>(responseStream);
            return result?.Body?.Products;
        }

        internal async Task<AmrodProductDetail> GetProductDetails(int productId)
        {
            var requestBody = new StringContent(
                JsonSerializer.Serialize(new { productId = productId }),
                Encoding.UTF8,
                "application/json"
                );

            var response = await Client.PostAsync("Catalogue/getProductDetail", requestBody);

            response.EnsureSuccessStatusCode();
            using var responseStream = await response.Content.ReadAsStreamAsync();

            var result = await JsonSerializer.DeserializeAsync<AmrodBaseResponse<AmrodProductDetail>>(responseStream);
            return result?.Body;
        }
    }
}
