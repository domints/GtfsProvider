using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.CityClient.Wroclaw.Extensions;
using GtfsProvider.CityClient.Wroclaw.iMPK;
using GtfsProvider.Common;
using GtfsProvider.Common.Enums;
using GtfsProvider.Common.Models;

namespace GtfsProvider.CityClient.Wroclaw
{
    public class WroclawDownloader : IDownloader
    {
        public City City => City.Wroclaw;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly iMPKClient _iMPK;
        private readonly ICityStorage _dataStorage;

        public WroclawDownloader(IHttpClientFactory httpClientFactory,
            IDataStorage dataStorage,
            iMPKClient iMPK)
        {
            _iMPK = iMPK;
            _dataStorage = dataStorage[City];
            _httpClientFactory = httpClientFactory;
        }

        public async Task RefreshIfNeeded()
        {
            await DownloadVehicles();
            await DownloadStops();
        }

        private async Task DownloadStops()
        {
            var stops = await _iMPK.GetStops();
            if (stops == null)
                return;

            var stopPosts = new List<Common.Models.Stop>();
            var stopGroups = new List<BaseStop>();

            foreach(var s in stops)
            {
                stopGroups.Add(new BaseStop
                {
                    Name = Decapsify(s.Name),
                    GroupId = s.StopId,
                    Type = s.Type.ToStopType()
                });

                stopPosts.AddRange(s.Posts.Select(p => new Common.Models.Stop
                {
                    GroupId = s.StopId,
                    Name = Decapsify(s.Name),
                    Latitude = p.Lat,
                    Longitude = p.Lon,
                    Type = p.Type.ToStopType(),
                    GtfsId = p.PostId
                }));
            }

            var existingPosts = (await _dataStorage.GetAllStopIds()).ToHashSet();
            var existingGroups = (await _dataStorage.GetAllStopGroupIds()).ToHashSet();

            var stopsToAdd = stopPosts.ExceptBy(existingPosts, s => s.GtfsId);
            var stopsToRemove = existingPosts.Except(stopPosts.Select(s => s.GtfsId).ToHashSet());

            await _dataStorage.RemoveStops(stopsToRemove);
            await _dataStorage.AddStops(stopsToAdd);

            var groupsToAdd = stopGroups.ExceptBy(existingGroups, s => s.GroupId);
            var groupsToRemove = existingGroups.Except(stopGroups.Select(s => s.GroupId).ToHashSet());

            await _dataStorage.RemoveStopGroups(groupsToRemove);
            await _dataStorage.AddStopGroups(groupsToAdd);

            await _dataStorage.MarkSyncDone();
        }

        private async Task DownloadVehicles()
        {
            var modelDict = new Dictionary<string, VehicleModel>();
            var vehicles = await _iMPK.GetVehicles();
            if (vehicles == null)
                return;

            var oldVehicles = (await _dataStorage.GetAllVehicles()).ToDictionary(v => v.SideNo);

            foreach (var vehicle in vehicles)
            {
                var veh = new Vehicle
                {
                    SideNo = vehicle.VehicleId.ToString(),
                    GtfsId = vehicle.VehicleId,
                    UniqueId = vehicle.VehicleId
                };

                if (modelDict.ContainsKey(vehicle.Model))
                {
                    veh.Model = modelDict[vehicle.Model];
                }
                else
                {
                    var model = new VehicleModel
                    {
                        Name = vehicle.Model,
                        LowFloor = vehicle.FloorType.ToFloorType(),
                        Type = vehicle.Type.ToVehicleType()
                    };
                    modelDict.Add(vehicle.Model, model);
                    veh.Model = model;
                }

                await _dataStorage.AddOrUpdateVehicle(veh, oldVehicles);
            }
        }

        private string Decapsify(string input)
        {
            if (input.All(c => !char.IsLetter(c) || char.IsUpper(c)))
                return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(input.ToLowerInvariant());

            return input;
        }
    }
}