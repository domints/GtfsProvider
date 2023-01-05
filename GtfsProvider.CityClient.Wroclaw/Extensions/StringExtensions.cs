using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.CityClient.Wroclaw.iMPK;
using GtfsProvider.Common.Enums;

namespace GtfsProvider.CityClient.Wroclaw.Extensions
{
    internal static class StringExtensions
    {
        public static LowFloor ToFloorType(this string value)
        {
            return value switch
            {
                "h" => LowFloor.None,
                "p" => LowFloor.Partial,
                "l" => LowFloor.Full,
                _ => LowFloor.Unknown
            };
        }

        public static VehicleType ToStopType(this string value)
        {
            return value switch
            {
                "b" => VehicleType.Bus,
                "t" => VehicleType.Tram,
                "o" => VehicleType.Bus | VehicleType.Tram,
                _ => VehicleType.None
            };
        }

        public static VehicleType ToVehicleType(this iMPKVehicleType impkType)
        {
            return impkType switch
            {
                iMPKVehicleType.Bus => VehicleType.Bus,
                iMPKVehicleType.Tram => VehicleType.Tram,
                _ => VehicleType.None
            };
        }
    }
}