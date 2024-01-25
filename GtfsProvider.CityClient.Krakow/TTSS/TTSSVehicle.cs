using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GtfsProvider.CityClient.Krakow.TTSS
{
    public class TTSSVehicle
    {
        [JsonProperty("isDeleted")]
        public bool IsDeleted { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("path")]
        public IList<Path> Path { get; set; } = new List<Path>();

        [JsonProperty("color")]
        public string Color { get; set; } = string.Empty;

        [JsonProperty("heading")]
        public int? Heading { get; set; }

        [JsonProperty("latitude")]
        public int? Latitude { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("tripId")]
        public string TripId { get; set; } = string.Empty;

        [JsonProperty("category")]
        public string Category { get; set; } = string.Empty;

        [JsonProperty("longitude")]
        public int? Longitude { get; set; }
    }
}