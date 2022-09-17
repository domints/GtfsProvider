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
        /// <summary>
        /// Adds or updates vehicle based on side no
        /// </summary>
        /// <param name="vehicle">vehicle to be stored in storage</param>
        /// <returns>Value indicating: true for update, false for add</returns>
        Task<AddUpdateResult> AddOrUpdateVehicle(Vehicle vehicle);
        Task<Vehicle?> GetVehicleByGtfsId(long vehicleId, VehicleType type);
        Task<Vehicle?> GetVehicleByTtssId(long vehicleId, VehicleType type);
        Task<Vehicle?> GetVehicleBySideNo(string sideNo);
        Task<IReadOnlyCollection<Vehicle>> GetAllVehicles();
    }
}