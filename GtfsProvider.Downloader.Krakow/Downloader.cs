using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CsvHelper;
using GtfsProvider.Common;
using GtfsProvider.Common.CityStorages;
using GtfsProvider.Common.Enums;
using GtfsProvider.Common.Extensions;
using GtfsProvider.Common.Models;
using GtfsProvider.Common.Models.Gtfs;
using GtfsProvider.Downloader.Krakow.Extensions;
using GtfsProvider.Downloader.Krakow.TTSS;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Logging;

namespace GtfsProvider.Downloader.Krakow
{
    public class Downloader : IDownloader
    {
        private const string _baseUrl = "https://gtfs.ztp.krakow.pl/";
        private const string _gtfZipBus = "GTFS_KRK_A.zip";
        private const string _gtfZipTram = "GTFS_KRK_T.zip";
        private const string _positionsFileBus = "VehiclePositions_A.pb";
        private const string _positionsFileTram = "VehiclePositions_T.pb";
        public City City => City.Krakow;
        private readonly IFileStorage _fileStorage;
        private readonly IKrakowStorage _dataStorage;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly VehicleDbBuilder _tramVehicleDbBuilder;
        private readonly VehicleDbBuilder _busVehicleDbBuilder;
        private readonly Regex _fileRegex = new("<a href=\"([A-z\\.]+)\">([A-z\\.]+)<\\/a>\\s+([0-9]{2}-[A-z]{3}-[0-9]{4}\\s[0-9]{2}:[0-9]{2})", RegexOptions.Compiled | RegexOptions.Multiline);
        private readonly ILogger<Downloader> _logger;
        private readonly ITTSSClient _ttssClient;

        public Downloader(
            IFileStorage fileStorage,
            IDataStorage dataStorage,
            IHttpClientFactory httpClientFactory,
            VehicleDbBuilder tramVehicleDbBuilder,
            VehicleDbBuilder busVehicleDbBuilder,
            ITTSSClient ttssClient,
            ILogger<Downloader> logger)
        {
            _fileStorage = fileStorage;
            _dataStorage = dataStorage[City] as IKrakowStorage ?? throw new InvalidOperationException("What is wrong with your DI configuration?!");
            _httpClientFactory = httpClientFactory;
            _tramVehicleDbBuilder = tramVehicleDbBuilder;
            _busVehicleDbBuilder = busVehicleDbBuilder;
            _logger = logger;
            this._ttssClient = ttssClient;
        }

        public async Task RefreshIfNeeded()
        {
            var httpClient = _httpClientFactory.CreateClient($"Downloader_{City}");
            var contents = await httpClient.GetStringAsync(_baseUrl);
            var fileList = _fileRegex.Matches(contents).Select(m => new { Name = m.Groups[1].Value, Time = DateTime.Parse(m.Groups[3].Value) }).ToList();

            foreach(var file in fileList)
            {
                var lastFileUpdate = await _fileStorage.GetFileTime(City, file.Name);
                if(lastFileUpdate == null || lastFileUpdate.Value < file.Time)
                {
                    using(var fileStream = await httpClient.GetStreamAsync($"{_baseUrl}{file.Name}"))
                    {
                        await _fileStorage.StoreFile(City, file.Name, fileStream);
                    }
                }
            }

            await _busVehicleDbBuilder.Build(VehicleType.Bus, _positionsFileBus);
            await _tramVehicleDbBuilder.Build(VehicleType.Tram, _positionsFileTram);

            if((await _fileStorage.GetFileTime(City, _gtfZipBus)).HasValue)
                await ParseGtfsZip(_gtfZipBus, VehicleType.Bus);

            if((await _fileStorage.GetFileTime(City, _gtfZipTram)).HasValue)
                await ParseGtfsZip(_gtfZipTram, VehicleType.Tram);
        }

        private async Task ParseGtfsZip(string name, VehicleType type)
        {
            using (ZipFile zip = new ZipFile(await _fileStorage.LoadFile(City, name)))
            {
                var stopsZipEntry = zip.GetEntry("stops.txt");
                var stops = new Dictionary<string, Stop>();
                using(var stopsStream = zip.GetInputStream(stopsZipEntry))
                using(var reader = new StreamReader(stopsStream))
                using(var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    while(await csv.ReadAsync())
                    {
                        var entry = csv.GetRecord<StopEntry>();
                        stops.Add(entry.Id, new Stop
                        {
                            GtfsId = entry.Id,
                            GroupId = entry.GetGroupId(),
                            Name = entry.Name,
                            Latitude = entry.Lat,
                            Longitude = entry.Lon,
                            Type = type
                        });
                    }
                }

                var existingIds = (await _dataStorage.GetStopIdsByType(type))
                    .ToHashSet();

                var toRemove = existingIds.ExceptIn(stops.Keys.ToHashSet());
                var toAdd = stops.ExceptIn(existingIds);

                await _dataStorage.RemoveStops(toRemove);
                await _dataStorage.AddStops(toAdd);
            }
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
    }
}