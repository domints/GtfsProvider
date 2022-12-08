using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GtfsProvider.CityClient.Krakow.TTSS.Passages
{
    public class TripPassage : BasePassage
    {
        /// <summary>
        /// Number in trip stops sequence
        /// </summary>
        [JsonProperty("stop_seq_num")]
        public int SequenceNo { get; set; }

        /// <summary>
        /// Stop
        /// </summary>
        [JsonProperty("stop")]
        public PassageStop? Stop { get; set; }
    }
}