using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common;

namespace GtfsProvider.Api
{
    public class DownloaderService : BackgroundService
    {
        public IServiceProvider Services { get; }
        public bool Initialized { get; private set; }

        public DownloaderService(IServiceProvider services)
        {
            Services = services;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var firstBootDone = false;
            var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
            while (!firstBootDone || (await timer.WaitForNextTickAsync(stoppingToken)
                && !stoppingToken.IsCancellationRequested))
            {
                firstBootDone = true;
                using (var scope = Services.CreateScope())
                {
                    var downloaders = scope.ServiceProvider.GetServices<IDownloader>();
                    foreach(var downloader in downloaders)
                    {
                        await downloader.RefreshIfNeeded();
                    }
                    Initialized = true;
                }
            }
        }
    }
}