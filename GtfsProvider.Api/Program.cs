using GtfsProvider.Api;
using GtfsProvider.Api.Binders;
using GtfsProvider.Common;
using GtfsProvider.Common.Enums;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAppServices();
builder.Services.AddHostedService<DownloaderService>();
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("localhost", "localhost:8300", "kklive.pl", "ttss.dszymanski.pl")));

var app = builder.Build();
// Configure the HTTP request pipeline.
app.UseCors();

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

app.MapGet("/autocomplete", (IStopService stopService, CaseInsensitiveBind<City>? city, string query, int? maxItems) =>
{
    var resolvedCity = city ?? City.Krakow;
    var itemLimit = maxItems ?? 10;

    return stopService.Autocomplete(resolvedCity, query);
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

app.MapGet("/vehicles/manyByTtss", (IVehicleService vehicleService, CaseInsensitiveBind<City>? city, CaseInsensitiveBind<VehicleType> type, CommaSeparated<long> ids) =>
{
    var resolvedCity = city ?? City.Krakow;
    return vehicleService.GetByTtssId(resolvedCity, type, ids);
});

app.MapGet("/vehicles/byTtss", async (IVehicleService vehicleService, CaseInsensitiveBind<City>? city, CaseInsensitiveBind<VehicleType> type, long id) =>
{
    var resolvedCity = city ?? City.Krakow;

    var foundVehicle = await vehicleService.GetByTtssId(resolvedCity, type, id);

    if (foundVehicle == null)
        return Results.NotFound();

    return Results.Ok(foundVehicle);
});

app.Run();
