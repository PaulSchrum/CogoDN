using CadFoundation.Coordinates.Transforms;
using Cogo.Plotting.Details;
using System;
using System.Collections.Generic;
using System.Text;
using CadFoundation.Coordinates;
using CadFoundation.Coordinates.Transforms;
using PdfSharpCore.Drawing;
using System.Linq;

// This effort, Cogo.Plotting, is suspended indefinitely pending 
// any future need to restart it, which may never happen.
namespace Cogo.Plotting.Sheets
{
    public class Chart7p5ByVariable : Sheet
    {
        private Chart7p5ByVariable()
        {

        }

        protected double leftmostXvalue { get; set; }
        protected double bottomMostYvalue { get; set; }

        public override double? PlotSheetToPdfFile(IEnumerable<Object> profileSeriesToPlot, 
            string printFileName, 
            double beginStation=0.0, double? bottomYvalue = null)
        {
            var allSeries = profileSeriesToPlot.OfType<DataSeries>().ToList(); // as this is a profile
                     // sheet, it has the responsibility to know what to do with
                     // profiles (Data Series).
            
            if (null == allSeries)
                throw new InvalidCastException("Unable to plot sheet as the data is of the wrong type.");

            var theString = DateTime.Now.ToString("h:mm tt");
            XFont font = new XFont("Verdana", 24, XFontStyle.Bold);
            gfx.DrawString(theString, font, XBrushes.Black, 72, 72 * 2);
            plotTheGrid();
            plotTheData();
            plotTheLeftAxis();
            plotTheBottomAxis();

            document.Save(printFileName);


            double widthPlotted = 0.0;
            return null;
        }

        private void plotTheGrid()
        {
            var gridScaleToSheet = (double)pUnit.Inch;
            var gridAT = new AffineTransform2d();
            gridAT.AddScale(1.0, -1.0);
            gridAT.AddTranslation(0, (double) this.height.GetValueAs(pUnit.Inch));
            gridAT.AddTranslation(ChartArea.LowerLeftPoint.x, ChartArea.LowerLeftPoint.y);
            gridAT.AddScale(gridScaleToSheet, gridScaleToSheet);

            var panelSize = this.ChartArea;
            var profileGrid = new SheetGrid(panelSize: panelSize,
                lowerLeftOffset: new Vector(0.05, 0.05), affineTrans: gridAT);
            profileGrid.DrawToGfx(gfx);
        }

        private void plotTheData() { }
        private void plotTheLeftAxis() { }
        private void plotTheBottomAxis() { }

        public static Chart7p5ByVariable Create()
        {
            Chart7p5ByVariable sheet = new Chart7p5ByVariable();
            sheet.sheetUnit = pUnit.Inch;

            var sheetHeight = 4.5m; var sheetWidth = 7.5m;
            sheet.height = new DecimalUnits(sheetHeight, sheet.sheetUnit);
            //sheet.variableVertical = true;
            sheet.width = new DecimalUnits(sheetWidth, sheet.sheetUnit);
            double widthLeftAxis = 0.5; double heightBottomAxis = 0.5;
            sheet.LeftYAxis = PlotArea.Create(sheet, sheet.sheetUnit, widthLeftAxis, 7.0, 0, 0.5);
            sheet.BottomXAxis = PlotArea.Create(sheet, sheet.sheetUnit, 
                width: 7.5, height: heightBottomAxis, offsetX: 0, offsetY: 0.0);
            //var chartPanelHeight = sheet.page.Height.Value - sheet.BottomXAxis.y;
            var w = (double)sheetHeight - heightBottomAxis; 
            var h = (double)sheetWidth - widthLeftAxis;
            sheet.ChartArea = PlotArea.Create(sheet, sheet.sheetUnit, 
                width: w, 
                height: h,
                offsetX: 0.5, offsetY: 0.5);



            return sheet;
        }


        //{
        //    var AllSeries = allSeries.OfType<DataSeries>();  // as this is a profile sheet, it has
        //                        // the responsibility to know what to do with profiles (Data Series).
        //    var scaleFactor = (double)(plotScale.AsMultiplierHorizontal * PageUnits.inch);

        //    var gridScaleToSheet = (double)pUnit.Inch;
        //    var gridAT = new AffineTransform2d();
        //    gridAT.AddScale(1.0, -1.0);
        //    gridAT.AddTranslation(0, pageHeightInches);
        //    gridAT.AddScale(gridScaleToSheet, gridScaleToSheet);

        //    var panelSize = new Vector(6.9, 4.9);
        //    var profileGrid = new SheetGrid(panelSize: panelSize,
        //        lowerLeftOffset: new Vector(0.05, 0.05), affineTrans: gridAT);
        //    profileGrid.DrawToGfx(gfx);

        //    var theString = DateTime.Now.ToString("h:mm tt");
        //    XFont font = new XFont("Verdana", 24, XFontStyle.Bold);
        //    gfx.DrawString(theString, font, XBrushes.Black, 72, 72 * 2);

        //    var dataAT = new AffineTransform2d();  // Data Affine Transform
        //    dataAT.AddScale(1.0, -1.0);
        //    dataAT.AddTranslation(0.0, pageHeightInches);
        //    dataAT.AddScale(scaleFactor, scaleFactor);

        //    foreach (var series in allSeries)
        //    {
        //        var points = series.theData;
        //        XPen pen = series.PenProperties;
        //        pen.LineCap = XLineCap.Round;
        //        pen.LineJoin = XLineJoin.Bevel;

        //        points = points.Select(pt => dataAT.TransformToNewPoint(pt))
        //            .ToList();

        //        XPoint[] Xpoints = points
        //            .Select(pt => new XPoint(pt.x, pt.y)).ToArray();
        //        gfx.DrawLines(pen, Xpoints);
        //    }

        //    document.Save(pdfFileName);
        //    return 0.0;
        //}
    }
}
