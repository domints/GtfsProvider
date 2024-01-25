using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common;
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
        public static IServiceCollection RegisterRedisDataStorage(this IServiceCollection services)
        {
            services.AddSingleton<ICityStorage, RedisCityStorage>();
            return services;
        }

        public static async Task<IRedisCollection<T>> GetCollection<T>(CancellationToken cancellationToken)
            where T : notnull, IDocument
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_connProvider == null)
            {
                _connProvider = new RedisConnectionProvider("redis://localhost:6379");
                var connection = _connProvider.Connection;
                await connection.CreateIndexAsync(typeof(StoreVehicle));
                await connection.CreateIndexAsync(typeof(StoreStop));
                await connection.CreateIndexAsync(typeof(StoreStopGroup));
            }

            return _connProvider.RedisCollection<T>();
        }
    }
}