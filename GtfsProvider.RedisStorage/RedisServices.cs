using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common;
using GtfsProvider.Common.Extensions;
using GtfsProvider.RedisStorage.Models;
using Microsoft.Extensions.DependencyInjection;
using Redis.OM;
using Redis.OM.Searching;

namespace GtfsProvider.RedisStorage
{
    public static class RedisServices
    {
        private static readonly ConcurrentDictionary<string, RedisConnectionProvider> _connProviders = [];

        public static IServiceCollection RegisterRedisDataStorage(this IServiceCollection services)
        {
            services.AddTransient<ICityStorage, RedisCityStorage>();
            return services;
        }

        public static async Task<RedisConnectionProvider> GetConnectionProvider(string redisUrl, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_connProviders.TryGetValue(redisUrl, out var connectionProvider))
            {
                var newProvider = new RedisConnectionProvider(redisUrl);
                _connProviders.AddOrUpdate(redisUrl, newProvider, (_, v) => v);
                var connection = newProvider.Connection;
                await connection.CreateIndexAsync(typeof(StoreVehicle));
                await connection.CreateIndexAsync(typeof(StoreStop));
                await connection.CreateIndexAsync(typeof(StoreStopGroup));
                await connection.CreateIndexAsync(typeof(StoreCalendar));
                return newProvider;
            }

            return connectionProvider;
        }

        public static async Task<IRedisCollection<T>> GetCollection<T>(string redisUrl, CancellationToken cancellationToken)
            where T : notnull, IDocument
        {
            cancellationToken.ThrowIfCancellationRequested();

            var connectionProvider = await GetConnectionProvider(redisUrl, cancellationToken);

            return connectionProvider.RedisCollection<T>();
        }
    }
}