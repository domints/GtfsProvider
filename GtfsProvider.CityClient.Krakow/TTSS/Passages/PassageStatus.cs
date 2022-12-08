using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GtfsProvider.CityClient.Krakow.TTSS.Passages
{
    public enum PassageStatus
    {
        Unknown = -1,
        Planned,
        Predicted,
        Stopping,
        Departed
    }
}