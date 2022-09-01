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
            var pageHeightInches = 5.0; var pageWidthInches = 7.0;
            page.Height = DecimalUnits.MakeFromLength(pageHeightInches, pUnit.Inch);
            page.Width = DecimalUnits.MakeFromLength(pageWidthInches, pUnit.Inch);
            XGraphics gfx = XGraphics.FromPdfPage(page);


            //PageSize[] pageSizes = (PageSize[])Enum.GetValues(typeof(PageSize));
            //var letter = pageSizes[24];
            var scaleFactor = (double)(plotScale.AsMultiplierHorizontal * PageUnits.inch);

            var gridScaleToSheet = (double)pUnit.Inch;
            var gridAT = new AffineTransform2d();
            gridAT.AddScale(1.0, -1.0);
            gridAT.AddTranslation(0, pageHeightInches);
            gridAT.AddScale(gridScaleToSheet, gridScaleToSheet);

            var panelSize = new Vector(6.9, 4.9);
            var profileGrid = new SheetGrid(panelSize: panelSize,
                lowerLeftOffset: new Vector(0.05, 0.05), affineTrans: gridAT);
            profileGrid.DrawToGfx(gfx);

            var theString = DateTime.Now.ToString("h:mm tt");
            XFont font = new XFont("Verdana", 24, XFontStyle.Bold);
            gfx.DrawString(theString, font, XBrushes.Black, 72, 72 * 2);

            var dataAT = new AffineTransform2d();  // Data Affine Transform
            dataAT.AddScale(1.0, -1.0);
            dataAT.AddTranslation(0.0, pageHeightInches);
            dataAT.AddScale(scaleFactor, scaleFactor);

            foreach(var series in allSeries)
            {
                var points = series.theData;
                XPen pen = series.PenProperties;
                pen.LineCap = XLineCap.Round;
                pen.LineJoin = XLineJoin.Bevel;

                points = points.Select(pt => dataAT.TransformToNewPoint(pt))
                    .ToList();

                XPoint[] Xpoints = points
                    .Select(pt => new XPoint(pt.x, pt.y)).ToArray();
                gfx.DrawLines(pen, Xpoints);
            }

            document.Save(pdfFileName);
        }
    }

}

