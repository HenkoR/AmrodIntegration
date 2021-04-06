using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AmrodWCIntegration.Clients.Amrod;
using AmrodWCIntegration.Clients.Wordpress;
using AmrodWCIntegration.Services;

namespace AmrodWCIntegration.ServiceHosts
{
    public sealed class AmrodImportService
    {
        private readonly WcAmrodSync _wcAmrodSync;
        public AmrodImportService(WcAmrodSync wcAmrodSync)
        {
            _wcAmrodSync = wcAmrodSync;
        }

        public async Task ImportAsync(CancellationToken ct = default)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var CategoryImportTask = _wcAmrodSync.ImportCategoriesAsync(ct);
                    var ProductImportTask = _wcAmrodSync.ImportProductsAsync(ct);
                    await Task.WhenAll(CategoryImportTask, ProductImportTask);
                }
            }
            finally
            {
                //cleanup
            }
        }

        
    }
}
