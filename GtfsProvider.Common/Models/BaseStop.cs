using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Enums;

namespace GtfsProvider.Common.Models
{
    public class BaseStop
    {
        public string GroupId { get; set; }
        public string Name { get; set; }
        public VehicleType Type { get; set; }
    }
}