using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Attributes;

namespace GtfsProvider.CityClient.Wroclaw.iMPK.Requests
{
    public class GetPostGroupInfoRequest : BaseiMPKRequest
    {
        public override string Function => "getPostGroupInfo";
        [Param("symbol")]
        public string? GroupId { get; set; }
    }
}