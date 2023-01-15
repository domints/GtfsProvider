using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

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
                using var scope = Services.CreateScope();

                foreach (var downloader in scope.ServiceProvider.GetServices<IDownloader>())
                {
                    try
                    {
                        await downloader.RefreshIfNeeded();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(Events.FailedToExecuteDownloader, ex, "Failed to execute downloader for {city}!", downloader.City);
                    }
                }

                if (!Initialized)
                {
                    var server = Services.GetService<Microsoft.AspNetCore.Hosting.Server.IServer>();
                    if (server != null)
                    {
                        var addressesFeature = server.Features.Get<IServerAddressesFeature>();
                        _logger.LogDebug("Initialized, swagger on: {address}", addressesFeature?.Addresses.FirstOrDefault() + "/swagger");
                    }
                    else
                    {
                        _logger.LogDebug("Where the heck is my sever?");
                    }
                }

                Initialized = true;
            }
        }
    }
}