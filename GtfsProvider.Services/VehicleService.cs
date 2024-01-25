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

        public Task<IReadOnlyCollection<Vehicle>> GetAll(City city, CancellationToken cancellationToken)
        {
            var store = _dataStorage[city];

            return store.GetAllVehicles(cancellationToken);
        }

        public async Task<IReadOnlyCollection<VehicleWLiveInfo>> GetAllWLiveInfo(City city, CancellationToken cancellationToken)
        {
            var store = _dataStorage[city];

            var vehicles = await store.GetAllVehicles(cancellationToken);
            var liveInfo = (await _liveDataService.GetAllPositions(city, cancellationToken)).ToDictionary(i => i.VehicleId);

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

        public Task<Vehicle?> GetByUniqueId(City city, VehicleType type, long id, CancellationToken cancellationToken)
        {
            var store = _dataStorage[city];

            return store.GetVehicleByUniqueId(id, type, cancellationToken);
        }

        public Task<IReadOnlyCollection<Vehicle>> GetByUniqueId(City city, VehicleType type, List<long> ids, CancellationToken cancellationToken)
        {
            var store = _dataStorage[city];

            return store.GetVehiclesByUniqueId(ids, type, cancellationToken);
        }

        public async Task<VehicleWLiveInfo?> GetLiveInfoBySideNo(City city, string sideNo, CancellationToken cancellationToken)
        {
            var store = _dataStorage[city];

            var vehicle = await store.GetVehicleBySideNo(sideNo, cancellationToken);

            if (vehicle == null)
                return null;

            var liveInfo = (await _liveDataService.GetAllPositions(city, cancellationToken)).ToDictionary(i => i.VehicleId);
            return new VehicleWLiveInfo
            {
                UniqueId = vehicle.UniqueId,
                GtfsId = vehicle.GtfsId,
                SideNo = vehicle.SideNo,
                Model = vehicle.Model,
                IsHeuristic = vehicle.IsHeuristic,
                HeuristicScore = vehicle.HeuristicScore,
                LiveInfo = liveInfo.GetValueOrDefault(vehicle.UniqueId)
            };
        }

        public async Task<Dictionary<string, JacekkVehicle>> GetVehicleMapping(City city, VehicleType type, CancellationToken cancellationToken)
        {
            var store = _dataStorage[city];
            var vehs = await store.GetAllVehicles(type, cancellationToken);
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