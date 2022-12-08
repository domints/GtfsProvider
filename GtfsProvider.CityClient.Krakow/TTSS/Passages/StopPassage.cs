using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GtfsProvider.CityClient.Krakow.TTSS.Passages
{
    public class StopPassage : BasePassage
    {
        /// <summary>
        /// Gets or sets the actual relative time (time to come) in seconds.
        /// </summary>
        /// <value>
        /// The actual relative time.
        /// </value>
        [JsonProperty("actualRelativeTime")]
        public int ActualRelativeTime { get; set; }

        /// <summary>
        /// Gets or sets the direction. Name of the last stop.
        /// </summary>
        /// <value>
        /// The direction.
        /// </value>
        [JsonProperty("direction")]
        public string? Direction { get; set; }

        /// <summary>
        /// Gets or sets the mixed time text. Like "5 Min".
        /// </summary>
        /// <value>
        /// The mixed time.
        /// </value>
        [JsonProperty("mixedTime")]
        public string? MixedTime { get; set; }

        /// <summary>
        /// Gets or sets the ID of the passage.
        /// </summary>
        /// <value>
        /// The passageid.
        /// </value>
        [JsonProperty("passageid")]
        public string? Passageid { get; set; }

        /// <summary>
        /// Gets or sets the pattern text. As I can see, quite like tram line number.
        /// </summary>
        /// <value>
        /// The pattern text.
        /// </value>
        [JsonProperty("patternText")]
        public string? PatternText { get; set; }

        /// <summary>
        /// Gets or sets the planned time text (like 12:34), as in schedule.
        /// </summary>
        /// <value>
        /// The planned time.
        /// </value>
        [JsonProperty("plannedTime")]
        public string? PlannedTime { get; set; }

        /// <summary>
        /// Gets or sets the route identifier.
        /// </summary>
        /// <value>
        /// The route identifier.
        /// </value>
        [JsonProperty("routeId")]
        public string? RouteID { get; set; }

        /// <summary>
        /// Gets or sets the trip identifier.
        /// </summary>
        /// <value>
        /// The trip identifier.
        /// </value>
        [JsonProperty("tripId")]
        public string? TripID { get; set; }

        /// <summary>
        /// Gets or sets the vehicle identifier.
        /// </summary>
        /// <value>
        /// The vehicle identifier.
        /// </value>
        [JsonProperty("vehicleId")]
        public string? VehicleID { get; set; }
    }
}