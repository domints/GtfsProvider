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
        protected readonly ConcurrentDictionary<(long id, VehicleType type), Vehicle> _vehiclesByGtfs = new();
        private readonly ConcurrentDictionary<(long id, VehicleType type), Vehicle> _vehiclesByTtss = new();
        private readonly ConcurrentDictionary<string, Vehicle> _vehiclesBySideNo = new();

        public virtual City City => City.Default;

        public Task<List<BaseStop>> FindStops(string pattern)
        {
            var result = _stops.Values
                .Where(s => s.Name.Matches(pattern))
                .GroupBy(s => s.GroupId)
                .Select(g => new BaseStop
                {
                    GroupId = g.Key,
                    Name = g.First().Name,
                    Type =
                            (g.Any(s => s.Type == VehicleType.Bus) ? VehicleType.Bus : VehicleType.None)
                            | (g.Any(s => s.Type == VehicleType.Tram) ? VehicleType.Tram : VehicleType.None)
                })
                .ToList();

            return Task.FromResult(result);
        }

        public Task<List<Stop>> GetAllStops() => Task.FromResult(_stops.Values.ToList());

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

        public Task<AddUpdateResult> AddOrUpdateVehicle(Vehicle vehicle)
        {
            var result = AddUpdateResult.Added;
            var existingVehicle = _vehiclesBySideNo.GetValueOrDefault(vehicle.SideNo);
            if (existingVehicle != null)
            {
                if (existingVehicle.GtfsId == vehicle.GtfsId && existingVehicle.TtssId == vehicle.TtssId)
                {
                    return Task.FromResult(AddUpdateResult.Skipped);
                }
                else
                {
                    _vehiclesBySideNo.TryRemove(vehicle.SideNo, out Vehicle? _);
                    _vehiclesByGtfs.TryRemove((existingVehicle.GtfsId, vehicle.Model.Type), out Vehicle? _);
                    _vehiclesByTtss.TryRemove((existingVehicle.TtssId, vehicle.Model.Type), out Vehicle? _);
                    result = AddUpdateResult.Updated;
                }
            }

            if (_vehiclesByTtss.TryRemove((vehicle.TtssId, vehicle.Model.Type), out Vehicle? existingttss))
            {
                _vehiclesBySideNo.TryRemove(existingttss.SideNo, out Vehicle? _);
                _vehiclesByGtfs.TryRemove((existingttss.GtfsId, vehicle.Model.Type), out Vehicle? _);
            }

            _vehiclesByGtfs.AddOrUpdate((vehicle.GtfsId, vehicle.Model.Type), vehicle, (_, _) => vehicle);
            _vehiclesByTtss.AddOrUpdate((vehicle.TtssId, vehicle.Model.Type), vehicle, (_, _) => vehicle);
            _vehiclesBySideNo.AddOrUpdate(vehicle.SideNo, vehicle, (_, _) => vehicle);

            return Task.FromResult(result);
        }

        public Task<Vehicle?> GetVehicleByGtfsId(long vehicleId, VehicleType type)
        {
            return Task.FromResult(_vehiclesByGtfs.GetValueOrDefault((vehicleId, type)));
        }

        public virtual Task<Vehicle?> GetVehicleByTtssId(long vehicleId, VehicleType type)
        {
            return Task.FromResult(_vehiclesByTtss.GetValueOrDefault((vehicleId, type)));
        }

        public Task<Vehicle?> GetVehicleBySideNo(string sideNo)
        {
            return Task.FromResult(_vehiclesBySideNo.GetValueOrDefault(sideNo));
        }

        public Task<IReadOnlyCollection<Vehicle>> GetAllVehicles()
        {
            return Task.FromResult(_vehiclesByGtfs.Values.AsReadOnly());
        }
    }
}