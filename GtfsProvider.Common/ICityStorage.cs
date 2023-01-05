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
        Task<Stop?> GetStopById(string stopId);
        Task<List<string>> GetAllStopIds();
        Task<List<BaseStop>> FindStops(string pattern, int? limit);
        Task RemoveStops(IEnumerable<string> gtfsIds);
        Task<IReadOnlyCollection<string>> GetAllStopGroupIds();
        Task AddStopGroups(IEnumerable<BaseStop> stopGroups);
        Task RemoveStopGroups(IEnumerable<string> groupIds);
        Task<List<string>> GetStopIdsByType(VehicleType type);
        /// <summary>
        /// Adds or updates vehicle based on side no
        /// </summary>
        /// <param name="vehicle">vehicle to be stored in storage</param>
        /// <returns>Value indicating: true for update, false for add</returns>
        Task<AddUpdateResult> AddOrUpdateVehicle(Vehicle vehicle, Dictionary<string, Vehicle> existingSideNos);
        Task<Vehicle?> GetVehicleByGtfsId(long vehicleId, VehicleType type);
        Task<Vehicle?> GetVehicleByUniqueId(long vehicleId, VehicleType type);
        Task<IReadOnlyCollection<Vehicle>> GetVehiclesByUniqueId(List<long> vehicleIds, VehicleType type);
        Task<Vehicle?> GetVehicleBySideNo(string sideNo);
        Task<IReadOnlyCollection<Vehicle>> GetAllVehicles();

        Task MarkSyncDone();
    }
}