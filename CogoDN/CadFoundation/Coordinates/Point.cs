using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CadFoundation.Coordinates
{
    [Serializable]
    public class Point
    {
        protected double x_; protected double y_; protected double z_;
        protected bool isEmpty_;

        public double x { get { return x_; } set { x_ = value; } }
        public double y { get { return y_; } set { y_ = value; } }
        public double z { get { return z_; } set { z_ = value; } }
        public bool isEmpty { get { return isEmpty_; } }

        public Point() { isEmpty_ = true; }

        public Point(Point otherPt)
        {
            isEmpty_ = false;
            x = otherPt.x; y = otherPt.y; z = otherPt.z;
        }

        public Point(double X, double Y, double Z)
        {
            isEmpty_ = false;
            x = X; y = Y; z = Z;
        }

        public Point(String X, String Y, String Z = null)
        {
            x = Double.Parse(X);
            y = Double.Parse(Y);
            z = (Z == null) ? 0.0 : Double.Parse(Z);
            isEmpty_ = false;
        }

        public Point(double X, double Y)
           : this(X, Y, 0.0)
        {

        }

        public Vector minus2d(Point p1)
        {
            return new Vector(p1.x - this.x, p1.y - this.y);
        }

        public static Vector operator -(Point p1, Point p2)
        {
            return new Vector(p2.x - p1.x, p2.y - p1.y, p2.z - p1.z);
        }

        public static Point operator +(Point point, Vector vector)
        {
            // dont know why I did this:  return new ptsPoint(point.x + vector.y, point.y + vector.x, point.z + vector.z);
            return new Point(point.x + vector.x, point.y + vector.y, point.z + vector.z);
        }

        public static Point operator -(Point point, Vector vector)
        {
            return new Point(point.x - vector.x, point.y - vector.y, point.z - vector.z);
        }

        public Double GetHorizontalDistanceTo(Point other)
        {
            Double dx = other.x - this.x;
            Double dy = other.y - this.y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public int compareByXthenY(Point other)
        {
            int xComp; int yComp;
            xComp = this.x_.CompareTo(other.x_);
            if (xComp == 0)
            {
                yComp = this.y_.CompareTo(other.y_);
                return yComp;
            }
            return xComp;
        }

        public int compareByYthenX(Point other)
        {
            int xComp; int yComp;
            yComp = this.y_.CompareTo(other.y_);
            if (yComp == 0)
            {
                xComp = this.x_.CompareTo(other.x_);
                return xComp;
            }
            return yComp;
        }

        public override string ToString()
        {
            return x.ToString(NumberFormatInfo.InvariantInfo) + ", " + y.ToString(NumberFormatInfo.InvariantInfo) + ", " +
               z.ToString(NumberFormatInfo.InvariantInfo);
        }

        public string ToStringSpaceDelimited()
        {
            return x.ToString(NumberFormatInfo.InvariantInfo) + " " + y.ToString(NumberFormatInfo.InvariantInfo) + " " +
               z.ToString(NumberFormatInfo.InvariantInfo);
        }

        public static double VerticalEqualsTolerance { get; set; } = 0.0025;

        /// <summary>
        /// Unless you overide the value, horizontalTolerance is 0.0025. Override it
        /// by calling ptsPoint.HorizontalEqualsTolerance.Push(myTolerance);
        /// then after you are done, restore it to its original value by calling
        /// ptsPoint.HorizontalEqualsTolerance.Pop();
        /// If you are okay with the default value, there is no need to call Push
        /// or Pop.
        /// </summary>
        public static Stack<double> HorizontalEqualsTolerance { get; set; } =
            new Stack<double>(new double[] { 0.006 });
        public override bool Equals(object obj)
        {
            var other = obj as Point;

            if (utilFunctions.tolerantCompare(this.z, other.z, VerticalEqualsTolerance) != 0)
                return false;

            return
                utilFunctions.tolerantCompare(
                    this.GetHorizontalDistanceTo(other), 0.0,
                    HorizontalEqualsTolerance.Peek())
                == 0;

        }

    }
}
