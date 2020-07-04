using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Surfaces.TIN
{
    internal struct bin
    {
        public int itemCount;
        public double binMax;
        public double binMin;
    };


    public class DistributionPlotter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="outputFile"></param>
        /// <param name="binCount"></param>
        /// <param name="collection">Must be already sorted.</param>
        public static void writeProbabilityCsv(IEnumerable<double> collection,
            int binCount, string outputFile=null)
        {
            if (null == collection) 
                return;
            
            string outFile = outputFile;
            if(null == outFile)
            {
                outFile = @"C:\Temp\data\" + DateTime.Now.ToString("yyyyMMddHHmmss"
                    + @".csv");
            }

            var min = collection.First();
            double max = collection.Last() + 0.00001;
            double range = max - min;
            double binWidth = range / (binCount - 1);
            
            var binCounts = new ConcurrentDictionary<int, int>();
            for (int i = 0; i < binCount; i++)
                binCounts[i] = 0;
            
            foreach(var item in collection)
            {
                int idx = Convert.ToInt32(Math.Floor((item - min) / binWidth));
                binCounts[idx]++;
            }

            var binMax = min;
            StringBuilder sb = new StringBuilder();
            foreach(var index in binCounts.Keys.OrderBy(x => x))
            {
                binMax += binWidth;
                sb.Append($"{binMax:F4},");
                sb.Append($"{binCounts[index]}"+Environment.NewLine);
            }
            File.WriteAllText(outFile, sb.ToString());

        }


    }
}
