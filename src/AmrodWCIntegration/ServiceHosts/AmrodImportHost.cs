using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AmrodWCIntegration.Clients.Amrod;

using Microsoft.Extensions.Hosting;

namespace AmrodWCIntegration.ServiceHosts
{
    public class AmrodImportHost : IHostedService
    {
        private readonly AmrodImportService _amrodImportService;

        public AmrodImportHost(AmrodImportService amrodImportService)
        {
            _amrodImportService = amrodImportService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return _amrodImportService.ImportAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
