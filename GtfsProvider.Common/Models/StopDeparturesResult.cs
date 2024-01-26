using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Enums;

namespace GtfsProvider.Common.Models
{
    public class StopDeparturesResult
    {
        public List<StopDeparture> Departures { get; set; } = [];
        public Dictionary<VehicleType, DepartureResultType> ResultTypes { get; set; } = [];
    }
}