using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GtfsProvider.CityClient.Krakow.TTSS.Passages
{
    /// <summary>
    /// Contains whole data returned by passage service.
    /// </summary>
    public class TripInfo : PassageList<TripPassage>
    {
        /// <summary>
        /// Gets or sets the directions. Kind of shit no one knows what it is.
        /// </summary>
        /// <value>
        /// The directions.
        /// </value>
        [JsonProperty("directionText")]
        public string? DirectionText { get; set; }

        /// <summary>
        /// Gets or sets the name of the stop.
        /// </summary>
        /// <value>
        /// The name of the stop.
        /// </value>
        [JsonProperty("routeName")]
        public string? RouteName { get; set; }
    }
}