using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GtfsProvider.Api.Binders
{
    /// <summary>
    /// Wrapper over List to add binder that'll separate elements from param string
    /// </summary>
    /// <typeparam name="T">Type you want inside. Pls make sure it's convertable from String...</typeparam>
    public class CommaSeparated<T> : List<T>
    {
        public CommaSeparated() : base()
        {
        }

        public CommaSeparated(int capacity) : base(capacity)
        {
        }

        public static bool TryParse(string? value, IFormatProvider? _, out CommaSeparated<T>? bindResult)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                bindResult = null;
                return false;
            }

            var values = value.Split(",");
            bindResult = new CommaSeparated<T>(values.Length);

            try
            {
                foreach (var v in values)
                {
                    bindResult.Add((T)Convert.ChangeType(v, typeof(T)));
                }

                return true;
            }
            catch
            {
                bindResult = null;
                return false;
            }
        }
    }
}