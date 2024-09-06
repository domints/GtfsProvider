using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Enums;

namespace GtfsProvider.RedisStorage
{
    public static class IdGenerator
    {
        public const string VehiclePrefix = "GtfsProvider:Vehicle_";
        public const string StopPrefix = "GtfsProvider:Stop_";
        public const string StopGroupPrefix = "GtfsProvider:StopGroup_";
        public const string CalendarPrefix = "GtfsProvider:Calendar_";
        public static string Vehicle(City city, VehicleType type, long uniqueId)
        {
            return $"{city}:{type}:{uniqueId}";
        }

        public static string Stop(City city, string gtfsId)
        {
            return $"{city}:{gtfsId}";
        }

        public static string StopGroup(City city, string groupId)
        {
            return $"{city}:{groupId}";
        }

        public static string Calendar(City city, VehicleType type, string calendarId) => $"{city}:{type}:{calendarId}";
    }
}