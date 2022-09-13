using CadFoundation.Coordinates;
using Cogo.Plotting.Details;
using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System;
using System.Collections.Generic;
using System.Text;

// This effort, Cogo.Plotting, is suspended indefinitely pending 
// any future need to restart it, which may never happen.
namespace Cogo.Plotting
{
    public abstract class Sheet : Vector
    {
        public PdfDocument document { get; private set; }
        public PdfPage page { get; private set; }
        public XGraphics gfx { get; private set; }
        public XFont font { get; private set; }
        public SheetPanel sheetPanel { get; private set; }

        public pUnit sheetUnit { get; set; }

        protected Margins Margins { get; set; }

        private DecimalUnits height_;
        public virtual DecimalUnits height 
        { 
            get { return height_; }
            set 
            { 
                height_ = value;
                page.Height = (XUnit) value.GetValueAs(pUnit.Pixel);
            }
        }

        private DecimalUnits width_;
        public virtual DecimalUnits width
        {
            get { return width_; }
            set
            {
                width_ = value;
                page.Height = (XUnit)value.GetValueAs(pUnit.Pixel);
            }

        }

        public PlotScale scale { get; set; }
        //protected PlotArea PlotArea { get; private set; }
        protected bool variableVertical {get; set;} = false;
        protected bool variableHorizontal { get; set; } = false;
        public PlotArea LeftMargin { get; protected set; } = PlotArea.Default();
        public PlotArea RightMargin { get; protected set; } = PlotArea.Default();
        public PlotArea BottomMargin { get; protected set; } = PlotArea.Default();
        public PlotArea TopMargin { get; protected set; } = PlotArea.Default();
        public PlotArea LeftYAxis { get; protected set; } = PlotArea.Default();
        public PlotArea RightYAxis { get; protected set; } = PlotArea.Default();
        public PlotArea TopXAxis { get; protected set; } = PlotArea.Default();
        public PlotArea BottomXAxis { get; protected set; } = PlotArea.Default();

        // Any concrete class inheriting Sheet must set ChartArea.
        public PlotArea ChartArea { get; set; } = PlotArea.Default();

        public Sheet()
        {
            document = new PdfDocument();
            page = this.document.AddPage();
            page.Orientation = PageOrientation.Landscape;
            gfx = XGraphics.FromPdfPage(page);
            font = new XFont("Verdana", 20, XFontStyle.BoldItalic);
            scale = new PlotScale(new DecimalUnits(1, pUnit.USfoot),
                new DecimalUnits(1, pUnit.Inch));
        }

        public abstract double? PlotSheetToPdfFile(
            IEnumerable<Object> dataToPlot, 
            string printFileName,
            double leftXValue = 0.0,
            double? bottomYvalue=null);
    }

    public class PlotArea : Vector
    {
        // Position coordinates are stored in Pixel (1 / 72 of an inch.)
        // This is equal to PDFplotCore's XUnit.
        Sheet topParent = null;
        public Vector LowerLeftPoint { get; protected set; }

        private PlotArea() { }

        public XPoint ConvertToSheetXPoint(Point thePoint, pUnit unit)
        {
            var x = (double) 
                (new DecimalUnits((decimal) thePoint.x, unit)).GetValueAs(pUnit.Pixel);
            var y = (double) 
                (new DecimalUnits((decimal)thePoint.y, unit)).GetValueAs(pUnit.Pixel);
            x += LowerLeftPoint.x;
            y += LowerLeftPoint.y;
            return new XPoint(x, y);
        }

        public static PlotArea Create(Sheet parent, pUnit unit, double width, double height, 
            double offsetX=0.0, double offsetY=0.0)
        {
            if (null == parent)
                throw new ArgumentException("Parent sheet cannot be null.");

            PlotArea pa = new PlotArea();
            pa.topParent = parent;
            pa.LowerLeftPoint = new Vector(offsetX, -offsetY);
            pa.x = width; pa.y = height;

            return pa;
        }

        public static PlotArea Default()
        {
            PlotArea pa = new PlotArea();
            pa.topParent = null;
            pa.LowerLeftPoint = new Vector(0, 0);
            pa.x = 0; pa.y = 0;
            return pa;
        }
    }
}
