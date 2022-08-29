using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                // XPen pen = new XPen(XColors.DarkSeaGreen, 1);
                XPen pen = series.PenProperties;
                pen.LineCap = XLineCap.Round;
                pen.LineJoin = XLineJoin.Bevel;
                XPoint[] points = series.theData
                    .Select(pt => new XPoint(pt.x, pt.y)).ToArray();
                var scaleFactor = plotScale.AsMultiplierHorizontal;
                points = points.Select(pt =>
                    new XPoint(pt.X * (double)plotScale.AsMultiplierHorizontal,
                        pt.Y * (double)plotScale.AsMultiplierVertical)
                    ).ToArray();
                gfx.DrawLines(pen, points);
            }

            document.Save(pdfFileName);
        }
    }

}

