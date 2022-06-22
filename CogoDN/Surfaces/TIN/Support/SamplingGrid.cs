using CadFoundation.Coordinates;
using MathNet.Numerics.LinearAlgebra.Complex;
using System;
using System.Collections.Generic;
using System.Text;
using MathNet.Numerics.LinearAlgebra;
using CadFoundation;
using System.Threading.Tasks;
using Surfaces.Raster;

namespace Surfaces.TIN.Support
{
    public class SamplingGrid
    {
        public BoundingBox myBB { get; protected set; }
        protected TINpoint p { get; set; }
        protected static double notDefined = Double.NegativeInfinity;
        public Matrix<double> x { get; protected set; }  = null;
        public Matrix<double> y { get; protected set; } = null;
        public Matrix<double> z { get; protected set; } = null;
        public Matrix<double> error { get; protected set; } = null;
        public int columns { get; protected set; }
        public int rows { get; protected set; }
        public double x_Spacing { get; protected set; }
        public double y_Spacing { get; protected set; }
        public int pointCount { get; protected set; }
        public double actualPointDensity { get; protected set; }


        public SamplingGrid(BoundingBox aBB, double pointSpacing)
        {
            myBB = aBB;
            columns = (int)(aBB.Width / pointSpacing);
            rows = (int)(aBB.Depth / pointSpacing);
            int pointCount = rows * columns;
            x_Spacing = pointSpacing;
            y_Spacing = pointSpacing;
            actualPointDensity = pointCount / myBB.Area;

            x = Matrix<double>.Build.Dense(rows, columns, notDefined);
            y = Matrix<double>.Build.Dense(rows, columns, notDefined);
            z = Matrix<double>.Build.Dense(rows, columns, notDefined);
            error = Matrix<double>.Build.Dense(rows, columns, notDefined);

            for(int i=0; i<columns; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    double xPos = myBB.lowerLeftPt.x + (i + 0.5) * x_Spacing;
                    double yPos = myBB.upperRightPt.y - (j + 0.5) * y_Spacing;
                    x[i,j] = xPos;
                    y[i, j] = yPos;
                }
            }
        }

        public void SetSourceElevationValues(TINsurface sourceSurface)
        {
            Parallel.For(0, columns, 
                i =>
            //for (int i = 0; i < columns; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    var xCoord = x[i, j];
                    var yCoord = y[i, j];
                    double? elevation;
                    try  // this try block is necessary because of an apparent bug
                    {    // .Net Core.
                        elevation = (double)sourceSurface.getElevation(
                                xCoord, yCoord);
                    }
                    catch(Exception e)
                    {
                        elevation = null;
                    }
                    z[i, j] = (elevation != null) ? (double) elevation : notDefined;
                }
            }
            );
        }

        public TINpoint GetXYofCell(int column, int row)
        {
            return new TINpoint(x[column, row], y[column, row]);
        }

        public RasterSurface GetAsRaster()
        {
            double xMargin = this.x_Spacing / 2.0;
            var returnRaster = RasterSurface.Zeroes(this.x_Spacing, this.columns, this.rows,
                myBB.lowerLeftPt.x - xMargin, 
                myBB.lowerLeftPt.y - xMargin);

            var gridArray = returnRaster.rasterGrid;

            Parallel.For(0, columns, i =>
            //for (int i = 0; i < columns; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    var elev = z[j, i];
                    gridArray[i, j] = elev;
                }
            }
            );

            return returnRaster;
        }
    }
}
