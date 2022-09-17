using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Models;

namespace GtfsProvider.Downloader.Krakow.TTSS
{
    public class TTSSCleanVehicle
    {
        public long Id { get; set; }
        public string Line { get; set; }
        public string Direction { get; set; }
        public Coords Coords { get; set; }
    }
}