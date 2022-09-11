using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GtfsProvider.Common.CityStorages
{
    public interface IKrakowStorage : ICityStorage
    {
        long VehicleIdOffset { get; set; }
    }
}