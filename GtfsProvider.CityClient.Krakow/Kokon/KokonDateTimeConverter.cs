using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GtfsProvider.CityClient.Krakow.Kokon
{
    public class KokonDateTimeConverter : JsonConverter<DateTime>
    {
        public const string KokonDateTimeFormat = "yyyy-MM-dd HH:mm";
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (var jsonDoc = JsonDocument.ParseValue(ref reader))
        {
            var stringValue = jsonDoc.RootElement.GetRawText().Trim('"').Trim('\'');
            var value = DateTime.ParseExact(stringValue, KokonDateTimeFormat, null);
            return value;
        }
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(KokonDateTimeFormat, System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}