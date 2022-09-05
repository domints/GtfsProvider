using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Enums;

namespace GtfsProvider.Common.Models
{
    public record Stop
    {
        public int Id { get; set; }
        public string GroupId { get; set; }
        public string GtfsId { get; set; }
        public string Name { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public VehicleType Type { get; set; }
    }
}