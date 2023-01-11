using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common;
using GtfsProvider.Common.Enums;
using GtfsProvider.Services.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace GtfsProvider.Services
{
    public class LocalFileStorage : IFileStorage
    {
        public LocalFileStorage(IConfiguration configuration)
        {
            BasePath = configuration["FileStoragePath"];
        }

        private readonly string BasePath;
        
        public Task<DateTime?> GetFileTime(City city, string name)
        {
            var metadata = LoadMetadata(city).FirstOrDefault(m => m.Name == name);
            if(metadata != null && File.Exists(LPath(city, name)))
                return Task.FromResult((DateTime?)metadata.LastUpdated);

            if(metadata != null)
                RemoveEntry(city, name);

            return Task.FromResult<DateTime?>(null);
        }

        public async Task StoreFile(City city, string name, Stream stream)
        {
            var time = DateTime.Now;
            using(var fileStream = File.Open(LPath(city, name), FileMode.Create))
            {
                stream.CopyTo(fileStream);
                await fileStream.FlushAsync();
            }

            AddOrUpdateEntry(city, name, time);
        }

        public Task<Stream> LoadFile(City city, string name)
        {
            return Task.FromResult((Stream)File.Open(LPath(city, name), FileMode.Open));
        }

        public Task RemoveFile(City city, string name)
        {
            var filePath = LPath(city, name);
            EnsureCityDir(city);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            RemoveEntry(city, name);

            return Task.CompletedTask;
        }

        private List<FileMetadata> LoadMetadata(City city)
        {
            var p = LPath(city, "metadata.json");
            if(!File.Exists(p))
            {
                return Enumerable.Empty<FileMetadata>().ToList();
            }

            return JsonConvert.DeserializeObject<List<FileMetadata>>(File.ReadAllText(p)) ?? Enumerable.Empty<FileMetadata>().ToList();
        }

        private void StoreMetadata(City city, List<FileMetadata> metadata)
        {
            var p = LPath(city, "metadata.json");
            File.WriteAllText(p, JsonConvert.SerializeObject(metadata));
        }

        private void RemoveEntry(City city, string name)
        {
            var metadata = LoadMetadata(city);
            var entry = metadata.Find(m => m.Name == name);
            if (entry == null)
                return;
            metadata.Remove(entry);
            StoreMetadata(city, metadata);
        }

        private void AddOrUpdateEntry(City city, string name, DateTime updateTime)
        {
            var metadata = LoadMetadata(city);
            var entry = metadata.FirstOrDefault(m => m.Name == name);
            if(entry == null)
            {
                metadata.Add(new FileMetadata { Name = name, LastUpdated = updateTime });
            }
            else
            {
                entry.LastUpdated = updateTime;
            }
            
            StoreMetadata(city, metadata);
        }

        private string LPath(City city, string fileName)
        {
            EnsureCityDir(city);
            return Path.Combine(BasePath, city.ToString(), fileName);
        }

        private void EnsureCityDir(City city)
        {
            var p = Path.Combine(BasePath, city.ToString());
            if (!Directory.Exists(p))
                Directory.CreateDirectory(p);
        }
    }
}