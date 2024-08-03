using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common;
using GtfsProvider.CityClient.Krakow.Kokon;
using GtfsProvider.CityClient.Krakow.TTSS;
using Microsoft.Extensions.DependencyInjection;

namespace GtfsProvider.CityClient.Krakow.Extensions
{
    public static class ServicesExtensions
    {
        public static IServiceCollection RegisterKrakowProvider(this IServiceCollection services)
        {
            services.AddScoped<IDownloader, KrakowDownloader>();
            services.AddScoped<ICityLiveDataProvider, KrakowLiveDataProvider>();
            services.AddScoped<IKrakowTTSSClient, KrakowTTSSClient>();
            services.AddTransient<VehicleDbBuilder>();
            services.AddTransient<KokonClient>();
            return services;
        }
    }
}