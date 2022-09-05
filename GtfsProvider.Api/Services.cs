using GtfsProvider.Common;
using GtfsProvider.Downloader.Krakow.Extensions;
using GtfsProvider.MemoryStorage;
using GtfsProvider.Services;

namespace GtfsProvider.Api
{
    public static class Services
    {
        public static IServiceCollection AddAppServices(this IServiceCollection services)
        {
            services.RegisterMemoryDataStorage();
            services.RegisterKrakowDownloader();
            services.AddHttpClient();
            services.AddScoped<IFileStorage, LocalFileStorage>();
            services.AddScoped<IStopService, StopService>();

            return services;
        }
    }
}