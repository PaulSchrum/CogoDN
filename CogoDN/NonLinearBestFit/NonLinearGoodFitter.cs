using CadFoundation.Coordinates;
using NonLinearBestFit.CsvManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        /// Collection of the data, x and y.
        /// </summary>
        public IEnumerable<IxyPair> dataset { get; private set; } = null;

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
        /// error tolerance
        /// </summary>
        public double errorTolerance { get; set; } = 0.1;

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
        /// 
        /// </summary>
        /// <param name="fitFunction">Function to be fit.</param>
        /// <param name="baselineData">Field0 is x; Field1 is y.</param>
        /// <param name="p1Guess">Seed value for parameter 1.</param>
        /// <param name="p2Guess">Seed value for parameter 2.</param>
        /// <param name="errTolerance"></param>
        public NonLinearGoodFitter(Func<double, double, double, double, double> fitFunction,
            IEnumerable<IxyPair> baselineData,
            double p1Guess = 1.0,
            double p2Guess = 1.0,
            double errTolerance = 0.1)
        {
            func = fitFunction;
            dataset = baselineData;
            param1Guess = p1Guess;
            param2Guess = p2Guess;
            errorTolerance = errTolerance;
        }

        List<Point> points = new List<Point>();
        StreamWriter writer = null;

        /// <summary>
        /// Find the two parameter values which result in the lowest 
        /// </summary>
        /// <param name="zTolerance"></param>
        /// <param name="param1Start"></param>
        /// <param name="param2Start"></param>
        /// <returns></returns>
        public GoodFitParameters solve()
        {
            GoodFitParameters returnValue = new GoodFitParameters();

            int depth = 0;
            double reportedError = errorTolerance * 1000d;
            double p1Guess = param1Guess;
            double p2Guess = param2Guess;
            Dictionary<int, double> depthVsError = new Dictionary<int, double>();  // diagnostic only

            //using (writer = new StreamWriter(@"E:\Research\Scratch\surface.csv"))
            //{
            while (reportedError > errorTolerance)
            {
                returnValue = solveOneIteration(p1Guess, p2Guess, depth);
                p1Guess = returnValue.parameter1;
                p2Guess = returnValue.parameter2;
                reportedError = returnValue.averageError;
                depthVsError[depth] = reportedError;
                if (depth >= maxDepth)
                    break;
                depth++;
            }
            //}

            return returnValue;
        }

        private class paramsAndError
        {
            public paramsAndError(double d1, double d2, double err)
            {
                param1 = d1;
                param2 = d2;
                error = err;
            }
            public double param1 { get; set; }
            public double param2 { get; set; }
            public double error { get; set; }

            public double[] ToArray()
            {
                return new double[3] { param1, param2, error };
            }

            public override string ToString()
            {
                return $"{param1:0.####},{param2:0.####},{error:0.####}";
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

        private GoodFitParameters solveOneIteration(
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

        public double computeAverageError(double zeroValue, double p1, double p2, IEnumerable<IxyPair> dataset)
        {
            double xPrev = 0d, xDiff = 0d, yDiffPrev = 0d;
            double yDiff = 0d, aggregateArea = 0d, x = 0d, y = 0d;
            double trapezoidArea, aggregateDistance=0d;
            List<temp> records = new List<temp>();
            foreach (var entry in dataset)
            {
                x = entry.getX();
                y = entry.getY();
                double fx = func(zeroValue, p1, p2, x);
                xDiff = x - xPrev;
                yDiff = fx - y;
                trapezoidArea = xDiff * ((yDiff + yDiffPrev) / 2d);
                aggregateArea += trapezoidArea;
                aggregateDistance += xDiff;
                xPrev = x;
                yDiffPrev = yDiff;
                records.Add(new temp(x, y, fx, yDiff));
            }
            double averageError = aggregateArea / aggregateDistance;
            return averageError;
        }

    }

    public class GoodFitParameters
    {
        public double parameter1 { get; set; }
        public double parameter2 { get; set; }
        public double averageError { get; set; }
        public double? widthExtent { get; set; }
    }
}
