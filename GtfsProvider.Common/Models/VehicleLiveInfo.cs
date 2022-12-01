using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Enums;

namespace GtfsProvider.Common.Models
{
    public class VehicleLiveInfo
    {
        public long VehicleId { get; set; }
        public long TripId { get; set; }
        public string Name { get; set; }
        public Coords? Coords { get; set; }
        public int? Heading { get; set; }
        public VehicleType Type { get; set; }
        public IList<PathEntry> Path { get; set; }
    }
}