using System;
using System.Collections.Generic;
using System.Text;

namespace CadFoundation
{
    public class utilFunctions
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

        /// <summary>
        /// Find the root of an equation.  That is,
        /// Given a function, at what x does the function equal a sought y?
        /// For optimization, the algorithm works best if the actual x value falls
        /// between the first guess and the second guess. Also, it has only been tested
        /// for the case where guess1 is less than guess2.
        /// </summary>
        /// <param name="guess1">First value to guess</param>
        /// <param name="guess2">Second value to guess</param>
        /// <param name="modelFunction">The function to use in seeking the root</param>
        /// <param name="yBeingSought">Optional: If not zero, what y-value to compute x for</param>
        /// <param name="tolerance">Optional: How close to yBeingSought is close enough?</param>
        /// <param name="guess1Result">Optional If known, the y-value for the x of guess1</param>
        /// <param name="guess2Result">Optional: If known, the y-value for the x of guess2</param>
        /// <returns>The x value for which the function equals yBeingSought.</returns>
        public static double givenYfindX(
            double guess1,
            double guess2,
            Func<double, double> modelFunction,
            double yBeingSought = 0.0,
            double tolerance = 0.0001,
            double? guess1Result = null,
            double? guess2Result = null
            )
        {
            if (null != guess1Result &&
                Math.Abs(yBeingSought - guess1Result.Value) < tolerance)
            {
                return guess1;
            }

            if (null != guess2Result &&
                Math.Abs(yBeingSought - guess2Result.Value) < tolerance)
            {
                return guess2;
            }


            double g1Result = 0.0;
            if (null == guess1Result)
                g1Result = modelFunction(guess1);
            else
                g1Result = guess1Result.Value;

            double g2Result = 0.0;
            if (null == guess2Result)
                g2Result = modelFunction(guess2);
            else
                g2Result = guess2Result.Value;

            double nextGuess = utilFunctions.interpolateGetX(
                x1: guess1,
                y1: g1Result,
                x2: guess2,
                y2: g2Result,
                Y: yBeingSought
                );

            double partitionFraction = (nextGuess - guess1) / (guess2 - guess1);
            if (partitionFraction <= 0.50)
                return givenYfindX(guess1, nextGuess, modelFunction, yBeingSought,
                    tolerance, g1Result, null);
            else
                return givenYfindX(nextGuess, guess2, modelFunction, yBeingSought,
                    tolerance, guess1Result: null, guess2Result: g2Result);

        }

        public static double interpolateGetY(double x1, double y1, double x2, double y2, double X)
        {
            if (x2 == x1)
                return Double.PositiveInfinity;

            double slope = (y2 - y1) / (x2 - x1);
            return y1 + slope * (X - x1);

        }

        public static double interpolateGetX(double x1, double y1, double x2, double y2, double Y)
        {
            return interpolateGetY(y1, x1, y2, x2, Y);
        }
    }

    public static class utilityExtensions
    {
        public static bool tolerantEquals(this Double first, Double second, Double toleration)
        {
            return Math.Abs(first - second) <= toleration;
        }

        public static T RetrieveByIndex<T>(this IList<T> theList, int index)
        {
            if (null == theList)
                return default;

            if (index >= theList.Count)
                return default;
            
            return theList[index];
        }
    }
}
