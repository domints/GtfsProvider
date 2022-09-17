using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common;
using GtfsProvider.Downloader.Krakow.Kokon;
using Microsoft.Extensions.DependencyInjection;

namespace GtfsProvider.Downloader.Krakow.Extensions
{
    public static class ServicesExtensions
    {
        public static IServiceCollection RegisterKrakowDownloader(this IServiceCollection services)
        {
            services.AddScoped<IDownloader, Downloader>();
            services.AddTransient<VehicleDbBuilder>();
            services.AddTransient<KokonClient>();
            return services;
        }
    }
}