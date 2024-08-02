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
        public static RedisConnectionProvider ConnectionProvider => _connProvider ?? throw new InvalidOperationException("Redis connection haven't been configured yet!");
        private static RedisConnectionProvider? _connProvider;
        private static readonly ConcurrentDictionary<string, RedisConnectionProvider> _connProviders = [];

        public static IServiceCollection RegisterRedisDataStorage(this IServiceCollection services)
        {
            services.AddTransient<ICityStorage, RedisCityStorage>();
            return services;
        }

        public static async Task<IRedisCollection<T>> GetCollection<T>(string redisUrl, CancellationToken cancellationToken)
            where T : notnull, IDocument
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
                return newProvider.RedisCollection<T>();
            }

            return connectionProvider.RedisCollection<T>();
        }
    }
}