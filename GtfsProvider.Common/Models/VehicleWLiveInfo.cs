using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GtfsProvider.Common.Models
{
    public class VehicleWLiveInfo : Vehicle
    {
        public VehicleLiveInfo? LiveInfo { get; set; }
    }
}