using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Enums;

namespace GtfsProvider.Downloader.Krakow.Kokon
{
    public class KokonVehicle
    {
        private static char[] BusDepos = new[] { 'B', 'P', 'D' };
        private static char[] TramDepos = new[] { 'H', 'R' };
        public KokonVehicle(char depotMarking, char typeMarking, int vehicleNo)
        {
            if (BusDepos.Contains(depotMarking))
                Type = VehicleType.Bus;
            if (TramDepos.Contains(depotMarking))
                Type = VehicleType.Tram;
            DepotMarking = depotMarking;
            VehicleModelMarking = typeMarking;
            VehicleNo = vehicleNo;
        }

        public char DepotMarking { get; set; }
        public char VehicleModelMarking { get; set; }
        public int VehicleNo { get; set; }
        public VehicleType Type { get; set; }

        public override string ToString()
        {
            return $"{DepotMarking}{VehicleModelMarking}{VehicleNo:D3}";
        }

        public static KokonVehicle FromSideNo(string sideNo)
        {
            var span = sideNo.AsSpan();
            var vehNo = int.Parse(span[2..5]);
            return new(span[0], span[1], vehNo);
        }
    }
}