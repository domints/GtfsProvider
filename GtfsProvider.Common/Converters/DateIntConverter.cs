using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using GtfsProvider.Common.Extensions;

namespace GtfsProvider.Common.Converters
{
    public class DateIntConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            var intValue = int.Parse(text);
            return intValue.ToDateOnly();
        }
    }
}