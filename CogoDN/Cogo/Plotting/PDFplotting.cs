using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CadFoundation.Coordinates;
using CadFoundation.Coordinates.Transforms;
using Cogo.Plotting.Details;
using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

using PdfSharpCore.Pdf.IO;


namespace Cogo.Plotting
{
    public static class PDFplotting
    {
        public static void CreateSheetFromProfiles(List<DataSeries> allSeries, 
            string pdfFileName, PlotScale plotScale)
        {
            var pfl = allSeries[0];
            PdfDocument document = new PdfDocument();
            PdfPage page = document.AddPage();
            page.Orientation = PageOrientation.Landscape;
            page.Height = 720/2;
            XGraphics gfx = XGraphics.FromPdfPage(page);
            XFont font = new XFont("Verdana", 20, XFontStyle.Bold);


            PageSize[] pageSizes = (PageSize[])Enum.GetValues(typeof(PageSize));
            var letter = pageSizes[24];

            var theString = DateTime.Now.ToString("h:mm tt");
            gfx.DrawString(theString, font, XBrushes.Black, 72, 72*2);
            foreach(var series in allSeries)
            {
                var points = series.theData;
                // XPen pen = new XPen(XColors.DarkSeaGreen, 1);
                XPen pen = series.PenProperties;
                pen.LineCap = XLineCap.Round;
                pen.LineJoin = XLineJoin.Bevel;

                var affineTransform = new AffineTransform2d();
                affineTransform.AddScale(1.0, -1.0);
                affineTransform.AddTranslation(-3.0, 4.0);
                points = points.Select(pt => affineTransform.TransformToNewPoint(pt))
                    .ToList();

                var scaleFactor = plotScale.AsMultiplierHorizontal * PageUnits.inch;
                
                points = points.Select(pt =>
                    new Point(pt.x * (double)scaleFactor, pt.y * (double)scaleFactor)
                    ).ToList();

                XPoint[] Xpoints = points
                    .Select(pt => new XPoint(pt.x, pt.y)).ToArray();
                gfx.DrawLines(pen, Xpoints);
            }

            document.Save(pdfFileName);
        }
    }

}

