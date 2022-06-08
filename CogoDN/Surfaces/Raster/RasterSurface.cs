using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Surfaces.Raster
{
    public class RasterSurface
    {
        private double cellSize { get; set; }
        private int numColumns { get; set; }
        private int numRows { get; set; }
        private double leftXCoordinate { get; set; }
        private double bottomYCoordinate { get; set; }
        private string NoDataValue { get; set; }
        private double[,] rasterGrid { get; set; }

        protected RasterSurface()
        {
        }

        public RasterSurface(string PathToOpen)
        {
            using (StreamReader sr = new StreamReader(PathToOpen))
            {
                numColumns = int.Parse(sr.ReadLine().Split(" ")[1]);
                numRows = int.Parse(sr.ReadLine().Split(" ")[1]);
                leftXCoordinate = double.Parse(sr.ReadLine().Split(" ")[1]);
                bottomYCoordinate = double.Parse(sr.ReadLine().Split(" ")[1]);
                cellSize = double.Parse(sr.ReadLine().Split(" ")[1]);
                NoDataValue = sr.ReadLine().Split(" ")[1];
                rasterGrid = new double[numRows, numColumns];

                string line;
                int rowCounter = -1;
                while (true)
                {
                    line = sr.ReadLine();
                    if (line == null) break;
                    var lineList = line.Split(" ");
                    rowCounter++;
                    int columnCounter = -1;

                    foreach (var entry in lineList)
                    {
                        columnCounter++;
                        if (entry == this.NoDataValue)
                            rasterGrid[rowCounter, columnCounter] = double.NaN;
                        else
                        {
                            rasterGrid[rowCounter, columnCounter] = double.Parse(entry);
                        }
                    }
                }

            }
        }

        public void WriteToFile(string PathToWriteTo, string fileName)
        {
            var filePath = PathToWriteTo + "\\" + fileName;

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine("ncols         " + numColumns);
                writer.WriteLine("nrows         " + numRows);
                writer.WriteLine("xllcorner     " + leftXCoordinate);
                writer.WriteLine("yllcorner     " + bottomYCoordinate);
                writer.WriteLine("cellsize      " + cellSize);
                writer.WriteLine("ncols         " + NoDataValue);

                for (int currentRow = 0; currentRow < numRows; currentRow++)
                {
                    for (int currentColumn = 0; currentColumn < numColumns; currentColumn++)
                    {
                        if (this.rasterGrid[currentRow, currentColumn] == double.NaN)
                        {
                            writer.Write(NoDataValue + " ");
                        }
                        else
                        {
                            writer.Write(rasterGrid[currentRow, currentColumn] + " ");
                        }
                    }
                    writer.WriteLine("");
                }
                writer.Flush();
            }
        }

        public static RasterSurface Zeroes(
            double cellSize,
            int numColumns,
            int numRows,
            double leftXCoordinate,
            double bottomYCoordinate,
            string noDataValue = "-9999")
        {
            var newRaster = new RasterSurface();
            newRaster.cellSize = cellSize;
            newRaster.numColumns = numColumns;
            newRaster.numRows = numRows;
            newRaster.leftXCoordinate = leftXCoordinate;
            newRaster.bottomYCoordinate = bottomYCoordinate;
            newRaster.NoDataValue = noDataValue;
            newRaster.rasterGrid = new double[numRows, numColumns];

            for (int currentRow = 0; currentRow < numRows; currentRow++)
            {
                for (int currentColumn = 0; currentColumn < numColumns; currentColumn++)
                {
                    newRaster.rasterGrid[currentRow, currentColumn] = 0;
                }
            }

            return newRaster;
        }
    }

}

