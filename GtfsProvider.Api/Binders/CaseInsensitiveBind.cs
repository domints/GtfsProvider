using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace GtfsProvider.Api.Binders
{
    public class CaseInsensitiveBind<TEnum> where TEnum : struct, Enum
    {
        public TEnum Value { get; set; }
        public CaseInsensitiveBind(TEnum value)
        {
            Value = value;
        }

        public static implicit operator TEnum(CaseInsensitiveBind<TEnum> ci) => ci.Value;

        public static bool TryParse(string? value, IFormatProvider? _, out CaseInsensitiveBind<TEnum>? bindResult)
        {
            bindResult = null;
            if (Enum.TryParse(value, true, out TEnum result))
            {
                bindResult = new CaseInsensitiveBind<TEnum>((TEnum)result);
                return true;
            }

            return false;
        }
    }
}