using System;
using System.Collections.Generic;
using System.Text;

namespace CadFoundation.Angles
{
    public struct DegreeOfCurve
    {
        public const double lengthBasis = 100.0;

        private readonly Double degrees_;
        public DegreeOfCurve(Double newVal)
        {
            this.degrees_ = newVal;
        }

        public Double getAsRadians()
        {
            return degrees_ * Math.PI / 180.0;
        }

        public Double getAsDouble()
        {
            return degrees_;
        }

        public static DegreeOfCurve newFromRadians(Double rad)
        {
            return new DegreeOfCurve(180.0 * rad / Math.PI);
        }

        public static DegreeOfCurve newFromDegrees(Double degreesDouble)
        {
            return new DegreeOfCurve(degreesDouble);
        }

        public static DegreeOfCurve newFromDegreesMinutesSeconds(int degrees, int minutes, double seconds)
        {
            return new DegreeOfCurve(
                  Math.Abs((double)degrees) +
                  (double)minutes / 60.0 + seconds / 3600.0
                           );
        }

        public static double AsDblFromRadius(double radius)
        {
            return 18000.0 / (Math.PI * radius);
        }

        public static double asRadiusFromDegDouble(double degrees, double basisLength = lengthBasis)
        {
            return 180 * basisLength / (Math.PI * degrees);
        }

        public static DegreeOfCurve FromRadius(double radius)
        {
            double radians = 180.0 / radius;
            var deg = newFromRadians(radians);
            return deg;
        }

        /// <summary>
        /// Arc sine.  (Also known as inverse sine.)
        /// </summary>
        /// <param name="val">Value in distance units.</param>
        /// <returns>An angle in radians.</returns>
        public static DegreeOfCurve Asin(Double val)
        {
            return newFromRadians(Math.Asin(val));
        }

        public static DegreeOfCurve Acos(Double val)
        {
            return newFromRadians(Math.Acos(val));
        }

        public static DegreeOfCurve Atan(Double val)
        {
            return newFromRadians(Math.Atan(val));
        }

        public static DegreeOfCurve Atan2(Double y, Double x)
        {
            return newFromRadians(Math.Atan2(y, x));
        }

        public static Double Sin(DegreeOfCurve deg)
        {
            return Math.Sin(deg.getAsRadians());
        }

        public static Double Cos(DegreeOfCurve deg)
        {
            return Math.Cos(deg.getAsRadians());
        }

        public static Double Tan(DegreeOfCurve deg)
        {
            return Math.Tan(deg.getAsRadians());
        }

        public static DegreeOfCurve Abs(DegreeOfCurve deg)
        {
            return Math.Abs(deg.degrees_);
        }

        public static implicit operator DegreeOfCurve(double doubleVal)
        {
            return new DegreeOfCurve(doubleVal);
        }

        public static bool operator >=(DegreeOfCurve left, DegreeOfCurve right)
        {
            return left.degrees_ >= right.degrees_;
        }

        public static bool operator <=(DegreeOfCurve left, DegreeOfCurve right)
        {
            return left.degrees_ <= right.degrees_;
        }

        public static bool operator >(DegreeOfCurve left, DegreeOfCurve right)
        {
            return left.degrees_ > right.degrees_;
        }

        public static bool operator <(DegreeOfCurve left, DegreeOfCurve right)
        {
            return left.degrees_ < right.degrees_;
        }

        public static bool operator !=(DegreeOfCurve left, DegreeOfCurve right)
        {
            return left.degrees_ != right.degrees_;
        }

        public static bool operator ==(DegreeOfCurve left, DegreeOfCurve right)
        {
            return left.degrees_ == right.degrees_;
        }

        public static DegreeOfCurve operator +(DegreeOfCurve left, Double right)
        {
            return left.degrees_ + right;
        }

        public static DegreeOfCurve operator -(DegreeOfCurve left, DegreeOfCurve right)
        {
            return left.degrees_ - right.degrees_;
        }

        public static DegreeOfCurve operator -(DegreeOfCurve left, Double right)
        {
            return left.degrees_ - right;
        }

        public static DegreeOfCurve operator -(DegreeOfCurve left, Deflection right)
        {
            return left.degrees_ - right.getAsDegrees();
        }

        public static DegreeOfCurve operator *(DegreeOfCurve left, Double right)
        {
            return left.degrees_ * right;
        }

        public static DegreeOfCurve operator /(DegreeOfCurve left, Double right)
        {
            return left.degrees_ * (1.0 / right);
        }

        public override string ToString()
        {
            return degrees_.ToString() + "°";
        }
    }

    public static class extendDoubleForPtsDegree
    {
        public static DegreeOfCurve AsPtsDegree(this Double val)
        {
            return new DegreeOfCurve(val);
        }

        public static double AsPtsDegreeDouble(this Double val)
        {
            return AsPtsDegree(val).getAsDouble();
        }

        public static double dblDegreeFromRadius(this Double inRadius)
        {
            return DegreeOfCurve.AsDblFromRadius(inRadius);
        }

        public static double RadiusFromDegreesDbl(this Double inDegree)
        {
            return DegreeOfCurve.asRadiusFromDegDouble(inDegree);
        }
    }
}
