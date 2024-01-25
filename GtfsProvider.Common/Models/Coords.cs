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

        public string ToGoogleMapsUrl()
        {
            return $"https://www.google.com/maps/@{Latitude:N7},{Longitude:N7},18z";
        }

        public static Coords Zero => new Coords(0d, 0d);
    }
}