using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common;
using GtfsProvider.Common.Enums;
using GtfsProvider.Common.Models;

namespace GtfsProvider.Services
{
    public class LiveDataService : ILiveDataService
    {
        private readonly IEnumerable<IDownloader> _downloaders;
        public LiveDataService(IEnumerable<IDownloader> downloaders)
        {
            _downloaders = downloaders;
        }

        public async Task<List<VehicleLiveInfo>> GetAllPositions(City city)
        {
            var downloader = _downloaders.FirstOrDefault(d => d.City == city);
            if (downloader == null)
                return new();

            return await downloader.GetLivePositions();
        }
    }
}