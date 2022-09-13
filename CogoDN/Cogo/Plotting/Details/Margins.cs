using CadFoundation.Coordinates;
using System;
using System.Collections.Generic;
using System.Text;

// This effort, Cogo.Plotting, is suspended indefinitely pending 
// any future need to restart it, which may never happen.
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
