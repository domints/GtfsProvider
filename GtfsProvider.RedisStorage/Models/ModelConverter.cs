using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Enums;
using GtfsProvider.Common.Models;

namespace GtfsProvider.RedisStorage.Models
{
    public static class ModelConverter
    {
        public static StoreVehicle ToStoreModel(this Vehicle vehicle, City city)
        {
            return new StoreVehicle
            {
                City = city,
                UniqueId = vehicle.UniqueId,
                GtfsId = vehicle.GtfsId,
                SideNo = vehicle.SideNo,
                IsHeuristic = vehicle.IsHeuristic,
                HeuristicScore = vehicle.HeuristicScore,
                ModelName = vehicle.Model?.Name ?? string.Empty,
                ModelLowFloor = vehicle.Model?.LowFloor ?? LowFloor.Unknown,
                ModelType = vehicle.Model?.Type ?? VehicleType.None
            };
        }

        public static Vehicle ToAppModel(this StoreVehicle vehicle, ConcurrentDictionary<string, VehicleModel> modelDict)
        {
            var model = modelDict.GetValueOrDefault(vehicle.ModelName);
            if (model == null)
            {
                var modelKey = string.IsNullOrWhiteSpace(vehicle.ModelName) ? vehicle.ModelType.ToString() : vehicle.ModelName;
                model = modelDict.GetValueOrDefault(modelKey);
                if (model == null)
                {
                    model = new VehicleModel
                    {
                        Name = vehicle.ModelName,
                        Type = vehicle.ModelType,
                        LowFloor = vehicle.ModelLowFloor
                    };
                    modelDict.AddOrUpdate(modelKey, model, (_, _) => model);
                }
            }

            return new Vehicle
            {
                UniqueId = vehicle.UniqueId,
                GtfsId = vehicle.GtfsId,
                SideNo = vehicle.SideNo,
                IsHeuristic = vehicle.IsHeuristic,
                HeuristicScore = vehicle.HeuristicScore,
                Model = model
            };
        }

        public static StoreStopGroup ToStoreModel(this BaseStop stopGroup, City city)
        {
            return new StoreStopGroup
            {
                City = city,
                GroupId = stopGroup.GroupId,
                Name = stopGroup.Name,
                Type = stopGroup.Type
            };
        }

        public static BaseStop ToAppModel(this StoreStopGroup stopGroup)
        {
            return new BaseStop
            {
                GroupId = stopGroup.GroupId,
                Name = stopGroup.Name,
                Type = stopGroup.Type
            };
        }

        public static StoreStop ToStoreModel(this Stop stop, City city)
        {
            return new StoreStop
            {
                City = city,
                Id = stop.Id,
                GtfsId = stop.GtfsId,
                GroupId = stop.GroupId,
                Name = stop.Name,
                Latitude = stop.Latitude,
                Longitude = stop.Longitude,
                Type = stop.Type
            };
        }

        public static Stop ToAppModel(this StoreStop stop)
        {
            return new Stop
            {
                Id = stop.Id,
                GtfsId = stop.GtfsId,
                GroupId = stop.GroupId,
                Name = stop.Name,
                Latitude = stop.Latitude,
                Longitude = stop.Longitude,
                Type = stop.Type
            };
        }
    }
}