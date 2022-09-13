using System;
using System.Collections.Generic;
using System.Text;

// This effort, Cogo.Plotting, is suspended indefinitely pending 
// any future need to restart it, which may never happen.
namespace Cogo.Plotting.Details
{
    public static class PageUnits
    {
        public const decimal pixel = 1m;
        public const decimal inch = pixel * 72m;
        public const decimal centimeter = inch / 2.54m;
        public const decimal foot = 12m * inch;
        public const decimal meter = centimeter * 100m;
        public const decimal USfoot = meter * 1200m / 3935m;

    }

    public enum pUnit
    {
        Pixel = (int) PageUnits.pixel,
        Inch = (int) PageUnits.inch,
        Centimeter = (int) PageUnits.centimeter,
        Foot = (int) PageUnits.foot,
        Meter = (int) PageUnits.meter,
        USfoot = ((int) PageUnits.USfoot) + 1
    }

    public static class pUnitExtensions
    {
        public static string ToString(this pUnit pu)
        {
            return
            pu switch
            {
                pUnit.Pixel => "Pixels",
                pUnit.Inch => "Inches",
                pUnit.Centimeter => "Centimeters",
                pUnit.Foot => "Feet",
                pUnit.Meter => "Meters",
                pUnit.USfoot => "US Feet",
                _ => "",
            };
        }
        public static decimal GetAsPixels(this pUnit unitEnum)
        {
            return
            unitEnum switch
            {
                pUnit.Pixel => PageUnits.pixel,
                pUnit.Inch => PageUnits.inch,
                pUnit.Centimeter => PageUnits.centimeter,
                pUnit.Foot => PageUnits.foot,
                pUnit.Meter => PageUnits.meter,
                pUnit.USfoot => PageUnits.USfoot,

                _ => throw new InvalidOperationException
                    ("Unkown enum value can't be processed.")
            };
        }
    }
}
