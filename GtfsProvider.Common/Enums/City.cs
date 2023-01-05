using System.ComponentModel.DataAnnotations;
using GtfsProvider.Common.Attributes;

namespace GtfsProvider.Common.Enums
{
    public enum City
    {
        [Ignore]
        Default,

        [Display(Name = "Kraków")]
        Krakow,

        [Ignore]
        [Display(Name = "Katowice")]
        Katowice,
        
        [Display(Name = "Wrocław")]
        Wroclaw
    }
}