using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Enums;

namespace GtfsProvider.Common
{
    public interface IFileStorage
    {
        Task<DateTime?> GetFileTime(City city, string name, CancellationToken cancellationToken);
        Task StoreFile(City city, string name, Stream stream, CancellationToken cancellationToken);
        Task<Stream> LoadFile(City city, string name, CancellationToken cancellationToken);
        Task RemoveFile(City city, string name, CancellationToken cancellationToken);
    }
}