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
                int gridsCellsPerPanelY = (int)Math.Ceiling(PanelDimensions.y / MajorGridWidth);
                yVal = yBottom = startY;
                for (int rowIndex = 0; rowIndex < gridsCellsPerPanelY; rowIndex++)
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

                int gridCellsPerPanelX = (int)Math.Ceiling(PanelDimensions.x / MajorGridWidth);
                xVal = startX;
                for (int columnIndex = 0; columnIndex < gridCellsPerPanelX; columnIndex++)
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

        /////////////////////////////////////////////////////////////////////

        public enum HorizontalJustification
        {
            NotSet = 0, Left = 1, Center = 2, Right = 3
        }

        public enum VerticalJustification
        {
            NotSet = 10, Bottom = 20, Center = 30, Top = 40
        }

        public class PointUnits
        {
            public PointUnits(Point aPoint, pUnit unit)
            {
                Point = aPoint;
                Unit = unit;
            }

            public Point Point { get; private set; }
            public pUnit Unit { get; private set; }

            public DecimalUnits getX()
            {
                return new DecimalUnits((Decimal) Point.x, Unit);
            }

            public DecimalUnits getY()
            {
                return new DecimalUnits((Decimal)Point.y, Unit);
            }

            public DecimalUnits getZ()
            {
                return new DecimalUnits((Decimal)Point.z, Unit);
            }
        }



//////// class AnchorPoint handles converting from world coordinates to panel coordinates.

        public class AnchorPoint
        {
            protected Vector PanelDimensions { get; set; }
            public HorizontalJustification HorizontalJustification { get; private set; }
            public VerticalJustification VerticalJustification { get; private set; }

            public PointUnits AnchorWorldCoordinates { get; private set; }
            public PointUnits AnchorPanelCoordinates { get; private set; }

            public AffineTransform2d WorldToPanelAT { get; private set; }
            public AffineTransform2d PanelToWorldAT { get; private set; }

            AnchorPoint(Vector panelDimensions,
                Point worldAnchor, pUnit worldUnit,
                Point panelAnchor, pUnit panelUnit,
                HorizontalJustification HorizontalJustification, 
                VerticalJustification VerticalJustification)
            {
                PanelDimensions = panelDimensions;
                AnchorWorldCoordinates = new PointUnits(worldAnchor, worldUnit);
                AnchorPanelCoordinates = new PointUnits(panelAnchor, panelUnit);

                this.HorizontalJustification = HorizontalJustification;
                this.VerticalJustification = VerticalJustification;


                recomputeTransforms();
            }

            public void recomputeTransforms()
            {
                double panelAnchorX = 0; double panelAnchorY = 0;
                switch (HorizontalJustification)
                {
                    case HorizontalJustification.Left:
                        {
                            panelAnchorX = 0;
                            break;
                        }
                    case HorizontalJustification.Center:
                        {
                            panelAnchorX = PanelDimensions.x / 2.0;
                            break;
                        }
                    case HorizontalJustification.Right:
                        {
                            panelAnchorX = PanelDimensions.x;
                            break;
                        }
                    case HorizontalJustification.NotSet:
                        {
                            panelAnchorX = 0;
                            break;
                        }
                    default:
                        break;
                }

                switch (VerticalJustification)
                {
                    case VerticalJustification.Bottom:
                        {
                            panelAnchorY = 0;
                            break;
                        }
                    case VerticalJustification.Center:
                        {
                            panelAnchorY = PanelDimensions.y / 2.0;
                            break;
                        }
                    case VerticalJustification.Top:
                        {
                            panelAnchorY = PanelDimensions.y;
                            break;
                        }
                    case VerticalJustification.NotSet:
                        {
                            panelAnchorY = 0;
                            break;
                        }
                    default:
                        break;
                }

                WorldToPanelAT = new AffineTransform2d();

                WorldToPanelAT   // Move to 0, 0, world.
                    .AddTranslation(-AnchorWorldCoordinates.Point.x,
                    -AnchorWorldCoordinates.Point.y);

                WorldToPanelAT   // scale down to XPoint (aka Pixel, aka 1/72 inch)
                    .AddScale(1 / AnchorWorldCoordinates.Unit.GetAsPixels(),
                    1 / AnchorWorldCoordinates.Unit.GetAsPixels());

                WorldToPanelAT   // scale up to panel unit
                    .AddScale(AnchorPanelCoordinates.Unit.GetAsPixels(),
                    AnchorPanelCoordinates.Unit.GetAsPixels());

                WorldToPanelAT   // move to panel anchor point
                    .AddTranslation(AnchorPanelCoordinates.Point.x,
                    AnchorPanelCoordinates.Point.y);

                WorldToPanelAT   // move anchor point to justification point
                    .AddTranslation(panelAnchorX, panelAnchorY);

                PanelToWorldAT = WorldToPanelAT.NewFromInverse();
            }
        }

    }
}
