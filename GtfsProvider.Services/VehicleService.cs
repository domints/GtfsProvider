using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common;
using GtfsProvider.Common.Enums;
using GtfsProvider.Common.Models;

namespace GtfsProvider.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly IDataStorage _dataStorage;
        public VehicleService(IDataStorage dataStorage)
        {
            _dataStorage = dataStorage;
        }

        public Task<IReadOnlyCollection<Vehicle>> GetAll(City city)
        {
            var store = _dataStorage[city];

            return store.GetAllVehicles();
        }

        public Task<Vehicle?> GetByTtssId(City city, VehicleType type, long id)
        {
            var store = _dataStorage[city];

            return store.GetVehicleByTtssId(id, type);
        }
    }
}