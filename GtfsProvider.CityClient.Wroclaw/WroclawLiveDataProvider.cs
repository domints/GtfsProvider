using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common;
using GtfsProvider.Common.Enums;
using GtfsProvider.Common.Models;

namespace GtfsProvider.CityClient.Wroclaw
{
    public class WroclawLiveDataProvider : ICityLiveDataProvider
    {
        public City City => City.Wroclaw;

        public Task<List<VehicleLiveInfo>> GetLivePositions()
        {
            throw new NotImplementedException();
        }

        public Task<List<StopDeparture>> GetStopDepartures(string groupId, DateTime? startTime, int? timeFrame)
        {
            throw new NotImplementedException();
        }

        public Task<TripDepartures> GetTripDepartures(string tripId, VehicleType vehicleType)
        {
            throw new NotImplementedException();
        }
    }
}