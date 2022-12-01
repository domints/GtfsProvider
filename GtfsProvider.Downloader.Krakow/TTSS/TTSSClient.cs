using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Enums;
using GtfsProvider.Common.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GtfsProvider.Downloader.Krakow.TTSS
{
    public interface ITTSSClient
    {
        Task<TTSSVehiclesInfo?> GetVehiclesInfo(VehicleType type);
    }

    public class TTSSClient : ITTSSClient
    {
        private const string busTTSSVehicleList = "http://ttss.mpk.krakow.pl/internetservice/geoserviceDispatcher/services/vehicleinfo/vehicles?positionType=CORRECTED";
        private const string tramTTSSVehicleList = "http://www.ttss.krakow.pl/internetservice/geoserviceDispatcher/services/vehicleinfo/vehicles?positionType=CORRECTED";
        private readonly ILogger<TTSSClient> logger;
        private readonly IHttpClientFactory clientFactory;
        private readonly ICreationSafeCache cache;

        public TTSSClient(ICreationSafeCache cache,
            ILogger<TTSSClient> logger,
            IHttpClientFactory clientFactory)
        {
            this.clientFactory = clientFactory;
            this.logger = logger;
            this.cache = cache;
        }

        public async Task<TTSSVehiclesInfo?> GetVehiclesInfo(VehicleType type)
        {
            return await cache.GetOrCreateSafeAsync($"Downloader_Krakow_TTSSClient_GetVehiclesInfo_{type}", async cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5);
                var client = clientFactory.CreateClient($"Downloader_Krakow_TTSSClient");
                var jsonData = string.Empty;
                try
                {
                    if (type == VehicleType.Tram)
                        jsonData = await client.GetStringAsync(tramTTSSVehicleList);
                    if (type == VehicleType.Bus)
                        jsonData = await client.GetStringAsync(busTTSSVehicleList);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to download TTSS data for type {VehicleType}", type);
                }

                if (string.IsNullOrEmpty(jsonData))
                    return null;

                return JsonConvert.DeserializeObject<TTSSVehiclesInfo>(jsonData);
            });
        }
    }
}