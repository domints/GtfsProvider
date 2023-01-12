using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Enums;

namespace GtfsProvider.Common.Models
{
    public class JacekkVehicle
    {
        public string Num { get; set; }
        public string Type { get; set; }
        public int Low { get; set; }
        public VehicleType VehicleType { get; set; }
    }
}