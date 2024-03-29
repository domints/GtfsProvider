using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common;
using GtfsProvider.Common.Enums;
using GtfsProvider.Common.Extensions;

namespace GtfsProvider.Common
{
    public class DataStorage : IDataStorage
    {
        private readonly ConcurrentDictionary<City, ICityStorage> _cityStores;
        private readonly ICityStorageFactory _cityStoreFactory;

        public ICityStorage this[City city] => _cityStores.GetValueOrDefault(city, () => _cityStoreFactory.GetCityStorage(city));

        public DataStorage(ICityStorageFactory cityStoreFactory)
        {
            _cityStores = new ConcurrentDictionary<City, ICityStorage>();
            _cityStoreFactory = cityStoreFactory;
        }
    }
}