using CadFoundation.Coordinates;
using NonLinearBestFit.CsvManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NonLinearBestFit
{
    public class NonLinearGoodFitter
    {
        /// <summary>
        /// The function to use to attempt a good fit to the data.
        /// double #1: y-value at x=0
        /// double #2: param1.  (a for hyperbolas)
        /// double #3: param2.  (Asymptotic slope for hyperbola)
        /// double #4: x-value.
        /// double #5: return value, i.e., the value of the function at x.
        /// </summary>
        public Func<double, double, double, double, double> func { get; private set; } = null;

        /// <summary>
        /// Collection of the x-values of the data to be fit.
        /// </summary>
        public IReadOnlyList<double> datasetX { get; private set; } = null;

        /// <summary>
        /// Collection of the x-values of the data to be fit.
        /// </summary>
        public IReadOnlyList<double> datasetY { get; private set; } = null;

        /// <summary>
        /// Starting guess for param1 when finding a good fit.
        /// </summary>
        public double param1Guess { get; set; } = 1.0;

        /// <summary>
        /// After calling Solve(), the resulting good-fit param1 stored in this variable.
        /// </summary>
        public double? param1Result { get; private set; } = 0.0;

        /// <summary>
        /// Starting guess for param2 when finding a good fit.
        /// </summary>
        public double param2Guess { get; set; } = 1.0;

        /// <summary>
        /// After calling Solve(), the resulting good-fit param2 stored in this variable.
        /// </summary>
        public double? param2Result { get; private set; } = 0.0;

        /// <summary>
        /// maximum depth
        /// </summary>
        public double maxDepth { get; set; } = 5;

        /// <summary>
        /// When computing the parameter space, this controls how many param1 samples are used
        /// </summary>
        public int param1Partitions { get; set; } = 200;

        /// <summary>
        /// Percent plus and minus around the initial guess to compute the parameter surface
        /// oriented around param1.
        /// </summary>
        public double param1PercentRange { get; set; } = 0.20;

        /// <summary>
        /// When computing the parameter space, this controls how many param2 samples are used
        /// </summary>
        public int param2Partitions { get; set; } = 200;


        /// <summary>
        /// Percent plus and minus around the initial guess to compute the parameter surface
        /// oriented around param2.
        /// </summary>
        public double param2PercentRange { get; set; } = 0.20;

        /// <summary>
        /// First guess at what the effective width of the non-linear path should be.
        /// </summary>
        public double guessMaxWidth { get; set; }

        public double widthMin { get; set; } = 10.0;

        public string profileName { get; set; } = "";


        /// <summary>
        /// Constructor for a good fit instance. After constructing, call Solve().
        /// </summary>
        /// <param name="fitFunction">Function to be fit.</param>
        /// <param name="baselineDataX">x-values of the </param>
        /// <param name="baselineDataY">y-values of the </param>
        /// <param name="p1Guess">Seed value for parameter 1. For the Hyperbola equation, this is a</param>
        /// <param name="p2Guess">Seed value for parameter 2. For the Hyperbola equation, this is Sa</param>
        /// <param name="errTolerance"></param>
        public NonLinearGoodFitter(Func<double, double, double, double, double> fitFunction,
            IReadOnlyList<double> baselineDataX,
            IReadOnlyList<double> baselineDataY,
            double p1Guess = 1.0,
            double p2Guess = 1.0,
            double effectiveWidthGuess = 1.0)
        {
            func = fitFunction;
            datasetX = baselineDataX;
            datasetY = baselineDataY;
            param1Guess = p1Guess;
            param2Guess = p2Guess;
            guessMaxWidth = effectiveWidthGuess;
        }

        private class bestValues
        {
            public double param1;
            public double param2;
            public double width;
            public double averageError;
            public double dilatedCummulativeError;
        }

        private class accumulatorRecord
        {
            public double station;
            public double elevation;
            public double functionElevation;
            public double verticalAbsError;
            public double distancePriorStation;
            public double accumulatedDistance;
            public double errorArea;
            public double cummulativeError;
            public double dilatedCummulativeError;

            public override string ToString()
            {
                return $"{station:0.00}  {elevation:0.00}  {functionElevation:0.00}  e:{verticalAbsError:0.00#}  d:{dilatedCummulativeError:0.00##}";
            }
        }

        List<Point> points = new List<Point>();
        StreamWriter writer = null;

        public GoodFitParameters solve(double minWidthPercent)
        {
            GoodFitParameters returnValue = null;
            double p1Guess = param1Guess;
            double p2Guess = param2Guess;
            double baseElevation = datasetY[0];
            double startStation = datasetX[0];

            var allCominations = new List<bestValues>();
            var param1ValueList = getValuesOverRange(p1Guess, 40, param1PercentRange);
            // param1ValueList.Insert(0, 86.0);
            foreach (double param1 in param1ValueList)
            //Parallel.ForEach(param1ValueList, param1 =>
            {
                var param2ValueList = getValuesOverRange(p2Guess, param2Partitions, param2PercentRange);
                // param2ValueList.Insert(0, 0.56);
                foreach (double param2 in param2ValueList)
                {
                    // populate accumlating list with computed values
                    var accumulatingList = new List<accumulatorRecord>();

                    for (int idx = 0; idx < datasetX.Count; idx++)
                    {
                        double station = datasetX[idx];
                        double elevation = datasetY[idx];
                        double functionElevation = func(baseElevation, param1, param2, station);
                        double absVerticalError = Math.Abs(functionElevation - elevation);
                        accumulatingList.Add(new accumulatorRecord
                        {
                            station = station,
                            elevation = elevation,
                            functionElevation = functionElevation,
                            verticalAbsError = absVerticalError
                        });
                    }

                    double priorX = startStation; double priorY = baseElevation;
                    double priorError = accumulatingList[0].functionElevation - priorY;
                    double minDCE = Double.PositiveInfinity; double stationOfMindDCE = 0;

                    // process accumulated values to find lowest Dilated Cummulative Error.
                    for (int idx = 1; idx < datasetX.Count; idx++)
                    {
                        var aRecord = accumulatingList[idx];
                        var priorRecord = accumulatingList[idx - 1];
                        double elevError = aRecord.verticalAbsError;
                        aRecord.distancePriorStation = aRecord.station - priorRecord.station;
                        aRecord.accumulatedDistance = aRecord.station - startStation;
                        aRecord.errorArea = aRecord.distancePriorStation * (elevError + priorRecord.verticalAbsError) / 2; // yes this is naive
                        aRecord.cummulativeError = priorRecord.cummulativeError + aRecord.errorArea;
                        aRecord.dilatedCummulativeError =
                            aRecord.cummulativeError * Math.Pow(aRecord.accumulatedDistance, -1.7);
                    }

                    var best = accumulatingList
                    .Where(r => null != r)
                        .Where(r => r.station > param1 * minWidthPercent)
                        .OrderBy(r => r.dilatedCummulativeError).First();

                    var bestVal = new bestValues
                    {
                        param1 = param1,
                        param2 = param2,
                        width = best.station,
                        averageError = best.cummulativeError / best.accumulatedDistance,
                        dilatedCummulativeError = best.dilatedCummulativeError
                    };
                    allCominations.Add(bestVal);

                }
            }
            //);

            var bestCombination = allCominations.OrderBy(rec => rec.dilatedCummulativeError).First();
            returnValue = new GoodFitParameters
            {
                parameter1 = bestCombination.param1,
                parameter2 = bestCombination.param2,
                widthExtent = bestCombination.width,
                averageError = bestCombination.averageError,
                dilatedCummulativeError = bestCombination.dilatedCummulativeError
            };

            return returnValue;
        }

        private class paramsAndError
        {
            public paramsAndError(double d1, double d2, double width, double err)
            {
                param1 = d1;
                param2 = d2;
                xWidth = width;
                error = err;
                errorOverD = err / xWidth;
            }
            public double param1 { get; set; }
            public double param2 { get; set; }
            public double xWidth { get; set; }
            public double error { get; set; }
            public double errorOverD { get; set; }

            public double[] ToArray()
            {
                return new double[5] { param1, param2, error, xWidth, errorOverD };
            }

            public override string ToString()
            {
                return $"{param1:0.###},  {param2:0.####},  Width: {xWidth:0.0},   {error:0.####},  {errorOverD:0.####}";
            }
        }

        public List<double> getValuesOverRange(double midValue, int partitions, double plusMinusPercentRange)
        {
            double lowValue = midValue * (1d - plusMinusPercentRange);
            double highValue = midValue * (1d + plusMinusPercentRange);
            double increment = (highValue - lowValue) / (double)partitions;

            List<double> returnList = new List<double>();

            for (int i = 0; i < partitions; i++)
                returnList.Add(lowValue + (double)i * increment);

            return returnList;
        }

        public List<double> getValuesOverRange(double start, double end, int partitions)
        {
            double length = end - start;
            double increment = length / partitions;

            var returnList = Enumerable.Range(0, partitions).Select(p => start + p * increment).ToList();
            returnList.Add(end);
            return returnList;
        }

        public List<double> getValuesOverRange(List<double> rangeStartEnd, int partitions)
        {
            return getValuesOverRange(rangeStartEnd.First(), rangeStartEnd.Last(), partitions);
        }

        private GoodFitParameters solveAnIteration(
            List<double> param1Values,
            List<double> param2Values,
            double zeroValue
            )
        {
            GoodFitParameters returnValue = new GoodFitParameters();
            List<paramsAndError> allValues = new List<paramsAndError>();
            var param1RangeOfValues = getValuesOverRange(param1Values, param1Partitions);
            var param2RangeOfValues = getValuesOverRange(param2Values, param2Partitions);
            if (profileName.Contains("V1A"))
            {
                int stopHere = 44;
            }

            foreach (var p1 in param1RangeOfValues)
            {
                if(p1 > 48)
                {
                    int stopHeretoo = 444;
                }
                var widthRange = getValuesOverRange(p1, datasetX.Last(), 10);
                foreach (var p2 in param2Values)
                {
                    foreach(var width in widthRange)
                    {
                        double averageError = computeAverageError(zeroValue,
                            p1, p2,
                            datasetX, datasetY, guessMaxWidth);
                        allValues.Add(new paramsAndError(p1, p2, width, Math.Abs(averageError)));
                    }
                }
            }
            //if (null != writer)
            //{
            //    foreach (var row in allValues)
            //    {
            //        writer.WriteLine(row.ToString());
            //    }
            //}

            var someGoodEns = allValues.OrderBy(row => row.errorOverD).Take(33).ToList();
            var bestOne = allValues.OrderBy(row => row.error).FirstOrDefault();
            returnValue.parameter1 = bestOne.param1;
            returnValue.parameter2 = bestOne.param2;
            returnValue.averageError = bestOne.error;
            return returnValue;
        }

        private GoodFitParameters solveOneIteration(  // start here, but rework the approach.
            double param1Start,
            double param2Start,
            int depth)
        {
            GoodFitParameters returnValue = new GoodFitParameters();
            List<paramsAndError> allValues = new List<paramsAndError>();
            double percentRange = param1PercentRange / Math.Pow(10d, depth);
            var p1Values = getValuesOverRange(param1Start, param1Partitions, percentRange);
            percentRange = param2PercentRange / Math.Pow(10d, depth);
            var p2Values = getValuesOverRange(param2Start, param2Partitions, percentRange);

            int counter = 0;
            foreach (var p1 in p1Values)
            {
                foreach(var p2 in p2Values)
                {
                    counter++;
                    if (counter >= 209 && counter <= 214)
                    {
                        int z = 99;
                    }
                    //double averageError = computeAverageError(whatDoIPutHere, p1, p2, dataset);
                    //allValues.Add(new paramsAndError(p1, p2, Math.Abs(averageError)));
                }
            }
            if(null != writer)
            {
                foreach (var row in allValues)
                {
                    writer.WriteLine(row.ToString());
                }
            }

            var bestOne = allValues.OrderBy(row => row.error).FirstOrDefault();
            returnValue.parameter1 = bestOne.param1;
            returnValue.parameter2 = bestOne.param2;
            returnValue.averageError = bestOne.error;
            return returnValue;
        }

        class temp
        {
            public double x, y, yF, yDiff;
            public temp(double x_, double y_, double yF_, double yDiff_)
            {
                x = x_; y = y_; yF = yF_; yDiff = yDiff_;
            }

            public override string ToString()
            {
                return $"{x: 0.0#},{y:0.0##},{yF:0.0##},{yDiff:0.0##}";
            }
        }

        public double computeAverageError(
            double zeroValue, 
            double p1, 
            double p2, 
            IReadOnlyList<double> xValues,
            IReadOnlyList<double> yValues,
            double maxXdistance
            //out List<double> computedValues
            )
        {
            var computedValues = new List<double>();
            double xPrev = 0d, xDiff = 0d, yDiffPrev = 0d;
            double yDiff = 0d, aggregateArea = 0d;
            double trapezoidArea, aggregateDistance=0d;
            //List<temp> records = new List<temp>();
            foreach (var entry in xValues.Zip(yValues, (xVal, yVal) => new { x = xVal, y = yVal }) )
            {
                if (entry.x > maxXdistance) break;
                double computedY = func(zeroValue, p1, p2, entry.x);
                computedValues.Add(computedY);
                xDiff = entry.x - xPrev;
                yDiff = computedY - entry.y;
                trapezoidArea = xDiff * ((yDiff + yDiffPrev) / 2d);
                aggregateArea += trapezoidArea;
                aggregateDistance += xDiff;
                xPrev = entry.x;
                yDiffPrev = yDiff;
                //records.Add(new temp(x, y, computedY, yDiff));
            }
            double averageError = aggregateArea / aggregateDistance;
            return averageError;
        }

        public static string GetStringForTextFile_Hyperbola(double aRt, double SaRt, double widthRt,
            double aLt, double SaLt, double widthLt)
        {
            StringBuilder retStr = new StringBuilder();
            retStr.Append($"aRt={aRt:0.##}\n");
            retStr.Append($"SaRt={SaRt*100.0:0.00}%\n");
            retStr.Append($"widthRt={widthRt:0.0}\n");
            retStr.Append($"aLt={aLt:0.##}\n");
            retStr.Append($"SaLt={SaLt * 100.0:0.00}%\n");
            retStr.Append($"widthLt={widthLt:0.0}\n");
            return retStr.ToString();
        }

    }

    public class GoodFitParameters
    {
        public double parameter1 { get; set; }
        public double parameter2 { get; set; }
        public double averageError { get; set; }  // Total average error. Lower is better.
        public double widthExtent { get; set; }   // Width of the current measured patch. Higher is better.
        public double errorPerDistance { get; set; }  // average error over width. Lower is better.
        public double dilatedCummulativeError { get; set; }  // Lower is better.

        public string param1Name { get; set; }
        public string param2Name { get; set; }

        public static void ToFile(string filename, GoodFitParameters leftParams, GoodFitParameters rightParams, 
            string curveType = null)
        {
            // todo: make this method save a txt file to store the good fit parameters.
        }
    }
}
