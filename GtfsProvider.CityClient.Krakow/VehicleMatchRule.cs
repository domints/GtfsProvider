using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GtfsProvider.CityClient.Krakow
{
    public class VehicleMatchRule
    {
        public int FromId { get; set; }
        public int ToId { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
    }
}