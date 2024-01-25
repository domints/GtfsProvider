using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Models;

namespace GtfsProvider.CityClient.Krakow.TTSS
{
    public class GTFSCleanVehicle
    {
        public long Id { get; set; }
        public string Num { get; set; } = string.Empty;
        public long TripId { get; set; }
        public Coords Coords { get; set; } = null!;
        public ulong Timestamp { get; set; }
    }
}