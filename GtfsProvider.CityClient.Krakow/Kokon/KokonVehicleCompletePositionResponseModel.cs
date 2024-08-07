using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using GtfsProvider.Common.Models;

namespace GtfsProvider.CityClient.Krakow.Kokon
{
    public class KokonVehicleCompletePositionResponseModel
    {
        private Coords? _coords;

        [JsonPropertyName("veh_no")]
        public string SideNo { get; set; } = string.Empty;

        [JsonPropertyName("lat")]
        public double Lat { get; private set; }

        [JsonPropertyName("lon")]
        public double Lon { get; private set; }

        [JsonPropertyName("is_bus")]
        public bool IsBus { get; set; }

        [JsonPropertyName("is_on")]
        public bool? IsOn { get; set; }

        [JsonPropertyName("variant")]
        public string Variant { get; set; } = string.Empty;

        [JsonPropertyName("direction")]
        public string Direction { get; set; } = string.Empty;

        [JsonPropertyName("veh_ts")]
        [JsonConverter(typeof(KokonDateTimeConverter))]
        public DateTime VehTs { get; set; }

        [JsonPropertyName("next_stop")]
        public string NextStop { get; set; } = string.Empty;

        [JsonPropertyName("depot_short_name")]
        public string DepotShortName { get; set; } = string.Empty;

        [JsonPropertyName("group_name")]
        public string GroupName { get; set; } = string.Empty;

        [JsonIgnore]
        public Coords Coords => _coords ??= new Coords(Lat, Lon);

        public override string ToString()
        {
            return SideNo;
        }
    }
}