using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GtfsProvider.Common.Models
{
    public class TripDepartures
    {
        public string? Line { get; set; }
        public string? Direction { get; set; }
        public List<TripDepartureListItem>? ListItems { get; set; }
    }
}