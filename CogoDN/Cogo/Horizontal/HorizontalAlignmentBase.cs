using CadFoundation.Angles;
using CadFoundation.Coordinates;
using CadFoundation.Coordinates.Curvilinear;
using netDxf;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cogo.Horizontal
{
    public class HorizontalAlignmentBase : GenericAlignment
    {
        public HorizontalAlignmentBase() : base() { }

        public HorizontalAlignmentBase(Point begPt, Point endPt)
           : base()
        {
            BeginPoint = begPt;
            EndPoint = endPt;
            BeginDegreeOfCurve = EndDegreeOfCurve = 0.0;
        }

        public HorizontalAlignmentBase(List<Double> stationEquationingList) : base(stationEquationingList)
        {

        }

        public virtual Point BeginPoint { get; protected set; }
        public virtual Point EndPoint { get; protected set; }

        public virtual Azimuth BeginAzimuth { get; protected set; }
        public virtual Azimuth EndAzimuth { get; protected set; }

        public virtual Angle BeginDegreeOfCurve { get; protected set; }
        public virtual Angle EndDegreeOfCurve { get; protected set; }

        public virtual Deflection Deflection { get; protected set; }
        public virtual Double Length { get; protected set; }
        public virtual Double Radius { get; protected set; }

        protected List<HorizontalAlignmentBase> incomingElements { get; set; }
        protected List<HorizontalAlignmentBase> outgoingElements { get; set; }

        protected BoundingBox boundingBox_ = null;
        public virtual BoundingBox BoundingBox { get; }

        public Ray BeginRay
        {
            get { return new Ray(this.BeginPoint, this.BeginAzimuth); }
        }
        public Ray EndRay
        {
            get { return new Ray(this.EndPoint, this.EndAzimuth); }
        }

        public virtual Vector LongChordVector
        { get { return (new Vector(this.BeginPoint, this.EndPoint)).flattenZnew(); } }


        /// <summary>
        /// DegeeOfCurveLength, usually 100, is the length of curve used to determine
        /// What the Degree of Curve.
        /// </summary>
        static HorizontalAlignmentBase() { degreeOfCurveLength = 100; }
        static public Double degreeOfCurveLength { get; set; }
        static public Angle computeDegreeOfCurve(Double radius)
        {
            return new Angle(radius, degreeOfCurveLength);
        }

        public virtual StringBuilder createTestSetupOfFundamentalGeometry() { return null; }

        public virtual List<Point> getPoints(StationOffsetElevation anSOE)
        {
            throw new NotImplementedException();
        }

        public Point getXYZcoordinates(double aStation)
        {
            var anSOE = new StationOffsetElevation(aStation, 0.0);
            return getXYZcoordinates(anSOE);
        }

        public virtual Point getXYZcoordinates(StationOffsetElevation anSOE)
        {
            return null;
        }

        public virtual Azimuth getAzimuth(double station)
        {
            return null;
        }

        public virtual Azimuth getPerpandicularAzimuth(double station)
        {
            return null;
        }

        public virtual Vector getPerpandicularVector(double station, double length)
        {
            return null;
        }

        /// <summary>
        /// If the point does not fall on the element, return value is null.
        /// </summary>
        /// <param name="aPoint"></param>
        /// <returns></returns>
        public virtual List<StationOffsetElevation> getStationOffsetElevation(Point aPoint)
        {
            throw new NotImplementedException();
        }

        /* * /
        public bool isUnitUSsurveyFoot { get { return thisUnit == Unit.SurveyFoot; } set { thisUnit = Unit.SurveyFoot; } }
        public bool isUnitFoot { get; set; }
        public bool isUnitMeter { get; set; }

        private enum Unit{Meter, Foot, SurveyFoot}
        private HorizontalAlignmentBase.Unit thisUnit { get; set; }  /*  */

        public virtual void drawHorizontalByOffset
           (IPersistantDrawer drawer, StationOffsetElevation soe1, StationOffsetElevation soe2)
        {

        }

        public virtual void draw(ILinearElementDrawer drawer)
        {

        }

        public virtual void WriteToDxf(string outFile)
        {
            throw new NotImplementedException();
        }

        public virtual void AddToDxf(DxfDocument doc)
        {
            throw new NotImplementedException();
        }

        //public virtual Vector MoveStartPtTo(Point newBeginPoint)
        //{
        //    var moveDistance = newBeginPoint - this.BeginPoint;
        //    this.MoveBy(moveDistance);
        //    return moveDistance;
        //}

        public virtual void MoveBy(Vector moveDistance)
        {
            this.BeginPoint = this.BeginPoint + moveDistance;
            this.EndPoint = this.EndPoint + moveDistance;
        }

        public virtual (Point point, StationOffsetElevation soe) LineIntersectSOE(
            Point firstPoint, Point secondPoint, double offset = 0d)
        {
            throw new NotImplementedException();
        }
    }
}
