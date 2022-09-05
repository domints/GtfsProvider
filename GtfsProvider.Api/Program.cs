using GtfsProvider.Api;
using GtfsProvider.Common;
using GtfsProvider.Common.Enums;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAppServices();
builder.Services.AddHostedService<DownloaderService>();

var app = builder.Build();
// Configure the HTTP request pipeline.

app.Use(async (HttpContext cx, Func<Task> next) => {
    var service =
        cx.RequestServices
        .GetServices<IHostedService>()
        .OfType<DownloaderService>()
        .FirstOrDefault();

    if(service?.Initialized == true)
    {
        await next();
    }
    else
    {
        cx.Response.StatusCode = 420;
        await cx.Response.WriteAsync("Enhance your calm. App is booting up.");
    }
});

app.MapGet("/autocomplete", (IStopService stopService, City? city, string query, int? maxItems) =>{
    var resolvedCity = city ?? City.Krakow;
    var itemLimit = maxItems ?? 10;

    return stopService.Autocomplete(resolvedCity, query);
});

app.Run();
