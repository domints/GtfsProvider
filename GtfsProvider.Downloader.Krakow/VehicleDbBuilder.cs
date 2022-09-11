using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common;
using GtfsProvider.Common.CityStorages;
using GtfsProvider.Common.Enums;
using GtfsProvider.Common.Extensions;
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
        private readonly IKrakowStorage _storage;
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

        private Dictionary<long, List<TTSSCleanVehicle>> ttssTrips = new();
        private Dictionary<long, List<GTFSCleanVehicle>> gtfsTrips = new();
        private Dictionary<string, TTSSCleanVehicle> ttssVehicleToTrip = new();

        public VehicleDbBuilder(
            IFileStorage fileStorage,
            IDataStorage storage,
            IHttpClientFactory httpClientFactory,
            ILogger<VehicleDbBuilder> logger)
        {
            _fileStorage = fileStorage;
            _storage = storage[City.Krakow] as IKrakowStorage ?? throw new InvalidOperationException("What is wrong with your DI configuration?!");
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task Build(VehicleType type,
            string positionsFileName)
        {
            var matchRulesFetchSuccess = await BuildMatchRules(type);
            if (!matchRulesFetchSuccess)
                return;

            await LoadTTSSData(type);
            await LoadGTFSData(positionsFileName);
            var offset = FindBestOffset();
            _storage.VehicleIdOffset = offset.Value;

            List<Vehicle> matchedSingle = new();
            List<(List<GTFSCleanVehicle> gtfs, List<TTSSCleanVehicle> ttss)> matchedMultiple = new();
            List<List<GTFSCleanVehicle>> unmatchedGtfs = new();
            List<List<TTSSCleanVehicle>> unmatchedTtss = new();
            foreach (var g in gtfsTrips)
            {
                var ttssTripId = g.Key + (offset ?? 0);
                if (!ttssTrips.ContainsKey(ttssTripId))
                {
                    unmatchedGtfs.Add(g.Value);
                    continue;
                }

                var ttssEntry = ttssTrips[ttssTripId];
                if (ttssEntry.Count > 1 || g.Value.Count > 1)
                {
                    matchedMultiple.Add((g.Value, ttssEntry));
                    continue;
                }

                Vehicle entry = new Vehicle
                {
                    TtssId = ttssEntry[0].Id,
                    GtfsId = g.Value[0].Id,
                    SideNo = g.Value[0].Num
                };
                matchedSingle.Add(entry);
            }
            foreach (var te in ttssTrips)
            {
                if (
                    !matchedMultiple.Any(mm => mm.ttss == te.Value) &&
                    !matchedSingle.Any(ms => te.Value.Any(t => t.Id == ms.TtssId))
                    )
                {
                    unmatchedTtss.Add(te.Value);
                }
            }
            Dictionary<long, Vehicle> byGtfsId = matchedSingle.ToDictionary(k => k.GtfsId);
            Dictionary<long, Vehicle> byTtssId = matchedSingle.ToDictionary(k => k.TtssId);
            Dictionary<long, GTFSCleanVehicle> unmatchedGtfsDict = unmatchedGtfs.SelectMany(u => u).ToDictionary(u => u.Id);

            foreach (var mm in matchedMultiple)
            {
                if (mm.gtfs.Count == 1)
                {
                    int bestIx = -1;
                    decimal bestCoordDiff = decimal.MaxValue;
                    for (int i = 0; i < mm.ttss.Count; i++)
                    {
                        decimal latDiff = Math.Abs((decimal)mm.gtfs[0].Latitude - mm.ttss[i].Latitude);
                        decimal lonDiff = Math.Abs((decimal)mm.gtfs[0].Longitude - mm.ttss[i].Longitude);
                        if (latDiff + lonDiff < bestCoordDiff)
                        {
                            bestCoordDiff = latDiff + lonDiff;
                            bestIx = i;
                        }
                    }

                    Vehicle entry = new Vehicle
                    {
                        TtssId = mm.ttss[bestIx].Id,
                        GtfsId = mm.gtfs[0].Id,
                        SideNo = mm.gtfs[0].Num,
                        IsHeuristic = true,
                        HeuristicScore = (100 / mm.ttss.Count) + 10
                    };

                    var others = mm.ttss.Where((_, ix) => ix != bestIx);
                    unmatchedTtss.Add(others.ToList());
                }
                else if (mm.ttss.Count == 1)
                {
                    int bestIx = -1;
                    decimal bestCoordDiff = decimal.MaxValue;
                    for (int i = 0; i < mm.gtfs.Count; i++)
                    {
                        decimal latDiff = Math.Abs(mm.ttss[0].Latitude - (decimal)mm.gtfs[i].Latitude);
                        decimal lonDiff = Math.Abs(mm.ttss[0].Longitude - (decimal)mm.gtfs[i].Longitude);
                        if (latDiff + lonDiff < bestCoordDiff)
                        {
                            bestCoordDiff = latDiff + lonDiff;
                            bestIx = i;
                        }
                    }

                    Vehicle entry = new Vehicle
                    {
                        TtssId = mm.ttss[0].Id,
                        GtfsId = mm.gtfs[bestIx].Id,
                        SideNo = mm.gtfs[bestIx].Num,
                        IsHeuristic = true,
                        HeuristicScore = (100 / mm.gtfs.Count) + 10
                    };

                    var others = mm.gtfs.Where((_, ix) => ix != bestIx);
                    unmatchedGtfs.Add(others.ToList());
                }
                else
                {
                    System.Diagnostics.Debugger.Break();
                }
            }

            List<TTSSCleanVehicle> fookedTTSS = new();
            foreach (var t in unmatchedTtss.SelectMany(x => x))
            {
                // TODO: Need to figure out what to do when between vehicles there is another with non-matching ID.
                var closestUp = 0;
                var closestDown = 0;
                for (int i = 1; i < 10; i++)
                {
                    var up = t.Id + (i * 2);
                    var down = t.Id - (i * 2);
                    if (closestUp == 0 && byTtssId.ContainsKey(up))
                    {
                        closestUp = i;
                    }

                    if (closestDown == 0 && byTtssId.ContainsKey(down))
                    {
                        closestDown = i;
                    }

                    if (closestDown != 0 && closestUp != 0)
                        break;
                }

                var confident = 0;
                var possibleGtfsId = 0L;
                if (closestDown > 0)
                {
                    confident += 25;
                    possibleGtfsId = byTtssId[t.Id - (closestDown * 2)].GtfsId + closestDown;
                    if(unmatchedGtfsDict.ContainsKey(possibleGtfsId))
                        confident += 25;
                }

                if (closestUp > 0)
                {
                    confident += 25;
                    var possibleGtfsVal = byTtssId[t.Id + (closestUp * 2)].GtfsId - closestUp;
                    if (possibleGtfsId == 0)
                    {
                        possibleGtfsId = possibleGtfsVal;
                    }
                    else if(possibleGtfsId != possibleGtfsVal)
                    {
                        confident = 0;
                        possibleGtfsId = 0;
                    }

                    if(unmatchedGtfsDict.ContainsKey(possibleGtfsId))
                        confident += 25;
                }

                if (possibleGtfsId > 0 && confident > 0)
                {
                    if(unmatchedGtfsDict.ContainsKey(possibleGtfsId))
                    {
                        var veh = new Vehicle
                        {
                            TtssId = t.Id,
                            GtfsId = possibleGtfsId,
                            SideNo = unmatchedGtfsDict[possibleGtfsId].Num,
                            IsHeuristic = true,
                            HeuristicScore = confident
                        };
                        matchedSingle.Add(veh);
                        unmatchedGtfsDict.Remove(possibleGtfsId);
                    }
                    else
                    {
                        var rule = FindMatchRule(possibleGtfsId);
                        var symbol = rule?.Symbol ?? "-";
                        var veh = new Vehicle
                        {
                            TtssId = t.Id,
                            GtfsId = possibleGtfsId,
                            SideNo = $"{symbol}{possibleGtfsId:D3}",
                            IsHeuristic = true,
                            HeuristicScore = rule == null ? confident / 4 : confident
                        };
                        matchedSingle.Add(veh);
                    }
                }
                else
                {
                    fookedTTSS.Add(t);
                }
            }

            foreach(var v in matchedSingle)
            {
                await _storage.AddOrUpdateVehicle(v);
            }

            System.Diagnostics.Debugger.Break();
            //var map = MapVehicleIdsToGtfsIds(offset ?? 0);
            // foreach(var veh in gtfsTrips.Values)
            // {
            //     var ttssTripId = veh.TripId - offset ?? 0;
            //     var ttssTripEntry = ttssTrips.GetValueOrDefault(ttssTripId);
            //     if(ttssTripEntry == null)
            //         continue;

            //     var vehicleDto = new Vehicle
            //     {
            //         GtfsId = veh.Id,
            //         TtssId = ttssTrips[ttssTripId].Id,

            //     };

            //     await _storage.AddOrUpdateVehicle(vehicleDto);
            // }
        }

        /*private Dictionary<long, long> MapVehicleIdsToGtfsIds(long offset)
        {
            var result = new Dictionary<long, long>();
            foreach(var gtfsTrip in gtfsTrips)
            {
                var ttssTripId = gtfsTrip.Key + offset;
                if(ttssTrips.ContainsKey(ttssTripId))
                {
                    result.Add(ttssTrips[ttssTripId].Id, gtfsTrip.Value.Id);
                }
            }
            return result;
        }*/

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

            foreach (var v in vehiclesInfo.Vehicles)
            {
                if (v.IsDeleted || string.IsNullOrWhiteSpace(v.TripId)
                    || string.IsNullOrWhiteSpace(v.Name)
                    || !v.Latitude.HasValue || !v.Longitude.HasValue)
                    continue;

                var name = v.Name.Split(' ');
                var vehicleEntry = new TTSSCleanVehicle
                {
                    Id = long.Parse(v.Id),
                    Line = name[0].Trim(),
                    Direction = name.Length > 1 ? name[1].Trim() : string.Empty,
                    Latitude = v.Latitude.Value / 3600000.0m,
                    Longitude = v.Longitude.Value / 3600000.0m
                };

                ttssTrips.AddListItem(long.Parse(v.TripId), vehicleEntry);
                ttssVehicleToTrip.Add(v.Id, vehicleEntry);
            }

            return true;
        }

        private async Task<bool> LoadGTFSData(string positionsFileName)
        {
            using var positionStream = await _fileStorage.LoadFile(City.Krakow, positionsFileName);
            var feedMessage = Serializer.Deserialize<FeedMessage>(positionStream);
            GTFSRefreshTime = (long)feedMessage.Header.Timestamp;
            FeedEntity tmp = null;
            foreach (var m in feedMessage.Entities)
            {
                var convertedTripId = ConvertTripId(m.Vehicle.Trip.TripId);
                var vehicle = new GTFSCleanVehicle
                {
                    Id = long.Parse(m.Id),
                    Num = m.Vehicle.Vehicle.LicensePlate,
                    TripId = convertedTripId,
                    Latitude = m.Vehicle.Position.Latitude,
                    Longitude = m.Vehicle.Position.Longitude,
                    Timestamp = m.Vehicle.Timestamp
                };
                if (convertedTripId != -1)
                {
                    gtfsTrips.AddListItem(convertedTripId, vehicle);
                }
            }

            return true;
        }

        private long? FindBestOffset()
        {
            if (gtfsTrips.Count == 0 || ttssTrips.Count == 0)
                return null;

            HashSet<long> possibleOffsets = new();
            foreach (var ttssKey in ttssTrips.Keys)
            {
                foreach (var gtfsKey in gtfsTrips.Keys)
                {
                    possibleOffsets.Add(ttssKey - gtfsKey);
                }
            }

            long bestMatch = 0;
            long bestOffset = 0;
            int options = 0;
            foreach (var offset in possibleOffsets)
            {
                long matchCount = 0;
                foreach (var gtfsId in gtfsTrips.Keys)
                {
                    if (ttssTrips.ContainsKey(gtfsId + offset))
                        matchCount++;
                }

                if (matchCount > bestMatch)
                {
                    bestOffset = offset;
                    bestMatch = matchCount;
                    options = 1;
                }
                else if (matchCount == bestMatch)
                {
                    options++;
                }
            }

            if (options != 1)
                throw new Exception($"Found {options} matching offsets.");

            return bestOffset;
        }

        private VehicleMatchRule? FindMatchRule(long gtfsId)
        {
            return matchRules.Find(r => r.FromId <= gtfsId && r.ToId >= gtfsId);
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
            foreach (var line in lines)
            {
                if (!gotStart)
                {
                    if (line.Contains("<<<'END'"))
                        gotStart = true;

                    continue;
                }

                if (line.Contains("END"))
                    break;

                var lineData = line.Split('\t');
                var modelName = lineData[3].Trim();
                var lowFloor = lineData.Length > 4 ? (LowFloor)int.Parse(lineData[4].Trim()) : LowFloor.Unknown;
                if (type == VehicleType.Bus)
                    lowFloor = LowFloor.Full;

                var matchRule = new VehicleMatchRule
                {
                    FromId = int.Parse(lineData[0].Trim()),
                    ToId = int.Parse(lineData[1].Trim()),
                    Symbol = lineData[2].Trim(),
                    ModelName = lineData[3].Trim()
                };

                matchRules.Add(matchRule);

                if (!modelDict.ContainsKey(modelName))
                {
                    modelDict.Add(modelName, new VehicleModel
                    {
                        Name = modelName,
                        LowFloor = lowFloor,
                        Type = type
                    });
                }
            }

            return true;
        }

        private static long ConvertTripId(string gtfsTripId)
        {
            var data = gtfsTripId.Split('_');
            if (data.Length < 4 || data[0] != "block" || data[2] != "trip")
                return -1;

            return (4096 * long.Parse(data[1])) + long.Parse(data[3]);
        }
    }
}