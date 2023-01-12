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

        public virtual City City => _city;

        public RedisCityStorage()
        { }

        public RedisCityStorage(City city, IAppCache cache)
        {
            _city = city;
            this._cache = cache;
        }

        public async Task<AddUpdateResult> AddOrUpdateVehicle(Vehicle vehicle, Dictionary<string, Vehicle> existingSideNos)
        {
            var modelKey = vehicle.Model?.Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(vehicle.Model?.Name))
                modelKey = vehicle.Model?.Type.ToString() ?? nameof(VehicleType.None);

            if (!_modelCache.ContainsKey(modelKey))
            {
                _modelCache.AddOrUpdate(modelKey, vehicle.Model!, (_, _) => vehicle.Model!);
            }

            var result = AddUpdateResult.Added;

            if (existingSideNos.ContainsKey(vehicle.SideNo))
            {
                var old = existingSideNos[vehicle.SideNo];
                if (vehicle.GtfsId == old.GtfsId && vehicle.UniqueId == old.UniqueId)
                    return AddUpdateResult.Skipped;

                result = AddUpdateResult.Updated;

                if (vehicle.Model != null)
                {
                    _cache.Remove(VehicleCacheKey(vehicle.Model.Type, vehicle.UniqueId));
                    _cache.GetOrAdd(VehicleCacheKey(vehicle.Model.Type, vehicle.UniqueId), e =>
                    {
                        e.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(2);
                        return vehicle;
                    });
                }
            }

            var vehicles = await RedisServices.GetCollection<StoreVehicle>();
            var storeVehicle = vehicle.ToStoreModel(_city);
            await vehicles.InsertAsync(storeVehicle);

            return result;
        }

        public async Task AddStopGroups(IEnumerable<BaseStop> stopGroups)
        {
            var stopColl = await RedisServices.GetCollection<StoreStopGroup>();
            await stopColl.Insert(stopGroups.Select(g => g.ToStoreModel(_city)));

            foreach (var group in stopGroups)
                _stopGroups.AddOrUpdate(group.GroupId, group, (_, _) => group);
        }

        public async Task AddStops(IEnumerable<Stop> stops)
        {
            var stopColl = await RedisServices.GetCollection<StoreStop>();
            await stopColl.Insert(stops.Select(s => s.ToStoreModel(_city)));
        }

        public Task<List<BaseStop>> FindStops(string pattern, int? limit)
        {
            var query = _stopGroups.Values
                .Where(s => s.Name.Matches(pattern));
            if (limit.HasValue)
                query = query.Take(limit.Value);
            var result = query.ToList();

            return Task.FromResult(result);
        }

        public async Task<IReadOnlyCollection<string>> GetAllStopGroupIds()
        {
            var stopColl = await RedisServices.GetCollection<StoreStopGroup>();

            var result = new List<string>();
            await foreach (var group in stopColl.Where(s => s.City == _city))
            {
                result.Add(group.GroupId);
            }

            return result;
        }

        public async Task<List<string>> GetAllStopIds()
        {
            var stopColl = await RedisServices.GetCollection<StoreStop>();

            var result = new List<string>();
            await foreach (var stop in stopColl.Where(s => s.City == _city))
            {
                result.Add(stop.GtfsId);
            }

            return result;
        }

        public async Task<List<Stop>> GetAllStops()
        {
            var stopColl = await RedisServices.GetCollection<StoreStop>();

            var result = new List<Stop>();
            await foreach (var stop in stopColl.Where(s => s.City == _city))
            {
                result.Add(stop.ToAppModel());
            }

            return result;
        }

        public async Task<List<BaseStop>> GetAllStopGroups()
        {
            var stopColl = await RedisServices.GetCollection<StoreStopGroup>();
            var result = new List<BaseStop>();
            await foreach (var group in stopColl.Where(s => s.City == _city))
            {
                result.Add(group.ToAppModel());
            }

            return result;
        }

        public async Task<IReadOnlyCollection<Vehicle>> GetAllVehicles()
        {
            var vehicles = await RedisServices.GetCollection<StoreVehicle>();

            var result = new List<Vehicle>();
            await foreach (var vehicle in vehicles.Where(v => v.City == _city))
            {
                result.Add(vehicle.ToAppModel(_modelCache));
            }

            return result;
        }

        public async Task<IReadOnlyCollection<Vehicle>> GetAllVehicles(VehicleType type)
        {
            if (type == VehicleType.None)
                return await GetAllVehicles();

            var vehicles = await RedisServices.GetCollection<StoreVehicle>();

            var result = new List<Vehicle>();
            await foreach (var vehicle in vehicles.Where(v => v.City == _city && v.ModelType == type))
            {
                result.Add(vehicle.ToAppModel(_modelCache));
            }

            return result;
        }

        public async Task<Stop?> GetStopById(string stopId)
        {
            var stopColl = await RedisServices.GetCollection<StoreStop>();
            var stop = await stopColl.Where(s => s.City == _city && s.GtfsId == stopId).FirstOrDefaultAsync();
            return stop?.ToAppModel();
        }

        public async Task<List<string>> GetStopIdsByType(VehicleType type)
        {
            var stopColl = await RedisServices.GetCollection<StoreStop>();
            var stops = await stopColl.Where(s => s.City == _city && s.Type == type).ToListAsync();
            var stopIds = stops?.Select(s => s.GtfsId).ToList();
            return stopIds?.ToList() ?? new List<string>();
        }

        public async Task<Vehicle?> GetVehicleByGtfsId(long vehicleId, VehicleType type)
        {
            var vehicles = await RedisServices.GetCollection<StoreVehicle>();
            var veh = await vehicles.Where(v => v.City == _city && v.ModelType == type && v.GtfsId == vehicleId).FirstOrDefaultAsync();
            return veh?.ToAppModel(_modelCache);
        }

        public async Task<Vehicle?> GetVehicleBySideNo(string sideNo)
        {
            var vehicles = await RedisServices.GetCollection<StoreVehicle>();
            var veh = await vehicles.Where(v => v.City == _city && v.SideNo == sideNo).FirstOrDefaultAsync();
            return veh?.ToAppModel(_modelCache);
        }

        public async Task<Vehicle?> GetVehicleByUniqueId(long vehicleId, VehicleType type)
        {
            return await _cache.GetOrAddAsync(VehicleCacheKey(type, vehicleId), async e =>
            {
                var vehicles = await RedisServices.GetCollection<StoreVehicle>();
                var veh = await vehicles.Where(v => v.City == _city && v.ModelType == type && v.UniqueId == vehicleId).FirstOrDefaultAsync();
                var appVeh = veh?.ToAppModel(_modelCache);

                e.AbsoluteExpirationRelativeToNow = appVeh == null ? TimeSpan.FromSeconds(15) : TimeSpan.FromDays(2);

                return appVeh;
            });
        }

        public async Task<IReadOnlyCollection<Vehicle>> GetVehiclesByUniqueId(List<long> vehicleIds, VehicleType type)
        {
            var vehicles = await RedisServices.GetCollection<StoreVehicle>();
            var results = await vehicles.FindByIdsAsync(vehicleIds.Select(i => IdGenerator.Vehicle(_city, type, i)));
            return results.Values.Where(v => v != null).Select(v => v!.ToAppModel(_modelCache)).ToList();
        }

        public async Task RemoveStopGroups(IEnumerable<string> groupIds)
        {
            foreach (var key in groupIds.Select(i => $"{IdGenerator.StopGroupPrefix}:{IdGenerator.StopGroup(_city, i)}"))
                await RedisServices.ConnectionProvider.Connection.UnlinkAsync(key);

            foreach (var id in groupIds)
                _stopGroups.TryRemove(id, out BaseStop _);
        }

        public async Task RemoveStops(IEnumerable<string> gtfsIds)
        {
            foreach (var key in gtfsIds.Select(i => $"{IdGenerator.StopPrefix}:{IdGenerator.Stop(_city, i)}"))
                await RedisServices.ConnectionProvider.Connection.UnlinkAsync(key);
        }

        public async Task MarkSyncDone()
        {
            var groups = (await GetAllStopGroups()).ToDictionary(g => g.GroupId);
            var newKeys = groups.Keys.ToHashSet();
            var oldKeys = _stopGroups.Keys.ToHashSet();
            var toRemove = oldKeys.Except(newKeys);

            foreach (var key in toRemove)
            {
                _stopGroups.Remove(key, out BaseStop _);
            }

            foreach (var key in groups.Keys)
            {
                var val = groups[key];
                _stopGroups.AddOrUpdate(key, val, (_, old) => old.Name == val.Name ? old : val);
            }
        }

        private static string VehicleCacheKey(VehicleType type, long uniqueId) => $"{type}:{uniqueId}";
    }
}