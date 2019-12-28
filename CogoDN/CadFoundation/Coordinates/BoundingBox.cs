using System;
using System.Collections.Generic;
using System.Text;

namespace CadFoundation.Coordinates
{
    [Serializable]
    public class BoundingBox
    {
        public Point lowerLeftPt { get; set; }
        public Point upperRightPt { get; set; }

        private BoundingBox() { }

        public BoundingBox(
             Double LLx, Double LLy,
             Double URx, Double URy,
             Double LLz = 0.0, Double URz = 0.0)
        {
            lowerLeftPt = new Point(LLx, LLy, LLz);
            upperRightPt = new Point(URx, URy, URz);
        }

        /// <summary>
        /// Returns a duplicate of the bounding box.
        /// </summary>
        /// <returns>BoundingBox</returns>
        public BoundingBox Duplicate()
        {
            var returnVal = new BoundingBox(this.lowerLeftPt.x, this.lowerLeftPt.y, 
                this.upperRightPt.x, this.upperRightPt.y);
            return returnVal;
        }

        public BoundingBox(Point aPoint)
        {
            lowerLeftPt = new Point(aPoint);
            upperRightPt = new Point(aPoint);
        }

        public void expandByPoint(Point aPoint)
        {
            expandByPoint(aPoint.x, aPoint.y, aPoint.z);
        }

        public void expandByPoint(Double x, Double y, Double z)
        {
            if (lowerLeftPt.isEmpty == true)
            {
                lowerLeftPt = new Point(x, y, z);
                upperRightPt = new Point(x, y, z);
            }
            else
            {
                if (x < lowerLeftPt.x)
                    lowerLeftPt.x = x;
                if (y < lowerLeftPt.y)
                    lowerLeftPt.y = y;
                if (z < lowerLeftPt.z)
                    lowerLeftPt.z = z;

                if (x > upperRightPt.x)
                    upperRightPt.x = x;
                if (y > upperRightPt.y)
                    upperRightPt.y = y;
                if (z > upperRightPt.z)
                    upperRightPt.z = z;
            }
            this.dimensions_ = null;
            this.center_ = null;
        }

        private Vector dimensions_ = null;
        private Vector Dimensions
        {
            get
            {
                if (dimensions_ is null)
                {
                    var dx = this.upperRightPt.x - this.lowerLeftPt.x;
                    var dy = this.upperRightPt.y - this.lowerLeftPt.y;
                    var dz = this.upperRightPt.z - this.lowerLeftPt.z;
                    this.dimensions_ = new Vector(dx, dy, dz);
                }
                return dimensions_;
            }
        }

        private Point center_ = null;
        public Point Center
        {
            get
            {
                if (center_ is null)
                {
                    var x2 = this.Dimensions.x / 2.0 + this.lowerLeftPt.x;
                    var y2 = this.Dimensions.y / 2.0 + this.lowerLeftPt.y;
                    var z2 = this.Dimensions.z / 2.0 + this.lowerLeftPt.z;
                    center_ = new Point(x2, y2, z2);
                }
                return center_;
            }
        }

        public double Width { get { return this.upperRightPt.x - this.lowerLeftPt.x; } }
        public double Depth { get { return this.upperRightPt.y - this.lowerLeftPt.y; } }
        public double Area { get { return this.Width * this.Depth; } }

        public bool isPointInsideBB2d(Double x, Double y)
        {
            if (x < lowerLeftPt.x)
                return false;
            if (y < lowerLeftPt.y)
                return false;

            if (x > upperRightPt.x)
                return false;
            if (y > upperRightPt.y)
                return false;

            return true;
        }

        public bool Overlaps(BoundingBox otherBB)
        { // from https://stackoverflow.com/a/20925869/1339950

            if (!(this.upperRightPt.x >= otherBB.lowerLeftPt.x
                && otherBB.upperRightPt.x >= this.lowerLeftPt.x))
                return false;

            if (!(this.upperRightPt.y >= otherBB.lowerLeftPt.y
                && otherBB.upperRightPt.y >= this.lowerLeftPt.y))
                return false;

            return true;
        }

        public bool isPointInsideBB2d(Point testPoint)
        {
            if (testPoint.x < lowerLeftPt.x)
                return false;
            if (testPoint.y < lowerLeftPt.y)
                return false;

            if (testPoint.x > upperRightPt.x)
                return false;
            if (testPoint.y > upperRightPt.y)
                return false;

            return true;
        }

        public bool isPointInsideBB3d(Point testPoint)
        {
            if (isPointInsideBB2d(testPoint) == false)
                return false;

            if (testPoint.z < lowerLeftPt.z)
                return false;

            if (testPoint.z > upperRightPt.z)
                return false;

            return true;
        }
    }
}
