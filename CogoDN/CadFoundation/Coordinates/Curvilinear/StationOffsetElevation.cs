using System;
using System.Collections.Generic;
using System.Text;

namespace CadFoundation.Coordinates.Curvilinear
{
    public class StationOffsetElevation : IComparable
    {
        //private StationOffsetElevation soePoint;

        public StationOffsetElevation() { offset = new Offset(0.0); elevation = new Elevation(0.0); }
        public StationOffsetElevation(double aStation, Offset anOffset, Elevation anElevation)
        {
            station = aStation; offset = anOffset; elevation = anElevation;
        }

        public StationOffsetElevation(double aStation, double anOffset, double anElevation = 0.0) :
            this(aStation, new Offset(anOffset), new Elevation(anElevation))
        {

        }

        public StationOffsetElevation(StationOffsetElevation soeOther)
        {
            station = soeOther.station;
            offset = soeOther.offset;
            elevation = soeOther.elevation;
        }

        public double station { get; set; }
        public Offset offset { get; set; }
        public Elevation elevation { get; set; }

        /// <summary>
        /// Has the effect of overriding assignment equals.
        /// </summary>
        /// <param name="sta"></param>
        public static implicit operator StationOffsetElevation(double sta)
        {
            return new StationOffsetElevation(sta, 0.0, 0.0);
        }

        public static bool operator <=(StationOffsetElevation first, StationOffsetElevation second)
        {
            return first.station <= second.station;
        }

        public static bool operator >=(StationOffsetElevation first, StationOffsetElevation second)
        {
            return first.station >= second.station;
        }

        public static Vector operator -(StationOffsetElevation first, StationOffsetElevation second)
        {
            var dist = first.station - second.station;
            var elChange = first.elevation - second.elevation;
            return new Vector(dist, elChange);
        }

        public override string ToString()
        {
            return station.ToString() + " " + offset.ToString() + "  (EL: " + elevation.ToString() + ")";
        }

        public int CompareTo(object obj)
        {
            var other = obj as StationOffsetElevation;
            return utilFunctions.tolerantCompare(this.station, other.station, tolerance: 0.00005);
        }

        public static int Subtract(StationOffsetElevation left, StationOffsetElevation right)
        {
            throw new NotImplementedException();
        }
    }

    public static class extendDoubleForSOE
    {
        /// <summary>
        /// returns doubleValue <= station
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static bool LTE(this Double val, StationOffsetElevation aStation)
        {
            return val <= aStation.station;
        }
    }
}
