using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Enums;
using Redis.OM.Modeling;

namespace GtfsProvider.RedisStorage.Models
{
    [Document(StorageType = StorageType.Json, Prefixes = new [] { IdGenerator.VehiclePrefix })]
    public class StoreVehicle : IDocument
    {
        [RedisIdField]
        public string Id => IdGenerator.Vehicle(City, ModelType, UniqueId);
        [Indexed]
        public City City { get; set; }
        [Indexed]
        public long UniqueId { get; set; }
        [Indexed]
        public long GtfsId { get; set; }
        [Indexed]
        public string SideNo { get; set; }
        public bool IsHeuristic { get; set; }
        public int HeuristicScore { get; set; }

        public string ModelName { get; set; }
        public LowFloor ModelLowFloor { get; set; }
        [Indexed]
        public VehicleType ModelType { get; set; }
    }
}