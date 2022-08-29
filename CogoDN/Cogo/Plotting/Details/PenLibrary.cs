using System;
using System.Collections.Generic;
using System.Text;
using PdfSharpCore.Drawing;

namespace Cogo.Plotting.Details
{
    public class PenLibrary : Dictionary<string, CogoDNPen>
    {
        public PenLibrary()
        {
            var defaultPen = new CogoDNPen(XColors.Black, XDashStyle.Solid, 1.0);
            defaultPen.DashStyle = XDashStyle.Solid;
            this["default"] = defaultPen;
        }
    }

    public class ProfilePenLibrary : PenLibrary
    {
        public ProfilePenLibrary()
        {
            var existingTerrainPen = 
                new CogoDNPen(XColors.Black, XDashStyle.Dash, 3.0);
            this["Existing Terrain"] = existingTerrainPen;

            var hyperbolaTerrainPen = 
                new CogoDNPen(XColors.AliceBlue, XDashStyle.Solid, 2.0);
            this["Hyperbola Terrain"] = hyperbolaTerrainPen;
        }
    }

    public class CogoDNPen
    {
        public XColor Color { get; set; }
        public XDashStyle DashStyle { get; set; }
        public double Width { get; set; }

        public CogoDNPen(XColor color = default, 
            XDashStyle dashStyle = XDashStyle.Solid,
            double width = 1.0)
        {
            Color = color;
            if (color == default)
                Color = XColors.Black;
            DashStyle = dashStyle;
            Width = width;
        }
    }
}
