using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GtfsProvider.CityClient.Wroclaw.iMPK
{
    public class PostGroupDeparture
    {
        [JsonProperty("l")]
        public string? Line { get; set; }

        [JsonProperty("d")]
        public string? DirectionStopId { get; set; }

        [JsonProperty("t")]
        public DateTime Time { get; set; }

        [JsonProperty("c")]
        public int CourseId { get; set; }

        [JsonProperty("s")]
        public string? StopPostId { get; set; }
    }
}