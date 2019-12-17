using System;
using System.Collections.Generic;
using System.Text;
using CadFoundation.Coordinates.Curvilinear;
using CadFoundation.Coordinates;
using Cogo.Horizontal;

namespace Cogo
{
    public interface IPersistantDrawer
    {
        void PlaceLine(HorLineSegment lineSegment,
           Point startPoint, StationOffsetElevation startSOE,
           Point endPoint, StationOffsetElevation endSOE);

        void PlaceArc(HorArcSegment arc,
           Point startPoint, StationOffsetElevation startSOE,
           Point endPoint, StationOffsetElevation endSOE);
    }
}
