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
        private readonly ILiveDataService _liveDataService;

        public VehicleService(IDataStorage dataStorage, ILiveDataService liveDataService)
        {
            _dataStorage = dataStorage;
            _liveDataService = liveDataService;
        }

        public Task<IReadOnlyCollection<Vehicle>> GetAll(City city)
        {
            var store = _dataStorage[city];

            return store.GetAllVehicles();
        }

        public async Task<IReadOnlyCollection<VehicleWLiveInfo>> GetAllWLiveInfo(City city)
        {
            var store = _dataStorage[city];

            var vehicles = await store.GetAllVehicles();
            var liveInfo = (await _liveDataService.GetAllPositions(city)).ToDictionary(i => i.VehicleId);

            return vehicles.Select(v => new VehicleWLiveInfo
            {
                UniqueId = v.UniqueId,
                GtfsId = v.GtfsId,
                SideNo = v.SideNo,
                Model = v.Model,
                IsHeuristic = v.IsHeuristic,
                HeuristicScore = v.HeuristicScore,
                LiveInfo = liveInfo.GetValueOrDefault(v.UniqueId)
            }).ToList();
        }

        public Task<Vehicle?> GetByUniqueId(City city, VehicleType type, long id)
        {
            var store = _dataStorage[city];

            return store.GetVehicleByUniqueId(id, type);
        }

        public Task<IReadOnlyCollection<Vehicle>> GetByUniqueId(City city, VehicleType type, List<long> ids)
        {
            var store = _dataStorage[city];

            return store.GetVehiclesByUniqueId(ids, type);
        }

        public async Task<Dictionary<string, JacekkVehicle>> GetVehicleMapping(City city, VehicleType type)
        {
            var store = _dataStorage[city];
            var vehs = await store.GetAllVehicles(type);
            return vehs.ToDictionary(v => v.UniqueId.ToString(), v => new JacekkVehicle
            {
                Num = v.SideNo,
                Type = v.Model.Name,
                Low = (int)v.Model.LowFloor - 1,
                VehicleType = v.Model.Type
            });
        }
    }
}