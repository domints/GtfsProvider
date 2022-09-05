using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Enums;

namespace GtfsProvider.Common
{
    public interface IFileStorage
    {
        Task<DateTime?> GetFileTime(City city, string name);
        Task StoreFile(City city, string name, Stream stream);
        Task<Stream> LoadFile(City city, string name);
    }
}