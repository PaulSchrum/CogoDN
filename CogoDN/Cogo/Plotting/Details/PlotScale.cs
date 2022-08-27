using System;
using System.Collections.Generic;
using System.Text;

namespace Cogo.Plotting.Details
{
    public class PlotScale
    {
        public DecimalUnits LeftSide { get; set; }
        public DecimalUnits RightSide { get; set; }
        public decimal VerticalExaggeration { get; set; }

        public PlotScale(DecimalUnits leftSide, DecimalUnits rightSide)
        {
            LeftSide = leftSide;
            RightSide = rightSide;
            VerticalExaggeration = 1m;
        }

        public PlotScale(DecimalUnits leftSide, DecimalUnits rightSide, 
            decimal verticalExaggeration) :
            this(leftSide, rightSide)
        {
            LeftSide = leftSide;
            RightSide = rightSide;
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
                return LeftSide.GetValueAs(pUnit.Pixel) / 
                    RightSide.GetValueAs(pUnit.Pixel);
            }
        }

        public decimal AsMultipleVertical
        {
            get
            {
                return AsMultiplierHorizontal / VerticalExaggeration;
            }
        }

        public override string ToString()
        {
            return $"{LeftSide}:{RightSide}";
        }
    }
}
