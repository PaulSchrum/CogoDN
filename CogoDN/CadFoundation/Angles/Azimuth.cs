using CadFoundation.Coordinates;
using System;
using System.Collections.Generic;
using System.Text;

namespace CadFoundation.Angles
{
    sealed public class Azimuth : Angle
    {
        public Azimuth() { }

        /// <summary>
        /// Ctor interprets double value as radians.
        /// </summary>
        /// <param name="anAngleDbl">Initialization value in radians.</param>
        public Azimuth(double anAngleDbl)
        {
            angle_ = anAngleDbl;
        }

        public Azimuth(DegreeOfCurve deg)
        {
            this.angle__ = Math.Atan2(DegreeOfCurve.Sin(deg), DegreeOfCurve.Cos(deg));
        }

        public int quadrant
        {
            get
            {
                double quad = 2.0 * angle_ / Math.PI;
                if (quad < 0.0)
                    quad = 3.0;
                return (int)quad + 1;
            }
        }

        public Azimuth(Point beginPt, Point endPt)
        {
            this.angle__ = Math.Atan2(endPt.y - beginPt.y, endPt.x - beginPt.x);
        }

        public Azimuth(string azStr, bool IsDegree = true)
        {
            double az = Convert.ToDouble(azStr);
            if (IsDegree)
                this.setFromDegreesDouble(az);
            else
                angle_ = az;
        }

        public new double angle_ { get { return getAsAzimuth(); } set { base.normalize(value); } }

        public Azimuth reverse()
        {
            return new Azimuth(this.angle__ + Math.PI);
        }

        //public override void setFromXY(double x, double y)
        //{
        //   double dbl = Math.Atan2(x, y);
        //   angle_ = dbl;
        //}

        private double getAsAzimuth()
        {
            double retVal;

            retVal = (-1.0 * base.angle_) + (Math.PI / 2.0);

            return retVal;
        }

        public override double getAsDegreesDouble()
        {
            double retValueDbl = getAsAzimuth() * 180 / Math.PI;
            return retValueDbl >= 0.0 ? retValueDbl : retValueDbl + 360.0;
        }

        public override DegreeOfCurve getAsDegrees()
        {
            DegreeOfCurve retValueDeg = getAsAzimuth() * 180 / Math.PI;
            return retValueDeg >= 0.0 ? retValueDeg : retValueDeg + 360.0;
        }

        public override void setFromDegreesDouble(double degrees)
        {
            //double adjustedDegrees = ((degrees / -180.0)+ 1) *180.0;
            var normalizedDegrees = Angle.normalizeToPlusOrMinus360Static(degrees);
            var radians = normalizedDegrees * Math.PI / 180.0;
            angle_ = Math.Atan2(Math.Cos(radians), Math.Sin(radians));  // This is flipped intentionally

        }

        public static Azimuth fromDegreesDouble(double degrees)
        {
            Azimuth retAz = new Azimuth();
            retAz.setFromDegreesDouble(degrees);
            return retAz;
        }

        public override void setFromDegreesMinutesSeconds(int degrees, int minutes, double seconds)
        {
            setFromDegreesDouble(
                  (double)degrees + (double)minutes / 60.0 + seconds / 3600.0
                           );
        }

        public static int getQuadrant(double angleDegrees)
        {
            return (int)Math.Round((angleDegrees / 90.0) + 0.5);
        }

        public Vector AsUnitVector
        {
            get
            {
                return new Vector(this, 1.0);
            }
        }

        //to do:
        //setAsAzimuth
        //getAsDegreeMinuteSecond
        //setAsDegree
        //setAsDegreeMinuteSecond
        //yada

        /// <summary>
        /// Create a new Azimuth 90 degree perpandicular to the 
        /// </summary>
        /// <param name="multiplier">-1.0 for the left Normal</param>
        /// <returns></returns>
        public Azimuth RightNormal(double multiplier = 1.0)
        {
            return newAzimuthFromAngle(this + Angle.HALFCIRCLE * multiplier / 2.0);
        }

        public static Azimuth newAzimuthFromAngle(Angle angle)
        {
            Azimuth retAz = new Azimuth();
            retAz.setFromDegreesDouble(angle.getAsDegreesDouble());
            return retAz;
        }

        // operator overloads
        public static implicit operator Azimuth(double angleAs_double)
        {
            Azimuth anAzimuth = new Azimuth();
            anAzimuth.setFromDegreesDouble(angleAs_double);
            return anAzimuth;
        }

        public static Azimuth operator +(Azimuth anAz, Angle anAngle)
        {
            return new Azimuth(anAz.getAsRadians() - anAngle.getAsRadians());  // Note: Subtraction is intentional since azimuths are clockwise
        }

        /// <summary>
        /// Gets the difference between two directions accounting for crossing 360.
        /// </summary>
        /// <param name="Az1"></param>
        /// <param name="Az2"></param>
        /// <returns>Difference in radians.</returns>
        public static double operator -(Azimuth Az1, Azimuth Az2)
        {
            return Az2.minus(Az1).getAsRadians();
        }

        public static Azimuth operator +(Azimuth Az1, Deflection defl)
        {
            var newAzDeg = Az1.getAsDegreesDouble() + defl.getAsDegreesDouble();
            Double retDbl = Angle.normalizeToPlusOrMinus360Static(newAzDeg);
            Azimuth retAz = new Azimuth();
            retAz.setFromDegreesDouble(retDbl);
            return retAz;
        }

        public Deflection minus(Azimuth Az2)
        {
            Double returnDeflection = (this.angle_ - Az2.angle_);

            var contraryQuadrants = this.quadrant == 4 && Az2.quadrant != 4 ||
                this.quadrant != 4 && Az2.quadrant == 4;


            var normalizedDeflection = Angle.normalizeToPlusOrMinus2PiStatic(returnDeflection);
            if (contraryQuadrants) normalizedDeflection -= 2 * Math.PI;
            return new Deflection(normalizedDeflection);
        }

        public override String ToString()
        {
            return String.Format("{0:0.0000}°", this.getAsDegreesDouble());
        }

        public static double MAX_AZIMUTH_RAD
        {
            get { return 2.0 * Math.PI; }
            private set { }
        }

        public static double MIN_AZIMUTH_RAD
        {
            get { return 0.0; }
            private set { }
        }

        public static Azimuth NORTH
        { get { return Azimuth.fromDegreesDouble(0d); } }

        public static Azimuth EAST
        { get { return Azimuth.fromDegreesDouble(90d); } }

        public static Azimuth SOUTH
        { get { return Azimuth.fromDegreesDouble(180d); } }

        public static Azimuth WEST
        { get { return Azimuth.fromDegreesDouble(270d); } }

    }

    public static class extendDoubleForAzimuth
    {
        public static Azimuth AsAzimuth(this Double val)
        {
            return new Azimuth(val);
        }
    }

}
