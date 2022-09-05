using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GtfsProvider.Downloader.Krakow.TTSS
{
    public class TTSSCleanVehicle
    {
        public string Id { get; set; }
        public string Line { get; set; }
        public string Direction { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public decimal Heading { get; set; }
    }
}