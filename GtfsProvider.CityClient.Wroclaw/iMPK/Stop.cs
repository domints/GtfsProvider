using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GtfsProvider.CityClient.Wroclaw.iMPK
{
    public class Stop
    {

        [JsonProperty("n")]
        public string Name { get; set; }

        [JsonProperty("s")]
        public string StopId { get; set; }

        [JsonProperty("t")]
        public string Type { get; set; }

        [JsonProperty("p")]
        public List<StopPost> Posts { get; set; }
    }
}