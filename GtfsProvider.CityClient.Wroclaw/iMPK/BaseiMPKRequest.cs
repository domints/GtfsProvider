using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Attributes;

namespace GtfsProvider.CityClient.Wroclaw.iMPK
{
    public abstract class BaseiMPKRequest
    {
        [Param("function")]
        public abstract string Function { get; }
    }
}