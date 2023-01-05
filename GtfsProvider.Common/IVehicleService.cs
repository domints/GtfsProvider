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
        Task<Vehicle?> GetByUniqueId(City city, VehicleType type, long id);
        Task<IReadOnlyCollection<Vehicle>> GetByUniqueId(City city, VehicleType type, List<long> ids);
        Task<IReadOnlyCollection<Vehicle>> GetAll(City city);
        Task<IReadOnlyCollection<VehicleWLiveInfo>> GetAllWLiveInfo(City city);
    }
}