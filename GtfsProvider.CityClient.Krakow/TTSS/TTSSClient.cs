using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.CityClient.Krakow.TTSS.Passages;
using GtfsProvider.Common.Enums;
using GtfsProvider.Common.Extensions;
using GtfsProvider.Common.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GtfsProvider.CityClient.Krakow.TTSS
{
    public interface IKrakowTTSSClient
    {
        Task<TTSSVehiclesInfo?> GetVehiclesInfo(VehicleType type);
        Task<StopInfo?> GetStopInfo(VehicleType type, string groupId, DateTime? startTime, int? timeFrame);
    }

    public class KrakowTTSSClient : IKrakowTTSSClient
    {
        private const string busHost = "http://ttss.mpk.krakow.pl/";
        private const string tramHost = "http://www.ttss.krakow.pl/";
        private const string stopPassagesPath = "internetservice/services/passageInfo/stopPassages/stop";
        private const string stopPostPassagesPath = "internetservice/services/passageInfo/stopPassages/stopPost";
        private const string vehiclesListPath = "internetservice/geoserviceDispatcher/services/vehicleinfo/vehicles?positionType=CORRECTED";
        private readonly ILogger<KrakowTTSSClient> _logger;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ICreationSafeCache _cache;

        public KrakowTTSSClient(ICreationSafeCache cache,
            ILogger<KrakowTTSSClient> logger,
            IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
            _logger = logger;
            _cache = cache;
        }

        public async Task<StopInfo?> GetStopInfo(VehicleType type, string groupId, DateTime? startTime, int? timeFrame)
        {
            return await _cache.GetOrCreateSafeAsync($"Downloader_Krakow_TTSSClient_GetStopInfo_{type}_{groupId}_{startTime}_{timeFrame}", async cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10);
                var client = GetHttpClient(type);
                if (client == null)
                    return null;

                int? intStartTime = null;
                if (startTime.HasValue)
                {
                    intStartTime = (int)Math.Round((startTime.Value - DateTime.UnixEpoch).TotalMilliseconds);
                }

                var request = new PassageRequest
                {
                    Stop = groupId,
                    Language = "pl",
                    Mode = "departure",
                    StartTime = intStartTime,
                    TimeFrame = timeFrame
                };

                return await client.PostFormToGetJson<PassageRequest, StopInfo>(stopPassagesPath, request);
            });
        }

        public async Task<TTSSVehiclesInfo?> GetVehiclesInfo(VehicleType type)
        {
            return await _cache.GetOrCreateSafeAsync($"Downloader_Krakow_TTSSClient_GetVehiclesInfo_{type}", async cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5);
                var client = GetHttpClient(type);
                if (client == null)
                    return null;

                var jsonData = string.Empty;
                try
                {
                    jsonData = await client.GetStringAsync(vehiclesListPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to download TTSS data for type {VehicleType}", type);
                }

                if (string.IsNullOrEmpty(jsonData))
                    return null;

                return JsonConvert.DeserializeObject<TTSSVehiclesInfo>(jsonData);
            });
        }

        private HttpClient? GetHttpClient(VehicleType type)
        {
            var client = _clientFactory.CreateClient($"Downloader_Krakow_TTSSClient_{type}");
            if (type == VehicleType.Bus)
            {
                client.BaseAddress = new Uri(busHost);
            }
            else if (type == VehicleType.Tram)
            {
                client.BaseAddress = new Uri(tramHost);
            }
            else
            {
                _logger.LogError("Unknown vehicle type: {type}", type);
                return null;
            }

            return client;
        }
    }
}