using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GtfsProvider.CityClient.Wroclaw.iMPK
{
    public class StopPost
    {

        [JsonProperty("s")]
        public string PostId { get; set; }

        [JsonProperty("x")]
        public decimal Lon { get; set; }

        [JsonProperty("y")]
        public decimal Lat { get; set; }

        [JsonProperty("t")]
        public string Type { get; set; }
    }
}