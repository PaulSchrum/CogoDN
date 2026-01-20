using System;
using System.Collections.Generic;
using System.Text;
using Cogo;
using MIConvexHull;
using netDxf;
using MathNet.Numerics.LinearAlgebra;
using CadFoundation.Coordinates;
using Surfaces.TIN;
using System.Collections.Concurrent;

namespace Surfaces.TIN;

[Serializable]
public class TINpoint : ILidarPoint
{
    public Double x { get; set; }
    public Double y { get; set; }
    public Double z { get; set; }

    public bool hasBeenSkipped = false;
    public int lidarClassification { get; set; }
    public int myIndex { get; set; }
    public bool isOnHull { get; internal set; } = false;
    public double retainProbability = 1.0;
    public int originalSequenceNumber { get; set; } = -1;

    public ConcurrentBag<TINtriangle> myTriangles { get; internal set; } = null;

    public override string ToString()
    {
        return $"{x:f2},{y:f2},{z:f2}";
    }

    public double[] Position
    {
        get { return new double[] { x, y }; }
    }

    [NonSerialized]
    private static String[] parsedStrings;

    internal TINpoint() { }

    public TINpoint(TINpoint otherPt) : this(otherPt.x, otherPt.y, otherPt.z)
    {
        lidarClassification = otherPt.lidarClassification;
        isOnHull = otherPt.isOnHull;
    }

    public TINpoint(double newX, double newY, double newZ = 0.0) : this()
    { x = newX; y = newY; z = newZ; } //myIndex = 0L; }

    public TINpoint(String ptAsString, UInt64 myIndx) : this()
    {
        parsedStrings = ptAsString.Split(' ');
        this.x = Double.Parse(parsedStrings[0]);
        this.y = Double.Parse(parsedStrings[1]);
        this.z = Double.Parse(parsedStrings[2]);
        //myIndex = myIndx;
    }

    public TINpoint(String x, String y, String z) : this()
    {
        this.x = Double.Parse(x);
        this.y = Double.Parse(y);
        this.z = Double.Parse(z);
    }

    static public TINpoint getAveragePoint(TINpoint pt1, TINpoint pt2, TINpoint pt3)
    {
        return new TINpoint(
           (pt1.x + pt2.x + pt3.x) / 3.0,
           (pt1.y + pt2.y + pt3.y) / 3.0,
           (pt1.z + pt2.z + pt3.z) / 3.0
           );
    }

    private static double gridFactor = 1.0 / 0.1;
    public Tuple<int, int> GridCoordinates
    {
        get
        {
            return new Tuple<int, int>(
                Convert.ToInt32(this.x * gridFactor),
                Convert.ToInt32(this.y * gridFactor));
        }
    }

    public static Vector operator -(TINpoint p1, TINpoint p2)
    {
        return new Vector(p2.x - p1.x, p2.y - p1.y, p2.z - p1.z);
    }

    public static explicit operator TINpoint(Point aPt)
    {
        return new TINpoint(aPt.x, aPt.y, aPt.z);
    }

    internal void AddToDxf(DxfDocument dxf, Matrix<double> affineTransform=null)
    {
        double x, y, z;

        if(null == affineTransform)
        {
            x = this.x; y = this.y; z = this.z;
        }
        else
        {
            var transformedPoint = affineTransform.Multiply(this.MathNetVector);
            x = transformedPoint[0];
            y = transformedPoint[1];
            z = transformedPoint[2];
        }

        var pt = new netDxf.Entities.Point(new Vector3(x, y, z));
        dxf.AddEntity(pt);
    }

    public static implicit operator Point(TINpoint aPt)
    {
        return new Point(aPt.x, aPt.y, aPt.z);
    }

    private Vector<double> MathNetVector
    {
        get
        {
            double[] vec = { this.x, this.y, this.z, 1.0 };
            return Vector<double>.Build.DenseOfArray(vec);
        }
    }

    private double sumOfInteriorFaceAngles_ = 0.0;
    internal void addToSumOfInteriorFaceAngles(double angleToAdd)
    {
        sumOfInteriorFaceAngles_ += angleToAdd;
    }

    private double gaussianCurvature_ = Double.NaN;
    /// <summary>
    /// The gaussian curvature of the point, computed by summing all connected
    /// triangle angles per the Gauss-Bonnet Equation.  Ref:
    /// Crane, Keenan, 2013, Digital Geometry Processing with Discrete Exterior
    ///    Calculus, In: ACM SIGGRAPH 2013 courses, SIGGRAPH ’13. ACM, New York, 
    ///    NY, USA (2013), p 56 (Se Exercise 7)
    /// </summary>
    public double GaussianCurvature
    {
        get
        {
            if (isOnHull)
                return Double.NaN;

            if(Double.IsNaN(gaussianCurvature_))
            {
                gaussianCurvature_ = Math.PI * 2.0 - sumOfInteriorFaceAngles_;
            }
            return gaussianCurvature_;
        }
        private set { }
    }

    private double sparsity_ = -1.0;
    public double Sparsity(TINsurface mySurface)
    {
        if (null == myTriangles)
            return 0.0;

        if(sparsity_ < 0.0)
        {
            sparsity_ = 0.0;
            foreach(var t in myTriangles)
            {
                sparsity_ += t.Area2d;
            }
            sparsity_ /= 3.0;
        }
        return sparsity_;
    }

    internal string ToString(Matrix<double> affineXform)
    {
        var transformedPoint = affineXform.Multiply(this.MathNetVector);

        var x = transformedPoint[0];
        var y = transformedPoint[1];
        var z = transformedPoint[2];
        return $"{x:0.000} {y:0.000} {z:0.000}";
    }
}
