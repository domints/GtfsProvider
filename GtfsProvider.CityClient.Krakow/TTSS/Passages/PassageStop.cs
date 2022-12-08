using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GtfsProvider.CityClient.Krakow.TTSS.Passages
{
    /// <summary>
    /// Class containing things about stop, from autocomplete service.
    /// </summary>
    public class PassageStop
    {
        /// <summary>
        /// Gets or sets the stop identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        [JsonProperty("id")]
        public string? ID { get; set; }


        /// <summary>
        /// Gets or sets the stop name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("shortName")]
        public string? ShortId { get; set; }
    }
}