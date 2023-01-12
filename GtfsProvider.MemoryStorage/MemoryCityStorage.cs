using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common;
using GtfsProvider.Common.Enums;
using GtfsProvider.Common.Extensions;
using GtfsProvider.Common.Models;

namespace GtfsProvider.MemoryStorage
{
    public class MemoryCityStorage : ICityStorage
    {
        private readonly ConcurrentDictionary<string, Stop> _stops = new();
        private readonly ConcurrentDictionary<string, BaseStop> _stopGroups = new();
        protected readonly ConcurrentDictionary<(long id, VehicleType type), Vehicle> _vehiclesByGtfs = new();
        private readonly ConcurrentDictionary<(long id, VehicleType type), Vehicle> _vehiclesByUniqueId = new();
        private readonly ConcurrentDictionary<string, Vehicle> _vehiclesBySideNo = new();

        private readonly City _city = City.Default;
        public virtual City City => _city;

        public MemoryCityStorage()
        { }
        
        public MemoryCityStorage(City city)
        {
            _city = city;
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

        public Task<List<Stop>> GetAllStops() => Task.FromResult(_stops.Values.ToList());

        public Task<List<string>> GetAllStopIds() => Task.FromResult(_stops.Values.Select(s => s.GtfsId).ToList());

        public Task<List<string>> GetStopIdsByType(VehicleType type)
        {
            return Task.FromResult(_stops.Values.Where(s => s.Type == type).Select(s => s.GroupId).ToList());
        }

        public Task RemoveStops(IEnumerable<string> gtfsIds)
        {
            foreach (var id in gtfsIds)
                _stops.TryRemove(id, out Stop _);

            return Task.CompletedTask;
        }

        public Task AddStops(IEnumerable<Stop> stops)
        {
            foreach (var stop in stops)
                _stops.AddOrUpdate(stop.GtfsId, stop, (_, _) => stop);

            return Task.CompletedTask;
        }

        public Task AddStopGroups(IEnumerable<BaseStop> stopGroups)
        {
            foreach (var group in stopGroups)
                _stopGroups.AddOrUpdate(group.GroupId, group, (_, _) => group);

            return Task.CompletedTask;
        }

        public Task RemoveStopGroups(IEnumerable<string> groupIds)
        {
            foreach (var id in groupIds)
                _stopGroups.TryRemove(id, out BaseStop _);

            return Task.CompletedTask;
        }

        public Task<AddUpdateResult> AddOrUpdateVehicle(Vehicle vehicle, Dictionary<string, Vehicle> existingSideNos)
        {
            var result = AddUpdateResult.Added;
            var existingVehicle = _vehiclesBySideNo.GetValueOrDefault(vehicle.SideNo);
            if (existingVehicle != null)
            {
                if (existingVehicle.GtfsId == vehicle.GtfsId && existingVehicle.UniqueId == vehicle.UniqueId)
                {
                    return Task.FromResult(AddUpdateResult.Skipped);
                }
                else
                {
                    _vehiclesBySideNo.TryRemove(vehicle.SideNo, out Vehicle? _);
                    _vehiclesByGtfs.TryRemove((existingVehicle.GtfsId, vehicle.Model.Type), out Vehicle? _);
                    _vehiclesByUniqueId.TryRemove((existingVehicle.UniqueId, vehicle.Model.Type), out Vehicle? _);
                    result = AddUpdateResult.Updated;
                }
            }

            if (_vehiclesByUniqueId.TryRemove((vehicle.UniqueId, vehicle.Model.Type), out Vehicle? existingUniqueId))
            {
                _vehiclesBySideNo.TryRemove(existingUniqueId.SideNo, out Vehicle? _);
                _vehiclesByGtfs.TryRemove((existingUniqueId.GtfsId, vehicle.Model.Type), out Vehicle? _);
            }

            _vehiclesByGtfs.AddOrUpdate((vehicle.GtfsId, vehicle.Model.Type), vehicle, (_, _) => vehicle);
            _vehiclesByUniqueId.AddOrUpdate((vehicle.UniqueId, vehicle.Model.Type), vehicle, (_, _) => vehicle);
            _vehiclesBySideNo.AddOrUpdate(vehicle.SideNo, vehicle, (_, _) => vehicle);

            return Task.FromResult(result);
        }

        public Task<Vehicle?> GetVehicleByGtfsId(long vehicleId, VehicleType type)
        {
            return Task.FromResult(_vehiclesByGtfs.GetValueOrDefault((vehicleId, type)));
        }

        public virtual Task<Vehicle?> GetVehicleByUniqueId(long vehicleId, VehicleType type)
        {
            return Task.FromResult(_vehiclesByUniqueId.GetValueOrDefault((vehicleId, type)));
        }

        public Task<Vehicle?> GetVehicleBySideNo(string sideNo)
        {
            return Task.FromResult(_vehiclesBySideNo.GetValueOrDefault(sideNo));
        }

        public Task<IReadOnlyCollection<Vehicle>> GetAllVehicles()
        {
            return Task.FromResult(_vehiclesByGtfs.Values.AsReadOnly());
        }

        public Task<IReadOnlyCollection<Vehicle>> GetAllVehicles(VehicleType type)
        {
            return Task.FromResult((IReadOnlyCollection<Vehicle>)_vehiclesByGtfs.Values.Where(v => type == VehicleType.None || v.Model.Type == type).ToList());
        }

        public Task<IReadOnlyCollection<Vehicle>> GetVehiclesByUniqueId(List<long> vehicleIds, VehicleType type)
        {
            var results = new List<Vehicle>();

            foreach(var id in vehicleIds)
            {
                var vehicle = _vehiclesByUniqueId.GetValueOrDefault((id, type));
                if(vehicle != null)
                    results.Add(vehicle);
            }

            return Task.FromResult((IReadOnlyCollection<Vehicle>)results);
        }

        public Task<IReadOnlyCollection<string>> GetAllStopGroupIds()
        {
            return Task.FromResult(_stopGroups.Keys.AsReadOnly());
        }

        public Task<Stop?> GetStopById(string stopId)
        {
            return Task.FromResult(_stops.GetValueOrDefault(stopId));
        }

        public Task MarkSyncDone()
        {
            return Task.CompletedTask;
        }
    }
}