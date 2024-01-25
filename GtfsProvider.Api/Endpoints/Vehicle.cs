using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Api.Binders;
using GtfsProvider.Common;
using GtfsProvider.Common.Enums;

namespace GtfsProvider.Api.Endpoints
{
    public static class Vehicle
    {
        public static IEndpointRouteBuilder MapVehicleEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/vehicles", (HttpContext cx, IVehicleService vehicleService, CaseInsensitiveBind<City>? city) =>
            {
                var resolvedCity = city ?? City.Krakow;

                return vehicleService.GetAll(resolvedCity, cx.RequestAborted);
            });

            app.MapGet("/vehicles/withLiveInfo", (HttpContext cx, IVehicleService vehicleService, CaseInsensitiveBind<City>? city) =>
            {
                var resolvedCity = city ?? City.Krakow;

                return vehicleService.GetAllWLiveInfo(resolvedCity, cx.RequestAborted);
            });

            app.MapGet("/vehicles/mapping", (HttpContext cx, IVehicleService vehicleService, CaseInsensitiveBind<City>? city, CaseInsensitiveBind<VehicleType>? vehicleType) =>
            {
                var resolvedType = vehicleType ?? VehicleType.None;
                var resolvedCity = city ?? City.Krakow;
                return vehicleService.GetVehicleMapping(resolvedCity, resolvedType, cx.RequestAborted);
            });

            _ = app.MapGet("/vehicles/position", async (HttpContext cx, IVehicleService vehicleService, CaseInsensitiveBind<City>? city, string sideNo) =>
            {
                var resolvedCity = city ?? City.Krakow;

                var veh = await vehicleService.GetLiveInfoBySideNo(resolvedCity, sideNo, cx.RequestAborted);

                if (veh == null)
                    return Results.NotFound();

                return Results.Ok(veh);
            });

            return app;
        }
    }
}