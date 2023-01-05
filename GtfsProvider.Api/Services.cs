using GtfsProvider.Common;
using GtfsProvider.Common.Utils;
using GtfsProvider.CityClient.Krakow.Extensions;
using GtfsProvider.MemoryStorage;
using GtfsProvider.Services;
using GtfsProvider.CityClient.Wroclaw.Extensions;
using GtfsProvider.RedisStorage;

namespace GtfsProvider.Api
{
    public static class Services
    {
        public static IServiceCollection AddAppServices(this IServiceCollection services)
        {
            services.AddLazyCache();
            services.RegisterRedisDataStorage();
            //services.RegisterMemoryDataStorage();
            services.RegisterKrakowProvider();
            services.RegisterWroclawProvider();
            services.AddHttpClient();
            services.AddMemoryCache();
            services.AddTransient<ICreationSafeCache, CreationSafeCache>();
            services.AddScoped<IFileStorage, LocalFileStorage>();
            services.AddScoped<IStopService, StopService>();
            services.AddScoped<IVehicleService, VehicleService>();
            services.AddScoped<ILiveDataService, LiveDataService>();
            services.AddSingleton<ICityStorageFactory, CityStorageFactory>();
            services.AddSingleton<IDataStorage, DataStorage>();

            return services;
        }
    }
}