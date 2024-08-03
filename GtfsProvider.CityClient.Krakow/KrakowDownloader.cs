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
    public class KrakowDownloader : IDownloader
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
        private readonly Regex _fileRegex = new("\\(aktualizacja:\\s([0-9\\-\\ \\:]{8,})\\)\\s<a href=\"([A-z\\._\\/]+)\">([A-z\\\\._]+)<\\/a>", RegexOptions.Compiled | RegexOptions.Multiline);
        private readonly ILogger<KrakowDownloader> _logger;

        public KrakowDownloader(
            IFileStorage fileStorage,
            IDataStorage dataStorage,
            IHttpClientFactory httpClientFactory,
            VehicleDbBuilder tramVehicleDbBuilder,
            VehicleDbBuilder busVehicleDbBuilder,
            ILogger<KrakowDownloader> logger)
        {
            _fileStorage = fileStorage;
            _dataStorage = dataStorage[City];
            _httpClientFactory = httpClientFactory;
            _tramVehicleDbBuilder = tramVehicleDbBuilder;
            _busVehicleDbBuilder = busVehicleDbBuilder;
            _logger = logger;
        }

        public async Task RefreshIfNeeded(CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient($"Downloader_{City}");
            var contents = await httpClient.GetStringAsync(_baseUrl, cancellationToken);
            var fileList = _fileRegex.Matches(contents).Select(m => new { Name = m.Groups[3].Value, Time = DateTime.Parse(m.Groups[1].Value) }).ToList();

            foreach (var file in fileList)
            {
                var lastFileUpdate = await _fileStorage.GetFileTime(City, file.Name, cancellationToken);
                if (lastFileUpdate == null || lastFileUpdate.Value < file.Time)
                {
                    using (var fileStream = await httpClient.GetStreamAsync($"{_baseUrl}{file.Name}", cancellationToken))
                    {
                        await _fileStorage.StoreFile(City, file.Name, fileStream, cancellationToken);
                    }
                }
            }

            try
            {
                await _busVehicleDbBuilder.Build(VehicleType.Bus, _positionsFileBus, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load buses for Krakow! Continuing without them.");
            }

            try
            {
                await _tramVehicleDbBuilder.Build(VehicleType.Tram, _positionsFileTram, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load trams for Krakow! Continuing without them.");
            }

            List<BaseStop> busStops = new();
            List<BaseStop> tramStops = new();

            if ((await _fileStorage.GetFileTime(City, _gtfZipBus, cancellationToken)).HasValue)
                busStops = await ParseGtfsZip(_gtfZipBus, VehicleType.Bus, cancellationToken);

            if ((await _fileStorage.GetFileTime(City, _gtfZipTram, cancellationToken)).HasValue)
                tramStops = await ParseGtfsZip(_gtfZipTram, VehicleType.Tram, cancellationToken);

            var newStopGroups = busStops.Concat(tramStops).GroupBy(g => g.GroupId)
                .Select(g => new BaseStop
                {
                    GroupId = g.Key,
                    Name = g.First().Name,
                    Type = g.Aggregate(VehicleType.None, (curr, stop) => curr | stop.Type)
                }).ToDictionary(k => k.GroupId);

            var previousStopGroups = (await _dataStorage.GetAllStopGroupIds(cancellationToken)).ToHashSet();

            var toRemove = previousStopGroups.ExceptIn(newStopGroups.Keys.ToHashSet());
            var toAdd = newStopGroups.ExceptIn(previousStopGroups);

            await _dataStorage.RemoveStopGroups(toRemove, cancellationToken);
            await _dataStorage.AddStopGroups(toAdd, cancellationToken);

            await _dataStorage.MarkSyncDone(cancellationToken);
        }

        private async Task<List<BaseStop>> ParseGtfsZip(string name, VehicleType type, CancellationToken cancellationToken)
        {
            try
            {
                using (ZipFile zip = new ZipFile(await _fileStorage.LoadFile(City, name, cancellationToken)))
                {
                    var stopsZipEntry = zip.GetEntry("stops.txt");
                    var stops = new Dictionary<string, Stop>();
                    using (var stopsStream = zip.GetInputStream(stopsZipEntry))
                    using (var reader = new StreamReader(stopsStream))
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        while (await csv.ReadAsync())
                        {
                            cancellationToken.ThrowIfCancellationRequested();
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

                    var existingIds = (await _dataStorage.GetStopIdsByType(type, cancellationToken))
                        .ToHashSet();

                    var toRemove = existingIds.ExceptIn(stops.Keys.ToHashSet());
                    var toAdd = stops.ExceptIn(existingIds);

                    await _dataStorage.RemoveStops(toRemove, cancellationToken);
                    await _dataStorage.AddStops(toAdd, cancellationToken);

                    var stopGroups = stops.GroupBy(s => s.Value.GroupId)
                        .Select(g => new BaseStop { GroupId = g.Key, Name = g.First().Value.Name, Type = type });
                    return stopGroups.ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load zip file {fileName} for city {city}. Ramoving that file.", name, City);
                await _fileStorage.RemoveFile(City, name, cancellationToken);
                throw;
            }
        }

        public async Task LogSummary(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Got {stopNumber} stops in memory", await _dataStorage.CountStops(cancellationToken));
        }
    }
}