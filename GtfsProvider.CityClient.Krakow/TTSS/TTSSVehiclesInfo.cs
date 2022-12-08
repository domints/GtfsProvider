using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GtfsProvider.CityClient.Krakow.TTSS
{
    public class TTSSVehiclesInfo
    {

        [JsonProperty("lastUpdate")]
        public long LastUpdate { get; set; }

        [JsonProperty("vehicles")]
        public IList<TTSSVehicle> Vehicles { get; set; }
    }
}