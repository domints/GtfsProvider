using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.CityClient.Wroclaw.Extensions;
using GtfsProvider.CityClient.Wroclaw.iMPK;
using GtfsProvider.Common;
using GtfsProvider.Common.Enums;
using GtfsProvider.Common.Models;
using Microsoft.Extensions.Logging;

namespace GtfsProvider.CityClient.Wroclaw
{
    public class WroclawDownloader : IDownloader
    {
        public City City => City.Wroclaw;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly iMPKClient _iMPK;
        private readonly ILogger<WroclawDownloader> _logger;
        private readonly ICityStorage _dataStorage;

        public WroclawDownloader(IHttpClientFactory httpClientFactory,
            IDataStorage dataStorage,
            iMPKClient iMPK,
            ILogger<WroclawDownloader> logger)
        {
            _iMPK = iMPK;
            _logger = logger;
            _dataStorage = dataStorage[City];
            _httpClientFactory = httpClientFactory;
        }

        public async Task RefreshIfNeeded(CancellationToken cancellationToken)
        {
            await DownloadStops(cancellationToken);
            await DownloadVehicles(cancellationToken);
        }

        private async Task DownloadStops(CancellationToken cancellationToken)
        {
            var stops = await _iMPK.GetStops(cancellationToken);
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

            var existingPosts = (await _dataStorage.GetAllStopIds(cancellationToken)).ToHashSet();
            var existingGroups = (await _dataStorage.GetAllStopGroupIds(cancellationToken)).ToHashSet();

            var stopsToAdd = stopPosts.ExceptBy(existingPosts, s => s.GtfsId);
            var stopsToRemove = existingPosts.Except(stopPosts.Select(s => s.GtfsId).ToHashSet());

            await _dataStorage.RemoveStops(stopsToRemove, cancellationToken);
            await _dataStorage.AddStops(stopsToAdd, cancellationToken);

            var groupsToAdd = stopGroups.ExceptBy(existingGroups, s => s.GroupId);
            var groupsToRemove = existingGroups.Except(stopGroups.Select(s => s.GroupId).ToHashSet());

            await _dataStorage.RemoveStopGroups(groupsToRemove, cancellationToken);
            await _dataStorage.AddStopGroups(groupsToAdd, cancellationToken);

            await _dataStorage.MarkSyncDone(cancellationToken);
        }

        private async Task DownloadVehicles(CancellationToken cancellationToken)
        {
            var modelDict = new Dictionary<string, VehicleModel>();
            var vehicles = await _iMPK.GetVehicles(cancellationToken);
            if (vehicles == null)
                return;

            var oldVehicles = (await _dataStorage.GetAllVehicles(cancellationToken)).ToDictionary(v => v.SideNo);

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

                await _dataStorage.AddOrUpdateVehicle(veh, oldVehicles, cancellationToken);
            }
        }

        private string Decapsify(string input)
        {
            if (input.All(c => !char.IsLetter(c) || char.IsUpper(c)))
                return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(input.ToLowerInvariant());

            return input;
        }

        public async Task LogSummary(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Got {stopNumber} stops in memory", await _dataStorage.CountStops(cancellationToken));
        }
    }
}