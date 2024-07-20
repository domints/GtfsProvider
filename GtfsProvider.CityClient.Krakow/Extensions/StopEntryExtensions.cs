using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Models.Gtfs;

namespace GtfsProvider.CityClient.Krakow.Extensions
{
    public static class StopEntryExtensions
    {
        public static string GetGroupId(this StopEntry entry)
        {
            var id = entry.Id;
            var stopNr = id.Split('_')[2];
            return stopNr[..^Math.Min(stopNr.Length, 2)];
        }
    }
}