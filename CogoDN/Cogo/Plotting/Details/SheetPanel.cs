using CadFoundation.Coordinates;
using System;
using System.Collections.Generic;
using System.Text;

// This effort, Cogo.Plotting, is suspended indefinitely pending 
// any future need to restart it, which may never happen.
namespace Cogo.Plotting.Details
{
    public class SheetPanel : BoundingBox
    {
        public SheetPanel Parent { get; private set; }
        protected DockLocation DockLocation = DockLocation.None;

        public SheetPanel(SheetPanel parent, DockLocation dockLocation=DockLocation.None, 
            double originX=0d, double originY=0d, double width=-1d, double height=-1d)
            : base(originX, originY, originX + width, originY + height)
        {
            this.Parent = parent;
            this.DockLocation = dockLocation;
        }

    }

    public enum DockLocation
    {
        None,
        Left,
        Top,
        Right,
        Bottom
    }
}
