using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GtfsProvider.Common
{
    public static class Events
    {
        public static EventId VehicleDbUpdated = new EventId(100, "VehicleDbUpdated");
        public static EventId FailedToExecuteDownloader = new EventId(200, "FailedToExecuteDownloader");
        public static EventId VehBuilderNoMatch = new EventId(1000, "NoMatchRule");
        public static EventId VehBuilderMismatchSideNo = new EventId(1001, "MismatchSideNo");
        public static EventId VehBuilderMissModelInfo = new EventId(1002, "MissingModelInfo");
        public static EventId VehBuilderDuplicateSideNo = new EventId(1003, "DuplicateSideNo");
        public static EventId VehBuilderDuplicateNonHeuristic = new EventId(1004, "DuplicateSideNoNonHeuristic");
        public static EventId VehicleWithNullModel = new EventId(9000, "VehicleWithNullModel");
    }
}