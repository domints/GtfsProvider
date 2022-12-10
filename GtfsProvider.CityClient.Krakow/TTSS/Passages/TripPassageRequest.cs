using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Attributes;

namespace GtfsProvider.CityClient.Krakow.TTSS.Passages
{
    public class TripPassageRequest
    {
        [Param("tripId")]
        public string TripId { get; set; } = null!;
        [Param("mode")]
        public string? Mode { get; set; }

        [Param("language")]
        public string? Language { get; set; }
    }
}