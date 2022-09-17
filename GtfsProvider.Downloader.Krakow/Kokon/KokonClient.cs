using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GtfsProvider.Downloader.Krakow.Kokon
{
    public class KokonClient
    {
        private const string GetVehiclesUrl = "http://91.223.13.52:3000/v_vehicles";
        private const string GetCompleteVehPos = "http://91.223.13.52:3000/v_complete_all_vehs_pos?lat=not.is.null&lon=not.is.null&veh_ts=not.is.null";
        private readonly ILogger<KokonClient> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Regex SideNoRx = new("^[PBDHR][A-Z][0-9]{3}$", RegexOptions.Compiled);

        public KokonClient(IHttpClientFactory httpClientFactory,
            ILogger<KokonClient> logger)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<KokonVehicle>> GetVehicles()
        {
            var client = _httpClientFactory.CreateClient("Downloader_Krakow_Kokon");
            var vehicles = await client.GetFromJsonAsync<List<KokonVehiclesResponseModel>>(GetVehiclesUrl);
            return vehicles
                .Where(v => SideNoRx.IsMatch(v.SideNo))
                .DistinctBy(v => v.SideNo)
                .Select(v => KokonVehicle.FromSideNo(v.SideNo))
                .ToList();
        }

        public async Task<List<KokonVehicleCompletePositionResponseModel>> GetCompleteVehsPos()
        {
            var client = _httpClientFactory.CreateClient("Downloader_Krakow_Kokon");
            return (await client.GetFromJsonAsync<List<KokonVehicleCompletePositionResponseModel>>(GetCompleteVehPos))
                .Where(v => SideNoRx.IsMatch(v.SideNo))
                .DistinctBy(v => v.SideNo)
                .OrderBy(v => KokonVehicle.FromSideNo(v.SideNo).VehicleNo)
                .ToList();
        }
    }
}