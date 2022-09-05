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
        Task AddStops(IEnumerable<Stop> stops);
        Task<List<Stop>> GetAllStops();
        Task<List<BaseStop>> FindStops(string pattern);
        Task RemoveStops(IEnumerable<string> gtfsIds);
        Task<List<string>> GetIdsByType(VehicleType type);
        Task<Vehicle?> GetVehicleById(long vehicleId);
    }
}