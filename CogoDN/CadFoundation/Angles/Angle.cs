﻿using CadFoundation.Coordinates;
using System;
using System.Collections.Generic;
using System.Text;

namespace CadFoundation.Angles
{
    [Serializable]
    public class Angle
    {
        protected double angle__;

        public static Angle QUARTERCIRCLE
        {
            get { return new Angle(Math.PI / 2.0); }
        }

        public static Angle HALFCIRCLE
        {
            get { return new Angle(Math.PI); }
            private set { }
        }

        public static Angle DEGREE
        {
            get { return new Angle(Math.PI / 180.0); }
            private set { }
        }

        public static Angle RADIAN
        {
            get { return new Angle(1.0); }
            private set { }
        }


        public Angle() { }

        public Angle(Double valueAsRadians)
        {
            angle_ = valueAsRadians;
        }

        public Angle(double radius, double degreeOfCurveLength)
        {
            angle__ = degreeOfCurveLength / radius;
        }
        internal virtual double angle_ { get { return angle__; } set { normalize(value); } }
        //private static double angleScratchPad;

        public virtual double getAsRadians() { return angle_; }

        public virtual double getAsDegreesDouble()
        {
            return 180.0 * angle_ / Math.PI;
        }

        public virtual DegreeOfCurve getAsDegrees()
        {
            return DegreeOfCurve.newFromRadians(angle_);
        }

        public virtual void setFromDegreesDouble(double degrees)
        {
            angle_ = Math.PI * degrees / 180.0;
        }

        public static double radiansFromDegree(double degrees)
        {
            return Math.PI * degrees / 180.0;
        }

        public static double degreesFromRadians(double radians)
        {
            return 180.0 * radians / Math.PI;
        }

        public virtual void setFromDegreesMinutesSeconds(int degrees, int minutes, double seconds)
        {
            setFromDegreesDouble(
                  (double)degrees + (double)minutes / 60.0 + seconds / 3600.0
                           );
        }

        public void setFromDMSstring(string angleInDMS)
        {
            throw new NotImplementedException();
        }

        public void setFromXY(double x, double y)
        {
            double dbl = Math.Atan2(y, x);
            angle_ = dbl;
            //angle_ = Math.Atan2(y, x);
        }

        protected double fp(double val)
        {
            if (val > 0.0)
                return val - Math.Floor(val);
            else if (val < 0.0)
                return -1.0 * (val - Math.Floor(val));
            else
                return 0.0;
        }

        public static explicit operator Angle(DegreeOfCurve deg)
        {
            double rads = deg.getAsRadians();
            var angl = new Angle(rads);
            return angl;
        }

        public static bool operator >(Angle left, Angle right)
        {
            return left.angle_ > right.angle_;
        }

        public static bool operator <(Angle left, Angle right)
        {
            return left.angle_ < right.angle_;
        }

        protected void normalize(double anAngle)
        {
            //angleScratchPad = anAngle / Math.PI;
            //angle__ = fp(angleScratchPad) * Math.PI;
            double oldAngle = anAngle * 180 / Math.PI;

            // approach to normalize #1, probably too slow
            angle__ = Math.Atan2(Math.Sin(anAngle), Math.Cos(anAngle));

            // approach to normalizae #2, can't get it to work -- will one day
            //int sign = Math.Sign(anAngle);
            //angleScratchPad = (anAngle * sign) / Math.PI;
            //angle__ = (fp(angleScratchPad) * sign) * Math.PI;
        }

        protected void normalizeToPlusOrMinus2Pi(Double anAngle)
        {
            Double TwoPi = Math.PI * 2.0;
            angle__ = Angle.ComputeRemainderScaledByDenominator(anAngle, TwoPi);
        }

        public static Double normalizeToPlusOrMinus2PiStatic(Double anAngle)
        {
            return ComputeRemainderScaledByDenominator(anAngle, 2 * Math.PI);
        }

        public static Double normalizeToPlusOrMinus360Static(Double val)
        {
            return ComputeRemainderScaledByDenominator(val, 360.0);
        }

        public static Double ComputeRemainderScaledByDenominator(Double numerator, double denominator)
        {
            Double sgn = Math.Sign(numerator);
            Double ratio = numerator / denominator;
            ratio = Math.Abs(ratio);
            Double fractionPart;
            fractionPart = 1 + ratio - Math.Round(ratio, MidpointRounding.AwayFromZero);
            if (sgn < 0.0)
            {
                fractionPart = fractionPart - 2;
                if (fractionPart < 1.0)
                    fractionPart = -1.0 * (fractionPart + 2);
                //1.0 + ratio - Math.Round(ratio, MidpointRounding.AwayFromZero);
            }

            Double returnDouble = sgn * Math.Abs(fractionPart) * Math.Abs(denominator);
            return returnDouble;
        }

        public static double normalizeToCustomRange(double inputVal, double minVal, double maxVal)
        {
            if (inputVal >= minVal && inputVal <= maxVal)
                return inputVal;

            double zeroBase = 0.0 - minVal;
            double realMin = minVal + zeroBase;
            double realMax = maxVal + zeroBase;
            double realInput = inputVal + zeroBase;
            int inSign = Math.Sign(realInput);

            double scratchNumber = realInput / realMax;
            double fp = 0.0;
            if (realInput > realMax)
            {
                fp = scratchNumber - Math.Truncate(scratchNumber);
            }
            else if (realInput < realMin)
            {
                scratchNumber *= -1.0;
                fp = scratchNumber - Math.Truncate(scratchNumber);
                fp *= -1.0;
                fp += 1.0;
            }

            fp = (fp * realMax)
                - zeroBase;

            return fp;
        }

        public override string ToString()
        {
            return (angle__ * 180 / Math.PI).ToString();
        }

        public static Angle operator +(Angle angle1, Angle angle2)
        {
            var retVal = new Angle();
            retVal.normalize(angle1.angle__ + angle2.angle__);
            return retVal;
        }

        public static Angle operator -(Angle angle1, Angle angle2)
        {
            var retVal = new Angle();
            retVal.normalize(angle1.angle__ - angle2.angle__);
            return retVal;
        }

        public static Angle operator *(Angle angl, double multiplier)
        {
            return new Angle(angl.angle__ * multiplier);
        }

        public static Angle operator /(Angle angle, double divisor)
        {
            return new Angle(angle.angle__ / divisor);
        }

        public static bool operator !=(Angle first, Angle second)
        {
            return first.angle_ != second.angle_;
        }

        public static bool operator ==(Angle first, Angle second)
        {
            return first.angle_ == second.angle_;
        }

        public Angle multiply(Double multiplier)
        {
            return new Angle(this.angle__ * multiplier);
        }

        // operator overloads
        public static implicit operator Angle(double angleAs_double)
        {
            Angle anAngle = new Angle();
            anAngle.angle_ = angleAs_double;
            return anAngle;
        }

        public static implicit operator Angle(Vector angleAs_vector)
        {
            Angle anAngle = new Angle();
            anAngle.angle__ = Math.Atan2(angleAs_vector.y, angleAs_vector.x);
            return anAngle;
        }

        public override bool Equals(object obj)
        {
            var other = obj as Angle;
            bool areEqual = utilFunctions.tolerantCompare(this.angle_, other.angle_, 0.00001) == 0;
            return areEqual;
        }
    }

    public static class angleExtensions
    {
        public static Angle DegreesToAngle(this Double degrees)
        {
            return Angle.DEGREE * degrees;
        }
    }
}
