using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.CityStorages;
using GtfsProvider.Common.Enums;
using GtfsProvider.Common.Models;

namespace GtfsProvider.MemoryStorage.CityStorages
{
    public class KrakowMemoryCityStorage : MemoryCityStorage, IKrakowStorage
    {
        public long VehicleIdOffset { get; set;  }
        public override City City => City.Krakow;

        public override Task<Vehicle?> GetVehicleByTtssId(long vehicleId)
        {
            return GetVehicleByGtfsId(vehicleId - VehicleIdOffset);
        }
    }
}