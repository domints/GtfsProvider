using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Enums;
using GtfsProvider.Common.Extensions;
using GtfsProvider.Common.Models;
using GtfsProvider.Common.Models.Gtfs;

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

        public static StoreCalendar ToStoreModel(this CalendarEntry calendar, City city, VehicleType type)
        {
            return new StoreCalendar
            {
                GtfsId = calendar.ServiceId,
                Monday = calendar.Monday == ServiceAvailability.Available,
                Tuesday = calendar.Tuesday == ServiceAvailability.Available,
                Wednesday = calendar.Wednesday == ServiceAvailability.Available,
                Thursday = calendar.Thursday == ServiceAvailability.Available,
                Friday = calendar.Friday == ServiceAvailability.Available,
                Saturday = calendar.Saturday == ServiceAvailability.Available,
                Sunday = calendar.Sunday == ServiceAvailability.Available,
                StartDate = calendar.StartDate.ToInt(),
                EndDate = calendar.EndDate.ToInt(),
                City = city,
                ServiceType = type
            };
        }

        public static CalendarEntry ToAppModel(this StoreCalendar calendar)
        {
            return new CalendarEntry
            {
                ServiceId = calendar.GtfsId,
                Monday = calendar.Monday ? ServiceAvailability.Available : ServiceAvailability.NotAvailable,
                Tuesday = calendar.Tuesday ? ServiceAvailability.Available : ServiceAvailability.NotAvailable,
                Wednesday = calendar.Wednesday ? ServiceAvailability.Available : ServiceAvailability.NotAvailable,
                Thursday = calendar.Thursday ? ServiceAvailability.Available : ServiceAvailability.NotAvailable,
                Friday = calendar.Friday ? ServiceAvailability.Available : ServiceAvailability.NotAvailable,
                Saturday = calendar.Saturday ? ServiceAvailability.Available : ServiceAvailability.NotAvailable,
                Sunday = calendar.Sunday ? ServiceAvailability.Available : ServiceAvailability.NotAvailable,
                StartDate = calendar.StartDate.ToDateOnly(),
                EndDate = calendar.EndDate.ToDateOnly()
            };
        }
    }
}