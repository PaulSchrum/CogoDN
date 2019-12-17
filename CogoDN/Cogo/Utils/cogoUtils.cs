using System;
using System.Collections.Generic;
using System.Text;

namespace Cogo.Utils
{
    class cogoUtils
    {
        public static double addNullableDoubles(double? dbl1, double? dbl2)
        {
            if (dbl1 == null && dbl2 == null)
            {
                return 0.0;
            }
            else if (dbl1 == null)
            {
                return (double)dbl2;
            }
            else if (dbl2 == null)
            {
                return (double)dbl1;
            }
            else
            {
                return (double)(dbl1 + dbl2);
            }
        }

        public static int tolerantCompare(double value1, double value2, double tolerance)
        {
            double diff = value2 - value1;
            if (Math.Abs(diff) < tolerance)
            {
                return 0;
            }

            if (value2 > value1) return 1;

            return -1;
        }

        public static int tolerantCompare(double? value1, double? value2, double tolerance)
        {
            if (value1 == null || value2 == null) return -1;
            double diff = (double)value2 - (double)value1;
            if (Math.Abs(diff) < tolerance)
            {
                return 0;
            }

            if (value2 > value1) return 1;

            return -1;
        }

        public static double addRecipricals(double? val1, double? val2)
        {
            if (null == val1)
            {
                if (null == val2)
                    return 0.0;
                else
                    return (double)val2;
            }
            else if (null == val2)
            {
                return (double)val1;
            }
            else
            {
                if (val1 == 0.0)
                {
                    if (val2 == 0.0)
                        return Double.PositiveInfinity;
                    else
                        return (double)val2;
                }
                else if (val2 == 0.0)
                    return (double)val1;
                else
                {
                    double? recip1 = 1 / val1;
                    double? recip2 = 1 / val2;
                    return 1 / (double)(recip1 + recip2);
                }
            }
        }
    }

    public static class utilityExtensions
    {
        public static bool tolerantEquals(this Double first, Double second, Double toleration)
        {
            return Math.Abs(first - second) <= toleration;
        }
    }
    public class Double_
    {
        public static Double OneTrillion
        { get { return 1000000000000.0000; } }
    }
}
