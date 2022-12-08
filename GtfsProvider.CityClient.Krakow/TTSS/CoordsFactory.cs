using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Models;

namespace GtfsProvider.CityClient.Krakow.TTSS
{
    public static class CoordsFactory
    {
        public static Coords? FromTTSS(int? lat, int? lon)
        {
            if (!lat.HasValue || !lon.HasValue)
                return null;
            return new(lat.Value / 3600000.0d, lon.Value / 3600000.0d);
        }
    }
}