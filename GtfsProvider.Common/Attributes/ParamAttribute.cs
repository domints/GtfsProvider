using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GtfsProvider.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ParamAttribute : Attribute
    {
        public string ParamName { get; }
        public ParamAttribute(string paramName)
        {
            ParamName = paramName;
        }
    }
}