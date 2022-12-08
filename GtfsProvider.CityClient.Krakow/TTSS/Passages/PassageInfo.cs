using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GtfsProvider.CityClient.Krakow.TTSS.Passages
{
    public class PassageList<TPassage>
        where TPassage : BasePassage
    {
        /// <summary>
        /// Gets or sets the list of actual passages.
        /// </summary>
        /// <value>
        /// The actual passages.
        /// </value>
        [JsonProperty("actual")]
        public List<TPassage>? ActualPassages { get; set; }

        /// <summary>
        /// Gets or sets the old passages, that were on stop and gone. Few of them.
        /// </summary>
        /// <value>
        /// The old passages.
        /// </value>
        [JsonProperty("old")]
        public List<TPassage>? OldPassages { get; set; }
    }
}