using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Enums;
using GtfsProvider.Common.Models;

namespace GtfsProvider.Common
{
    public interface ICityLiveDataProvider
    {
        City City { get; }
        Task<List<VehicleLiveInfo>> GetLivePositions(CancellationToken cancellationToken);
        Task<StopDeparturesResult> GetStopDepartures(string groupId, DateTime? startTime, int? timeFrame, VehicleType? vehicleType, CancellationToken cancellationToken);
        Task<TripDepartures> GetTripDepartures(string tripId, VehicleType vehicleType, CancellationToken cancellationToken);
    }
}