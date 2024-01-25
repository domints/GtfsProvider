using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.CityClient.Wroclaw.iMPK;
using GtfsProvider.Common;
using GtfsProvider.Common.Enums;
using GtfsProvider.Common.Models;

namespace GtfsProvider.CityClient.Wroclaw
{
    public class WroclawLiveDataProvider : ICityLiveDataProvider
    {
        private readonly iMPKClient _mpkClient;
        private ICityStorage _dataStorage;

        public City City => City.Wroclaw;


        public WroclawLiveDataProvider(iMPKClient mpkClient, IDataStorage dataStorage)
        {
            _mpkClient = mpkClient;
            _dataStorage = dataStorage[City];
        }

        public Task<List<VehicleLiveInfo>> GetLivePositions(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<List<StopDeparture>> GetStopDepartures(string groupId, DateTime? startTime, int? timeFrame, CancellationToken cancellationToken)
        {
            var departures = await _mpkClient.GetStopGroupInfo(groupId, cancellationToken);
            if (departures == null)
                return new();

            var result = new List<StopDeparture>();
            foreach (var d in departures)
            {
                if (d.DirectionStopId == null)
                    continue;
                var stop = await _dataStorage.GetStopById(d.DirectionStopId, cancellationToken);
                result.Add(new StopDeparture
                {
                    Line = d.Line ?? "-",
                    Direction = stop?.Name ?? "-",
                    TimeString = d.Time.ToString("HH:mm"),
                    TripId = d.CourseId.ToString()
                });
            }

            return result;
        }

        public Task<TripDepartures> GetTripDepartures(string tripId, VehicleType vehicleType, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}