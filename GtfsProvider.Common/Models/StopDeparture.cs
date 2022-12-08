using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Enums;

namespace GtfsProvider.Common.Models
{
    public class StopDeparture
    {
        public string Line { get; set; }
        public string Direction { get; set; }
        public string? ModelName { get; set; }
        public string? SideNo { get; set; }
        public string TimeString { get; set; }
        public int RelativeTime { get; set; }
        public int? DelayMinutes { get; set; }
        public string VehicleId { get; set; }
        public bool IsOld { get; set; }
        public string? TripId { get; set; }
        public VehicleType VehicleType { get; set; }
        public LowFloor FloorType { get; set; }
    }
}