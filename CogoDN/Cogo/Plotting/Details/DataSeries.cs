using CadFoundation.Coordinates;
using PdfSharpCore.Drawing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cogo.Plotting.Details
{
    public class DataSeries
    {
        public pUnit unit { get; set; }
        public List<Point> theData { get; set; } = new List<Point>();
        public XPen PenProperties { get; set; }

        public DataSeries(List<Point> dataset, pUnit measurementUnits,
            CogoDNPen penProperties = null)
        {
            theData = dataset;
            unit = measurementUnits;
            var tempPen = penProperties;
            if (null == tempPen)
                tempPen = (new ProfilePenLibrary())["default"];

            PenProperties = new XPen(tempPen.Color, tempPen.Width);
            PenProperties.DashStyle = tempPen.DashStyle;

        }

        public void SetPenProperties(PenLibrary penLibrary, string penName)
        {
            var tempPen = (new ProfilePenLibrary())[penName];
            PenProperties = new XPen(tempPen.Color, tempPen.Width);
            PenProperties.DashStyle = tempPen.DashStyle;
        }
    }
}
