using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GtfsProvider.Common.Models
{
    public record Coords(double Latitude, double Longitude)
    {
        public double DistanceTo(Coords other)
        {
            GeographicLib.Geodesic.WGS84.Inverse(Latitude, Longitude, other.Latitude, other.Longitude, out double distance);
            return distance;
        }
    }
}