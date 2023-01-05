using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.CityClient.Wroclaw.iMPK.Requests;
using GtfsProvider.Common.Extensions;

namespace GtfsProvider.CityClient.Wroclaw.iMPK
{
    public class iMPKClient
    {
        private const string APIUrl = "https://62.233.178.84:8088/mobile";
        private readonly IHttpClientFactory _httpClientFactory;

        public iMPKClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<VehicleInfo>?> GetVehicles()
        {
            var client = _httpClientFactory.CreateClient(Consts.iMPK_HttpClient_Name);
            return await client.GetJson<GetAllVehiclesRequest, List<VehicleInfo>>(APIUrl);
        }

        public async Task<List<Stop>?> GetStops()
        {
            var client = _httpClientFactory.CreateClient(Consts.iMPK_HttpClient_Name);
            return await client.GetJson<GetAllPostsRequest, List<Stop>>(APIUrl);
        }

        public async Task<List<PostGroupDeparture>?> GetStopGroupInfo(string groupId)
        {
            var client = _httpClientFactory.CreateClient(Consts.iMPK_HttpClient_Name);
            return await client.GetJson<GetPostGroupInfoRequest, List<PostGroupDeparture>>(APIUrl, new GetPostGroupInfoRequest { GroupId = groupId });
        }
    }
}