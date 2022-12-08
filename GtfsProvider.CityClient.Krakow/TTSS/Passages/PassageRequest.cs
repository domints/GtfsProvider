using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Attributes;

namespace GtfsProvider.CityClient.Krakow.TTSS.Passages
{
    public class PassageRequest
    {
        [Param("stop")]
        public string? Stop { get; set; }

        [Param("stopPost")]
        public string? StopPost { get; set; }

        [Param("mode")]
        public string? Mode { get; set; }

        [Param("language")]
        public string? Language { get; set; }

        /// <summary>
        /// Time which for which passages will be no older than this time. In milliseconds epoch.
        /// </summary>
        [Param("startTime")]
        public int? StartTime { get; set; }

        /// <summary>
        /// TimeFrame is number of minutes from StartTime which should be fetched. Min: 5, Max: 120
        /// </summary>
        [Param("timeFrame")]
        public int? TimeFrame { get; set; }
    }
}