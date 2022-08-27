using System;
using System.Collections.Generic;
using System.Text;

namespace Cogo.Plotting.Details
{
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
    }
}
