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

        public async Task<List<VehicleLiveInfo>> GetAllPositions(City city, CancellationToken cancellationToken)
        {
            var provider = _liveDataProviders.FirstOrDefault(d => d.City == city);
            if (provider == null)
                return new();

            return await provider.GetLivePositions(cancellationToken);
        }

        public async Task<StopDeparturesResult> GetStopDepartures(City city, string groupId, DateTime? startTime, int? timeFrame, CancellationToken cancellationToken)
        {
            var provider = _liveDataProviders.FirstOrDefault(d => d.City == city);
            if (provider == null)
                return new();

            return await provider.GetStopDepartures(groupId, startTime, timeFrame, cancellationToken);
        }

        public async Task<TripDepartures> GetTripDepartures(City city, string tripId, VehicleType vehicleType, CancellationToken cancellationToken)
        {
            var provider = _liveDataProviders.FirstOrDefault(d => d.City == city);
            if (provider == null)
                return new();

            return await provider.GetTripDepartures(tripId, vehicleType, cancellationToken);
        }
    }
}