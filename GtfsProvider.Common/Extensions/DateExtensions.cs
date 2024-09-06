using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GtfsProvider.Common.Extensions
{
    public static class DateExtensions
    {
        public static DateOnly ToDateOnly(this int value)
        {
            var year = value / 10000;
            var month = value % 10000 / 100;
            var day = value % 100;

            return new DateOnly(year, month, day);
        }

        public static int ToInt(this DateOnly value)
        {
            return value.Day + value.Month * 100 + value.Year * 10000;
        }
    }
}