using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common;
using GtfsProvider.Common.CityStorages;
using GtfsProvider.Common.Enums;
using GtfsProvider.Common.Extensions;
using GtfsProvider.Common.Models;
using GtfsProvider.CityClient.Krakow.Kokon;
using GtfsProvider.CityClient.Krakow.TTSS;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ProtoBuf;
using TransitRealtime;

namespace GtfsProvider.CityClient.Krakow
{
    public class VehicleDbBuilder
    {
        private readonly IFileStorage _fileStorage;
        private readonly IKrakowStorage _storage;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<VehicleDbBuilder> _logger;

        // private const string busMatchRulesUrl = "https://raw.githubusercontent.com/jacekkow/mpk-ttss-mapping/master/lib/BusTypes.php";
        // private const string tramMatchRulesUrl = "https://raw.githubusercontent.com/jacekkow/mpk-ttss-mapping/master/lib/TramTypes.php";

        // I have more control over my fork so I can add vehicles if Jacek doesn't notice them and create fork to his repo
        private const string busMatchRulesUrl = "https://raw.githubusercontent.com/domints/mpk-ttss-mapping/master/lib/BusTypes.php";
        private const string tramMatchRulesUrl = "https://raw.githubusercontent.com/domints/mpk-ttss-mapping/master/lib/TramTypes.php";

        private const string busTTSSVehicleList = "http://ttss.mpk.krakow.pl/internetservice/geoserviceDispatcher/services/vehicleinfo/vehicles";
        private const string tramTTSSVehicleList = "http://www.ttss.krakow.pl/internetservice/geoserviceDispatcher/services/vehicleinfo/vehicles?positionType=CORRECTED";

        private long TTSSRefreshTime;
        private long GTFSRefreshTime;

        private List<VehicleMatchRule> matchRules = new();
        private Dictionary<string, VehicleModel> modelDict = new();

        private Dictionary<long, List<TTSSCleanVehicle>> ttssTrips = new();
        private Dictionary<long, List<GTFSCleanVehicle>> gtfsTrips = new();
        private Dictionary<string, TTSSCleanVehicle> ttssVehicleToTrip = new();
        private List<KokonVehicle> kokonVehicles;
        private List<KokonVehicleCompletePositionResponseModel> kokonPositions;
        private readonly KokonClient _kokonClient;
        private readonly IKrakowTTSSClient _tttssClient;

        public VehicleDbBuilder(
            IFileStorage fileStorage,
            IDataStorage storage,
            IHttpClientFactory httpClientFactory,
            IKrakowTTSSClient tttssClient,
            ILogger<VehicleDbBuilder> logger,
            KokonClient kokonClient)
        {
            _fileStorage = fileStorage;
            _storage = storage[City.Krakow] as IKrakowStorage ?? throw new InvalidOperationException("What is wrong with your DI configuration?!");
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _kokonClient = kokonClient;
            _tttssClient = tttssClient;
        }

        public async Task<bool> Build(VehicleType type,
            string positionsFileName)
        {
            var matchRulesFetchSuccess = await BuildMatchRules(type);
            if (!matchRulesFetchSuccess)
                return false;

            var kokonDataTask = LoadKokonData();
            var ttssDataTask = LoadTTSSData(type);
            var gtfsDataTask = LoadGTFSData(positionsFileName);
            await Task.WhenAll(kokonDataTask, ttssDataTask, gtfsDataTask);

            var offset = FindBestOffset();
            if (!offset.HasValue)
                return false;

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
                    double bestDistance = double.MaxValue;
                    for (int i = 0; i < mm.ttss.Count; i++)
                    {
                        var distance = mm.gtfs[0].Coords.DistanceTo(mm.ttss[i].Coords);
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
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
                    double bestDistance = double.MaxValue;
                    for (int i = 0; i < mm.ttss.Count; i++)
                    {
                        var distance = mm.gtfs[0].Coords.DistanceTo(mm.ttss[i].Coords);
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
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

            List<TTSSCleanVehicle> vehiclesLeftForKokonMatch = new();
            var theoreticalFirstTtssId = matchedSingle
                .OrderBy(v => v.GtfsId)
                .Select(v => v.TtssId - (v.GtfsId * 2) - 2)
                .First();

            var oldVehicles = await _storage.GetAllVehicles();
            foreach (var v in oldVehicles.Where(ov => ov.Model.Type == type))
            {
                if (!byTtssId.ContainsKey(v.TtssId) && !byGtfsId.ContainsKey(v.GtfsId))
                {
                    byTtssId.Add(v.TtssId, v);
                    byGtfsId.Add(v.GtfsId, v);
                }
            }

            foreach (var t in unmatchedTtss.SelectMany(x => x))
            {
                if(byTtssId.ContainsKey(t.Id))
                    continue;

                var closestUp = 0;
                var closestDown = 0;
                var skipUp = 0;
                var skipDown = 0;
                for (int i = 1; i < 10; i++)
                {
                    var up = t.Id + (i * 2);
                    var down = t.Id - (i * 2);
                    if (closestUp == 0 && byTtssId.ContainsKey(up))
                    {
                        if (byTtssId[up].GtfsId < theoreticalFirstTtssId)
                        {
                            skipUp++;
                            continue;
                        }

                        closestUp = i;
                    }

                    if (closestDown == 0 && byTtssId.ContainsKey(down))
                    {
                        if (byTtssId[down].GtfsId < theoreticalFirstTtssId)
                        {
                            skipDown++;
                            continue;
                        }

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
                    possibleGtfsId = byTtssId[t.Id - ((closestDown - skipDown) * 2)].GtfsId + closestDown;
                    if (unmatchedGtfsDict.ContainsKey(possibleGtfsId))
                        confident += 25;
                }

                if (closestUp > 0)
                {
                    confident += 25;
                    var possibleGtfsVal = byTtssId[t.Id + ((closestUp - skipUp) * 2)].GtfsId - closestUp;
                    if (possibleGtfsId == 0)
                    {
                        possibleGtfsId = possibleGtfsVal;
                    }
                    else if (possibleGtfsId != possibleGtfsVal)
                    {
                        confident = 0;
                        possibleGtfsId = 0;
                    }

                    if (unmatchedGtfsDict.ContainsKey(possibleGtfsId))
                        confident += 25;
                }

                if (possibleGtfsId > 0 && confident > 0)
                {
                    if (unmatchedGtfsDict.ContainsKey(possibleGtfsId))
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
                    vehiclesLeftForKokonMatch.Add(t);
                }
            }

            var failedToMatchCount = 0;
            foreach (var v in vehiclesLeftForKokonMatch)
            {
                var closeVehicles = kokonPositions.Where(k => k.Coords.DistanceTo(v.Coords) < 10).ToArray();
                if (closeVehicles.Length == 1)
                {
                    var kv = KokonVehicle.FromSideNo(closeVehicles[0].SideNo);
                    if (byGtfsId.ContainsKey(kv.VehicleNo))
                        System.Diagnostics.Debugger.Break();

                    matchedSingle.Add(new Vehicle
                    {
                        TtssId = v.Id,
                        GtfsId = kv.VehicleNo,
                        SideNo = closeVehicles[0].SideNo,
                        IsHeuristic = true,
                        HeuristicScore = 80
                    });
                }
                else
                {
                    failedToMatchCount++;
                }
            }

            var added = 0;
            var updated = 0;
            foreach (var v in matchedSingle)
            {
                var ruleMatch = FindMatchRule(v.GtfsId);
                if (ruleMatch != null && modelDict.ContainsKey(ruleMatch.ModelName))
                    v.Model = modelDict[ruleMatch.ModelName];

                if (v.Model == null)
                {
                    _logger.LogWarning("Missing model information for {type} no {id}! Heuristic: {heuristic} w score {score}", type, v.GtfsId, v.IsHeuristic, v.HeuristicScore);
                    v.Model = new VehicleModel { Type = type };
                }

                var addOrUpdateResult = await _storage.AddOrUpdateVehicle(v);
                if (addOrUpdateResult == AddUpdateResult.Updated)
                    updated++;
                else if (addOrUpdateResult == AddUpdateResult.Added)
                    added++;
            }

            _logger.LogInformation("Vehicle DB updated for {type}, {added} added entries, {updated} updated entries. Failed to match {failed} vehicles this time.", type, added, updated, failedToMatchCount);
            return true;
        }

        private async Task LoadKokonData()
        {
            kokonPositions = await _kokonClient.GetCompleteVehsPos();
        }

        private async Task<bool> LoadTTSSData(VehicleType type)
        {
            var vehiclesInfo = await _tttssClient.GetVehiclesInfo(type);
            if (vehiclesInfo == null)
                return false;

            TTSSRefreshTime = vehiclesInfo.LastUpdate;

            foreach (var v in vehiclesInfo.Vehicles)
            {
                if (v.IsDeleted || string.IsNullOrWhiteSpace(v.TripId)
                    || string.IsNullOrWhiteSpace(v.Name)
                    || !v.Latitude.HasValue || !v.Longitude.HasValue)
                {
                    continue;
                }

                var name = v.Name.Split(' ');
                var vehicleEntry = new TTSSCleanVehicle
                {
                    Id = long.Parse(v.Id),
                    Line = name[0].Trim(),
                    Direction = name.Length > 1 ? name[1].Trim() : string.Empty,
                    Coords = CoordsFactory.FromTTSS(v.Latitude.Value, v.Longitude.Value)
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
                    Coords = new(m.Vehicle.Position.Latitude, m.Vehicle.Position.Longitude),
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
            {
                _logger.LogError("Found {options} matching offsets. Cancelling current refresh cycle.", options);
                return null;
            }

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