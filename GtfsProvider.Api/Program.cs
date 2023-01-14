using GtfsProvider.Api;
using GtfsProvider.Api.Binders;
using GtfsProvider.Common;
using GtfsProvider.Common.Attributes;
using GtfsProvider.Common.Enums;
using GtfsProvider.Common.Extensions;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Configuration;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog(
            (hostingContext, loggerConfiguration) =>
                loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration));
builder.Services.AddAppServices();
builder.Services.AddHostedService<DownloaderService>();
builder.Services.AddCors();

var app = builder.Build();
// Configure the HTTP request pipeline.
app.UseCors(builder =>
    builder
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithOrigins("http://localhost", "http://localhost:8300", "http://localhost:4200", "https://kklive.pl", "https://ttss.dszymanski.pl"));

app.Use(async (HttpContext cx, Func<Task> next) =>
{
    var service =
        cx.RequestServices
        .GetServices<IHostedService>()
        .OfType<DownloaderService>()
        .FirstOrDefault();

    if (service?.Initialized == true)
    {
        await next();
    }
    else
    {
        cx.Response.StatusCode = 420;
        await cx.Response.WriteAsync("Enhance your calm. App is booting up.");
    }
});

app.MapGet("/cities", () =>
{
    return Enum.GetValues<City>()
        .Where(c => c.GetAttribute<IgnoreAttribute>() == null)
        .Select(c => new { Name = c.GetDisplayName(), Value = c.ToString().ToLowerInvariant() });
});

app.MapGet("/autocomplete", (IStopService stopService, CaseInsensitiveBind<City>? city, string query, int? maxItems) =>
{
    var resolvedCity = city ?? City.Krakow;
    var itemLimit = maxItems ?? 10;

    return stopService.Autocomplete(resolvedCity, query, itemLimit);
});

app.MapGet("/stops", (IStopService stopService, CaseInsensitiveBind<City>? city) =>
{
    var resolvedCity = city ?? City.Krakow;

    return stopService.AllStops(resolvedCity);
});

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

app.MapGet("/vehicles", (IVehicleService vehicleService, CaseInsensitiveBind<City>? city) =>
{
    var resolvedCity = city ?? City.Krakow;

    return vehicleService.GetAll(resolvedCity);
});

app.MapGet("/vehicles/withLiveInfo", (IVehicleService vehicleService, CaseInsensitiveBind<City>? city) =>
{
    var resolvedCity = city ?? City.Krakow;

    return vehicleService.GetAllWLiveInfo(resolvedCity);
});

app.MapGet("/krakow/mapping", (IVehicleService vehicleService, CaseInsensitiveBind<VehicleType>? vehicleType) =>
{
    var resolvedType = vehicleType ?? VehicleType.None;
    return vehicleService.GetVehicleMapping(City.Krakow, resolvedType);
});
app.Logger.LogInformation("Application is being started.");
app.Run();
