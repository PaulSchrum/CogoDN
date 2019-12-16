using System;
using System.Collections.Generic;
using System.Text;

namespace CadFoundation.Coordinates.Curvilinear
{
    public class Offset
    {
        public Offset(Offset other) { OFST = other.OFST; }
        public Offset(double newVal) { OFST = newVal; }

        public double OFST { get; set; }

        private static String formatString;
        public override string ToString()
        {
            formatString = "0.###";
            if (OFST < 0.0)
            {
                return String.Format((-OFST).ToString(formatString) + " LT");
            }
            else if (OFST > 0.0)
            {
                return String.Format(OFST.ToString(formatString) + " RT");
            }
            else
            {
                return "0.0";
            }
        }

        public static Offset operator +(Offset anEL, Double other) { return new Offset(anEL.OFST + other); }
        public static Offset operator -(Offset anEL, Double other) { return new Offset(anEL.OFST - other); }
        public static bool operator <(Offset anOffset, Double other) { return anOffset.OFST < other; }
        public static bool operator <(Double other, Offset anOffset) { return anOffset.OFST > other; }
        public static bool operator >(Offset anOffset, Double other) { return anOffset.OFST > other; }
        public static bool operator >(Double other, Offset anOffset) { return anOffset.OFST < other; }
        public static double operator -(Offset leftOfOperand, Offset rightOfOperand) { return leftOfOperand.OFST - rightOfOperand.OFST; }
        public static implicit operator Offset(double aDouble) { return new Offset(aDouble); }
        public static implicit operator double(Offset anEL) { return anEL.OFST; }
    }
}
