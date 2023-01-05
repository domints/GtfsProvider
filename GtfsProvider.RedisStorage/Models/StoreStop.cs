using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Enums;
using Redis.OM.Modeling;

namespace GtfsProvider.RedisStorage.Models
{
    [Document(StorageType = StorageType.Json, Prefixes = new [] { IdGenerator.StopPrefix })]
    public class StoreStop : IDocument
    {
        [RedisIdField]
        public string RedisId => IdGenerator.Stop(City, GtfsId);
        [Indexed]
        public City City { get; set; }
        [Indexed]
        public int Id { get; set; }
        [Indexed]
        public string GroupId { get; set; }
        [Indexed]
        public string GtfsId { get; set; }
        public string Name { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        [Indexed]
        public VehicleType Type { get; set; }
    }
}