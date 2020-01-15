using CadFoundation.Coordinates;
using System;
using System.Collections.Generic;
using System.Text;

namespace Surfaces.TIN
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>Class only works with indices. Calling code must have the
    /// point list and the triangle list that are being indexed into.
    /// </remarks>
    [Serializable]
    public class TINtriangleLine
    {
        public TINpoint firstPoint { get; set; }
        public TINpoint secondPoint { get; set; }
        internal TINtriangle oneTriangle { get; set; }
        internal TINtriangle theOtherTriangle { get; set; }
        public bool IsOnHull
        {
            get
            {
                return TriangleCount == 1 ? true : false;
            }
        }

        public void SetMyPointsOnHull()
        {
            if(this.IsOnHull)
            {
                this.firstPoint.isOnHull = true;
                this.secondPoint.isOnHull = true;
            }
        }

        internal TINtriangleLine(TINpoint pt1, TINpoint pt2, TINtriangle tngle)
        {
            firstPoint = pt1;
            secondPoint = pt2;
            oneTriangle = tngle;
            theOtherTriangle = null;
        }

        public bool isSameAs(double x1, double y1, double x2)
        {
            if (Math.Truncate(this.firstPoint.x) != Math.Truncate(x1) &&
                Math.Truncate(this.secondPoint.x) != Math.Truncate(x1))
                return false;

            if (Math.Truncate(this.firstPoint.y) != Math.Truncate(y1) &&
                Math.Truncate(this.secondPoint.y) != Math.Truncate(y1))
                return false;

            if (Math.Truncate(this.firstPoint.x) != Math.Truncate(x2) &&
                Math.Truncate(this.secondPoint.x) != Math.Truncate(x2))
                return false;

            return true;
        }

        internal TINtriangle GetOtherTriangle(TINtriangle currentTriangle)
        {
            if (oneTriangle == currentTriangle)
                return theOtherTriangle;
            return oneTriangle;
        }

        public bool IsValid
        {
            get { return FirstAvailableTriangle.IsValid || theOtherTriangle.IsValid; }
        }

        private BoundingBox boundingBox_ = null;
        public BoundingBox BoundingBox
        {
            get
            {
                if (boundingBox_ is null)
                {
                    Point pt = new Point(this.firstPoint.x, this.firstPoint.y);
                    boundingBox_ = new BoundingBox(pt);
                    pt = new Point(this.secondPoint.x, this.secondPoint.y);
                    boundingBox_.expandByPoint(pt);
                }
                return boundingBox_;
            }
        }

        internal TINtriangle FirstAvailableTriangle
        {
            get
            {
                if (this.oneTriangle.IsValid)
                    return oneTriangle;

                if (!(this.theOtherTriangle is null)
                        && this.theOtherTriangle.IsValid)
                    return this.theOtherTriangle;

                return null;
            }
        }

        public int TriangleCount
        {
            get
            {
                int retVal = 2;
                if (this.theOtherTriangle is null ||
                    !this.theOtherTriangle.IsValid)
                    retVal--;

                if (this.oneTriangle is null ||
                    !this.oneTriangle.IsValid)
                    retVal--;


                return retVal;
            }
        }

        public double Length2d
        {
            get 
            {
                return (BoundingBox.upperRightPt - BoundingBox.lowerLeftPt).flattenZnew().Length; 
            }
        }

        public double? DeltaCrossSlopeAsAngleRad 
        { 
            get
            {
                if (this.oneTriangle == null || this.theOtherTriangle == null)
                    return null;

                var triangle1Normal = this.oneTriangle.normalVec;
                var triangle2Normal = this.theOtherTriangle.normalVec;
                var result = Math.Acos(triangle1Normal.dotProduct(triangle2Normal)
                    / (triangle1Normal.Length * triangle2Normal.Length)
                    );
                if (Double.IsNaN(result))
                    return 0.0;
                return result;
            }
        }

        internal static triangleLineComparer compr = new triangleLineComparer();
    }

    internal class triangleLineComparer : IEqualityComparer<TINtriangleLine>
    {
        public bool Equals(TINtriangleLine x, TINtriangleLine y)
        {
            return (x.firstPoint.myIndex == y.firstPoint.myIndex &&
                x.secondPoint.myIndex == y.secondPoint.myIndex);
        }

        public int GetHashCode(TINtriangleLine obj)
        {
            return obj.firstPoint.myIndex - obj.secondPoint.myIndex
                + obj.firstPoint.myIndex % 101;
        }
    }
}
