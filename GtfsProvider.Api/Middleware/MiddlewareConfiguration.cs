using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GtfsProvider.Api.Middleware
{
    public static class MiddlewareConfiguration
    {
        public static IApplicationBuilder ConfigureMiddleware(this IApplicationBuilder app)
        {
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
            app.UseSwagger();
            app.UseSwaggerUI();

            return app;
        }
    }
}