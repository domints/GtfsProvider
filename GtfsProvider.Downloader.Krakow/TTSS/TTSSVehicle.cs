using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GtfsProvider.Downloader.Krakow.TTSS
{
    public class TTSSVehicle
    {
        [JsonProperty("isDeleted")]
        public bool IsDeleted { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("path")]
        public IList<Path> Path { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }

        [JsonProperty("heading")]
        public int? Heading { get; set; }

        [JsonProperty("latitude")]
        public int? Latitude { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("tripId")]
        public string TripId { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("longitude")]
        public int? Longitude { get; set; }
    }
}