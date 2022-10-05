using System;
using System.Collections.Generic;
using System.Text;

namespace NonLinearBestFit.CsvManager
{
    public class GoodFitRecord : IxyPair
    {
        public double station { get; private set; }
        public double elevation { get; private set; }
        public double vcLength { get; private set; }
        public double? hyperbolaValue { get; set; } = null;
        public double? cosineValue { get; set; } = null;
        public double? rationalValue { get; set; } = null;

        public static int hyperbolaColumnIndex { get; set; } = -1;
        public static int cosineColumnIndex { get; set; } = -1;
        public static int rationalColumnIndex { get; set; } = -1;

        public GoodFitRecord(string inString)
        {
            var asList = inString.Split(",");
            station = Convert.ToDouble(asList[0]);
            elevation = Convert.ToDouble(asList[1]);
            vcLength = Convert.ToDouble(asList[2]);
            if (hyperbolaColumnIndex > -1) hyperbolaValue = Convert.ToDouble(asList[hyperbolaColumnIndex]);
            if (cosineColumnIndex > -1) cosineValue = Convert.ToDouble(asList[cosineColumnIndex]);
            if (rationalColumnIndex > -1) rationalValue = Convert.ToDouble(asList[rationalColumnIndex]);
        }

        public static string headerRow()
        {
            var str = new StringBuilder("station,elevation,vcLength");
            if (hyperbolaColumnIndex > -1)
                str.Append(",hyperbolaValue");
            if (cosineColumnIndex > -1)
                str.Append(",cosineValue");
            if (rationalColumnIndex > -1)
                str.Append(",rationalValue");
            return str.ToString();
        }

        public override string ToString()
        {
            var str = new StringBuilder($"{station},{elevation},{vcLength}");
            if (hyperbolaColumnIndex > -1)
            {
                if (null != hyperbolaValue)
                    str.Append($",{hyperbolaValue}");
                else
                    str.Append(",");
            }

            if (cosineColumnIndex > -1)
            {
                if (null != cosineValue)
                    str.Append($",{cosineValue}");
                else
                    str.Append(",");
            }
            
            if (rationalColumnIndex > -1)
            {
                if(null != rationalValue)
                    str.Append($",{rationalValue}");
                else
                    str.Append(",");
            }
            return str.ToString();
        }

        public double getX()
        {
            return this.station;
        }

        public double getY()
        {
            return this.elevation;
        }

    }
}
