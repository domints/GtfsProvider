using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CsvHelper;
using GtfsProvider.Common;
using GtfsProvider.Common.Enums;
using GtfsProvider.Common.Extensions;
using GtfsProvider.Common.Models;
using GtfsProvider.Common.Models.Gtfs;
using GtfsProvider.CityClient.Krakow.Extensions;
using GtfsProvider.CityClient.Krakow.TTSS;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Logging;

namespace GtfsProvider.CityClient.Krakow
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
        private readonly ICityStorage _dataStorage;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly VehicleDbBuilder _tramVehicleDbBuilder;
        private readonly VehicleDbBuilder _busVehicleDbBuilder;
        private readonly Regex _fileRegex = new("<a href=\"([A-z\\.]+)\">([A-z\\.]+)<\\/a>\\s+([0-9]{2}-[A-z]{3}-[0-9]{4}\\s[0-9]{2}:[0-9]{2})", RegexOptions.Compiled | RegexOptions.Multiline);
        private readonly ILogger<Downloader> _logger;

        public Downloader(
            IFileStorage fileStorage,
            IDataStorage dataStorage,
            IHttpClientFactory httpClientFactory,
            VehicleDbBuilder tramVehicleDbBuilder,
            VehicleDbBuilder busVehicleDbBuilder,
            ILogger<Downloader> logger)
        {
            _fileStorage = fileStorage;
            _dataStorage = dataStorage[City];
            _httpClientFactory = httpClientFactory;
            _tramVehicleDbBuilder = tramVehicleDbBuilder;
            _busVehicleDbBuilder = busVehicleDbBuilder;
            _logger = logger;
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

            try
            {
                await _busVehicleDbBuilder.Build(VehicleType.Bus, _positionsFileBus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load buses for Krakow! Continuing without them.");
            }

            try
            {
                await _tramVehicleDbBuilder.Build(VehicleType.Tram, _positionsFileBus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load trams for Krakow! Continuing without them.");
            }

            List<BaseStop> busStops = new();
            List<BaseStop> tramStops = new();

            if((await _fileStorage.GetFileTime(City, _gtfZipBus)).HasValue)
                busStops = await ParseGtfsZip(_gtfZipBus, VehicleType.Bus);

            if((await _fileStorage.GetFileTime(City, _gtfZipTram)).HasValue)
                tramStops = await ParseGtfsZip(_gtfZipTram, VehicleType.Tram);

            var newStopGroups = busStops.Concat(tramStops).GroupBy(g => g.GroupId)
                .Select(g => new BaseStop
                {
                    GroupId = g.Key,
                    Name = g.First().Name,
                    Type = g.Aggregate(VehicleType.None, (curr, stop) => curr | stop.Type)
                }).ToDictionary(k => k.GroupId);

            var previousStopGroups = (await _dataStorage.GetAllStopGroupIds()).ToHashSet();

            var toRemove = previousStopGroups.ExceptIn(newStopGroups.Keys.ToHashSet());
            var toAdd = newStopGroups.ExceptIn(previousStopGroups);

            await _dataStorage.RemoveStopGroups(toRemove);
            await _dataStorage.AddStopGroups(toAdd);

            await _dataStorage.MarkSyncDone();
        }

        private async Task<List<BaseStop>> ParseGtfsZip(string name, VehicleType type)
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

                var stopGroups = stops.GroupBy(s => s.Value.GroupId)
                    .Select(g => new BaseStop { GroupId = g.Key, Name = g.First().Value.Name, Type = type });
                return stopGroups.ToList();
            }
        }
    }
}