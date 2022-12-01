using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GtfsProvider.Common.Models
{
    public class PathEntry
    {
        public Coords? PointA { get; set; }
        public Coords? PointB { get; set; }
        public decimal Length { get; set; }
        public int Angle { get; set; }
    }
}