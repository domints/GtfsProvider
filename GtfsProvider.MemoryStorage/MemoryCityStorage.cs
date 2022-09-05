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
        private readonly ConcurrentDictionary<long, Vehicle> _vehicles = new();

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

        public Task<List<string>> GetIdsByType(VehicleType type)
        {
            return Task.FromResult(_stops.Values.Where(s => s.Type == type).Select(s => s.GroupId).ToList());
        }

        public Task RemoveStops(IEnumerable<string> gtfsIds)
        {
            foreach(var id in gtfsIds)
                _stops.TryRemove(id, out Stop _);

            return Task.CompletedTask;
        }

        public Task AddStops(IEnumerable<Stop> stops)
        {
            foreach(var stop in stops)
                _stops.AddOrUpdate(stop.GtfsId, stop, (_, _) => stop);

            return Task.CompletedTask;
        }

        public Task<Vehicle?> GetVehicleById(long vehicleId)
        {
            return Task.FromResult(_vehicles.GetValueOrDefault(vehicleId));
        }
    }
}