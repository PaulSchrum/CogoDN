using System;
using System.Collections.Generic;
using System.Text;
using CadFoundation.Angles;

namespace CadFoundation.Coordinates
{
    [Serializable]
    public class Vector
    {
        public double x { get; set; }
        public double y { get; set; }
        public double z { get; set; }

        public Vector() { }
        public Vector(double x_, double y_, double z_ = 0.0)
        {
            x = x_; y = y_; z = z_;
        }

        public Vector(Point beginPt, Point endPoint)
        {
            x = endPoint.x - beginPt.x;
            y = endPoint.y - beginPt.y;
            z = endPoint.z - beginPt.z;
        }

        public Vector(Azimuth direction, Double length)
        {
            x = length * Math.Sin(direction.angle_);
            y = length * Math.Cos(direction.angle_);
            z = 0.0;
        }

        public Vector(Ray startRay, double length) :
            this(startRay.HorizontalDirection, length)
        { }

        public void flattenThisZ()
        {
            this.z = 0.0;
        }

        public Vector flattenZnew()
        {
            return new Vector(this.x, this.y, 0.0);
        }

        public Point plus(Point aPoint)
        {
            if (aPoint.isEmpty)
            {
                return new Point();
            }

            return new Point(aPoint.x + this.x, aPoint.y + this.y, aPoint.z + this.z);
        }

        /// <summary>
        /// In polar coordinates, phi, with due north = 0, rotating
        /// clockwise, so due east is 90 degrees.
        /// </summary>
        public Azimuth Azimuth
        {
            get
            {
                return new Azimuth(Math.Atan2(y, x));
            }
            private set { }
        }

        public Double Length
        {
            get { return Math.Sqrt(x * x + y * y + z * z); }
            private set { }
        }

        /// <summary>
        /// Same thing as Azimuth. In polar coordinates, phi.
        /// </summary>
        public Azimuth DirectionHorizontal
        {
            get { return new Azimuth(Math.Atan2(y, x)); }
            private set { }
        }

        private double? baseLength_ = null;
        public double BaseLength
        {
            get
            {
                baseLength_ ??= Math.Sqrt(x * x + y * y);
                return (double) baseLength_;
            }
        }

        /// <summary>
        /// The slope of the base of this vector if it is a normal vector.
        /// </summary>
        public Slope NormalSlope
        {
            get
            {
                return new Slope(z, BaseLength);
            }
        }

        /// <summary>
        /// Theta in polar notation. 0 is straight up. pi/2 is horizontal.
        /// pi is straight down.
        /// </summary>
        public Angle Theta
        {
            get
            {
                var val = Math.Atan2(BaseLength, z);
                return new Angle(Math.Atan2(BaseLength, z));
            }
        }

        /// <summary>
        /// Computes the angle 
        /// </summary>
        /// <param name="otherVec"></param>
        /// <returns></returns>
        public double AngleBetween(Vector otherVec)
        {
            if (otherVec.Length == 0.0 || this.Length == 0.0)
                throw new DivideByZeroException("Vector length must be nonzero.");

            var dotProduct = this.dotProduct(otherVec);
            return Math.Acos(dotProduct / (this.Length * otherVec.Length));
        }

        public double dotProduct(Vector otherVec)
        {
            return (this.x * otherVec.x) + (this.y * otherVec.y) + (this.z * otherVec.z);
        }

        public Vector crossProduct(Vector otherVec)
        {
            Vector newVec = new Vector();
            newVec.x = this.y * otherVec.z - this.z * otherVec.y;
            newVec.y = this.z * otherVec.x - this.x * otherVec.z;
            newVec.z = this.x * otherVec.y - this.y * otherVec.x;
            return newVec;
        }

        public Vector right90degrees()
        {
            var az = this.Azimuth + Angle.HALFCIRCLE / 2.0; ;
            return new Vector(az, this.Length);
        }

        public Vector left90degrees()
        {
            var az = this.Azimuth + Angle.HALFCIRCLE * 1.5;
            return new Vector(az, this.Length);
        }

        public static Vector operator +(Vector vec1, Deflection defl)
        {
            Azimuth newAz = vec1.Azimuth + defl;
            Vector newVec = new Vector(newAz, vec1.Length);
            newVec.z = vec1.z;

            return newVec;
        }

        public static Vector operator +(Vector vec1, Vector vec2)
        {
            Vector newVec = new Vector();
            newVec.x = vec1.x + vec2.x;
            newVec.y = vec1.y + vec2.y;
            newVec.z = vec1.z + vec2.z;

            return newVec;
        }

        public static Vector operator *(Vector vec, double dbl)
        {
            Vector newVec = new Vector();
            newVec.x = vec.x * dbl;
            newVec.y = vec.y * dbl;
            newVec.z = vec.z * dbl;

            return newVec;
        }

        public override String ToString()
        {
            return 
                $"L: {Length:#.0000}, Az: {Azimuth:0.00}°, θ: {Theta.getAsDegreesDouble():0.00}°";
        }


    }
}
