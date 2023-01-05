using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GtfsProvider.Common.Models
{
    public class Vehicle
    {
        public long UniqueId { get; set; }
        public long GtfsId { get; set; }
        public string SideNo { get; set; }
        public VehicleModel Model { get; set; }
        public bool IsHeuristic { get; set; }
        public int HeuristicScore { get; set; }

        public override string ToString()
        {
            return $"{SideNo} - {GtfsId:D3} - {UniqueId} - {(IsHeuristic ? "?" : "")}";
        }
    }
}