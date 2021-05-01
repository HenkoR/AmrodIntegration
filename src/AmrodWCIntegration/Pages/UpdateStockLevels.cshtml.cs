using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using AmrodWCIntegration.Services;
using AmrodWCIntegration.Utils;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace AmrodWCIntegration.Pages
{
    public class UpdateStockLevelsModel : PageModel
    {
        private readonly ILogger<UpdateStockLevelsModel> _logger;
        public Dictionary<int, string> Progress = new Dictionary<int, string>();
        public string StatusMessage = string.Empty;
        private readonly WcAmrodSync _wcAmrodSync;

        public UpdateStockLevelsModel(ILogger<UpdateStockLevelsModel> logger, WcAmrodSync wcAmrodSync)
        {
            _logger = logger;
            _wcAmrodSync = wcAmrodSync;
        }

        public async Task<IActionResult> OnGetAsync(string requestId = null)
        {
            if (!string.IsNullOrEmpty(requestId))
            {
                StatusMessage = ProgressTracker.GetValue(requestId).ToString();
                if (StatusMessage == "done")
                {
                    StatusMessage = "Stock Levels Update complete!!";
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
                Progress.Add(Progress.Count + 1, StatusMessage);
                SessionHelper.SetObjectAsJson(HttpContext.Session, "progress", Progress);
                return Page();
            }
            else
            {
                requestId = Guid.NewGuid().ToString();
                ProgressTracker.Add(requestId, $"{DateTimeOffset.UtcNow} :: Starting Stock Levels Update!!!");
                Progress.Add(Progress.Count + 1, "Starting Stock Levels Update!!!");
                SessionHelper.SetObjectAsJson(HttpContext.Session, "progress", Progress);
                //Call Long running task
                _wcAmrodSync.RunTask(new Func<Task>(() => _wcAmrodSync.UpdateStockLevels()), requestId);

            }

            return RedirectToPage(new { requestId = requestId.ToString() });
        }
    }
}
