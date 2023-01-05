using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Attributes;

namespace GtfsProvider.CityClient.Wroclaw.iMPK.Requests
{
    public class GetPostInfoRequest : BaseiMPKRequest
    {
        public override string Function => "getPostInfo";
        [Param("symbol")]
        public string? StopPostId { get; set; }
    }
}