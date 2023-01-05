using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Enums;
using Redis.OM.Modeling;

namespace GtfsProvider.RedisStorage.Models
{
    [Document(StorageType = StorageType.Json, Prefixes = new [] { IdGenerator.StopGroupPrefix })]
    public class StoreStopGroup : IDocument
    {
        [RedisIdField]
        public string RedisId => IdGenerator.StopGroup(City, GroupId);
        [Indexed]
        public City City { get; set; }
        [Indexed]
        public string GroupId { get; set; }
        [Indexed]
        public string Name { get; set; }
        [Indexed]
        public VehicleType Type { get; set; }
    }
}