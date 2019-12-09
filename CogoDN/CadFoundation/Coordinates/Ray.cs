using System;
using System.Collections.Generic;
using System.Text;
using CadFoundation.Angles;

namespace CadFoundation.Coordinates
{
    public class Ray
    {
        public Ray() { }

        public Ray(Point startPt, Azimuth directionAZ, Slope slope = null)
        {
            this.StartPoint = startPt;
            this.HorizontalDirection = directionAZ;
            this.advanceDirection = 1;
            this.Slope = slope;
        }

        public Ray(double startX, double startY, double azimuthDegrees, Slope slope = null) :
            this(new Point(startX, startY), Azimuth.fromDegreesDouble(azimuthDegrees), slope)
        {

        }

        public Ray(double startX, double startY, double endX, double endY, Slope slope = null)
        {
            this.StartPoint = new Point(startX, startY);
            var endPoint = new Point(endX, endY);
            this.HorizontalDirection = new Azimuth(this.StartPoint, endPoint);
            this.advanceDirection = 1;
            this.Slope = slope;
        }

        public Ray(Point startPt, Point secondPt, Slope slope = null) :
            this(startPt.x, startPt.y, secondPt.x, secondPt.y, slope)
        {
            var dx = secondPt.x - startPt.x;
            var dy = secondPt.y - startPt.y;
            var run = Math.Sqrt(dx * dx + dy * dy);
            this.Slope = new Slope(run: run, rise: dy);
        }


        public Ray(string x, string y, string z = null, string azimuth = null, string slope = null)
        {
            this.StartPoint = new Point(x, y, z);
            this.advanceDirection = 1;
            this.HorizontalDirection = new Azimuth(azimuth);
            this.Slope = new Slope(slope);
        }

        public Point StartPoint { get; set; }
        public Slope Slope { get; set; }
        private int advanceDirection_ = 1;
        public int advanceDirection
        {
            get { return advanceDirection_; }
            set
            {
                advanceDirection_ = Math.Sign(value);
                if (0 == value) advanceDirection_ = 1;
            }
        }

        public Azimuth HorizontalDirection { get; set; }

        public double? getElevationAlong(double X)
        {
            if (true == Slope.isVertical())
                return null;

            double horizDistance = X - StartPoint.x;

            if (Math.Sign(horizDistance) != Math.Sign(Slope.getAsSlope()))
                return null;

            return (double?)
               ((horizDistance * Slope.getAsSlope()) + this.StartPoint.z);

        }

        public double get_m() { return this.Slope.getAsSlope() * this.advanceDirection; }
        public double get_b()
        {
            if (true == Slope.isVertical())
                return Double.NaN;

            return this.StartPoint.z - (StartPoint.x * Slope.getAsSlope() * this.advanceDirection);
        }

        public Point IntersectWith_2D(Ray otherRay)
        {
            if (this.HorizontalDirection == otherRay.HorizontalDirection)
            {
                double offset = this.getOffset(otherRay.StartPoint);
                if (offset.tolerantEquals(0.0, 0.00001))
                    throw new Exception(
                        "Two rays are colinear. They intersect at all points.");
                else
                    throw new Exception(
                        "Two rays with identical horizontal direction never intersect.");
            }

            // Solution is from https://stackoverflow.com/a/2932601/1339950 .
            // But he refers to this: https://stackoverflow.com/a/2931703/1339950
            var ad = this.HorizontalDirection.AsUnitVector;
            var bd = otherRay.HorizontalDirection.AsUnitVector;
            var as_x = this.StartPoint.x; var as_y = this.StartPoint.y;
            var bs_x = otherRay.StartPoint.x; var bs_y = otherRay.StartPoint.y;

            // Transform points to small values.
            var xTransform = Math.Min(as_x, bs_x);
            as_x -= xTransform; bs_x -= xTransform;
            var yTransform = Math.Min(as_y, bs_y);
            as_y -= yTransform; bs_y -= yTransform;

            var dx = bs_x - as_x;
            var dy = bs_y - as_y;
            var determinate = bd.x * ad.y - bd.y * ad.x;
            var U = (dy * bd.x - dx * bd.y) / determinate;
            var V = (dy * ad.x - dx * ad.y) / determinate;

            var newX = as_x + ad.x * U;
            var newY = as_y + ad.y * U;

            var newPt = new Point(newX + xTransform, newY + yTransform);
            Azimuth directionStartToNewPt = (this.StartPoint - newPt).Azimuth;
            Deflection defl = this.HorizontalDirection - directionStartToNewPt;
            if (Math.Abs(defl.getAsRadians()) > 0.001)
                throw new IsOnBackSideOfRayException(newPt);
            // intersection is off the ray, opposite direction.

            return newPt;
        }

        public bool isWithinDomain(double testX)
        {
            if (true == Slope.isVertical())
                return (testX == this.StartPoint.x);

            int sign = Math.Sign(testX - this.StartPoint.x);

            if (Math.Sign(testX - this.StartPoint.x) == this.advanceDirection)
                return true;

            return false;
        }

        public double getOffset(Point endPt)
        {
            Vector directVectr = endPt - this.StartPoint;
            Angle alpha = this.HorizontalDirection - directVectr;
            Double offset = -1.0 * directVectr.Length * Math.Sin(alpha.getAsRadians());
            return offset;
        }

        public override bool Equals(object obj)
        {
            Ray other = obj as Ray;

            bool pointEqual = this.StartPoint.Equals(other.StartPoint);
            if (!pointEqual) return false;

            bool horDirectionEqual = this.HorizontalDirection.Equals(other.HorizontalDirection);
            if (!horDirectionEqual) return false;

            if (this.Slope is null && other.Slope is null)
                return true;

            if (null == this.Slope ^ null == other.Slope) return false;

            if (this.Slope != other.Slope) return false;

            return true;
        }

        public override string ToString()
        {
            return this.StartPoint.ToString() + " " + this.HorizontalDirection.ToString();
        }
    }

    public class IsOnBackSideOfRayException : Exception
    {
        public Point resultingPoint { get; set; } = null;

        private IsOnBackSideOfRayException() { }

        public IsOnBackSideOfRayException(Point thePoint)
        {
            resultingPoint = thePoint;
        }
    }
}
