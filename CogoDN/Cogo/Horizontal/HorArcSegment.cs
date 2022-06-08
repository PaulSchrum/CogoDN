using CadFoundation;
using CadFoundation.Angles;
using CadFoundation.Coordinates;
using CadFoundation.Coordinates.Curvilinear;
using netDxf;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cogo.Horizontal
{
    public class HorArcSegment : HorizontalAlignmentBase
    {

        private int deflDirection { get; set; }
        public Point ArcCenterPt { get; protected set; }
        public override Double Radius { get; protected set; }
        public Vector BeginRadiusVector { get; protected set; }
        public Vector EndRadiusVector { get; protected set; }

        public static HorArcSegment Create(Point begPt, Point endPt,
            Azimuth incomingAzimuth, Double radius)
        {
            return new HorArcSegment(begPt, endPt, incomingAzimuth, radius);
        }

        public HorArcSegment(Point begPt, Point endPt, Azimuth incomingAzimuth, Double radius)
           : base(begPt, endPt)
        {
            if (0 == utilFunctions.tolerantCompare((endPt - begPt).Length, 0.0, 0.000001))
                throw new ArcExceptionZeroLengthNotDefined();

            Double tanOffsetToEndPt;
            Ray incomingRay = new Ray(); incomingRay.advanceDirection = 1;
            incomingRay.StartPoint = begPt; incomingRay.HorizontalDirection = incomingAzimuth;
            tanOffsetToEndPt = incomingRay.getOffset(endPt);

            if (0 == utilFunctions.tolerantCompare(tanOffsetToEndPt, 0.0, 0.000001))
                throw new ArcExceptionZeroDeflectionNotDefined();

            this.BeginStation = 0.0;
            this.Radius = Math.Abs(radius);
            this.BeginAzimuth = incomingAzimuth;

            deflDirection = Math.Sign(tanOffsetToEndPt);
            //Deflection defl = Deflection.ctorDeflectionFromAngle(90.0, deflDirection);
            Deflection defl = Deflection.ctorDeflectionFromAngle(90.0, deflDirection);
            Azimuth azToCenter = incomingAzimuth + defl;
            Vector traverseToCenterVec = new Vector(azToCenter, radius);
            ArcCenterPt = begPt + traverseToCenterVec;

            this.BeginRadiusVector = this.ArcCenterPt - this.BeginPoint;
            Azimuth endVecAz = (this.ArcCenterPt - endPt).Azimuth;
            this.EndRadiusVector = new Vector(endVecAz, radius);
            this.EndPoint = this.ArcCenterPt + this.EndRadiusVector;

            var deflectionDbl = endVecAz - this.BeginRadiusVector.Azimuth;
            this.Deflection = new Deflection(deflectionDbl, deflDirection);
            this.EndAzimuth = this.BeginAzimuth + this.Deflection;

            // applies to English projects only (for now)
            this.BeginDegreeOfCurve = HorizontalAlignmentBase.computeDegreeOfCurve(this.Radius);
            this.EndDegreeOfCurve = this.BeginDegreeOfCurve;

            this.Length = 100.0 * this.Deflection.getAsRadians() / this.BeginDegreeOfCurve.getAsRadians();
            this.Length = Math.Abs(this.Length);
            this.EndStation = this.BeginStation + this.Length;


        }

        public HorArcSegment(Point begPt, Point centerPt, Point endPt, expectedType ExpectedType,
           int deflectionDirection)
           : base(begPt, endPt)
        {
            this.deflDirection = deflectionDirection;
            this.ArcCenterPt = centerPt;
            this.BeginRadiusVector = this.ArcCenterPt - this.BeginPoint;
            this.EndRadiusVector = this.ArcCenterPt - this.EndPoint;

            this.Radius = this.BeginRadiusVector.Length;
            Double validationRadius = this.EndRadiusVector.Length;
            if (Math.Abs(this.Radius - validationRadius) > 0.00014)
                throw new Exception("Given points do not represent a circle.");

            Double degreesToAdd = 90 * deflectionDirection;
            if (ExpectedType == expectedType.ArcSegmentOutsideSoluion)
                deflectionDirection *= -1;

            this.BeginAzimuth = this.BeginRadiusVector.Azimuth + Angle.radiansFromDegree(degreesToAdd);
            this.EndAzimuth = this.EndRadiusVector.Azimuth + Angle.radiansFromDegree(degreesToAdd);

            // applies to English projects only (for now)
            this.BeginDegreeOfCurve = HorizontalAlignmentBase.computeDegreeOfCurve(this.Radius);
            this.EndDegreeOfCurve = this.BeginDegreeOfCurve;

            if (ExpectedType == expectedType.ArcSegmentOutsideSoluion)
            {
                computeDeflectionForOutsideSolutionCurve();
                this.Length = 100.0 * this.Deflection.getAsRadians() / this.BeginDegreeOfCurve.getAsRadians();
                this.Length = Math.Abs(this.Length);
            }
            else
            {
                Deflection = new Deflection(this.BeginAzimuth, this.EndAzimuth, true);
                Double deflAsRadians = this.Deflection.getAsRadians();
                Double DcAsRadians = this.BeginDegreeOfCurve.getAsRadians();
                this.Length = 100.0 * deflAsRadians / DcAsRadians;
                this.Length = 100.0 * this.Deflection.getAsRadians() / this.BeginDegreeOfCurve.getAsRadians();
                this.Length = Math.Abs(this.Length);
            }
        }

        public static HorArcSegment Create(Ray inRay, double length, double degree)
        {
            double radius = Math.Abs(degree.RadiusFromDegreesDbl());
            DegreeOfCurve deg = DegreeOfCurve.newFromDegrees(degree);

            // Equations from Hickerson, pp 64 - 66
            // Traverse from start to PI, turn by Defl, then traverse to endPt
            Deflection Defl = length * deg.getAsRadians() / degreeOfCurveLength;

            double Tlength = radius * Math.Tan(Math.Abs(Defl.getAsRadians()) / 2.0);
            Vector tan1 = new Vector(inRay.HorizontalDirection, Tlength);
            Point PointIntersection = inRay.StartPoint + tan1;
            Vector tan2 = new Vector(inRay.HorizontalDirection + Defl, Tlength);

            Point endPt = PointIntersection + tan2;

            return Create(inRay.StartPoint, endPt, inRay.HorizontalDirection, radius);
        }

        /// <summary>
        /// Factory method for use when accepting arcs from dxf polylines.
        /// </summary>
        /// <param name="begPt">Arc Begin Point</param>
        /// <param name="endPt">Arc End Point</param>
        /// <param name="bulge">Bulge from dxf lwpolyline data</param>
        /// <returns></returns>
        public static HorArcSegment Create(Point begPt, Point endPt, double bulge)
        {
            double deflectionDelta = 4.0 * Math.Atan(Math.Abs(bulge));

            // alpha here is angle between long chord and a tangent.
            Deflection alpha = new Deflection(deflectionDelta / 2.0);

            Azimuth longChordDirection = new Azimuth(begPt, endPt);
            if (bulge < 0.0) alpha *= -1.0;
            else deflectionDelta *= -1.0;
            var incomingDirection = longChordDirection + alpha;

            var outgoingDirection = incomingDirection + deflectionDelta;
            var startRadialDirection = incomingDirection + Deflection.QUARTERCIRCLE;
            var startRadialRay = new Ray(begPt, startRadialDirection);
            var endRadialDirection = outgoingDirection + Deflection.QUARTERCIRCLE;
            var endRadialRay = new Ray(endPt, endRadialDirection);
            Point arcCenterPt = null;
            try
            {
                arcCenterPt = startRadialRay.IntersectWith_2D(endRadialRay);
            }
            catch (IsOnBackSideOfRayException getItAnyway)
            {
                arcCenterPt = getItAnyway.resultingPoint;
            }
            var radius = (arcCenterPt - begPt).Length;

            var sumpin = Create(begPt, endPt, incomingDirection, radius);

            return Create(begPt, endPt, incomingDirection, radius);
        }

        public override BoundingBox BoundingBox
        {
            get
            {
                //if (base.boundingBox_ is null)
                if (true) // for debugging.
                {
                    boundingBox_ = new BoundingBox(this.BeginPoint);
                    boundingBox_.expandByPoint(this.EndPoint);

                    var testAz = Azimuth.NORTH;
                    Point testPt = null;
                    if (isInArcSweep(testAz))
                    {
                        testPt = this.ArcCenterPt + new Vector(testAz, this.Radius);
                        boundingBox_.expandByPoint(testPt);
                    }

                    testAz = Azimuth.EAST;
                    if (isInArcSweep(testAz))
                    {
                        testPt = this.ArcCenterPt + new Vector(testAz, this.Radius);
                        boundingBox_.expandByPoint(testPt);
                    }

                    testAz = Azimuth.SOUTH;
                    if (isInArcSweep(testAz))
                    {
                        testPt = this.ArcCenterPt + new Vector(testAz, this.Radius);
                        boundingBox_.expandByPoint(testPt);
                    }

                    testAz = Azimuth.WEST;
                    if (isInArcSweep(testAz))
                    {
                        testPt = this.ArcCenterPt + new Vector(testAz, this.Radius);
                        boundingBox_.expandByPoint(testPt);
                    }


                }
                return boundingBox_;
            }
        }

        private bool isInArcSweep(Azimuth testAzimuth)
        {
            Deflection testDefl = null;
            if (deflDirection == 1)
                testDefl = this.BeginRadiusVector.Azimuth - testAzimuth;
            else
                testDefl = this.EndRadiusVector.Azimuth - testAzimuth;

            var testDefDeg = testDefl.getAsDegreesDouble();
            var actualDefDeg = this.Deflection.getAsDegreesDouble();
            var ratio = deflDirection * testDefl.getAsRadians() / this.Deflection.getAsRadians();
            if (ratio <= 1.0 && ratio >= 0.0)
                return true;
            return false;
        }

        public override (Point point, StationOffsetElevation soe) LineIntersectSOE(
                    Point firstPoint, Point secondPoint, double offset = 0d)
        {
            return (null, null);
        }

        private void computeDeflectionForOutsideSolutionCurve()
        {
            Double radVector1Az = this.BeginRadiusVector.Azimuth.getAsDegreesDouble();
            Double radVector2Az = this.EndRadiusVector.Azimuth.getAsDegreesDouble();

            int quadrantAZ1 = Azimuth.getQuadrant(radVector1Az);
            int quadrantAZ2 = Azimuth.getQuadrant(radVector2Az);

            Double defl = radVector2Az - radVector1Az;

            if (1 == this.deflDirection)
            {
                if (defl < 0.0) defl += 360.0;
            }
            else
            {
                defl = defl - 360.0;
            }

            Deflection = Deflection.ctorDeflectionFromAngle(defl, this.deflDirection);

        }

        public override StringBuilder createTestSetupOfFundamentalGeometry()
        {
            throw new NotImplementedException();
        }

        public override List<StationOffsetElevation> getStationOffsetElevation(Point interestPoint)
        {
            Vector arcCenterToInterestPtVector = new Vector(this.ArcCenterPt, interestPoint);
            Deflection deflToInterestPt = new Deflection(this.BeginRadiusVector.Azimuth, arcCenterToInterestPtVector.Azimuth, true);
            int arcDeflDirection = Math.Sign(this.Deflection.getAsDegreesDouble());
            if (arcDeflDirection * deflToInterestPt.getAsDegreesDouble() < 0.0)
            {
                return null;
            }
            else if (Math.Abs(this.Deflection.getAsDegreesDouble()) - Math.Abs(deflToInterestPt.getAsDegreesDouble()) < 0.0)
            {
                return null;
            }

            Double interestLength = this.Length * deflToInterestPt.getAsRadians() / this.Deflection.getAsRadians();
            Offset offset = new Offset(arcDeflDirection * (this.Radius - arcCenterToInterestPtVector.Length));

            var soe = new StationOffsetElevation(this.BeginStation + interestLength, offset);
            var returnList = new List<StationOffsetElevation>();
            returnList.Add(soe);
            return returnList;
        }

        public override Point getXYZcoordinates(StationOffsetElevation anSOE)
        {
            if (anSOE.station < this.BeginStation || anSOE.station > this.EndStation)
                return null;

            Double lengthIntoCurve = anSOE.station - this.BeginStation;

            Deflection deflToSOEpoint = new Deflection(this.BeginDegreeOfCurve.getAsRadians() * lengthIntoCurve / 100.0, this.deflDirection);
            Azimuth ccToSOEpointAzimuth = this.BeginRadiusVector.Azimuth + deflToSOEpoint;

            Double ccToSOEpointDistance = this.Radius - this.deflDirection * anSOE.offset.OFST;

            Vector ccToSOEpoint = new Vector(ccToSOEpointAzimuth, ccToSOEpointDistance);

            return this.ArcCenterPt + ccToSOEpoint;
        }

        public override void drawHorizontalByOffset
           (IPersistantDrawer drawer, StationOffsetElevation soe1, StationOffsetElevation soe2)
        {
            Point startPoint = this.getXYZcoordinates(soe1);
            Point endPoint = this.getXYZcoordinates(soe2);
            drawer.PlaceArc(this, startPoint, soe1, endPoint, soe2);
        }


        internal void setDeflection(Deflection newDeflection)
        {
            if (newDeflection.getAsRadians() == 0.0)
                throw new Exception("Can't create arc with zero degree deflection.");

            this.Deflection = newDeflection;
            this.deflDirection = Math.Sign(newDeflection.getAsRadians());

            //var anAngle = this.BeginRadiusVector + this.Deflection;
            //var newAz = Azimuth.newAzimuthFromAngle(anAngle);
            this.EndRadiusVector = this.BeginRadiusVector + this.Deflection;
            this.EndAzimuth = this.BeginAzimuth + this.Deflection;
            this.Length = 100.0 * this.Deflection.getAsRadians() / this.BeginDegreeOfCurve.getAsRadians();
            this.Length = Math.Abs(this.Length);
            this.EndStation = this.BeginStation + this.Length;
            this.EndPoint = this.ArcCenterPt + this.EndRadiusVector;
        }

        public override void draw(ILinearElementDrawer drawer)
        {
            drawer.drawArcSegment(this.BeginPoint, this.ArcCenterPt, this.EndPoint,
               this.Deflection.getAsRadians());
        }

        public override void AddToDxf(DxfDocument dxfDoc)
        {
            double getCadAngle(Vector vec)
            {
                return Angle.degreesFromRadians(Math.Atan2(vec.y, vec.x));
            }

            var startAngle = getCadAngle(this.BeginRadiusVector);
            var endAngle = getCadAngle(this.EndRadiusVector);
            netDxf.Entities.Arc theArc = null;
            if (this.deflDirection < 0)
                theArc = new netDxf.Entities.Arc(
                    new Vector2(this.ArcCenterPt.x, this.ArcCenterPt.y),
                    this.Radius, startAngle, endAngle);
            else
                theArc = new netDxf.Entities.Arc(
                    new Vector2(this.ArcCenterPt.x, this.ArcCenterPt.y),
                    this.Radius, endAngle, startAngle);

            dxfDoc.AddEntity(theArc);
        }

    }
    public class ArcException : Exception
    {
        public ArcException(String msg) : base(msg) { }
    }

    public class ArcExceptionZeroDeflectionNotDefined : ArcException
    {
        public ArcExceptionZeroDeflectionNotDefined() :
           base("Arc with zero-degree deflection not defined.")
        { }
    }

    public class ArcExceptionZeroLengthNotDefined : ArcException
    {
        public ArcExceptionZeroLengthNotDefined() :
           base("Arc with zero-length not defined.")
        { }
    }
}
