using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common;
using GtfsProvider.Common.Enums;
using GtfsProvider.Common.Extensions;
using GtfsProvider.Common.Models;
using GtfsProvider.RedisStorage.Models;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Redis.OM;
using Redis.OM.Searching;
using Redis.OM.Searching.Query;

namespace GtfsProvider.RedisStorage
{
    public class RedisCityStorage : ICityStorage
    {
        private readonly ConcurrentDictionary<string, VehicleModel> _modelCache = new();
        private readonly ConcurrentDictionary<(long id, VehicleType type), Vehicle> _vehicleCache = new();
        private readonly ConcurrentDictionary<string, BaseStop> _stopGroups = new();
        private readonly City _city = City.Default;
        private readonly IAppCache _cache;
        private readonly ILogger<RedisCityStorage> _logger;
        private readonly string _redisUrl;

        public virtual City City => _city;

        /// <summary>
        /// Default constructor for city storage template. It's supposed to be empty and slim
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        public RedisCityStorage(IAppCache cache, ILogger<RedisCityStorage> logger, IConfiguration configuration)
        {
            _cache = cache;
            _logger = logger;
            _redisUrl = configuration["RedisUrl"] ?? "redis://localhost:6379";
        }

        public RedisCityStorage(City city, IAppCache cache, ILogger<RedisCityStorage> logger, IConfiguration configuration)
        {
            _city = city;
            _cache = cache;
            _logger = logger;
            _redisUrl = configuration["RedisUrl"] ?? "redis://localhost:6379";
        }

        public async Task<AddUpdateResult> AddOrUpdateVehicle(Vehicle vehicle, Dictionary<string, Vehicle> existingSideNos, CancellationToken cancellationToken)
        {
            var modelKey = vehicle.Model?.Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(vehicle.Model?.Name))
                modelKey = vehicle.Model?.Type.ToString() ?? nameof(VehicleType.None);

            if (!_modelCache.ContainsKey(modelKey))
            {
                _modelCache.AddOrUpdate(modelKey, vehicle.Model!, (_, _) => vehicle.Model!);
            }

            var result = AddUpdateResult.Added;
            var vehicles = await RedisServices.GetCollection<StoreVehicle>(_redisUrl, cancellationToken);

            if (existingSideNos.ContainsKey(vehicle.SideNo))
            {
                var old = existingSideNos[vehicle.SideNo];
                if (vehicle.GtfsId == old.GtfsId && vehicle.UniqueId == old.UniqueId)
                    return AddUpdateResult.Skipped;

                result = AddUpdateResult.Updated;

                if (vehicle.Model != null)
                {
                    var vehsToUpdate = await vehicles.Where(v => v.GtfsId == vehicle.GtfsId || v.UniqueId == vehicle.UniqueId || v.GtfsId == old.GtfsId || v.UniqueId == old.UniqueId || v.SideNo == vehicle.SideNo).ToListAsync();
                    foreach(var toRemove in vehsToUpdate.Where(v => v.ModelType == vehicle.Model.Type))
                        await vehicles.DeleteAsync(toRemove).WaitAsync(cancellationToken);

                    _cache.Remove(VehicleCacheKey(vehicle.Model.Type, vehicle.UniqueId));
                    _cache.GetOrAdd(VehicleCacheKey(vehicle.Model.Type, vehicle.UniqueId), e =>
                    {
                        e.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(2);
                        return vehicle;
                    });
                }
                else
                {
                    _logger.LogError(Events.VehicleWithNullModel, "Vehicle with null model! SideNo: {sideNo}, GtfsId: {gtfsId}, UniqId: {uniqueId}, Heuristic: {heuristicScore}", vehicle.SideNo, vehicle.GtfsId, vehicle.UniqueId, vehicle.HeuristicScore);
                }
            }

            var storeVehicle = vehicle.ToStoreModel(_city);
            await vehicles.InsertAsync(storeVehicle).WaitAsync(cancellationToken);

            return result;
        }

        public async Task AddStopGroups(IEnumerable<BaseStop> stopGroups, CancellationToken cancellationToken)
        {
            var stopColl = await RedisServices.GetCollection<StoreStopGroup>(_redisUrl, cancellationToken);
            await stopColl.InsertAsync(stopGroups.Select(g => g.ToStoreModel(_city))).WaitAsync(cancellationToken);

            foreach (var group in stopGroups)
            {
                _stopGroups.AddOrUpdate(group.GroupId, group, (_, _) => group);
            }
        }

        public async Task AddStops(IEnumerable<Stop> stops, CancellationToken cancellationToken)
        {
            var stopColl = await RedisServices.GetCollection<StoreStop>(_redisUrl, cancellationToken);
            await stopColl.InsertAsync(stops.Select(s => s.ToStoreModel(_city))).WaitAsync(cancellationToken);
        }

        public Task<List<BaseStop>> FindStops(string pattern, int? limit, CancellationToken _)
        {
            var query = _stopGroups.Values
                .Where(s => s.Name.Matches(pattern));
            if (limit.HasValue)
                query = query.Take(limit.Value);
            var result = query.ToList();

            return Task.FromResult(result);
        }

        public async Task<IReadOnlyCollection<string>> GetAllStopGroupIds(CancellationToken cancellationToken)
        {
            var stopColl = await RedisServices.GetCollection<StoreStopGroup>(_redisUrl, cancellationToken);

            var result = new List<string>();
            await foreach (var group in stopColl.Where(s => s.City == _city))
            {
                cancellationToken.ThrowIfCancellationRequested();
                result.Add(group.GroupId);
            }

            return result;
        }

        public async Task<List<string>> GetAllStopIds(CancellationToken cancellationToken)
        {
            var stopColl = await RedisServices.GetCollection<StoreStop>(_redisUrl, cancellationToken);

            var result = new List<string>();
            await foreach (var stop in stopColl.Where(s => s.City == _city))
            {
                cancellationToken.ThrowIfCancellationRequested();
                result.Add(stop.GtfsId);
            }

            return result;
        }

        public async Task<List<Stop>> GetAllStops(CancellationToken cancellationToken)
        {
            var stopColl = await RedisServices.GetCollection<StoreStop>(_redisUrl, cancellationToken);

            var result = new List<Stop>();
            await foreach (var stop in stopColl.Where(s => s.City == _city))
            {
                cancellationToken.ThrowIfCancellationRequested();
                result.Add(stop.ToAppModel());
            }

            return result;
        }

        public async Task<List<BaseStop>> GetAllStopGroups(CancellationToken cancellationToken)
        {
            var stopColl = await RedisServices.GetCollection<StoreStopGroup>(_redisUrl, cancellationToken);
            var result = new List<BaseStop>();
            await foreach (var group in stopColl.Where(s => s.City == _city))
            {
                cancellationToken.ThrowIfCancellationRequested();
                result.Add(group.ToAppModel());
            }

            return result;
        }

        public async Task<IReadOnlyCollection<Vehicle>> GetAllVehicles(CancellationToken cancellationToken)
        {
            var vehicles = await RedisServices.GetCollection<StoreVehicle>(_redisUrl, cancellationToken);

            var result = new List<Vehicle>();
            await foreach (var vehicle in vehicles.Where(v => v.City == _city))
            {
                cancellationToken.ThrowIfCancellationRequested();
                result.Add(vehicle.ToAppModel(_modelCache));
            }

            return result;
        }

        public async Task<IReadOnlyCollection<Vehicle>> GetAllVehicles(VehicleType type, CancellationToken cancellationToken)
        {
            if (type == VehicleType.None)
                return await GetAllVehicles(cancellationToken);

            var vehicles = await RedisServices.GetCollection<StoreVehicle>(_redisUrl, cancellationToken);

            var result = new List<Vehicle>();
            await foreach (var vehicle in vehicles.Where(v => v.City == _city && v.ModelType == type))
            {
                cancellationToken.ThrowIfCancellationRequested();
                result.Add(vehicle.ToAppModel(_modelCache));
            }

            return result;
        }

        public async Task<Stop?> GetStopById(string stopId, CancellationToken cancellationToken)
        {
            var stopColl = await RedisServices.GetCollection<StoreStop>(_redisUrl, cancellationToken);
            var stop = await stopColl.Where(s => s.City == _city && s.GtfsId == stopId).FirstOrDefaultAsync().WaitAsync(cancellationToken);
            return stop?.ToAppModel();
        }

        public async Task<List<string>> GetStopIdsByType(VehicleType type, CancellationToken cancellationToken)
        {
            var stopColl = await RedisServices.GetCollection<StoreStop>(_redisUrl, cancellationToken);
            var stops = await stopColl.Where(s => s.City == _city && s.Type == type).ToListAsync().WaitAsync(cancellationToken);
            var stopIds = stops?.Select(s => s.GtfsId).ToList();
            return stopIds?.ToList() ?? new List<string>();
        }

        public async Task<Vehicle?> GetVehicleByGtfsId(long vehicleId, VehicleType type, CancellationToken cancellationToken)
        {
            var vehicles = await RedisServices.GetCollection<StoreVehicle>(_redisUrl, cancellationToken);
            var veh = await vehicles.Where(v => v.City == _city && v.ModelType == type && v.GtfsId == vehicleId).FirstOrDefaultAsync().WaitAsync(cancellationToken);
            return veh?.ToAppModel(_modelCache);
        }

        public async Task<Vehicle?> GetVehicleBySideNo(string sideNo, CancellationToken cancellationToken)
        {
            var vehicles = await RedisServices.GetCollection<StoreVehicle>(_redisUrl, cancellationToken);
            var veh = await vehicles.Where(v => v.City == _city && v.SideNo == sideNo).FirstOrDefaultAsync().WaitAsync(cancellationToken);
            return veh?.ToAppModel(_modelCache);
        }

        public async Task<Vehicle?> GetVehicleByUniqueId(long vehicleId, VehicleType type, CancellationToken cancellationToken)
        {
            return await _cache.GetOrAddAsync(VehicleCacheKey(type, vehicleId), async e =>
            {
                var vehicles = await RedisServices.GetCollection<StoreVehicle>(_redisUrl, cancellationToken);
                var key = IdGenerator.Vehicle(_city, type, vehicleId);
                var veh = await vehicles.FindByIdAsync(key);
                var appVeh = veh?.ToAppModel(_modelCache);

                e.AbsoluteExpirationRelativeToNow = appVeh == null ? TimeSpan.FromSeconds(15) : TimeSpan.FromMinutes(15);

                return appVeh;
            });
        }

        public async Task<IReadOnlyCollection<Vehicle>> GetVehiclesByUniqueId(List<long> vehicleIds, VehicleType type, CancellationToken cancellationToken)
        {
            var vehicles = await RedisServices.GetCollection<StoreVehicle>(_redisUrl, cancellationToken);
            var results = await vehicles.FindByIdsAsync(vehicleIds.Select(i => IdGenerator.Vehicle(_city, type, i))).WaitAsync(cancellationToken);
            return results.Values.Where(v => v != null).Select(v => v!.ToAppModel(_modelCache)).ToList();
        }

        public async Task RemoveStopGroups(IEnumerable<string> groupIds, CancellationToken cancellationToken)
        {
            foreach (var key in groupIds.Select(i => $"{IdGenerator.StopGroupPrefix}:{IdGenerator.StopGroup(_city, i)}"))
                await (await RedisServices.GetConnectionProvider(_redisUrl, cancellationToken)).Connection.UnlinkAsync(key).WaitAsync(cancellationToken);

            foreach (var id in groupIds)
                _stopGroups.TryRemove(id, out BaseStop? _);
        }

        public async Task RemoveStops(IEnumerable<string> gtfsIds, CancellationToken cancellationToken)
        {
            foreach (var key in gtfsIds.Select(i => $"{IdGenerator.StopPrefix}:{IdGenerator.Stop(_city, i)}"))
                await (await RedisServices.GetConnectionProvider(_redisUrl, cancellationToken)).Connection.UnlinkAsync(key).WaitAsync(cancellationToken);
        }

        public async Task MarkSyncDone(CancellationToken cancellationToken)
        {
            var groups = (await GetAllStopGroups(cancellationToken)).ToDictionary(g => g.GroupId);
            var newKeys = groups.Keys.ToHashSet();
            var oldKeys = _stopGroups.Keys.ToHashSet();
            var toRemove = oldKeys.Except(newKeys);

            foreach (var key in toRemove)
            {
                _stopGroups.Remove(key, out BaseStop? _);
            }

            foreach (var key in groups.Keys)
            {
                var val = groups[key];
                _stopGroups.AddOrUpdate(key, val, (_, old) => old.Name == val.Name ? old : val);
            }
        }

        private static string VehicleCacheKey(VehicleType type, long uniqueId) => $"{type}:{uniqueId}";

        public Task<int> CountStops(CancellationToken _) => Task.FromResult(_stopGroups.Values.Count);
    }
}