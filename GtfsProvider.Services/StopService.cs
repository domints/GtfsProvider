using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common;
using GtfsProvider.Common.Enums;
using GtfsProvider.Common.Models;

namespace GtfsProvider.Services
{
    public class StopService : IStopService
    {
        private readonly IDataStorage _dataStorage;
        public StopService(IDataStorage dataStorage)
        {
            _dataStorage = dataStorage;
        }

        public Task<List<Stop>> AllStops(City city, CancellationToken cancellationToken)
        {
            var store = _dataStorage[city];

            return store.GetAllStops(cancellationToken);
        }

        public Task<List<BaseStop>> Autocomplete(City city, string query, int? limit, CancellationToken cancellationToken)
        {
            if(query.Length < 3)
                return Task.FromResult(Enumerable.Empty<BaseStop>().ToList());
            var store = _dataStorage[city];

            return store.FindStops(query, limit, cancellationToken);
        }
    }
}