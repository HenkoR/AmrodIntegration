using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using AmrodWCIntegration.Clients.Amrod;
using AmrodWCIntegration.Clients.Wordpress;
using AmrodWCIntegration.Models.Amrod;
using AmrodWCIntegration.Services;
using AmrodWCIntegration.Utils;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

using WooCommerceNET.WooCommerce.v3;

namespace AmrodWCIntegration.Pages
{
    public class CategoryImportModel : PageModel
    {
        private readonly ILogger<CategoryImportModel> _logger;
        private readonly WoocommerceClient woocommerce;
        private readonly AmrodClient amrod;
        private readonly WcAmrodSync _wcAmrodSync;

        public ProductCategory wcCategory;
        public AmrodCategory amrodCategory;
        public Dictionary<int, string> Progress = new Dictionary<int, string>();
        public string StatusMessage = string.Empty;

        public CategoryImportModel(ILogger<CategoryImportModel> logger, WoocommerceClient woocommerceClient, AmrodClient amrodClient, WcAmrodSync wcAmrodSync)
        {
            _logger = logger;
            woocommerce = woocommerceClient;
            amrod = amrodClient;
            _wcAmrodSync = wcAmrodSync;
        }

        public async Task<IActionResult> OnGetAsync(int categoryId, string requestId = null)
        {
            amrodCategory = SessionHelper.GetObjectFromJson<AmrodCategory>(HttpContext.Session, "amrodCategory");
            wcCategory = SessionHelper.GetObjectFromJson<ProductCategory>(HttpContext.Session, "wcCategory");
            if ((amrodCategory == null || wcCategory == null) || amrodCategory?.CategoryId != categoryId)
            {
                amrodCategory = await amrod.GetCategoryAsync(categoryId);
                wcCategory = await woocommerce.GetCategory(amrodCategory.CategoryName);
                SessionHelper.SetObjectAsJson(HttpContext.Session, "amrodCategory", amrodCategory);
                SessionHelper.SetObjectAsJson(HttpContext.Session, "wcCategory", wcCategory);
            }

            if (!string.IsNullOrEmpty(requestId))
            {
                StatusMessage = ProgressTracker.GetValue(requestId).ToString();
                if (StatusMessage == "done")
                {
                    StatusMessage = "Import complete!!";
                }
                else if (StatusMessage.StartsWith("An Error occured"))
                {
                    //TODO: Improve how we handle this
                }
                else
                {
                    Response.Headers.Add("Refresh", "5");
                }
                Progress = SessionHelper.GetObjectFromJson<Dictionary<int, string>>(HttpContext.Session, "progress");
                Progress.Add(Progress.Count+1, StatusMessage);
                SessionHelper.SetObjectAsJson(HttpContext.Session, "progress", Progress);
                return Page();
            }
            else
            {
                requestId = Guid.NewGuid().ToString();
                ProgressTracker.Add(requestId, $"{DateTimeOffset.UtcNow} :: Starting Import!!!");
                Progress.Add(Progress.Count + 1,"Starting Import!!!");
                SessionHelper.SetObjectAsJson(HttpContext.Session, "progress", Progress);
                //Call Long running task
                _wcAmrodSync.RunTask(new Func<Task>(() => _wcAmrodSync.ImportCategoryProducts(amrodCategory, wcCategory)), requestId);
                
            }

            return RedirectToPage(new { requestId = requestId.ToString() });
        }
    }
}
