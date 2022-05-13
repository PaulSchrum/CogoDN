using CadFoundation;
using CadFoundation.Angles;
using CadFoundation.Coordinates;
using CadFoundation.Coordinates.Curvilinear;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cogo.Horizontal
{
    public class HorSpiralSegment : HorizontalAlignmentBase
    {
        public int SpiralType { get; protected set; }
        public Vector spiralDX { get; protected set; }
        public Vector spiralDY { get; protected set; }
        public Vector spiralU { get; protected set; }
        public Vector spiralV { get; protected set; }
        public double AnchorPhantomStation { get; protected set; }


        public Angle DcChangeRate { get; protected set; }

        /// <summary>
        /// Used internally to find by getStationOffsetElevation to
        /// iteratate to a solution.
        /// </summary>
        private SpiralIterator myIterator { get; set; }

        /// <summary>
        /// The point where Dc = 0. For connecting spirals,
        /// this point is off the alignment, but we still have to use it.
        /// </summary>
        internal Point AnchorPoint
        { get { return this.AnchorRay.StartPoint; } }

        /// <summary>
        /// The point and ahead-bearing where Dc = 0. For connecting spirals,
        /// this point is off the alignment, but we still have to use it.
        /// </summary>
        protected Ray AnchorRay { get; set; }

        /// <summary>
        /// Total length along the element from the CS/SC Point to the
        /// Anchor Point. 
        /// </summary>
        protected Double AnchorLength { get; set; }

        /// <summary>
        /// True for Type 1 and Type 3.
        /// </summary>
        public bool CurvatureIncreasesAhead
        {
            get { return (Math.Abs(EndDegreeOfCurve.getAsRadians() - BeginDegreeOfCurve.getAsRadians()) > 0.0); }
        }

        /// <summary>
        /// True for Type 2 and Type 4.
        /// </summary>
        public bool CurvatureDecreasesAhead
        {
            get { return (Math.Abs(EndDegreeOfCurve.getAsRadians() - BeginDegreeOfCurve.getAsRadians()) < 0.0); }
        }


        public bool isConnecting { get { return BeginDegreeOfCurve != 0.0 && EndDegreeOfCurve != 0.0; } }

        public Double LengthFraction(Double L)
        {
            return L / this.Length;
        }

        /// <summary>
        /// From Hickerson, p 169.
        /// </summary>
        /// <param name="L"></param>
        /// <returns></returns>
        public Double ThetaFraction(Double L)
        {
            return this.Deflection.getAsRadians() * LengthFraction(L) * LengthFraction(L);
        }

        /// <summary>
        /// The spiral X equation from Hickerson, Appendix C, p 374
        /// This is the equation based on Radians, not Degrees.
        /// </summary>
        /// <param name="distanceAlong"></param>
        /// <returns></returns>
        private Double computeXlength(Double distanceAlong)
        {
            var theta = ThetaFraction(distanceAlong);
            Double x =
                +1.0
                - Math.Pow(theta, 2.0) / 10.0
                + Math.Pow(theta, 4.0) / 216.0
                - Math.Pow(theta, 6.0) / 9360.0
                + Math.Pow(theta, 8.0) / 685440.0
                ;

            return Math.Abs(distanceAlong * x);
        }

        /// <summary>
        /// The spiral Y equation from Hickerson, Appendix C, p 374
        /// This is the equation based on Radians, not Degrees.
        /// </summary>
        /// <param name="distanceAlong"></param>
        /// <returns></returns>
        private Double computeYlength(Double distanceAlong)
        {
            var theta = ThetaFraction(distanceAlong);
            Double y =
                +theta / 3.0
                - Math.Pow(theta, 3.0) / 42.0
                + Math.Pow(theta, 5.0) / 1320.0
                - Math.Pow(theta, 7.0) / 75600.0
                + Math.Pow(theta, 9.0) / 6894720.0
                ;

            return Math.Abs(distanceAlong * y);
        }

        public override Azimuth getAzimuth(double station)
        {
            var anchorLengthAlong = station - this.AnchorPhantomStation;
            return this.AnchorRay.HorizontalDirection + ThetaFraction(anchorLengthAlong);
        }

        /// <summary>
        /// Based on clockwise being positive.
        /// </summary>
        /// <param name="anSOE"></param>
        /// <returns></returns>
        public override Azimuth getPerpandicularAzimuth(double station)
        {
            return this.getAzimuth(station) + Deflection.HALFCIRCLE / 2.0;
        }

        /// <summary>
        /// Based on clockwise being positive.
        /// </summary>
        /// <param name="anSOE"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public override Vector getPerpandicularVector(double station, double length)
        {
            var az = getPerpandicularAzimuth(station);
            return new Vector(az, length);
        }

        public override List<StationOffsetElevation> getStationOffsetElevation(Point interestPoint)
        {
            throw new NotImplementedException();
            //var returnList = new List<StationOffsetElevation>();

            //var spiratIterator = SpiralIterator.createIterator(this, interestPoint);

            //return returnList;
        }

        public override Point getXYZcoordinates(StationOffsetElevation anSOE)
        {
            if (anSOE.station < this.BeginStation || anSOE.station > this.EndStation)
                return null;

            Double localDistAlong = anSOE.station - this.BeginStation;
            Double anchorDistAlong = anSOE.station - this.AnchorPhantomStation;

            Double xDist = computeXlength(anchorDistAlong);
            var dx = new Vector(this.AnchorRay, xDist);

            Double yDist = computeYlength(anchorDistAlong);
            var dy =
                (new Vector(this.AnchorRay, yDist * this.Deflection.deflectionDirection))
                .left90degrees();

            Vector chordVector = dx + dy;
            Point targetPoint = this.AnchorPoint + chordVector;

            var offsetIsNotZero = utilFunctions.tolerantCompare(
                    anSOE.offset.OFST, 0.0,
                    0.00001) != 0;

            if (offsetIsNotZero)
            {
                var perpandicluarVector =
                    this.getPerpandicularVector(anSOE.station, anSOE.offset.OFST);
                targetPoint = targetPoint + perpandicluarVector;
            }

            return targetPoint;
        }

        internal override void restationFollowOnOperation()
        {
            // Mend AnchorPhantomStation
            if (this.SpiralType == 1)
                this.AnchorPhantomStation = this.BeginStation;
            else if (this.SpiralType == 2)
                this.AnchorPhantomStation = this.EndStation;
            else if (this.SpiralType == 3)
                throw new NotImplementedException();
            else // this.SpiralType == 4
                throw new NotImplementedException();

            return;
        }

        protected static void setSpiralType(HorSpiralSegment spi, double degreeIn, double degreeOut)
        {
            if (degreeIn == 0.0)
                spi.SpiralType = 1;
            else if (degreeOut == 0.0)
                spi.SpiralType = 2;
            else if (Math.Abs(degreeIn) < Math.Abs(degreeOut))
                spi.SpiralType = 3;
            else
                spi.SpiralType = 4;
        }

        public static HorSpiralSegment Create(
            Ray inRay,
            double length,
            double degreeIn,
            double degreeOut,
            double beginStation = 0.0)
        {
            HorSpiralSegment newSpi = new HorSpiralSegment();
            setSpiralType(newSpi, degreeIn, degreeOut);

            newSpi.BeginPoint = inRay.StartPoint;
            newSpi.BeginAzimuth = inRay.HorizontalDirection;

            newSpi.BeginDegreeOfCurve = Angle.radiansFromDegree(degreeIn);
            newSpi.EndDegreeOfCurve = Angle.radiansFromDegree(degreeOut);
            newSpi.Length = length;
            newSpi.BeginStation = beginStation;
            newSpi.EndStation = beginStation + length;

            newSpi.DcChangeRate = (newSpi.EndDegreeOfCurve - newSpi.BeginDegreeOfCurve)
                                    / length;
            var v = newSpi.DcChangeRate.getAsDegreesDouble();

            var highestDegree = Math.Max(Math.Abs(degreeIn), Math.Abs(degreeOut));
            var minRadius = DegreeOfCurve.asRadiusFromDegDouble(highestDegree);

            if (newSpi.SpiralType == 1)
            {
                var deflDirection = Math.Sign(newSpi.DcChangeRate.getAsRadians());
                var thetaS = deflDirection * length
                    / (2 * minRadius);
                newSpi.Deflection = new Deflection(thetaS);
                newSpi.EndAzimuth = newSpi.BeginAzimuth + newSpi.Deflection;

                double spiralX = newSpi.computeXlength(length);
                newSpi.spiralDX = new Vector(newSpi.BeginAzimuth, spiralX);
                double spiralY = Math.Abs(newSpi.computeYlength(length));
                int sign = newSpi.Deflection.deflectionDirection;
                var yDir = newSpi.BeginAzimuth.RightNormal(sign);
                newSpi.spiralDY = new Vector(yDir, spiralY);
                newSpi.EndPoint = newSpi.BeginPoint + newSpi.spiralDX + newSpi.spiralDY;

                double tan_thetaS = Math.Tan(Math.Abs(newSpi.Deflection.getAsRadians()));
                double Ulength = newSpi.spiralDX.Length - (newSpi.spiralDY.Length /
                                                            tan_thetaS);
                newSpi.spiralU = new Vector(newSpi.BeginAzimuth, Ulength);

                double sin_thetaS = Math.Sin(Math.Abs(newSpi.Deflection.getAsRadians()));
                double Vlength = newSpi.spiralDY.Length /
                                    sin_thetaS;
                newSpi.spiralV = new Vector(newSpi.EndAzimuth, Vlength);
                newSpi.AnchorRay = new Ray(newSpi.BeginPoint, newSpi.BeginAzimuth);
                newSpi.AnchorPhantomStation = newSpi.BeginStation;
            }
            else if (newSpi.SpiralType == 2)
            {
                var deflDirection = -1 * Math.Sign(newSpi.DcChangeRate.getAsRadians());
                var thetaS = deflDirection * length
                    / (2 * minRadius);
                newSpi.Deflection = new Deflection(thetaS);
                double spiralX = newSpi.computeXlength(length);
                double spiralY = newSpi.computeYlength(length);
                //newSpi.EndPoint = newSpi

                newSpi.EndAzimuth = newSpi.BeginAzimuth + newSpi.Deflection;
                var anchorAzimuth = newSpi.EndAzimuth + Angle.HALFCIRCLE;
                newSpi.spiralDX = new Vector(anchorAzimuth, spiralX);
                int sign = newSpi.Deflection.deflectionDirection;
                var yDir = anchorAzimuth.RightNormal(sign);
                newSpi.spiralDY = new Vector(yDir, Math.Abs(spiralY));

                newSpi.EndPoint = newSpi.BeginPoint + newSpi.spiralDY
                    - newSpi.spiralDX;

                newSpi.AnchorRay = new Ray(newSpi.EndPoint, anchorAzimuth);
                newSpi.AnchorLength = length;
                newSpi.AnchorPhantomStation = newSpi.EndStation;

                double tan_thetaS = Math.Tan(Math.Abs(newSpi.Deflection.getAsRadians()));
                double Ulength = newSpi.spiralDX.Length - (newSpi.spiralDY.Length /
                                                            tan_thetaS);
                newSpi.spiralU = new Vector(anchorAzimuth, Ulength);

                double sin_thetaS = Math.Sin(Math.Abs(newSpi.Deflection.getAsRadians()));
                double Vlength = newSpi.spiralDY.Length /
                                    sin_thetaS;
                newSpi.spiralV = new Vector(newSpi.BeginAzimuth, Vlength);

            }
            else if (newSpi.SpiralType == 3)
            {
                // not implemented
            }
            else if (newSpi.SpiralType == 4)
            {
                // not implemented
            }
            else
            {
                // this should never happen
            }


            return newSpi;
        }

        /// <summary>
        /// 
        /// </summary>
        private class SpiralIterator
        {
            /// <summary>
            /// Factory method to create a spiral iterator and populate it with its
            /// first five points
            /// </summary>
            /// <param name="mySpiral"></param>
            /// <returns></returns>
            public static SpiralIterator createIterator(HorSpiralSegment mySpiral, Point aTargetPoint)
            {
                if (mySpiral.myIterator != null)
                    return mySpiral.myIterator;

                var newSpiralIter = new SpiralIterator(mySpiral, aTargetPoint);
                double aLen = mySpiral.Length;
                double begSta = mySpiral.BeginStation;
                double segmentStations = 4.0;
                for (double i = 0; i <= segmentStations; i++)
                {
                    double intermediateStation = begSta + (i * aLen / segmentStations);
                    Point pos = mySpiral.getXYZcoordinates(intermediateStation);
                    newSpiralIter.iterationList.Add(
                        new SpiralIterationPoint(intermediateStation, pos));
                }

                foreach (var iterPoint in newSpiralIter.iterationList)
                {
                    iterPoint.computeParameters(newSpiralIter, aTargetPoint);
                }

                return mySpiral.myIterator;
            }

            public HorSpiralSegment mySpiral { get; private set; }
            public Point targetPoint { get; private set; }
            private List<SpiralIterationPoint> iterationList { get; set; }
            private SpiralIterator(HorSpiralSegment mySpi, Point aTargetPoint)
            {
                this.mySpiral = mySpi;
                this.targetPoint = aTargetPoint;
                this.iterationList = new List<SpiralIterationPoint>();
                mySpi.myIterator = this;
            }

            private struct SpiralIterationPoint
            {
                double station { get; set; }
                Point pointOnStation { get; set; }
                Azimuth tangentDirection { get; set; }
                Vector vectorToTarget { get; set; }
                Deflection deflectionToTarget { get; set; }

                public SpiralIterationPoint(double sta, Point pointOnSta)
                {
                    station = sta;
                    pointOnStation = pointOnSta;
                    tangentDirection = null;
                    vectorToTarget = null;
                    deflectionToTarget = null;
                }

                internal void computeParameters(SpiralIterator newSpiralIter,
                    Point targetPoint)
                {
                    var spi = newSpiralIter.mySpiral;
                    double sta = this.station;
                    this.tangentDirection = spi.getAzimuth(sta);
                    this.vectorToTarget = targetPoint - pointOnStation;
                    this.deflectionToTarget = tangentDirection - vectorToTarget.Azimuth;
                }
            }
        }
    }
}
