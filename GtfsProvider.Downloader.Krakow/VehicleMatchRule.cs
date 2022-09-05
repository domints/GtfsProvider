using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GtfsProvider.Downloader.Krakow
{
    public class VehicleMatchRule
    {
        public int FromId { get; set; }
        public int ToId { get; set; }
        public string Symbol { get; set; }
        public string ModelName { get; set; }
    }
}