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
        Task AddStops(IEnumerable<Stop> stops, CancellationToken cancellationToken);
        Task<List<Stop>> GetAllStops(CancellationToken cancellationToken);
        Task<Stop?> GetStopById(string stopId, CancellationToken cancellationToken);
        Task<List<string>> GetAllStopIds(CancellationToken cancellationToken);
        Task<int> CountStops(CancellationToken cancellationToken);
        Task<List<BaseStop>> FindStops(string pattern, int? limit, CancellationToken cancellationToken);
        Task RemoveStops(IEnumerable<string> gtfsIds, CancellationToken cancellationToken);
        Task<IReadOnlyCollection<string>> GetAllStopGroupIds(CancellationToken cancellationToken);
        Task AddStopGroups(IEnumerable<BaseStop> stopGroups, CancellationToken cancellationToken);
        Task RemoveStopGroups(IEnumerable<string> groupIds, CancellationToken cancellationToken);
        Task<List<string>> GetStopIdsByType(VehicleType type, CancellationToken cancellationToken);
        /// <summary>
        /// Adds or updates vehicle based on side no
        /// </summary>
        /// <param name="vehicle">vehicle to be stored in storage</param>
        /// <returns>Value indicating: true for update, false for add</returns>
        Task<AddUpdateResult> AddOrUpdateVehicle(Vehicle vehicle, Dictionary<string, Vehicle> existingSideNos, CancellationToken cancellationToken);
        Task<Vehicle?> GetVehicleByGtfsId(long vehicleId, VehicleType type, CancellationToken cancellationToken);
        Task<Vehicle?> GetVehicleByUniqueId(long vehicleId, VehicleType type, CancellationToken cancellationToken);
        Task<IReadOnlyCollection<Vehicle>> GetVehiclesByUniqueId(List<long> vehicleIds, VehicleType type, CancellationToken cancellationToken);
        Task<Vehicle?> GetVehicleBySideNo(string sideNo, CancellationToken cancellationToken);
        Task<IReadOnlyCollection<Vehicle>> GetAllVehicles(CancellationToken cancellationToken);
        Task<IReadOnlyCollection<Vehicle>> GetAllVehicles(VehicleType type, CancellationToken cancellationToken);

        Task MarkSyncDone(CancellationToken _);
    }
}