using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Enums;

namespace GtfsProvider.Common
{
    public interface IDataStorage
    {
        ICityStorage this[City city] { get; }
    }
}