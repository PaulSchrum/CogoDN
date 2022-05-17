using CadFoundation.Coordinates;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using gdal = OSGeo.GDAL.Gdal;


namespace Surfaces.Raster
{
    public class Raster
    {
        int xColumns = 0;
        int yRows= 0;
        int cellCount = 0;
        Point anchorPt;
        double cellXsize = 0d;
        double cellYsize = 0d;
        double[] cellArray;

        public Raster()
        {

        }

        public Raster(string FilePathToOpen)
        {
            GdalConfiguration_.ConfigureGdal();
            gdal.UseExceptions();
            using (var gDataset = gdal.Open(FilePathToOpen, OSGeo.GDAL.Access.GA_ReadOnly))
            {
                xColumns = gDataset.RasterXSize;
                yRows = gDataset.RasterYSize;
                cellCount = xColumns * yRows;
                double[] geotransform = new double[6];
                gDataset.GetGeoTransform(geotransform);

                cellXsize = geotransform[1];
                cellYsize = geotransform[5];
                anchorPt = new Point(geotransform[0], geotransform[3]);

                var band1 = gDataset.GetRasterBand(1);
                cellArray = new double[cellCount];
                band1.ReadRaster(0, 0, xColumns, yRows, cellArray, xColumns, yRows, 0, 0);
            }

        }
    
    
        public Point GetCenterPoint(int xIndex, int yIndex)
        {
            double x = cellXsize * xIndex + anchorPt.x + cellXsize / 2.0;
            double y = cellYsize * xIndex + anchorPt.y + cellYsize / 2.0;
            int localIdx = xIndex + (yIndex * xColumns);
            double z = cellArray[localIdx];

            return new Point(x, y, z);
        }

        public IReadOnlyCollection<Point> CellsAsPoints()
        {
            var returnCollection = new ConcurrentBag<Point>();
            Parallel.For(0, cellCount, idx =>
            {
                double elevation = cellArray[idx];
                int rowIndex = idx / xColumns;
                int columnIndex = idx % xColumns;

                double x = (columnIndex * cellXsize) + anchorPt.x;
                double y = (rowIndex * cellYsize) + anchorPt.y;

                returnCollection.Add(new Point(x, y, elevation));
            });

            return returnCollection;
        }
    }
}
