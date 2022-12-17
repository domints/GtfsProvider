using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common;
using GtfsProvider.Common.Enums;

namespace GtfsProvider.CityClient.Wroclaw
{
    public class WroclawDownloader : IDownloader
    {
        public City City => City.Wroclaw;
        private readonly IHttpClientFactory _httpClientFactory;

        public WroclawDownloader(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            
        }

        public async Task RefreshIfNeeded()
        {
            var httpClient = _httpClientFactory.CreateClient($"Downloader_{City}");

            await DownloadVehicles(httpClient);
        }

        private async Task DownloadVehicles(HttpClient client)
        {

        }
    }
}