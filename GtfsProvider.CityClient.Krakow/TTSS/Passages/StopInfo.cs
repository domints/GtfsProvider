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
    public class StopInfo : PassageList<StopPassage>
    {
        /// <summary>
        /// Gets or sets the directions. Kind of shit no one knows what it is.
        /// </summary>
        /// <value>
        /// The directions.
        /// </value>
        [JsonProperty("directions")]
        public List<object>? Directions { get; set; }

        /// <summary>
        /// Gets or sets the first passage time. Kind of weird timestamp, multiplied by 1000. Don't think it's needed.
        /// </summary>
        /// <value>
        /// The first passage time.
        /// </value>
        [JsonProperty("firstPassageTime")]
        public long FirstPassageTime { get; set; }

        /// <summary>
        /// Gets or sets the general alerts. Another weird list, no one knows of what. Possibly strings.
        /// </summary>
        /// <value>
        /// The general alerts.
        /// </value>
        [JsonProperty("generalAlerts")]
        public List<object>? GeneralAlerts { get; set; }

        /// <summary>
        /// Gets or sets the last passage time. Another weird timestamp, like before.
        /// </summary>
        /// <value>
        /// The last passage time.
        /// </value>
        [JsonProperty("lastPassageTime")]
        public long LastPassageTime { get; set; }

        /// <summary>
        /// Gets or sets the routes. List of routes going by that stop.
        /// </summary>
        /// <value>
        /// The routes.
        /// </value>
        [JsonProperty("routes")]
        public List<Route>? Routes { get; set; }

        /// <summary>
        /// Gets or sets the name of the stop.
        /// </summary>
        /// <value>
        /// The name of the stop.
        /// </value>
        [JsonProperty("stopName")]
        public string? StopName { get; set; }

        /// <summary>
        /// Gets or sets the short name of the stop.
        /// </summary>
        /// <value>
        /// The short name of the stop.
        /// </value>
        [JsonProperty("stopShortName")]
        public string? StopShortName { get; set; }
    }
}