using CadFoundation.Coordinates;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cogo.Horizontal
{
    public interface ILinearElementDrawer
    {
        void setDrawingStateTemporary();
        void setDrawingStatePermanent();
        void drawLineSegment(Point startPt, Point endPt);
        void drawArcSegment(Point startPt, Point centerPt, Point endPt, Double deflection);
        //void drawEulerSpiralSegment(List<ptsPoint> allPoints, Double offset);
        void setAlignmentValues(List<alignmentDataPacket> dataSummary);

    }
}
