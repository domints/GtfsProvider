using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common;
using GtfsProvider.Common.Enums;
using GtfsProvider.Common.Models;
using GtfsProvider.Downloader.Krakow.TTSS;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ProtoBuf;
using TransitRealtime;

namespace GtfsProvider.Downloader.Krakow
{
    public class VehicleDbBuilder
    {
        private readonly IFileStorage _fileStorage;
        private readonly ICityStorage _storage;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<VehicleDbBuilder> _logger;
        private const string busMatchRulesUrl = "https://raw.githubusercontent.com/jacekkow/mpk-ttss-mapping/master/lib/BusTypes.php";
        private const string tramMatchRulesUrl = "https://raw.githubusercontent.com/jacekkow/mpk-ttss-mapping/master/lib/TramTypes.php";

        private const string busTTSSVehicleList = "http://ttss.mpk.krakow.pl/internetservice/geoserviceDispatcher/services/vehicleinfo/vehicles";
        private const string tramTTSSVehicleList = "http://www.ttss.krakow.pl/internetservice/geoserviceDispatcher/services/vehicleinfo/vehicles?positionType=CORRECTED";

        private long TTSSRefreshTime;
        private long GTFSRefreshTime;

        private List<VehicleMatchRule> matchRules = new();
        private Dictionary<string, VehicleModel> modelDict = new();

        private Dictionary<string, List<TTSSCleanVehicle>> ttssTrips = new();
        private Dictionary<string, TTSSCleanVehicle> ttssVehicleToTrip = new();

        public VehicleDbBuilder(
            IFileStorage fileStorage,
            IDataStorage storage,
            IHttpClientFactory httpClientFactory,
            ILogger<VehicleDbBuilder> logger)
        {
            this._fileStorage = fileStorage;
            this._storage = storage[City.Krakow];
            this._httpClientFactory = httpClientFactory;
            this._logger = logger;
        }

        public async Task Build(VehicleType type,
            string positionsFileName)
        {
            var matchRulesFetchSuccess = await BuildMatchRules(type);
            if (!matchRulesFetchSuccess)
                return;

            await LoadTTSSData(type);
            await LoadGTFSData(positionsFileName);
        }

        private async Task<bool> LoadTTSSData(VehicleType type)
        {
            var client = _httpClientFactory.CreateClient($"Downloader_Krakow_TTSS");
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
                _logger.LogError(ex, "Failed to download TTSS data for type {VehicleType}", type);
            }

            if (string.IsNullOrEmpty(jsonData))
                return false;

            var vehiclesInfo = JsonConvert.DeserializeObject<TTSSVehiclesInfo>(jsonData);
            TTSSRefreshTime = vehiclesInfo.LastUpdate;

            foreach(var v in vehiclesInfo.Vehicles)
            {
                if(v.IsDeleted || string.IsNullOrWhiteSpace(v.TripId)
                    || string.IsNullOrWhiteSpace(v.Name)
                    || !v.Latitude.HasValue || !v.Longitude.HasValue)
                    continue;
                
                var name = v.Name.Split(' ');
                var vehicleEntry = new TTSSCleanVehicle
                {
                    Id = v.Id,
                    Line = name[0].Trim(),
                    Direction = name.Length > 1 ? name[1].Trim() : string.Empty,
                    Latitude = v.Latitude.Value / 3600000.0m,
                    Longitude = v.Latitude.Value / 3600000.0m
                };

                if(!ttssTrips.ContainsKey(v.TripId))
                    ttssTrips.Add(v.TripId, vehicleEntry);
                else
                    System.Diagnostics.Debugger.Break();
                ttssVehicleToTrip.Add(v.Id, vehicleEntry);
            }

            return true;
        }

        private async Task<bool> LoadGTFSData(string positionsFileName)
        {
            using var positionStream = await _fileStorage.LoadFile(City.Krakow, positionsFileName);
            var feedMessage = Serializer.Deserialize<FeedMessage>(positionStream);
            GTFSRefreshTime = (long)feedMessage.Header.Timestamp;
            return true;
        }

        private async Task<bool> BuildMatchRules(VehicleType type)
        {
            var client = _httpClientFactory.CreateClient($"Downloader_Krakow_MatchRules");
            var matchRulesRawPhp = string.Empty;
            try
            {
                if (type == VehicleType.Tram)
                    matchRulesRawPhp = await client.GetStringAsync(tramMatchRulesUrl);
                if (type == VehicleType.Bus)
                    matchRulesRawPhp = await client.GetStringAsync(busMatchRulesUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download matchrules file for type {VehicleType}", type);
            }

            if (string.IsNullOrEmpty(matchRulesRawPhp))
                return false;

            var lines = matchRulesRawPhp.Split('\n');
            var gotStart = false;
            foreach(var line in lines)
            {
                if(!gotStart)
                {
                    if(line.Contains("<<<'END'"))
                        gotStart = true;

                    continue;
                }

                if(line.Contains("END"))
                    break;

                var lineData = line.Split('\t');
                var modelName = lineData[3].Trim();
                var lowFloor = lineData.Length > 4 ? (LowFloor)int.Parse(lineData[4].Trim()) : LowFloor.Unknown;
                if(type == VehicleType.Bus)
                    lowFloor = LowFloor.Full;

                var matchRule = new VehicleMatchRule
                {
                    FromId = int.Parse(lineData[0].Trim()),
                    ToId = int.Parse(lineData[1].Trim()),
                    Symbol = lineData[2].Trim(),
                    ModelName = lineData[3].Trim()
                };

                matchRules.Add(matchRule);

                if(!modelDict.ContainsKey(modelName))
                {
                    modelDict.Add(modelName, new VehicleModel
                    {
                        Name = modelName,
                        LowFloor = lowFloor
                    });
                }
            }

            return true;
        }
    }
}