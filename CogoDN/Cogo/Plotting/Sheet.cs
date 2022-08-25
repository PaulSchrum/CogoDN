using CadFoundation.Coordinates;
using Cogo.Plotting.Details;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cogo.Plotting
{
    public abstract class Sheet : BoundingBox
    {
        protected Margins Margins { get; set; }
        //protected PlotArea PlotArea { get; private set; }
    }
}
