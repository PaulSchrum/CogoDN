using CadFoundation.Coordinates;
using Cogo.Plotting.Details;
using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cogo.Plotting
{
    public abstract class Sheet : BoundingBox
    {
        public PdfDocument document { get; private set; }
        public PdfPage page { get; private set; }
        public XGraphics gfx { get; private set; }
        public XFont font { get; private set; }
        public SheetPanel sheetPanel { get; private set; }

        protected Margins Margins { get; set; }
        public virtual DecimalUnits height {get; set; }
        public virtual DecimalUnits width { get; set; }
        public PlotScale scale { get; set; }
        //protected PlotArea PlotArea { get; private set; }

        public Sheet()
        {
            document = new PdfDocument();
            PdfPage page = this.document.AddPage();
            page.Orientation = PageOrientation.Landscape;
            gfx = XGraphics.FromPdfPage(page);
            font = new XFont("Verdana", 20, XFontStyle.BoldItalic);
            scale = new PlotScale(new DecimalUnits(1, pUnit.USfoot),
                new DecimalUnits(1, pUnit.Inch));
        }
}
}
