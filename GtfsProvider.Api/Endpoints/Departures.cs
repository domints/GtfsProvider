using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Api.Binders;
using GtfsProvider.Common;
using GtfsProvider.Common.Enums;

namespace GtfsProvider.Api.Endpoints
{
    public static class Departures
    {
        public static IEndpointRouteBuilder MapDeparturesEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/departures/stop/{groupId}", (ILiveDataService liveDataService, string groupId, CaseInsensitiveBind<City>? city, DateTime? startTime, int? timeFrame) =>
            {
                var resolvedCity = city ?? City.Krakow;

                return liveDataService.GetStopDepartures(resolvedCity, groupId, startTime, timeFrame);
            });

            app.MapGet("/departures/trip/{tripId}", (ILiveDataService liveDataService, string tripId, CaseInsensitiveBind<City>? city, CaseInsensitiveBind<VehicleType> vehicleType) =>
            {
                var resolvedCity = city ?? City.Krakow;

                return liveDataService.GetTripDepartures(resolvedCity, tripId, vehicleType);
            });
            return app;
        }
    }
}