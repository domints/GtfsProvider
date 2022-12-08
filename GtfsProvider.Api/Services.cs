using GtfsProvider.Common;
using GtfsProvider.Common.Utils;
using GtfsProvider.CityClient.Krakow.Extensions;
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
            services.AddMemoryCache();
            services.AddTransient<ICreationSafeCache, CreationSafeCache>();
            services.AddScoped<IFileStorage, LocalFileStorage>();
            services.AddScoped<IStopService, StopService>();
            services.AddScoped<IVehicleService, VehicleService>();
            services.AddScoped<ILiveDataService, LiveDataService>();
            services.AddSingleton<ICityStorageFactory, CityStorageFactory>();

            return services;
        }
    }
}