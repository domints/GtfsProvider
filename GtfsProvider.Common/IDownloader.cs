using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Enums;
using GtfsProvider.Common.Models;

namespace GtfsProvider.Common
{
    public interface IDownloader
    {
        City City { get; }
        Task RefreshIfNeeded(CancellationToken cancellationToken);
    }
}