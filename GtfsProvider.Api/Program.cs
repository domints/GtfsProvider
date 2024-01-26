using GtfsProvider.Api;
using GtfsProvider.Api.Configuration;
using GtfsProvider.Api.Endpoints;
using GtfsProvider.Api.Middleware;
using Microsoft.AspNetCore.Http.Timeouts;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog(
            (hostingContext, loggerConfiguration) =>
                loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration));
builder.Services.AddRequestTimeouts(options => {
    options.DefaultPolicy = TimeoutPolicies.DefaultPolicyConfiguration;
    options.AddPolicy(TimeoutPolicies.DeparturePolicy, TimeoutPolicies.DeparturePolicyConfiguration);
});
builder.Services.AddAppServices();
builder.Services.AddHostedService<DownloaderService>();
builder.Services.AddCors();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.ConfigureMiddleware();
app.UseRequestTimeouts();
app.MapStopEndpoints()
   .MapDeparturesEndpoints()
   .MapVehicleEndpoints()
   .MapServiceEndpoints();

app.Logger.LogInformation("Application is being started.");
app.Run();
