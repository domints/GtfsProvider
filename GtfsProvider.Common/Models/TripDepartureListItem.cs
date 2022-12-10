using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GtfsProvider.Common.Models
{
    public class TripDepartureListItem
    {
        public string? TimeString { get; set; }
        public string? StopId { get; set; }
        public string? StopName { get; set; }
        public int SeqNumber { get; set; }
        public bool IsOld { get; set; }
        public bool IsStopping { get; set; }
    }
}