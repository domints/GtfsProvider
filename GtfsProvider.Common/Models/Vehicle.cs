using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GtfsProvider.Common.Models
{
    public class Vehicle
    {
        public long GtfsId { get; set; }
        public string SideNo { get; set; }
        public VehicleModel Model { get; set; }
    }
}