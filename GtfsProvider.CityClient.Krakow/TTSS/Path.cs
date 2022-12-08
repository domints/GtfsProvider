using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GtfsProvider.CityClient.Krakow.TTSS
{
    public class Path
    {
        [JsonProperty("y1")]
        public int Y1 { get; set; }

        [JsonProperty("length")]
        public decimal Length { get; set; }

        [JsonProperty("x1")]
        public int X1 { get; set; }

        [JsonProperty("y2")]
        public int Y2 { get; set; }

        [JsonProperty("angle")]
        public int Angle { get; set; }

        [JsonProperty("x2")]
        public int X2 { get; set; }
    }
}