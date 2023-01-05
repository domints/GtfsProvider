using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GtfsProvider.CityClient.Wroclaw.iMPK
{
    public class VehicleInfo
    {

        [JsonProperty("v")]
        public int VehicleId { get; set; }

        [JsonProperty("f")]
        public string FloorType { get; set; }

        [JsonProperty("m")]
        public string Model { get; set; }

        [JsonProperty("a")]
        public iMPKVehicleType Type { get; set; }
    }
}