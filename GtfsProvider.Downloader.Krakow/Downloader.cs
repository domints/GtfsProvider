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
using GtfsProvider.Downloader.Krakow.Extensions;
using ICSharpCode.SharpZipLib.Zip;

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
        private readonly ICityStorage _dataStorage;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly VehicleDbBuilder _vehicleDbBuilder;
        private readonly Regex _fileRegex = new("<a href=\"([A-z\\.]+)\">([A-z\\.]+)<\\/a>\\s+([0-9]{2}-[A-z]{3}-[0-9]{4}\\s[0-9]{2}:[0-9]{2})", RegexOptions.Compiled | RegexOptions.Multiline);

        public Downloader(
            IFileStorage fileStorage,
            IDataStorage dataStorage,
            IHttpClientFactory httpClientFactory,
            VehicleDbBuilder vehicleDbBuilder)
        {
            _fileStorage = fileStorage;
            _dataStorage = dataStorage[City];
            _httpClientFactory = httpClientFactory;
            _vehicleDbBuilder = vehicleDbBuilder;
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

            await _vehicleDbBuilder.Build(VehicleType.Bus, _positionsFileBus);
            await _vehicleDbBuilder.Build(VehicleType.Tram, _positionsFileTram);

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

                var existingIds = (await _dataStorage.GetIdsByType(type))
                    .ToHashSet();

                var toRemove = existingIds.ExceptIn(stops.Keys.ToHashSet());
                var toAdd = stops.ExceptIn(existingIds);

                await _dataStorage.RemoveStops(toRemove);
                await _dataStorage.AddStops(toAdd);
            }
        }
    }
}