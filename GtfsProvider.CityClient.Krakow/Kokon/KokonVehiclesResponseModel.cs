using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GtfsProvider.CityClient.Krakow.Kokon
{
    public class KokonVehiclesResponseModel
    {
        [JsonProperty("veh_no")]
        [JsonPropertyName("veh_no")]
        public string SideNo { get; set; } = string.Empty;
    }
}