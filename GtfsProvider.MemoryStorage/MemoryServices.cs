using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common;
using Microsoft.Extensions.DependencyInjection;

namespace GtfsProvider.MemoryStorage
{
    public static class MemoryServices
    {
        public static IServiceCollection RegisterMemoryDataStorage(this IServiceCollection services)
        {
            services.AddSingleton<IDataStorage, MemoryDataStorage>();
            services.AddSingleton<ICityStorage, MemoryCityStorage>();
            return services;
        }
    }
}