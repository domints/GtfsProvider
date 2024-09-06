using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper.Configuration.Attributes;
using GtfsProvider.Common.Converters;
using GtfsProvider.Common.Enums;

namespace GtfsProvider.Common.Models.Gtfs
{
    public class CalendarEntry
    {
        [Name("service_id")]
        public required string ServiceId { get; set; }
        [Name("monday")]
        public required ServiceAvailability Monday { get; set; }
        [Name("tuesday")]
        public required ServiceAvailability Tuesday { get; set; }
        [Name("wednesday")]
        public required ServiceAvailability Wednesday { get; set; }
        [Name("thursday")]
        public required ServiceAvailability Thursday { get; set; }
        [Name("friday")]
        public required ServiceAvailability Friday { get; set; }
        [Name("saturday")]
        public required ServiceAvailability Saturday { get; set; }
        [Name("sunday")]
        public required ServiceAvailability Sunday { get; set; }
        [Name("start_date")]
        [TypeConverter(typeof(DateIntConverter))]
        public required DateOnly StartDate { get; set; }
        [Name("end_date")]
        [TypeConverter(typeof(DateIntConverter))]
        public required DateOnly EndDate { get; set; }
    }
}