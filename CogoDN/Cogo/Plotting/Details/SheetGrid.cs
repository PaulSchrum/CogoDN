using CadFoundation.Coordinates;
using System;
using System.Collections.Generic;
using System.Text;
using CadFoundation.Coordinates;
using System.Linq;
using PdfSharpCore.Drawing;

namespace Cogo.Plotting.Details
{
    public class SheetGrid
    {
        public Vector LowerLeftCorner { get; set; }
        public Vector PanelDimensions { get; set; }
        public pUnit units { get; set; }
        public double MajorGridWidth { get; set; }
        protected double majorHorizontalCount { get; set; }
        protected double majorVerticalCount { get; set; }
        public double MinorGridWidth { get; set; }
        public short MinorPerMajor { get; set; }
        protected double minorHorizontalCount { get; set; }
        protected double minorVerticalCount { get; set; }
        protected List<DataSeries> majorLines_ = null;
        protected List<DataSeries> minorLines_ { get; set; } = null;
        CogoDNPen MajorGridPen { get; set; } = (new PenLibrary())["Major"];
        CogoDNPen MinorGridPen { get; set; } = (new PenLibrary())["Minor"];

        public SheetGrid(Vector panelSize, Vector lowerLeftOffset=null, 
            double majorGLwidth=1.0, pUnit units=pUnit.Inch, short minorPerMajor=5)
        {
            PanelDimensions = panelSize;
            LowerLeftCorner = lowerLeftOffset;
            if (null == lowerLeftOffset)
                LowerLeftCorner = new Vector(0, 0);
            MajorGridWidth = majorGLwidth;
            MinorGridWidth = MajorGridWidth / minorPerMajor;
            MinorPerMajor = minorPerMajor;

            majorHorizontalCount = PanelDimensions.x / MajorGridWidth;
            majorVerticalCount = PanelDimensions.y / MajorGridWidth;
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
                for (int rowIndex = 0; rowIndex < MajorGridWidth; rowIndex++)
                {
                    yVal = rowIndex * MajorGridWidth;
                    pointList = new List<Point>
                    {
                        new Point(xLeft, yVal),
                        new Point(xRight, yVal),
                    };
                    gridLine = new DataSeries(pointList, units, MajorGridPen);
                    majorLines_.Add(gridLine);
                }
                yTop = LowerLeftCorner.y + PanelDimensions.y;
                pointList = new List<Point>
                {
                    new Point(xLeft, yTop),
                    new Point(xRight, yTop),
                };
                gridLine = new DataSeries(pointList, units, MajorGridPen);
                majorLines_.Add(gridLine);
                #endregion

                #region plot vertical grid lines

                yBottom = LowerLeftCorner.y;
                yTop = LowerLeftCorner.y + PanelDimensions.y;
                xLeft = startX; xRight = startX + PanelDimensions.x;

                for (int columnIndex = 0; columnIndex < MajorGridWidth; columnIndex++)
                {
                    xVal = columnIndex * MajorGridWidth;
                    pointList = new List<Point>
                    {
                        new Point(xVal, yBottom),
                        new Point(xVal, yTop),
                    };
                    gridLine = new DataSeries(pointList, units, MajorGridPen);
                    majorLines_.Add(gridLine);
                }
                yTop = LowerLeftCorner.y + PanelDimensions.y;
                pointList = new List<Point>
                {
                        new Point(xRight, yBottom),
                        new Point(xRight, yTop),
                };
                gridLine = new DataSeries(pointList, units, MajorGridPen);
                majorLines_.Add(gridLine);
                #endregion

                return majorLines_;
            }
        }

        public List<DataSeries> MinorLines
        {
            get
            {
                if (null != minorLines_)
                    return minorLines_;
                double startX = LowerLeftCorner.x;
                double startY = LowerLeftCorner.y;
                double yVal, yBottom, yTop;
                double xVal, xLeft, xRight;
                DataSeries gridLine = null;
                majorLines_ = new List<DataSeries>();
                List<Point> pointList = null;

                #region plot horizontal grid lines
                xLeft = startX; xRight = startX + PanelDimensions.x;
                for (int rowIndex = 0; rowIndex < MajorGridWidth; rowIndex++)
                {
                    for(int minorIndex=1; minorIndex< MinorPerMajor; minorIndex++)
                    {
                        yVal = (minorIndex * MinorGridWidth) + (rowIndex * MajorGridWidth);
                        pointList = new List<Point>
                        {
                            new Point(xLeft, yVal),
                            new Point(xRight, yVal),
                        };
                        gridLine = new DataSeries(pointList, units, MinorGridPen);
                        minorLines_.Add(gridLine);
                    }
                }
                #endregion

                #region plot vertical grid lines

                yBottom = LowerLeftCorner.y;
                yTop = LowerLeftCorner.y + PanelDimensions.y;
                xLeft = startX; xRight = startX + PanelDimensions.x;

                for (int columnIndex = 0; columnIndex < MajorGridWidth; columnIndex++)
                {
                    xVal = columnIndex * MajorGridWidth + LowerLeftCorner.x;
                    for (int minorIndex = 1; minorIndex < MinorPerMajor; minorIndex++)
                    {
                        xVal = (minorIndex * MinorGridWidth) + (columnIndex * MajorGridWidth);
                        pointList = new List<Point>
                        {
                            new Point(xVal, yBottom),
                            new Point(xVal, yTop),
                        };
                        gridLine = new DataSeries(pointList, units, MinorGridPen);
                        minorLines_.Add(gridLine);
                    }
                }
                #endregion

                return minorLines_;
            }
        }

    }
}
