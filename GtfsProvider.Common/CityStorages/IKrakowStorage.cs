using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Models;

namespace GtfsProvider.Common.CityStorages
{
    public interface IKrakowStorage : ICityStorage
    {
        long VehicleIdOffset { get; set; }
    }
}