using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Enums;
using GtfsProvider.Common.Models;

namespace GtfsProvider.Common
{
    public interface IStopService
    {
        Task<List<BaseStop>> Autocomplete(City city, string query, int? limit, CancellationToken cancellationToken);
        Task<List<Stop>> AllStops(City city, CancellationToken cancellationToken);
    }
}