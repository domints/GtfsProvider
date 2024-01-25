using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Models;

namespace GtfsProvider.CityClient.Krakow.TTSS
{
    public class TTSSCleanVehicle
    {
        public long Id { get; set; }
        public string Line { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;
        public Coords Coords { get; set; } = null!;
    }
}