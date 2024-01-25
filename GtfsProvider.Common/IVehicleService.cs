using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Enums;
using GtfsProvider.Common.Models;

namespace GtfsProvider.Common
{
    public interface IVehicleService
    {
        Task<Vehicle?> GetByUniqueId(City city, VehicleType type, long id, CancellationToken cancellationToken);
        Task<IReadOnlyCollection<Vehicle>> GetByUniqueId(City city, VehicleType type, List<long> ids, CancellationToken cancellationToken);
        Task<IReadOnlyCollection<Vehicle>> GetAll(City city, CancellationToken cancellationToken);
        Task<IReadOnlyCollection<VehicleWLiveInfo>> GetAllWLiveInfo(City city, CancellationToken cancellationToken);
        Task<VehicleWLiveInfo?> GetLiveInfoBySideNo(City city, string sideNo, CancellationToken cancellationToken);
        Task<Dictionary<string, JacekkVehicle>> GetVehicleMapping(City city, VehicleType type, CancellationToken cancellationToken);
    }
}