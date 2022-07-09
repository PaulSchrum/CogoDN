using CadFoundation.Coordinates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NonLinearBestFit
{
    public class NonLinearGoodFitter
    {
        public Func<double, double, double, double> func { get; private set; } = null;
        public IEnumerable<double[]> dataset { get; private set; } = null;
        public double param1Guess { get; set; } = 1.0;
        public double param2Guess { get; set; } = 1.0;
        public double errorTolerance { get; set; } = 0.1;
        public double maxDepth { get; set; } = 5;
        public int param1Partitions { get; set; } = 200;
        public double param1PercentRange { get; set; } = 0.20;
        public int param2Partitions { get; set; } = 200;
        public double param2PercentRange { get; set; } = 0.20;


        public NonLinearGoodFitter(Func<double, double, double, double> fitFunction,
            IEnumerable<double[]> baselineData,
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
        public double[] solve()
        {
            double[] returnValue = new double[3];

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
                p1Guess = returnValue[0];
                p2Guess = returnValue[1];
                reportedError = returnValue[2];
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

        private double[] solveOneIteration(
            double param1Start,
            double param2Start,
            int depth)
        {
            double[] returnValue = new double[3];
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
                    double averageError = computeAverageError(p1, p2, dataset);
                    allValues.Add(new paramsAndError(p1, p2, Math.Abs(averageError)));
                }
            }
            if(null != writer)
            {
                foreach (var row in allValues)
                {
                    writer.WriteLine(row.ToString());
                }
            }

            return allValues.OrderBy(row => row.error)
                .FirstOrDefault().ToArray();
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

        public double computeAverageError(double p1, double p2, IEnumerable<double[]> dataset)
        {
            double xPrev = 0d, xDiff = 0d, yDiffPrev = 0d;
            double yDiff = 0d, aggregateArea = 0d, x = 0d, y = 0d;
            double trapezoidArea, aggregateDistance=0d;
            List<temp> records = new List<temp>();
            foreach (var entry in dataset)
            {
                x = entry[0];
                y = entry[1];
                double fx = func(p1, p2, x);
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
}
