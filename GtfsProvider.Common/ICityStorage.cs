using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Enums;
using GtfsProvider.Common.Models;

namespace GtfsProvider.Common
{
    public interface ICityStorage
    {
        City City { get; }
        Task AddStops(IEnumerable<Stop> stops);
        Task<List<Stop>> GetAllStops();
        Task<List<BaseStop>> FindStops(string pattern);
        Task RemoveStops(IEnumerable<string> gtfsIds);
        Task<List<string>> GetStopIdsByType(VehicleType type);
        Task AddOrUpdateVehicle(Vehicle vehicle);
        Task<Vehicle?> GetVehicleByGtfsId(long vehicleId);
        Task<Vehicle?> GetVehicleByTtssId(long vehicleId);
        Task<Vehicle?> GetVehicleBySideNo(string sideNo);
    }
}