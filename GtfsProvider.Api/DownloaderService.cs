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
        private readonly ILogger<DownloaderService> _logger;

        public DownloaderService(IServiceProvider services, ILogger<DownloaderService> logger)
        {
            _logger = logger;
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
                        try
                        {
                            await downloader.RefreshIfNeeded();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to execute downloader for {city}!", downloader.City);
                        }
                    }
                    Initialized = true;
                }
            }
        }
    }
}