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

        public void write()
        {
            bool hyperbolaPresent = this.Any(record => record.hyperbolaValue != null);
            bool cosinePresent = this.Any(record => record.cosineValue != null);
            bool rationalPresent = this.Any(record => record.rationalValue != null);

            StringBuilder headerString = new StringBuilder("station,elevation,vcLength");
            int idxCounter = 3;
            if (hyperbolaPresent)
            {
                headerString.Append(",hyperbolaValue");
                GoodFitRecord.hyperbolaColumnIndex = idxCounter;
                idxCounter++;
            }

            if (cosinePresent)
            {
                headerString.Append(",cosineValue");
                GoodFitRecord.cosineColumnIndex = idxCounter;
                idxCounter++;
            }

            if (rationalPresent)
            {
                headerString.Append(",rationalValue");
                GoodFitRecord.rationalColumnIndex = idxCounter;
                idxCounter++;
            }

            string newLine = Environment.NewLine;
            headerString.Append(newLine);
            List<string> allText = new List<string>();
            allText.Add(headerString.ToString());

            foreach(var row in this)
            {
                allText.Add(row.ToString() + newLine);
            }
            var asOneString = string.Join("", allText);
            File.WriteAllText(fileName.path, string.Join("", asOneString));
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
