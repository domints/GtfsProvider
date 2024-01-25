using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Api.Binders;
using GtfsProvider.Common;
using GtfsProvider.Common.Enums;

namespace GtfsProvider.Api.Endpoints
{
    public static class Stop
    {
        public static IEndpointRouteBuilder MapStopEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/autocomplete", (HttpContext cx, IStopService stopService, CaseInsensitiveBind<City>? city, string query, int? maxItems) =>
            {
                var resolvedCity = city ?? City.Krakow;
                var itemLimit = maxItems ?? 10;

                return stopService.Autocomplete(resolvedCity, query, itemLimit, cx.RequestAborted);
            });

            app.MapGet("/stops", (HttpContext cx, IStopService stopService, CaseInsensitiveBind<City>? city) =>
            {
                var resolvedCity = city ?? City.Krakow;

                return stopService.AllStops(resolvedCity, cx.RequestAborted);
            });

            return app;
        }
    }
}