using CadFoundation.Coordinates;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cogo.Plotting.Details
{
    public class Margins
    {
        Margin TopMargin { get; set; }
        Margin LeftMargin { get; set; }
        Margin RightMargin { get; set; }
        Margin BottomMargin { get; set; }
    }

    public class Margin : BoundingBox
    {

    }
}
