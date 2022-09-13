using PdfSharpCore.Drawing;
using System;
using System.Collections.Generic;
using System.Text;

// This effort, Cogo.Plotting, is suspended indefinitely pending 
// any future need to restart it, which may never happen.
namespace Cogo.Plotting.Details
{
    /// <summary>
    /// Represents a decimal number with associated length units. Decimal is used
    /// instead of float or double because precision geospatial units can be very
    /// large.
    /// </summary>
    public struct DecimalUnits
    {
        public decimal value { get; set; }
        public pUnit Units { get; set; }

        public DecimalUnits(decimal value, pUnit pageUnits = pUnit.USfoot)
        {
            this.value = value;
            Units = pageUnits;
        }

        public decimal GetValueAs(pUnit destinationUnit)
        {
            if (this.Units == destinationUnit)
                return value;

            decimal thisAsPixels = this.value * this.Units.GetAsPixels();

            return thisAsPixels / destinationUnit.GetAsPixels();
        }

        /// <summary>
        /// No side effects. This creates a new instance.
        /// </summary>
        /// <param name="distnationUnits"></param>
        /// <returns></returns>
        public DecimalUnits CastToUnits(pUnit destinationUnits)
        {
            DecimalUnits newOne = new DecimalUnits(1m, destinationUnits);
            newOne.value = this.GetValueAs(destinationUnits);

            return newOne;
        }

        public override string ToString()
        {
            return $"{value:0.0} {Units}";
        }

        public static DecimalUnits operator +(DecimalUnits a, DecimalUnits b)
        {
            DecimalUnits newOne = new DecimalUnits(1m, a.Units);
            decimal addon = b.GetValueAs(a.Units);
            newOne.value = a.value + b.value;
            return newOne;
        }

        public static XUnit MakeFromLength(double newVal, pUnit unit)
        {
            var aUnit = new DecimalUnits((decimal) newVal, unit);
            return (XUnit)aUnit.GetValueAs(pUnit.Pixel);
        }
    }
}
