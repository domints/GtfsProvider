using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GtfsProvider.Downloader.Krakow.TTSS
{
    public class GTFSCleanVehicle
    {
        public long Id { get; set; }
        public string Num { get; set; }
        public long TripId { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public ulong Timestamp { get; set; }
    }
}