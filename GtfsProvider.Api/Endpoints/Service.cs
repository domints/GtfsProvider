using GtfsProvider.Common.Attributes;
using GtfsProvider.Common.Enums;
using GtfsProvider.Common.Extensions;

namespace GtfsProvider.Api.Endpoints
{
    public static class Service
    {
        public static IEndpointRouteBuilder MapServiceEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/cities", () =>
            {
                return Enum.GetValues<City>()
                    .Where(c => c.GetAttribute<IgnoreAttribute>() == null)
                    .Select(c => new { Name = c.GetDisplayName(), Value = c.ToString().ToLowerInvariant() });
            });

            return app;
        }
    }
}