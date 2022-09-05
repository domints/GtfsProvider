using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper.Configuration.Attributes;
using GtfsProvider.Common.Enums;

namespace GtfsProvider.Common.Models.Gtfs
{
    public class StopEntry
    {
        [Name("stop_id")]
        public string Id { get; set; }

        [Name("stop_code")]
        public string Code { get; set; }

        [Name("stop_name")]
        public string Name { get; set; }

        [Name("stop_desc")]
        public string Desc { get; set; }

        [Name("stop_lat")]
        public decimal Lat { get; set; }

        [Name("stop_lon")]
        public decimal Lon { get; set; }

        [Name("zone_id")]
        public int? ZoneId { get; set; }

        [Name("stop_url")]
        public string Url { get; set; }

        [Name("location_type")]
        public LocationType LocationType { get; set; }

        [Name("parent_station")]
        public string ParentStation { get; set; }

        [Name("stop_timezone")]
        public string TimeZone { get; set; }

        [Name("wheelchair_boarding")]
        public string WheelchairBoarding { get; set; }
    }
}