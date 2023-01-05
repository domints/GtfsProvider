using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GtfsProvider.CityClient.Wroclaw.iMPK;
using GtfsProvider.Common;
using Microsoft.Extensions.DependencyInjection;

namespace GtfsProvider.CityClient.Wroclaw.Extensions
{
    public static class ServicesExtensions
    {
        public static IServiceCollection RegisterWroclawProvider(this IServiceCollection services)
        {
            services.AddTransient<iMPKClient>();
            services.AddScoped<IDownloader, WroclawDownloader>();
            services.AddScoped<ICityLiveDataProvider, WroclawLiveDataProvider>();
            services.ConfigureiMPKHttpClient();
            return services;
        }

        private static IServiceCollection ConfigureiMPKHttpClient(this IServiceCollection services)
        {
            var credCache = new CredentialCache();
            credCache.Add(new Uri("https://62.233.178.84:8088/"), "Digest", new NetworkCredential("android-mpk", "g5crehAfUCh4Wust"));
            services.AddHttpClient(Consts.iMPK_HttpClient_Name, c => c.DefaultRequestHeaders.Add("User-Agent", Consts.iMPK_HttpClient_UA))
                .ConfigurePrimaryHttpMessageHandler(() =>
            {
                return new HttpClientHandler()
                {
                    UseDefaultCredentials = true,
                    Credentials = credCache,
                    ClientCertificateOptions = ClientCertificateOption.Manual,
                    ServerCertificateCustomValidationCallback =
                        (_, _, _, _) => true
                };
            });

            return services;
        }
    }
}