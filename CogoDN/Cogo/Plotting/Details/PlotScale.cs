using System;
using System.Collections.Generic;
using System.Text;

// This effort, Cogo.Plotting, is suspended indefinitely pending 
// any future need to restart it, which may never happen.
namespace Cogo.Plotting.Details
{
    public class PlotScale
    {
        public DecimalUnits WorldScale { get; set; }
        public DecimalUnits PanelScale { get; set; }
        public decimal VerticalExaggeration { get; set; }

        /// <summary>
        /// Left "worldScale" / "panelScale" refers to the plot ratio. And example would be
        /// 50 USfeet per inch or 100 m per centimeter. 100 m is the left side. 
        /// One centimeter is the right side.
        /// </summary>
        /// <param name="worldScale">World distance for the plot ratio.</param>
        /// <param name="panelScale">Panel (Paper) distance for the plot ratio.</param>
        public PlotScale(DecimalUnits worldScale, DecimalUnits panelScale)
        {
            WorldScale = worldScale;
            PanelScale = panelScale;
            VerticalExaggeration = 1m;
        }

        public PlotScale(DecimalUnits leftSide, DecimalUnits rightSide, 
            decimal verticalExaggeration) :
            this(leftSide, rightSide)
        {
            WorldScale = leftSide;
            PanelScale = rightSide;
            VerticalExaggeration = verticalExaggeration;
        }

        public PlotScale(DecimalUnits leftSide, DecimalUnits rightSide,
            double verticalExaggeration) :
            this(leftSide, rightSide, (decimal) verticalExaggeration)
        {

        }

        public decimal AsMultiplierHorizontal
        {
            get
            {
                return WorldScale.GetValueAs(pUnit.Pixel) / 
                    PanelScale.GetValueAs(pUnit.Pixel);
            }
        }

        public decimal AsMultiplierVertical
        {
            get
            {
                return AsMultiplierHorizontal / VerticalExaggeration;
            }
        }

        public override string ToString()
        {
            return $"{WorldScale}:{PanelScale}";
        }
    }
}
