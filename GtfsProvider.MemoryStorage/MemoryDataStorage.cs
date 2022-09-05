using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common;
using GtfsProvider.Common.Enums;
using GtfsProvider.Common.Extensions;

namespace GtfsProvider.MemoryStorage
{
    public class MemoryDataStorage : IDataStorage
    {
        private readonly ConcurrentDictionary<City, ICityStorage> _cityStores;
        public ICityStorage this[City city] => _cityStores.GetValueOrDefault(city, () => new MemoryCityStorage());

        public MemoryDataStorage()
        {
            _cityStores = new ConcurrentDictionary<City, ICityStorage>();
        }
    }
}