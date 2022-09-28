using CadFoundation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NonLinearBestFit.CsvManager
{
    public class GoodFitDataFrame : List<GoodFitRecord>
    {
        public DirectoryManager fileName { get; private set; }

        private GoodFitDataFrame()
        {

        }

        public static GoodFitDataFrame Create(string csvFilePath)
        {
            var lines = File.ReadAllLines(csvFilePath).ToList();
            var headerLine = lines[0].Split(",").ToList();
            GoodFitRecord.hyperbolaColumnIndex = headerLine.FindIndex(a => a.Contains("hyperbolaValue"));
            GoodFitRecord.cosineColumnIndex = headerLine.FindIndex(a => a.Contains("cosineValue"));
            GoodFitRecord.rationalColumnIndex = headerLine.FindIndex(a => a.Contains("rationalValue"));

            var newDF = new GoodFitDataFrame();
            newDF.fileName = DirectoryManager.FromPathString(csvFilePath);

            foreach(var aRow in lines.Skip(1))
            {
                newDF.Add(new GoodFitRecord(aRow));
            }

            return newDF;
        }
    }
}
