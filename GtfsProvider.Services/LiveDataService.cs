using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common;
using GtfsProvider.Common.Enums;
using GtfsProvider.Common.Models;

namespace GtfsProvider.Services
{
    public class LiveDataService : ILiveDataService
    {
        private readonly IEnumerable<ICityLiveDataProvider> _liveDataProviders;
        public LiveDataService(IEnumerable<ICityLiveDataProvider> liveDataProviders)
        {
            _liveDataProviders = liveDataProviders;
        }

        public async Task<List<VehicleLiveInfo>> GetAllPositions(City city)
        {
            var provider = _liveDataProviders.FirstOrDefault(d => d.City == city);
            if (provider == null)
                return new();

            return await provider.GetLivePositions();
        }

        public async Task<List<StopDeparture>> GetStopDepartures(City city, string groupId, DateTime? startTime, int? timeFrame)
        {
            var provider = _liveDataProviders.FirstOrDefault(d => d.City == city);
            if (provider == null)
                return new();

            return await provider.GetStopDepartures(groupId, startTime, timeFrame);
        }

        public async Task<TripDepartures> GetTripDepartures(City city, string tripId, VehicleType vehicleType)
        {
            var provider = _liveDataProviders.FirstOrDefault(d => d.City == city);
            if (provider == null)
                return new();

            return await provider.GetTripDepartures(tripId, vehicleType);
        }
    }
}