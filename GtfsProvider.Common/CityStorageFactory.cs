using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Enums;

namespace GtfsProvider.Common
{
    public class CityStorageFactory : ICityStorageFactory
    {
        private readonly IEnumerable<ICityStorage> _cityStorages;

        public CityStorageFactory(IEnumerable<ICityStorage> cityStorages)
        {
            _cityStorages = cityStorages;
        }

        public ICityStorage GetCityStorage(City city)
        {
            var cityStore = _cityStorages.FirstOrDefault(c => c.City == city);

            if (cityStore != null)
                return cityStore;

            var defaultCityStoreTemplate = _cityStorages.FirstOrDefault(c => c.City == City.Default);
            if(defaultCityStoreTemplate == null)
                throw new NotImplementedException("Default city store not implemented or not configured into DI container.");

            var defaultInstance = Activator.CreateInstance(defaultCityStoreTemplate.GetType());
            if(defaultInstance == null)
                throw new InvalidOperationException("Default city storage MUST have parameterless contructor.");
            return (ICityStorage)defaultInstance;
        }
    }
}