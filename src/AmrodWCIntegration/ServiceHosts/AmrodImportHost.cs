using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AmrodWCIntegration.Clients.Amrod;
using AmrodWCIntegration.Config;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using NCrontab;

namespace AmrodWCIntegration.ServiceHosts
{
    public class AmrodImportHost : IHostedService
    {
        private readonly AmrodImportService _amrodImportService;
        private readonly CrontabSchedule _crontabSchedule;
        private DateTime _nextRun;
        private readonly AmrodImportService _task;
        private readonly CronOptions _cronOptions;

        public AmrodImportHost(AmrodImportService amrodImportService, IOptions<CronOptions> optionsAccessor)
        {
            _cronOptions = optionsAccessor.Value;
            _amrodImportService = amrodImportService;
            _crontabSchedule = CrontabSchedule.Parse(_cronOptions.Schedule, new CrontabSchedule.ParseOptions { IncludingSeconds = true });
            _nextRun = _crontabSchedule.GetNextOccurrence(DateTime.Now);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(UntilNextExecution(), cancellationToken); // wait until next time

                    await _task.ImportAsync(cancellationToken); //execute some task

                    _nextRun = _crontabSchedule.GetNextOccurrence(DateTime.Now);
                }
            }, cancellationToken);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private int UntilNextExecution() => Math.Max(0, (int)_nextRun.Subtract(DateTime.Now).TotalMilliseconds);

    }
}
