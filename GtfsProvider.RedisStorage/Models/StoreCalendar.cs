using GtfsProvider.Common.Enums;
using Redis.OM.Modeling;

namespace GtfsProvider.RedisStorage.Models
{
    [Document(StorageType = StorageType.Json, Prefixes = new [] { IdGenerator.CalendarPrefix })]
    public class StoreCalendar : IDocument
    {
        [RedisIdField]
        public string Id => IdGenerator.Calendar(City, ServiceType, GtfsId);
        [Indexed]
        public City City { get; set; }
        [Indexed]
        public VehicleType ServiceType { get; set; }
        [Indexed]
        public required string GtfsId { get; set; }
        public required bool Monday { get; set; }
        public required bool Tuesday { get; set; }
        public required bool Wednesday { get; set; }
        public required bool Thursday { get; set; }
        public required bool Friday { get; set; }
        public required bool Saturday { get; set; }
        public required bool Sunday { get; set; }
        [Indexed]
        public required int StartDate { get; set; }
        [Indexed]
        public required int EndDate { get; set; }
    }
}