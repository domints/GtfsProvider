using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GtfsProvider.CityClient.Wroclaw.iMPK.Requests
{
    public class GetAllPostsRequest : BaseiMPKRequest
    {
        public override string Function => "getPosts";
    }
}