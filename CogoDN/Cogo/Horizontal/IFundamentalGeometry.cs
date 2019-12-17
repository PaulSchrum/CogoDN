using CadFoundation.Coordinates;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cogo.Horizontal
{
    public interface IFundamentalGeometry
    {
        List<Point> getPointList();
        Double getBeginningDegreeOfCurve();
        Double getEndingDegreeOfCurve();
        expectedType getExpectedType();
        int getDeflectionSign();
    }

    public enum expectedType
    {
        LineSegment = 0,
        ArcSegmentInsideSolution = 1,
        ArcSegmentOutsideSoluion = 2,
        ArcHalfCircle = 3,
        EulerSpiral = 4
    };
}
