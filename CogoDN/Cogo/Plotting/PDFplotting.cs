using System;
using System.Collections.Generic;
using System.Text;
using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

using PdfSharpCore.Pdf.IO;


namespace Cogo.Plotting
{
    public static class PDFplotting
    {
        public static void CreateSheetFromProfiles(string pdfFileName)
        {
            PdfDocument document = new PdfDocument();
            PdfPage page = document.AddPage();
            page.Orientation = PageOrientation.Landscape;
            page.Height = 720/2;
            XGraphics gfx = XGraphics.FromPdfPage(page);
            XFont font = new XFont("Verdana", 20, XFontStyle.Bold);


            PageSize[] pageSizes = (PageSize[])Enum.GetValues(typeof(PageSize));
            var letter = pageSizes[24];

            gfx.DrawString("Hi.", font, XBrushes.Black, 72, 72*2);
            XPen pen = new XPen(XColors.DarkSeaGreen, 6);
            pen.LineCap = XLineCap.Round;
            pen.LineJoin = XLineJoin.Bevel;
            XPoint[] points = new XPoint[]
            {
                new XPoint(20, 30), new XPoint(60, 120),
                new XPoint(90, 20), new XPoint(170, 90),
                new XPoint(230, 40)
            };
            gfx.DrawLines(pen, points);
            
            document.Save(pdfFileName);
        }
    }

}

