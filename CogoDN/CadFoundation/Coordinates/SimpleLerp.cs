using System;
using System.Collections.Generic;
using System.Text;

namespace CadFoundation.Coordinates
{
    public class SimpleLerp
    {
        /// <summary>
        /// For a pair of points, given x, return the interpolated y.
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="givenX"></param>
        /// <returns></returns>
        public static double LERP(double x1, double y1, double x2, double y2, double givenX)
        {
            double yInterp = y1 - (x2 - givenX) * (y2 - y1) / (x2 - x1);
            return yInterp;
        }
    }
}
