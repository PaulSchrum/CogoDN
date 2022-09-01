using CadFoundation.Coordinates;
using System;
using System.Collections.Generic;
using System.Text;
using CadFoundation.Coordinates;
using System.Linq;
using PdfSharpCore.Drawing;
using CadFoundation.Coordinates.Transforms;

// Note: All coordinate work is done in sheet coordinates like inches or centimeters
// depending on what you set Unit to. Conversion to PDF coordinates (XPoint) doesn't happen
// until the very end via the affine transform.  In other words, work in inches, etc, and
// don't worry about the XPoint conversion.

namespace Cogo.Plotting.Details
{
    public class SheetGrid
    {
        public Vector LowerLeftCorner { get; set; }
        public Vector PanelDimensions { get; set; }
        public pUnit Unit { get; set; }
        public double MajorGridWidth { get; set; }
        protected double majorHorizontalCount { get; set; }
        protected double majorVerticalCount { get; set; }
        public double MinorGridWidth { get; set; }
        public short MinorPerMajor { get; set; }
        protected double minorHorizontalCount { get; set; }
        protected double minorVerticalCount { get; set; }
        protected List<DataSeries> majorLines_ = null;
        protected List<DataSeries> minorLines_ { get; set; } = null;
        CogoDNPen MajorGridPen { get; set; } = (new GridPenLibrary())["Major"];
        CogoDNPen MinorGridPen { get; set; } = (new GridPenLibrary())["Minor"];
        public AffineTransform2d affineXform = null;

        public SheetGrid(Vector panelSize, Vector lowerLeftOffset=null, 
            double majorGLwidth=1.0, pUnit units=pUnit.Inch, short minorPerMajor=5,
            AffineTransform2d affineTrans = null)
        {
            Unit = units;
            PanelDimensions = panelSize;
            LowerLeftCorner = lowerLeftOffset;
            if (null == lowerLeftOffset)
                LowerLeftCorner = new Vector(0, 0);
            MajorGridWidth = majorGLwidth;
            MinorGridWidth = MajorGridWidth / minorPerMajor;
            MinorPerMajor = minorPerMajor;

            majorHorizontalCount = panelSize.x / MajorGridWidth;
            majorVerticalCount = panelSize.y / MajorGridWidth;

            affineXform = affineTrans;
            if (null == affineXform)
                affineXform = new AffineTransform2d();
        }

        public List<DataSeries> MajorLines
        {
            get
            {
                if (null != majorLines_)
                    return majorLines_;
                double startX = LowerLeftCorner.x;
                double startY = LowerLeftCorner.y;
                double yVal, yBottom, yTop;
                double xVal, xLeft, xRight;
                DataSeries gridLine = null;
                majorLines_ = new List<DataSeries>();
                List<Point> pointList = null;

                #region plot horizontal grid lines
                xLeft = startX; xRight = startX + PanelDimensions.x;
                int gridsCellsPerPanelX = (int)Math.Ceiling(PanelDimensions.x / MajorGridWidth);
                yVal = yBottom = startY;
                for (int rowIndex = 0; rowIndex < gridsCellsPerPanelX; rowIndex++)
                {
                    pointList = new List<Point>
                    {
                        new Point(xLeft, yVal),
                        new Point(xRight, yVal),
                    };
                    gridLine = new DataSeries(pointList, Unit, MajorGridPen);
                    majorLines_.Add(gridLine);
                    yVal += MajorGridWidth;
                }
                yTop = LowerLeftCorner.y + PanelDimensions.y;
                pointList = new List<Point>
                {
                    new Point(xLeft, yTop),
                    new Point(xRight, yTop),
                };
                gridLine = new DataSeries(pointList, Unit, MajorGridPen);
                majorLines_.Add(gridLine);
                #endregion

                #region plot vertical grid lines

                yBottom = LowerLeftCorner.y;
                yTop = LowerLeftCorner.y + PanelDimensions.y;
                xLeft = startX; xRight = startX + PanelDimensions.x;

                int gridCellsPerPanelY = (int)Math.Ceiling(PanelDimensions.y / MajorGridWidth);
                xVal = startX;
                for (int columnIndex = 0; columnIndex < gridCellsPerPanelY; columnIndex++)
                {
                    pointList = new List<Point>
                    {
                        new Point(xVal, yBottom),
                        new Point(xVal, yTop),
                    };
                    gridLine = new DataSeries(pointList, Unit, MajorGridPen);
                    majorLines_.Add(gridLine);
                    xVal += MajorGridWidth;
                }
                yTop = LowerLeftCorner.y + PanelDimensions.y;
                pointList = new List<Point>
                {
                        new Point(xRight, yBottom),
                        new Point(xRight, yTop),
                };
                gridLine = new DataSeries(pointList, Unit, MajorGridPen);
                majorLines_.Add(gridLine);
                #endregion

                return majorLines_;
            }
        }

        public List<DataSeries> MinorLines
        {
            get
            {
                //if (null != minorLines_)
                //    return minorLines_;
                double startX = LowerLeftCorner.x;
                double startY = LowerLeftCorner.y;
                double yVal, yBottom, yTop;
                double xVal, xLeft, xRight;
                DataSeries gridLine = null;
                minorLines_ = new List<DataSeries>();
                List<Point> pointList = null;
                yBottom = startY; yTop = yBottom + PanelDimensions.y;
                xLeft = startX; xRight = startX + PanelDimensions.x;

                #region plot horizontal grid lines
                int minorIndex;
                int gridsCellsPerPanelX = (int)Math.Ceiling(PanelDimensions.x / MajorGridWidth);
                for (int rowIndex = 0; rowIndex < gridsCellsPerPanelX-1; rowIndex++)
                {
                    for(minorIndex=1; minorIndex< MinorPerMajor; minorIndex++)
                    {
                        yVal = (minorIndex * MinorGridWidth) + 
                            (rowIndex * MajorGridWidth) + yBottom;
                        pointList = new List<Point>
                        {
                            new Point(xLeft, yVal),
                            new Point(xRight, yVal),
                        };
                        gridLine = new DataSeries(pointList, Unit, MinorGridPen);
                        minorLines_.Add(gridLine);
                    }
                }

                minorIndex = 1;
                yVal = (minorIndex * MinorGridWidth) +
                            ((gridsCellsPerPanelX-1) * MajorGridWidth) + yBottom;
                while(yVal < yTop)
                {
                    pointList = new List<Point>
                        {
                            new Point(xLeft, yVal),
                            new Point(xRight, yVal),
                        };
                    gridLine = new DataSeries(pointList, Unit, MinorGridPen);
                    minorLines_.Add(gridLine);
                    yVal += MinorGridWidth;
                }

                #endregion

                #region plot vertical grid lines

                yBottom = LowerLeftCorner.y;
                yTop = LowerLeftCorner.y + PanelDimensions.y;
                xLeft = startX; xRight = startX + PanelDimensions.x;

                int gridsCellsPerPanelY = (int)Math.Ceiling(PanelDimensions.y / MajorGridWidth);
                for (int columnIndex = 0; columnIndex < gridsCellsPerPanelY - 1; columnIndex++)
                {
                    for (minorIndex = 1; minorIndex < MinorPerMajor; minorIndex++)
                    {
                        xVal = (minorIndex * MinorGridWidth) +
                            (columnIndex * MajorGridWidth) + xLeft;
                        pointList = new List<Point>
                        {
                            new Point(xVal, yBottom),
                            new Point(xVal, yTop),
                        };
                        gridLine = new DataSeries(pointList, Unit, MinorGridPen);
                        minorLines_.Add(gridLine);
                    }
                }

                minorIndex = 1;
                xVal = (minorIndex * MinorGridWidth) +
                            ((gridsCellsPerPanelY - 1) * MajorGridWidth) + xLeft;
                while (xVal < xRight)
                {
                    pointList = new List<Point>
                        {
                            new Point(xVal, yBottom),
                            new Point(xVal, yTop),
                        };
                    gridLine = new DataSeries(pointList, Unit, MinorGridPen);
                    minorLines_.Add(gridLine);
                    xVal += MinorGridWidth;
                }
                #endregion

                return minorLines_;
            }
        }

        public void DrawToGfx(XGraphics gfx)
        {
            foreach (var aLine in this.MinorLines)
            {
                var points = aLine.theData;
                XPen pen = aLine.PenProperties;
                pen.LineCap = XLineCap.Round;
                pen.LineJoin = XLineJoin.Bevel;

                points = points.Select(pt => affineXform.TransformToNewPoint(pt))
                    .ToList();
                XPoint[] Xpoints = points.Select(pt => new XPoint(pt.x, pt.y)).ToArray();
                gfx.DrawLines(pen, Xpoints);
            }
            int stopHere=0;
            foreach (var aLine in this.MajorLines)
            {
                var points = aLine.theData;
                XPen pen = aLine.PenProperties;
                pen.LineCap = XLineCap.Round;
                pen.LineJoin = XLineJoin.Bevel;

                points = points.Select(pt => affineXform.TransformToNewPoint(pt))
                    .ToList();
                XPoint[] Xpoints = points.Select(pt => new XPoint(pt.x, pt.y)).ToArray();
                gfx.DrawLines(pen, Xpoints);
            }
        }

    }
}
