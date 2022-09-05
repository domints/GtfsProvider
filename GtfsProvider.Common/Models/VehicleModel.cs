using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Enums;

namespace GtfsProvider.Common.Models
{
    public class VehicleModel
    {
        public string Name { get; set; }
        public LowFloor LowFloor { get; set; }
    }
}