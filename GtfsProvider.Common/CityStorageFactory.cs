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
        private readonly IServiceProvider _svcProvider;

        public CityStorageFactory(IEnumerable<ICityStorage> cityStorages, IServiceProvider svcProvider)
        {
            _cityStorages = cityStorages;
            _svcProvider = svcProvider;
        }

        public ICityStorage GetCityStorage(City city)
        {
            var cityStore = _cityStorages.FirstOrDefault(c => c.City == city);

            if (cityStore != null)
                return cityStore;

            var defaultCityStoreTemplate = _cityStorages.FirstOrDefault(c => c.City == City.Default);
            if(defaultCityStoreTemplate == null)
                throw new NotImplementedException("Default city store not implemented or not configured into DI container.");

            var templateType = defaultCityStoreTemplate.GetType();
            var cityConstructor = templateType.GetConstructors().FirstOrDefault(c => c.GetParameters().Any(p => p.ParameterType == typeof(City)));
            if (cityConstructor == null)
                throw new InvalidOperationException("Default city storage MUST have city-taking contructor.");

            var args = cityConstructor.GetParameters().Select(p => p.ParameterType == typeof(City) ? (object?)city : _svcProvider.GetService(p.ParameterType)).ToArray();
            var defaultInstance = Activator.CreateInstance(templateType, args);
            if(defaultInstance == null)
                throw new InvalidOperationException("Default city storage MUST have parameterless contructor.");
            return (ICityStorage)defaultInstance;
        }
    }
}