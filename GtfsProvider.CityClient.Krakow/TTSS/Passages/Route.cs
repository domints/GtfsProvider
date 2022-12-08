using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GtfsProvider.CityClient.Krakow.TTSS.Passages
{
    /// <summary>
    /// class containing route details
    /// </summary>
    public class Route
    {
        /// <summary>
        /// Gets or sets the alerts. Didn't found any yet, probably list of strings, but no one knows.
        /// </summary>
        /// <value>
        /// The alerts.
        /// </value>
        [JsonProperty("alerts")]
        public List<object>? Alerts { get; set; }

        /// <summary>
        /// Gets or sets the authority, that sent that data. In our case always "MPK"
        /// </summary>
        /// <value>
        /// The authority.
        /// </value>
        [JsonProperty("authority")]
        public string? Authority { get; set; }

        /// <summary>
        /// Gets or sets the possible directions for that route. Like ["Bronowice Małe", "Wzgórza Krzesławickie"]
        /// </summary>
        /// <value>
        /// The directions.
        /// </value>
        [JsonProperty("directions")]
        public List<string>? Directions { get; set; }

        /// <summary>
        /// Gets or sets the route identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        [JsonProperty("id")]
        public string? ID { get; set; }

        /// <summary>
        /// Gets or sets the route name. In our case, afaik it's line number (like "52" or "4")
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        [JsonProperty("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the type of the route. In our case it's only tram. No TTSS for busses yet.
        /// </summary>
        /// <value>
        /// The type of the route.
        /// </value>
        [JsonProperty("routeType")]
        public string? RouteType { get; set; }

        /// <summary>
        /// Gets or sets the short name. AFAIK the same as Name.
        /// </summary>
        /// <value>
        /// The short name.
        /// </value>
        [JsonProperty("shortName")]
        public string? ShortName { get; set; }
    }
}