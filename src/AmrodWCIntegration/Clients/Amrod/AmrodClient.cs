using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

        public async Task<IEnumerable<AmrodCategory>> GetCategoriesAsync()
        {
            var response = await Client.PostAsync("Catalogue/getCategoryTree", null);

            response.EnsureSuccessStatusCode();

            using var responseStream = await response.Content.ReadAsStreamAsync();

            var result = await JsonSerializer.DeserializeAsync<AmrodBaseResponse<AmrodCategoryTreeResponse>>(responseStream);
            return result?.Body?.Categories;
        }
    }
}
