using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common;
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
        private readonly ICityStorage _storage;
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

        private List<VehicleMatchRule> _matchRules = new();
        private Dictionary<string, VehicleModel> _modelDict = new();

        private Dictionary<long, List<TTSSCleanVehicle>> ttssTrips = new();
        private Dictionary<long, List<GTFSCleanVehicle>> gtfsTrips = new();
        private Dictionary<string, TTSSCleanVehicle> ttssVehicleToTrip = new();
        private List<KokonVehicleCompletePositionResponseModel> kokonPositions = new();
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
            _storage = storage[City.Krakow];
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _kokonClient = kokonClient;
            _tttssClient = tttssClient;
        }

        public async Task<bool> Build(VehicleType type, string positionsFileName, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Building vehicleDb for {type}", type);
            var matchRulesFetchSuccess = await BuildMatchRules(type, cancellationToken);
            if (!matchRulesFetchSuccess)
                return false;

            var kokonDataTask = LoadKokonData(cancellationToken);
            var ttssDataTask = LoadTTSSData(type, cancellationToken);
            var gtfsDataTask = LoadGTFSData(positionsFileName, cancellationToken);
            await Task.WhenAll(kokonDataTask, ttssDataTask, gtfsDataTask);

            var offset = FindBestOffset();
            if (!offset.HasValue)
                return false;

            List<(int sourceId, Vehicle vehicle)> matchedSingle = new();
            Dictionary<string, Vehicle> matchedSideNos = new();
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
                    UniqueId = ttssEntry[0].Id,
                    GtfsId = g.Value[0].Id,
                    SideNo = g.Value[0].Num
                };
                // Nevelo hack
                if (entry.GtfsId == 899 && string.IsNullOrWhiteSpace(entry.SideNo))
                    entry.SideNo = "RY899";

                if (matchedSideNos.ContainsKey(entry.SideNo))
                {
                    _logger.LogWarning("Already just matched this side no: {sideno}. What the hell? Skipping.", entry.SideNo);
                    continue;
                }

                matchedSingle.Add((1, entry));
                matchedSideNos.Add(entry.SideNo, entry);
            }
            foreach (var te in ttssTrips)
            {
                if (
                    !matchedMultiple.Any(mm => mm.ttss == te.Value) &&
                    !matchedSingle.Any(ms => te.Value.Any(t => t.Id == ms.vehicle.UniqueId))
                    )
                {
                    unmatchedTtss.Add(te.Value);
                }
            }
            
            Dictionary<long, Vehicle> byGtfsId = matchedSingle.ToDictionary(k => k.vehicle.GtfsId, v => v.vehicle);
            Dictionary<long, Vehicle> byTtssId = matchedSingle.ToDictionary(k => k.vehicle.UniqueId, v => v.vehicle);
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
                .OrderBy(v => v.vehicle.GtfsId)
                .Select(v => v.vehicle.UniqueId - (v.vehicle.GtfsId * 2) - 2)
                .First();

            var oldVehicles = await _storage.GetAllVehicles(cancellationToken);
            foreach (var v in oldVehicles.Where(ov => !ov.IsHeuristic && ov.Model.Type == type))
            {
                if (!byTtssId.ContainsKey(v.UniqueId) && !byGtfsId.ContainsKey(v.GtfsId))
                {
                    byTtssId.Add(v.UniqueId, v);
                    byGtfsId.Add(v.GtfsId, v);
                }
            }

            foreach (var t in unmatchedTtss.SelectMany(x => x))
            {
                if (byTtssId.ContainsKey(t.Id))
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
                            UniqueId = t.Id,
                            GtfsId = possibleGtfsId,
                            SideNo = unmatchedGtfsDict[possibleGtfsId].Num,
                            IsHeuristic = true,
                            HeuristicScore = confident
                        };
                        if (!matchedSideNos.ContainsKey(veh.SideNo))
                        {
                            matchedSingle.Add((2, veh));
                            matchedSideNos.Add(veh.SideNo, veh);
                            unmatchedGtfsDict.Remove(possibleGtfsId);
                        }
                    }
                    else
                    {
                        var rule = FindMatchRule(possibleGtfsId);
                        var symbol = rule?.Symbol ?? "-";
                        var veh = new Vehicle
                        {
                            UniqueId = t.Id,
                            GtfsId = possibleGtfsId,
                            SideNo = $"{symbol}{possibleGtfsId:D3}",
                            IsHeuristic = true,
                            HeuristicScore = rule == null ? confident / 4 : confident
                        };
                        if (!matchedSideNos.ContainsKey(veh.SideNo))
                        {
                            matchedSingle.Add((3, veh));
                            matchedSideNos.Add(veh.SideNo, veh);
                        }
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

                    var veh = new Vehicle
                    {
                        UniqueId = v.Id,
                        GtfsId = kv.VehicleNo,
                        SideNo = closeVehicles[0].SideNo,
                        IsHeuristic = true,
                        HeuristicScore = 80
                    };
                    if (!matchedSideNos.ContainsKey(veh.SideNo))
                    {
                        matchedSingle.Add((4, veh));
                        matchedSideNos.Add(veh.SideNo, veh);
                    }
                }
                else
                {
                    failedToMatchCount++;
                }
            }

            var existingSideNos = new Dictionary<string, Vehicle>();
            foreach (var v in oldVehicles)
            {
                if (!existingSideNos.ContainsKey(v.SideNo))
                {
                    existingSideNos.Add(v.SideNo, v);
                    continue;
                }

                var ev = existingSideNos[v.SideNo];
                if (ev.IsHeuristic && !v.IsHeuristic)
                {
                    _logger.LogWarning(Events.VehBuilderDuplicateSideNo, "Ugh, found duplicate, {sideNo}", v.SideNo);
                    existingSideNos[v.SideNo] = v;
                }
                else if (v.IsHeuristic && !ev.IsHeuristic)
                {
                    _logger.LogWarning(Events.VehBuilderDuplicateSideNo, "Ugh, found duplicate, {sideNo}", v.SideNo);
                }
                else if (v.IsHeuristic && ev.IsHeuristic)
                {
                    _logger.LogWarning(Events.VehBuilderDuplicateSideNo, "Ugh, found duplicate, {sideNo}, but both are heuristic.", v.SideNo);
                }
                else
                {
                    _logger.LogError(Events.VehBuilderDuplicateNonHeuristic, "Ugh, found duplicate, {sideNo}, but none is heuristic. Screw this.", v.SideNo);
                }
            }

            var added = 0;
            var updated = 0;
            foreach (var match in matchedSingle)
            {
                var ruleMatch = FindMatchRule(match.vehicle.GtfsId);
                if (ruleMatch == null)
                {
                    _logger.LogWarning(Events.VehBuilderNoMatch, "Cannot find model match for {type} no {id}! Heuristic: {heuristic} w score {score}. Skipping.", type, match.vehicle.GtfsId, match.vehicle.IsHeuristic, match.vehicle.HeuristicScore);
                    continue;
                }

                if (ruleMatch != null && BuildSideNo(ruleMatch, match.vehicle.GtfsId) != match.vehicle.SideNo)
                {
                    _logger.LogWarning(Events.VehBuilderMismatchSideNo, "Matched different sideno than was found in gtfs (Match: {matchSideNo}, GTFS: {gtfsSideNo}). Going with GTFS one!", BuildSideNo(ruleMatch, match.vehicle.GtfsId), match.vehicle.SideNo);
                }

                if (ruleMatch != null && _modelDict.ContainsKey(ruleMatch.ModelName))
                    match.vehicle.Model = _modelDict[ruleMatch.ModelName];

                if (match.vehicle.Model == null)
                {
                    _logger.LogWarning(Events.VehBuilderMissModelInfo, "Missing model information for {type} no {id}! Heuristic: {heuristic} w score {score}", type, match.vehicle.GtfsId, match.vehicle.IsHeuristic, match.vehicle.HeuristicScore);
                    match.vehicle.Model = new VehicleModel { Type = type };
                }

                var addOrUpdateResult = await _storage.AddOrUpdateVehicle(match.vehicle, existingSideNos, cancellationToken);
                if (addOrUpdateResult == AddUpdateResult.Updated)
                    updated++;
                else if (addOrUpdateResult == AddUpdateResult.Added)
                    added++;
            }

            _logger.LogInformation(Events.VehicleDbUpdated, "Vehicle DB in {city} updated for {type}, {added} added entries, {updated} updated entries. Failed to match {failed} vehicles this time.", City.Krakow, type, added, updated, failedToMatchCount);
            return true;
        }

        private string BuildSideNo(VehicleMatchRule rule, long gtfsId)
        {
            return string.Format("{0}{1:D3}", rule.Symbol, gtfsId);
        }

        private void MatchedSingleAdd(List<Vehicle> matchedSingle, Vehicle v)
        {
            matchedSingle.Add(v);
        }

        private async Task LoadKokonData(CancellationToken cancellationToken)
        {
            kokonPositions = await _kokonClient.GetCompleteVehsPos(cancellationToken);
        }

        private async Task<bool> LoadTTSSData(VehicleType type, CancellationToken cancellationToken)
        {
            var vehiclesInfo = await _tttssClient.GetVehiclesInfo(type, cancellationToken);
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
                    Coords = CoordsFactory.FromTTSS(v.Latitude.Value, v.Longitude.Value) ?? Coords.Zero
                };

                ttssTrips.AddListItem(long.Parse(v.TripId), vehicleEntry);
                ttssVehicleToTrip.Add(v.Id, vehicleEntry);
            }

            return true;
        }

        private async Task<bool> LoadGTFSData(string positionsFileName, CancellationToken cancellationToken)
        {
            using var positionStream = await _fileStorage.LoadFile(City.Krakow, positionsFileName, cancellationToken);
            var feedMessage = Serializer.Deserialize<FeedMessage>(positionStream);
            GTFSRefreshTime = (long)feedMessage.Header.Timestamp;
            foreach (var m in feedMessage.Entities)
            {
                var convertedTripId = ConvertTripId(m.Vehicle.Trip?.TripId);
                if (convertedTripId == -1)
                    continue;
                var vehicle = new GTFSCleanVehicle
                {
                    Id = long.Parse(m.Id),
                    Num = m.Vehicle.Vehicle.LicensePlate,
                    TripId = convertedTripId,
                    Coords = new(m.Vehicle.Position.Latitude, m.Vehicle.Position.Longitude),
                    Timestamp = m.Vehicle.Timestamp
                };

                gtfsTrips.AddListItem(convertedTripId, vehicle);
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
            return _matchRules.Find(r => r.FromId <= gtfsId && r.ToId >= gtfsId);
        }

        private async Task<bool> BuildMatchRules(VehicleType type, CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient($"Downloader_Krakow_MatchRules");
            var matchRulesRawPhp = string.Empty;
            try
            {
                if (type == VehicleType.Tram)
                    matchRulesRawPhp = await client.GetStringAsync(tramMatchRulesUrl, cancellationToken);
                if (type == VehicleType.Bus)
                    matchRulesRawPhp = await client.GetStringAsync(busMatchRulesUrl, cancellationToken);
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
                var lowFloor = lineData.Length > 4 ? (LowFloor)(int.Parse(lineData[4].Trim()) + 1) : LowFloor.Unknown;
                if (type == VehicleType.Bus)
                    lowFloor = LowFloor.Full;

                var matchRule = new VehicleMatchRule
                {
                    FromId = int.Parse(lineData[0].Trim()),
                    ToId = int.Parse(lineData[1].Trim()),
                    Symbol = lineData[2].Trim(),
                    ModelName = lineData[3].Trim()
                };

                _matchRules.Add(matchRule);

                if (!_modelDict.ContainsKey(modelName))
                {
                    _modelDict.Add(modelName, new VehicleModel
                    {
                        Name = modelName,
                        LowFloor = lowFloor,
                        Type = type
                    });
                }
            }

            return true;
        }

        private static long ConvertTripId(string? gtfsTripId)
        {
            if (gtfsTripId == null)
                return -1;
            var data = gtfsTripId.Split('_');
            if (data.Length < 4 || data[0] != "block" || data[2] != "trip")
                return -1;

            return (4096 * long.Parse(data[1])) + long.Parse(data[3]);
        }
    }
}