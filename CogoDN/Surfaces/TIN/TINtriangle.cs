using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CadFoundation.Angles;
using CadFoundation.Coordinates;
using CadFoundation.Coordinates.Indexing;
using Cogo;

namespace Surfaces.TIN
{
    [Serializable]
    public class TINtriangle : IComparable, IBoxBounded
    {
        // temporary scratch pad members -- do not serialize
        [NonSerialized]
        private String[] indexStrings;

        // substantitve fields - Do Serialize
        public TINpoint point1 { get; set; }
        public TINpoint point2 { get; set; }
        public TINpoint point3 { get; set; }

        [NonSerialized]
        private Vector normalVec_;
        public Vector normalVec 
        { 
            get 
            {
                if (null == normalVec_)
                    setupNormalVec();
                return normalVec_; 
            } 
        }

        public double SlopeDouble
        {
            get
            {
                return
                    Math.Tan((Math.PI / 2.0) - normalVec.Theta.getAsRadians());
            }
        }

        [NonSerialized]
        private TINpoint centroid_ = null;
        public TINpoint Centroid
        {
            get
            {
                if (null == centroid_)
                    centroid_ = new TINpoint(
                        (point1.x + point2.x + point3.x) / 3.0,
                        (point1.y + point2.y + point3.y) / 3.0,
                        (point1.z + point2.z + point3.z) / 3.0
                        );
                return centroid_;
            }
        }

        public bool IsValid { get; set; } = true;

        private List<TINtriangleLine> lines { get; set; } =
            new List<TINtriangleLine>() { null, null, null };
        public TINtriangleLine myLine1
        {
            get { return this.lines[0]; }
            internal set { this.lines[0] = value; }
        }

        public TINtriangleLine myLine2
        {
            get { return this.lines[1]; }
            internal set { this.lines[1] = value; }
        }

        public TINtriangleLine myLine3
        {
            get { return this.lines[2]; }
            internal set { this.lines[2] = value; }
        }

        private void computeAngles2D()
        {
            Vector vec12 = new Vector(point2.x - point1.x, point2.y - point1.y);
            Vector vec23 = new Vector(point3.x - point2.x, point3.y - point2.y);
            Vector vec31 = new Vector(point1.x - point3.x, point1.y - point3.y);
            angle1_ = (new Deflection(vec31.Azimuth, vec12.Azimuth, false))
                .piCompliment.getAsDegreesDouble();
            angle2_ = new Deflection(vec12.Azimuth, vec23.Azimuth, false)
                .piCompliment.getAsDegreesDouble();
            angle3_ = new Deflection(vec23.Azimuth, vec31.Azimuth, false)
                .piCompliment.getAsDegreesDouble();

            angle1_ = Math.Abs((double)angle1_);
            angle2_ = Math.Abs((double)angle2_);
            angle3_ = Math.Abs((double)angle3_);
        }

        [NonSerialized]
        private double? angle1_ = null;
        public double angle1
        {
            get
            {
                if (this.angle1_ is null)
                    this.computeAngles2D();
                return (Double)this.angle1_;
            }
        }

        [NonSerialized]
        private double? angle2_ = null;
        public double angle2
        {
            get
            {
                if (this.angle2_ is null)
                    this.computeAngles2D();
                return (Double)this.angle2_;
            }
        }

        [NonSerialized]
        private double? angle3_ = null;
        public double angle3
        {
            get
            {
                if (this.angle3_ is null)
                    this.computeAngles2D();
                return (Double)this.angle3_;
            }
        }

        [NonSerialized]
        private double? area2d_ = null;
        public double Area2d
        {
            get
            {
                if(null == area2d_)
                {
                    Vector vec12 = new Vector(point2.x - point1.x, point2.y - point1.y);
                    Vector vec23 = new Vector(point3.x - point2.x, point3.y - point2.y);
                    area2d_ = vec12.crossProduct(vec23).Length / 2.0;
                }
                return (double)area2d_;
            }
        }

        // non-substantive fields
        [NonSerialized]
        private BoundingBox myBoundingBox_;
        public BoundingBox BoundingBox { get { return myBoundingBox_; } }

        public TINtriangle(List<TINpoint> pointList, string pointRefs)
        {
            int[] indices = new int[3];
            indexStrings = pointRefs.Split(' ');
            int.TryParse(indexStrings[0], out indices[0]);
            int.TryParse(indexStrings[1], out indices[1]);
            int.TryParse(indexStrings[2], out indices[2]);

            point1 = pointList[indices[0] - 1];
            point2 = pointList[indices[1] - 1];
            point3 = pointList[indices[2] - 1];

            computeBoundingBox();
            normalVec_ = null;
        }

        public static TINtriangle CreateTriangle(List<TINpoint> pointList, int ptIndex1,
           int ptIndex2, int ptIndex3)
        {
            if (ptIndex1 == ptIndex2 || ptIndex2 == ptIndex3 || ptIndex3 == ptIndex1)
                return null;
            return new TINtriangle(pointList, ptIndex1, ptIndex2, ptIndex3);
        }

        private TINtriangle(List<TINpoint> pointList, int ptIndex1,
           int ptIndex2, int ptIndex3)
        {
            point1 = pointList[ptIndex1];
            point2 = pointList[ptIndex2];
            point3 = pointList[ptIndex3];

            computeBoundingBox();
            setupNormalVec();
        }

        public void computeBoundingBox()
        {
            myBoundingBox_ = new BoundingBox(point1.x, point1.y, point1.x, point1.y);
            myBoundingBox_.expandByPoint(point2.x, point2.y, point2.z);
            myBoundingBox_.expandByPoint(point3.x, point3.y, point3.z);
        }

        public bool isPointInBoundingBox(TINpoint aPoint)
        {
            return myBoundingBox_.isPointInsideBB2d(aPoint.x, aPoint.y);
        }

        public IEnumerable<TINpoint> myPoints
        {
            get
            {
                yield return point1;
                yield return point2;
                yield return point3;
            }
        }

        public IEnumerable<TINtriangleLine> myLines
        {
            get
            {
                yield return myLine1;
                yield return myLine2;
                yield return myLine3;
            }
        }

        public bool IsPointAVertex(TINpoint aPoint)
        {
            return this.myPoints.Where(p => p.myIndex == aPoint.myIndex).Any();
        }

        #region IComparable Members

        /// <summary>
        /// Makes TINtriangles automatically sort itself based on x-axis order.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>int</returns>
        int IComparable.CompareTo(object obj)
        {
            TINtriangle other = (TINtriangle)obj;
            return this.myBoundingBox_.lowerLeftPt.compareByXthenY(other.myBoundingBox_.lowerLeftPt);
        }

        #endregion

        // adapted from
        // http://stackoverflow.com/questions/2049582/how-to-determine-a-point-in-a-triangle
        internal bool contains(TINpoint aPoint)
        {
            bool b1, b2, b3;

            b1 = sign(aPoint, point1, point2) < 0.0f;
            b2 = sign(aPoint, point2, point3) < 0.0f;
            b3 = sign(aPoint, point3, point1) < 0.0f;

            return ((b1 == b2) && (b2 == b3));
        }

        double sign(TINpoint p1, TINpoint p2, TINpoint p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }
        // End: adapted from

        public double givenXYgetZ(TINpoint aPoint)
        {
            setupNormalVec();

            // Use equation         ax + bx
            //                 z = ----------     taken from Wolfram Alpha
            //                        -c
            //
            //  where a is normalVec_.i, b is .j, and c is .k
            //    and X is aPoint.x - point1.x
            //    and Y is aPoint.y - point1.y
            //
            //  Ultimately add z to point1.z to get the elevation

            double X = aPoint.x - point1.x;
            double Y = aPoint.y - point1.y;

            double Z = ((normalVec.x * X) + (normalVec.y * Y)) /
                        (-1.0 * normalVec.z);

            return Z + point1.z;
        }

        public double? givenXYgetSlopePercent(Point aPoint)
        {
            return givenXYgetSlopePercent((TINpoint)aPoint);
        }

        public double? givenXYgetSlopePercent(TINpoint aPoint)
        {
            setupNormalVec();

            if (0.0 == normalVec_.z) return null;

            return Math.Abs(100.0 *
               Math.Sqrt(normalVec_.x * normalVec_.x + normalVec_.y * normalVec_.y) /
                           normalVec_.z);
        }

        public Azimuth givenXYgetSlopeAzimuth(Point aPoint)
        {
            return givenXYgetSlopeAzimuth((TINpoint)aPoint);
        }

        public Azimuth givenXYgetSlopeAzimuth(TINpoint aPoint)
        {
            setupNormalVec();

            Azimuth slopeAz = new Azimuth();
            slopeAz.setFromXY(normalVec_.y, normalVec.x);

            return slopeAz;

        }

        private void setupNormalVec()
        {
            if (normalVec_ == null)
            {
                normalVec_ = (point2 - point1).crossProduct(point3 - point1);
                if (normalVec_.z < 0.0)
                {
                    var tmp = this.point2;
                    this.point2 = this.point1;
                    this.point1 = tmp;
                    normalVec_ = (point2 - point1).crossProduct(point3 - point1);
                }
            }
        }

        public bool HasBeenVisited { get; set; } = false;

        internal static double triangleInternalAngleThreshold = 157.0;
        internal static double slopeThreshold = 79.5; // degrees.
        internal bool shouldRemove()
        {
            var normalTheta = this.normalVec.Theta.getAsDegreesDouble();
            if (normalTheta > slopeThreshold)
                return true;
            // if max interior angle > threshold, true
            var maxInteriorAngle =
                Math.Max(this.angle1, Math.Max(this.angle2, this.angle3));
            if (maxInteriorAngle > triangleInternalAngleThreshold)
                return true;

            var maxXslope = lines.Select(Line => Line.DeltaCrossSlopeAsAngleRad).OrderBy(x => x).Last();
            if (maxXslope > 3.0)
                return true;

            return false;
        }

        private bool includesPointByGridCoord(int xInt, int yInt, int? zInt = null)
        {
            var gridCoords = this.point1.GridCoordinates;
            if (gridCoords.Item1 == xInt && gridCoords.Item2 == yInt)
                return true;

            gridCoords = this.point2.GridCoordinates;
            if (gridCoords.Item1 == xInt && gridCoords.Item2 == yInt)
                return true;

            gridCoords = this.point3.GridCoordinates;
            if (gridCoords.Item1 == xInt && gridCoords.Item2 == yInt)
                return true;

            return false;
        }

        public static long callDepth = 0;

        /// <summary>
        /// Performs a depth-first search, but limited to 200 calls deep to avoid stack overflows.
        /// </summary>
        [Obsolete]
        internal void walkNetwork()
        {
            if (this.HasBeenVisited) return;
            callDepth++;
            if(callDepth > 200)
            {
                callDepth--;
                return;
            }
            this.HasBeenVisited = true;
            var trianglesToRemove = new List<TINtriangle>();
            var neighborTriangles = this.lines.Select(line => line.GetOtherTriangle(this));
            foreach (var neighborTri in neighborTriangles)
            {
                if (!(neighborTri is null))
                {
                    if (neighborTri.shouldRemove())
                    {
                        neighborTri.IsValid = false;
                        trianglesToRemove.Add(neighborTri);
                    }
                }
            }
            foreach(var neighborTri in trianglesToRemove)
                neighborTri.walkNetwork();
            
            callDepth--;
        }

        public override string ToString()
        {
            StringBuilder retString = new StringBuilder($"{(int)(Centroid.x)}, ");
            retString.Append($"{(int)(Centroid.y)}");
            return retString.ToString();
        }

        internal string IndicesToWavefrontString()
        {
            int p2Idx, p3Idx;
            if(this.normalVec.z > 0.0)
            { p2Idx = this.point2.myIndex + 1; p3Idx = this.point3.myIndex + 1; }
            else
            { p2Idx = this.point3.myIndex + 1; p3Idx = this.point2.myIndex + 1; }

                return $"f {this.point1.myIndex + 1} {p2Idx} {p3Idx}";
        }

        internal void SwapPoint1And2()
        {
            var holdMypoint = point2;
            point2 = point1;
            point1 = holdMypoint;

            normalVec_ = (point2 - point1).crossProduct(point3 - point1);
        }
    }

    internal class ConvexFaceTriangle : MIConvexHull.TriangulationCell<TINpoint, ConvexFaceTriangle>
    {
        double Det(double[,] m)
        {
            return m[0, 0] * ((m[1, 1] * m[2, 2]) - (m[2, 1] * m[1, 2])) - m[0, 1] * (m[1, 0] * m[2, 2] - m[2, 0] * m[1, 2]) + m[0, 2] * (m[1, 0] * m[2, 1] - m[2, 0] * m[1, 1]);
        }

        double LengthSquared(double[] v)
        {
            double norm = 0;
            for (int i = 0; i < v.Length; i++)
            {
                double t = v[i];
                norm += t * t;
            }
            return norm;
        }
    }

}
