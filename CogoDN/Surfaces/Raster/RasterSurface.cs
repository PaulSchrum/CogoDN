using CadFoundation.Coordinates;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Surfaces.Raster
{
    public class RasterSurface
    {
        public double cellSize { get; protected set; }
        public int numColumns { get; protected set; }
        public int numRows { get; protected set; }
        public double leftXCoordinate { get; protected set; }
        public double bottomYCoordinate { get; protected set; }
        public double topYCoordinate { get; protected set; }
        public Point anchorPoint { get; protected set; } // upper left point of the raster
        public string NoDataValue { get; protected set; }
        public double[,] cellArray { get; protected set; }

        protected RasterSurface()
        {
        }

        public RasterSurface(string PathToOpen)
        {
            using (StreamReader sr = new StreamReader(PathToOpen))
            {
                int rowCount = 0;
                while(rowCount < 6)
                {
                    var lineArray = sr.ReadLine().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    switch(lineArray[0])
                    {
                        case "ncols":
                        {
                            numColumns = Convert.ToInt32(lineArray[1]);
                            rowCount++;
                            break;
                        }
                        case "nrows":
                        {
                            numRows = Convert.ToInt32(lineArray[1]);
                            rowCount++;
                            break;
                        }
                        case "xllcorner":
                        {
                            leftXCoordinate = Convert.ToDouble(lineArray[1]);
                            rowCount++;
                            break;
                        }
                        case "yllcorner":
                        {
                            bottomYCoordinate = Convert.ToDouble(lineArray[1]);
                            rowCount++;
                            break;
                        }
                        case "cellsize":
                        {
                            cellSize = Convert.ToDouble(lineArray[1]);
                            rowCount++;
                            break;
                        }
                        case "NODATA_value":
                        {
                            NoDataValue = lineArray[1];
                            rowCount++;
                            break;
                        }
                        default:
                            break;
                    }
                }

                topYCoordinate = bottomYCoordinate + cellSize * numRows;
                anchorPoint = new Point(leftXCoordinate, topYCoordinate);
                cellArray = new double[numColumns, numRows];

                string line;
                int rowCounter = -1;
                while (true)
                {
                    line = sr.ReadLine();
                    if (line == null) break;
                    var lineList = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    rowCounter++;
                    int columnCounter = -1;

                    foreach (var entry in lineList)
                    {
                        columnCounter++;
                        if (entry == this.NoDataValue)
                            cellArray[columnCounter, rowCounter] = double.NaN;
                        else
                        {
                            cellArray[columnCounter, rowCounter] = double.Parse(entry);
                        }
                    }
                }

            }
        }

        public Point GetCenterPoint(int xIndex, int yIndex)
        {
            double x = cellSize * xIndex + anchorPoint.x + cellSize / 2.0;
            double y = (anchorPoint.y - cellSize / 2.0) - (cellSize * yIndex);
            //int localIdx = xIndex + (yIndex * numColumns);
            double z = cellArray[xIndex, yIndex];

            return new Point(x, y, z);
        }

        public IReadOnlyCollection<Point> CellsAsPoints()
        {
            var returnCollection = new ConcurrentBag<Point>();
            Parallel.For(0, numRows, rowIdx =>
            {
                double y = (anchorPoint.y - cellSize / 2.0) - (cellSize * rowIdx);
                for (int colIdx = 0; colIdx < numColumns; colIdx++)
                {
                    double x = cellSize * colIdx + anchorPoint.x + cellSize / 2.0;
                    double z = cellArray[colIdx, rowIdx];
                    var thePoint = new Point(x, y, z);
                    returnCollection.Add(thePoint);
                }
            } );

            return returnCollection;
        }
    

        public void WriteToFile(string PathToWriteTo, string fileName)
        {
            var filePath = PathToWriteTo + "\\" + fileName;
            WriteToFile(filePath);
        }

        public void WriteToFile(string filePath)
        { 
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
                        if (this.cellArray[currentRow, currentColumn] == double.NaN)
                        {
                            writer.Write(NoDataValue + " ");
                        }
                        else
                        {
                            string outValue = $"{cellArray[currentRow, currentColumn]:0.###} ";
                            writer.Write(outValue);
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
            newRaster.cellArray = new double[numRows, numColumns];

            for (int currentRow = 0; currentRow < numRows; currentRow++)
            {
                for (int currentColumn = 0; currentColumn < numColumns; currentColumn++)
                {
                    newRaster.cellArray[currentRow, currentColumn] = 0;
                }
            }

            return newRaster;
        }

        public static double peakCellSize(string PathToOpen)
        {
            double cellSize = 0d;
            using (StreamReader sr = new StreamReader(PathToOpen))
            {
                int rowCount = 0;
                while (rowCount < 6)
                {
                    var lineArray = sr.ReadLine().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    switch (lineArray[0])
                    {
                        case "cellsize":
                            {
                                cellSize = Convert.ToDouble(lineArray[1]);
                                rowCount++;
                                break;
                            }
                        default:
                            rowCount++;
                            break;
                    }
                }

            }

            return cellSize;
        }
        public static BoundingBox GetRasterBoundingBox(string PathToOpen)
        {
            int numColumns = 0;
            int numRows = 0;
            double leftXCoordinate = 0d;
            double bottomYCoordinate = 0d;
            double cellSize = 0d;
            string NoDataValue = String.Empty;

            using (StreamReader sr = new StreamReader(PathToOpen))
            {
                int rowCount = 0;
                while (rowCount < 6)
                {
                    var lineArray = sr.ReadLine().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    switch (lineArray[0])
                    {
                        case "ncols":
                            {
                                numColumns = Convert.ToInt32(lineArray[1]);
                                rowCount++;
                                break;
                            }
                        case "nrows":
                            {
                                numRows = Convert.ToInt32(lineArray[1]);
                                rowCount++;
                                break;
                            }
                        case "xllcorner":
                            {
                                leftXCoordinate = Convert.ToDouble(lineArray[1]);
                                rowCount++;
                                break;
                            }
                        case "yllcorner":
                            {
                                bottomYCoordinate = Convert.ToDouble(lineArray[1]);
                                rowCount++;
                                break;
                            }
                        case "cellsize":
                            {
                                cellSize = Convert.ToDouble(lineArray[1]);
                                rowCount++;
                                break;
                            }
                        case "NODATA_value":
                            {
                                NoDataValue = lineArray[1];
                                rowCount++;
                                break;
                            }
                        default:
                            break;
                    }
                }

                double topYCoordinate = bottomYCoordinate + cellSize * numRows;
                double rightXCoordiante = leftXCoordinate + cellSize * numColumns;

                // contract each by half-cell size
                double halfCell = cellSize / 2d;
                leftXCoordinate += halfCell;
                rightXCoordiante -= halfCell;
                bottomYCoordinate += halfCell;
                topYCoordinate -= halfCell;

                return new BoundingBox(
                    leftXCoordinate, bottomYCoordinate, rightXCoordiante, topYCoordinate);
            }
        }
    }

}

