using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using AmrodWCIntegration.Config;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

using WooCommerce.NET.WordPress.v2;

using WooCommerceNET;

namespace AmrodWCIntegration.Clients.Wordpress
{
    public class WordPressClient
    {
        readonly RestAPI restAPI;
        private readonly WpOptions wpOptions;
        private readonly IWebHostEnvironment _env;

        public WordPressClient(IOptions<WpOptions> optionsAccessor, IWebHostEnvironment env)
        {
            wpOptions = optionsAccessor.Value;
            _env = env;
            restAPI = new RestAPI(wpOptions.ApiUri, wpOptions.ApiKey, wpOptions.ApiSecret);
            //using OAuth
            //restAPI.oauth_token = wpOptions.OAuth_Token;
            //restAPI.oauth_token_secret = wpOptions.OAuth_Token_Secret;
        }

        public async Task<Media> AddImageMedia(Media mediaItem)
        {
            WPObject wp = new WPObject(restAPI);
            var filePath = Path.Combine(_env.ContentRootPath, "wpImage");

            if (Directory.Exists(filePath))
            {
                Directory.Delete(filePath, true);
            }
            
            Directory.CreateDirectory(filePath);

            filePath = Path.Combine(_env.ContentRootPath, "wpImage", mediaItem.title);

            using var imageClient = new HttpClient();
            using (var file = await imageClient.GetAsync(mediaItem.source_url))
            {
                if (file.IsSuccessStatusCode)
                {
                    var s = await file.Content.ReadAsByteArrayAsync();
                    var contentType = file.Content.Headers.ContentType.MediaType;

                    using FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                    stream.Write(s, 0, s.Length);
                }
            }

            return await wp.Media.Add(mediaItem.title, filePath);
        }
    }
}
