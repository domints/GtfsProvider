using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.CityClient.Krakow.TTSS;
using GtfsProvider.CityClient.Krakow.TTSS.Passages;
using GtfsProvider.Common;
using GtfsProvider.Common.CityStorages;
using GtfsProvider.Common.Enums;
using GtfsProvider.Common.Extensions;
using GtfsProvider.Common.Models;

namespace GtfsProvider.CityClient.Krakow
{
    public class KrakowLiveDataProvider : ICityLiveDataProvider
    {
        public City City => City.Krakow;
        private readonly IKrakowTTSSClient _ttssClient;
        private readonly IKrakowStorage _dataStorage;

        public KrakowLiveDataProvider(
            IKrakowTTSSClient ttssClient,
            IDataStorage dataStorage)
        {
            _ttssClient = ttssClient;
            _dataStorage = dataStorage[City] as IKrakowStorage ?? throw new InvalidOperationException("What is wrong with your DI configuration?!");
        }

        public async Task<List<StopDeparture>> GetStopDepartures(string groupId, DateTime? startTime, int? timeFrame)
        {
            var busDepartures = await _ttssClient.GetStopInfo(VehicleType.Bus, groupId, startTime, timeFrame);
            var tramDepartures = await _ttssClient.GetStopInfo(VehicleType.Tram, groupId, startTime, timeFrame);

            var result = new List<StopDeparture>();

            foreach (var departure in busDepartures?.OldPassages ?? Enumerable.Empty<StopPassage>())
            {
                result.Add(await MapTTSSStopDepartureToCommon(departure, isOld: true, VehicleType.Bus));
            }
            foreach (var departure in tramDepartures?.OldPassages ?? Enumerable.Empty<StopPassage>())
            {
                result.Add(await MapTTSSStopDepartureToCommon(departure, isOld: true, VehicleType.Tram));
            }
            foreach (var departure in busDepartures?.ActualPassages ?? Enumerable.Empty<StopPassage>())
            {
                result.Add(await MapTTSSStopDepartureToCommon(departure, isOld: false, VehicleType.Bus));
            }
            foreach (var departure in tramDepartures?.ActualPassages ?? Enumerable.Empty<StopPassage>())
            {
                result.Add(await MapTTSSStopDepartureToCommon(departure, isOld: false, VehicleType.Tram));
            }

            return result.OrderByDescending(d => d.IsOld).ThenBy(d => d.RelativeTime).ToList();
        }

        public async Task<TripDepartures> GetTripDepartures(string tripId, VehicleType vehicleType)
        {
            var departures = await _ttssClient.GetTripInfo(vehicleType, tripId);
            if (departures == null)
                return new TripDepartures();
            return new TripDepartures
            {
                Line = departures.RouteName,
                Direction = departures.DirectionText,
                ListItems = (departures.OldPassages ?? Enumerable.Empty<TripPassage>())
                    .Select(p => MapTTSSTripDepartureToCommon(p, true))
                    .Concat((departures.ActualPassages ?? Enumerable.Empty<TripPassage>())
                        .Select(p => MapTTSSTripDepartureToCommon(p, false)))
                    .OrderBy(p => p.SeqNumber)
                    .ToList()
            };
        }

        public async Task<List<VehicleLiveInfo>> GetLivePositions()
        {
            var busInfo = await _ttssClient.GetVehiclesInfo(VehicleType.Bus);
            var tramInfo = await _ttssClient.GetVehiclesInfo(VehicleType.Tram);
            return (busInfo?.Vehicles ?? new List<TTSSVehicle>())
                .Concat(tramInfo?.Vehicles ?? new List<TTSSVehicle>())
                .Where(i => !i.IsDeleted)
                .Select(i =>
            new VehicleLiveInfo
            {
                VehicleId = long.Parse(i.Id),
                TripId = long.Parse(i.TripId),
                Name = i.Name,
                Coords = CoordsFactory.FromTTSS(i.Latitude, i.Longitude),
                Heading = i.Heading,
                Type = i.Category switch
                {
                    "tram" => VehicleType.Tram,
                    "bus" => VehicleType.Bus,
                    _ => VehicleType.None
                },
                Path = i.Path != null ? i.Path.Select(p => new PathEntry
                {
                    PointA = CoordsFactory.FromTTSS(p.X1, p.Y1),
                    PointB = CoordsFactory.FromTTSS(p.X2, p.Y2),
                    Length = p.Length,
                    Angle = p.Angle
                }).ToList() : new()
            }).ToList();
        }

        private TripDepartureListItem MapTTSSTripDepartureToCommon(TripPassage passage, bool isOld)
        {
            return new TripDepartureListItem
            {
                TimeString = passage.ActualTime ?? passage.PlannedTime ?? "--:--",
                StopId = passage?.Stop?.ShortId,
                StopName = passage?.Stop?.Name,
                SeqNumber = passage.SequenceNo,
                IsOld = isOld,
                IsStopping = PassageStatusConverter.Convert(passage?.StatusString ?? "PLANNED") == PassageStatus.Stopping
            };
        }

        private async Task<StopDeparture> MapTTSSStopDepartureToCommon(StopPassage passage, bool isOld, VehicleType type)
        {
            var vehicle = await _dataStorage.GetVehicleByTtssId(long.Parse(passage?.VehicleID ?? "0"), VehicleType.Tram)
                ?? await _dataStorage.GetVehicleByTtssId(long.Parse(passage?.VehicleID ?? "0"), VehicleType.Bus);

            var status = PassageStatusConverter.Convert(passage?.StatusString ?? "");
            var actualTime = (passage?.ActualTime).ToTimeSpan();
            var plannedTime = (passage?.PlannedTime).ToTimeSpan();

            return new StopDeparture
            {
                Line = passage?.PatternText ?? "-",
                Direction = passage?.Direction ?? "-",
                ModelName = vehicle?.Model?.Name,
                SideNo = vehicle?.SideNo,
                FloorType = vehicle?.Model?.LowFloor ?? LowFloor.Unknown,
                VehicleType = vehicle?.Model?.Type ?? type,
                TimeString = passage?.MixedTime?.Replace("%UNIT_MIN%", "min") ?? "-",
                RelativeTime = passage?.ActualRelativeTime ?? 0,
                DelayMinutes = status == PassageStatus.Planned ? (int?)null : (int)Math.Ceiling((actualTime - plannedTime).TotalMinutes),
                IsOld = isOld,
                VehicleId = passage?.VehicleID ?? "0",
                TripId = passage?.TripID
            };
        }
    }
}