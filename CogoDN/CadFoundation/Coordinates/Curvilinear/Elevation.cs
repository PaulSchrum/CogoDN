using System;
using System.Collections.Generic;
using System.Text;

namespace CadFoundation.Coordinates.Curvilinear
{
    public class Elevation
    {
        public Elevation(double newVal) { EL = newVal; }

        public double EL { get; set; }

        public override string ToString()
        {
            return String.Format(EL.ToString("0.000 FT"));
        }

        public static Elevation operator +(Elevation anEL, Double other) { return new Elevation(anEL.EL + other); }
        public static double operator -(Elevation leftOfOperand, Elevation rightOfOperand) { return leftOfOperand.EL - rightOfOperand.EL; }
        public static implicit operator Elevation(double aDouble) { return new Elevation(aDouble); }
        public static implicit operator double(Elevation anEL) { return anEL.EL; }

        public static implicit operator double?(Elevation anEL)
        {
            if (anEL == null)
                return null;
            else
                return anEL.EL;
        }
    }
}
