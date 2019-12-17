using System;
using System.Collections.Generic;
using System.Text;

namespace Cogo
{
    public class Side
    {
        public Side() { }
        public Side(rm21RightLeftSide rightLeftSide, rm21InOutSide inOutSide)
        {
            this.RLside = rightLeftSide;
            this.InOutside = inOutSide;
        }  // inOutside means "inside or outside"

        public Side(int leftOrRightSide, int inOrOutSide)
        {
            if (leftOrRightSide > 0)
                this.RLside = rm21RightLeftSide.Right;
            else
                this.RLside = rm21RightLeftSide.Left;

            if (inOrOutSide > 0)
                this.InOutside = rm21InOutSide.Outside;
            else
                this.InOutside = rm21InOutSide.Inside;
        }

        public rm21RightLeftSide RLside { get; set; }
        public rm21InOutSide InOutside { get; set; }

        public int Sign()
        {
            return (int)RLside * (int)InOutside;
        }

        public override string ToString()
        {
            String InOut; String LeftRight;
            InOut = InOutside == rm21InOutSide.Inside ? "Inside " : "Outside ";
            LeftRight = RLside == rm21RightLeftSide.Left ? "Left" : "Right";
            return InOut + LeftRight + "(" + this.Sign() + ")";
        }
    }

    public static class rm21SideHelper
    {
        public static void toggle(rm21RightLeftSide s)
        {
            if (s == rm21RightLeftSide.Left)
                s = rm21RightLeftSide.Right;
            else
                s = rm21RightLeftSide.Left;
        }
    }

    public enum rm21RightLeftSide
    {
        Left = -1,
        Right = 1
    }

    public enum rm21InOutSide
    {
        Inside = -1,
        Outside = 1
    }
}
