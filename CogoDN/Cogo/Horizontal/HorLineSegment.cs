using CadFoundation.Angles;
using CadFoundation.Coordinates;
using CadFoundation.Coordinates.Curvilinear;
using netDxf;
using NDE = netDxf.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cogo.Horizontal
{
    public class HorLineSegment : HorizontalAlignmentBase
    {

        public HorLineSegment(Point begPt, Point endPt)
         : base(begPt, endPt)
        {
            //this.BeginBearing = do this later
            //this.EndBearing = do this later
            this.BeginDegreeOfCurve = 0.0;
            this.EndDegreeOfCurve = 0.0;
            this.BeginStation = 0.0;
            this.EndStation = (endPt - begPt).Length;
            this.BeginAzimuth = this.EndAzimuth = (endPt - begPt).Azimuth;
        }

        public HorLineSegment(Ray inRay, double length) :
            base(inRay.StartPoint,
                inRay.StartPoint + new Vector(inRay.HorizontalDirection, length)
                )
        { }

        public override Azimuth BeginAzimuth
        {
            get
            {
                return new Azimuth(this.BeginPoint, this.EndPoint);
            }
        }
        public override Azimuth EndAzimuth
        {
            get { return this.BeginAzimuth; }
        }

        public override Deflection Deflection
        {
            get { return new Deflection(0.0); }
            protected set { }
        }

        public override Double Length
        {
            get
            {
                return BeginPoint.GetHorizontalDistanceTo(EndPoint);
            }
            protected set { }
        }

        public override Double Radius
        {
            get { return Double.PositiveInfinity; }
            protected set { }
        }

        public override StringBuilder createTestSetupOfFundamentalGeometry()
        {
            StringBuilder returnSB = new StringBuilder();

            return returnSB;
        }

        public override BoundingBox BoundingBox
        {
            get
            {
                if (base.boundingBox_ is null)
                {
                    boundingBox_ = new BoundingBox(this.BeginPoint);
                    boundingBox_.expandByPoint(this.EndPoint);
                }
                return boundingBox_;
            }
        }

        public override List<StationOffsetElevation> getStationOffsetElevation(Point interestPoint)
        {
            Vector BeginToInterestPtVector = new Vector(this.BeginPoint, interestPoint);
            Deflection BeginToInterestDeflection = new Deflection(this.BeginAzimuth, BeginToInterestPtVector.Azimuth, true);
            if (Math.Abs(BeginToInterestDeflection.getAsDegreesDouble()) > 90.0)
                return null;

            Vector EndToInterestPtVector = new Vector(this.EndPoint, interestPoint);
            Deflection EndToInterestDeflection = new Deflection(this.EndAzimuth, EndToInterestPtVector.Azimuth, true);
            if (Math.Abs(EndToInterestDeflection.getAsDegreesDouble()) < 90.0)
                return null;

            Double length = BeginToInterestPtVector.Length * Math.Cos(BeginToInterestDeflection.getAsRadians());
            Double theStation = this.BeginStation + length;

            Double offset = BeginToInterestPtVector.Length * Math.Sin(BeginToInterestDeflection.getAsRadians());

            var soe = new StationOffsetElevation(this.BeginStation + length, offset, 0.0);
            var returnList = new List<StationOffsetElevation>();
            returnList.Add(soe);
            return returnList;
        }

        public override Point getXYZcoordinates(StationOffsetElevation anSOE)
        {
            Double piOver2 = Math.PI / 2.0;
            Vector alongVector = new Vector(this.BeginAzimuth, anSOE.station - this.BeginStation);
            Point returnPoint = this.BeginPoint + alongVector;
            returnPoint.z = anSOE.elevation.EL;

            Azimuth perpandicularAzimuth = this.BeginAzimuth + piOver2;
            Vector perpandicularVector = new Vector(perpandicularAzimuth, anSOE.offset.OFST);
            returnPoint = returnPoint + perpandicularVector;

            return returnPoint;
        }

        public override (Point point, StationOffsetElevation soe) LineIntersectSOE(
                    Point firstPoint, Point secondPoint, double offset = 0d)
        {
            Point candidatePoint = null;
            var crossLine = new HorLineSegment(firstPoint, secondPoint);
            try
            {
                candidatePoint = this.BeginRay.IntersectWith_2D(crossLine.BeginRay);
            }
            catch (Exception)
            {
                return (null, null);
            }
            
            var distanceAlongCrossLineToCandidate = (candidatePoint.minus2d(firstPoint)).BaseLength;
            if (distanceAlongCrossLineToCandidate < 0.0 || 
                    distanceAlongCrossLineToCandidate > crossLine.Length)
                return (null, null);

            // Get the station of intersection point.
            double distanceAlongAlignment = (candidatePoint - this.BeginPoint).BaseLength;
            if (distanceAlongAlignment < 0.0 ||
                    distanceAlongAlignment > this.Length)
                return (null, null);
            var newStation = this.BeginStation + distanceAlongAlignment;

            // Get the elevation at the intersection point.
            var x = distanceAlongCrossLineToCandidate;
            var dx = crossLine.Length;
            var dz = crossLine.EndPoint.z - crossLine.BeginPoint.z;
            Slope slope = new Slope(run: dx, rise: dz);
            var elevation = crossLine.BeginPoint.z + (x * (Double)slope);

            return (point: candidatePoint,
                soe: new StationOffsetElevation(newStation, 0d, elevation));
        }

        public override void drawHorizontalByOffset
           (IPersistantDrawer drawer, StationOffsetElevation soe1, StationOffsetElevation soe2)
        {
            Point startPoint = this.getXYZcoordinates(soe1);
            Point endPoint = this.getXYZcoordinates(soe2);
            drawer.PlaceLine(this, startPoint, soe1, endPoint, soe2);
        }

        public override void draw(ILinearElementDrawer drawer)
        {
            drawer.drawLineSegment(this.BeginPoint, this.EndPoint);
        }

        public override void AddToDxf(DxfDocument dxfDoc)
        {
            NDE.Polyline poly = new NDE.Polyline();
            poly.Vertexes.Add(new NDE.PolylineVertex(this.BeginPoint.x,
                this.BeginPoint.y, 0));
            poly.Vertexes.Add(new NDE.PolylineVertex(this.EndPoint.x,
                this.EndPoint.y, 0));
            dxfDoc.AddEntity(poly);
        }

    }
}
