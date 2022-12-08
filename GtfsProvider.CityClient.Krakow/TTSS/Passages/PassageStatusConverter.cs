using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GtfsProvider.CityClient.Krakow.TTSS.Passages
{
    internal static class PassageStatusConverter
    {
        internal static PassageStatus Convert(string value)
        {
            switch(value)
            {
                case "PLANNED":
                    return PassageStatus.Planned;

                case "PREDICTED":
                    return PassageStatus.Predicted;

                case "STOPPING":
                    return PassageStatus.Stopping;

                case "DEPARTED":
                    return PassageStatus.Departed;
            }

            return PassageStatus.Unknown;
        }
    }
}