using System;
using System.Collections.Generic;
using CadFoundation.Angles;
using CadFoundation.Coordinates;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Cogo;
using Cogo.Horizontal;
using Cogo.Utils;
using CadFoundation;
using CadFoundation.Coordinates.Curvilinear;
using System.Linq;
using System.IO;
using NonLinearBestFit;

namespace Unit_Tests
{
    [TestClass]
    public class CogoTests
    {
        public List<Double> testList { get; set; }
        private double stdDelta { get; set; } = 0.0005;

        //[SetUp]
        public void HAtestSetup()
        {

        }

        [TestMethod]
        public void Deflection_positiveLessThan180_getAsDegrees()
        {
            Double expectedValue = 45.0;
            Deflection defl = new Deflection(0.785398164, 1);
            Double actualValue = defl.getAsDegreesDouble();
            Assert.AreEqual(expected: expectedValue, actual: actualValue, delta: stdDelta);
        }

        [TestMethod]
        public void Deflection_positiveGreaterThan180_getAsDegrees()
        {
            Double expectedValue = 310.0;
            Deflection defl = new Deflection(5.41052068118, 1);
            Double actualValue = defl.getAsDegreesDouble();
            Assert.AreEqual(expected: expectedValue, actual: actualValue, delta: stdDelta);
        }

        [TestMethod]
        public void Deflection_negativeLessThan180_getAsDegrees()
        {
            Double expectedValue = -45.0;
            Deflection defl = new Deflection(0.785398164, -1);
            Double actualValue = defl.getAsDegreesDouble();
            Assert.AreEqual(expected: expectedValue, actual: actualValue, delta: stdDelta);
        }

        [TestMethod]
        public void Deflection_negativeGreaterThan180_getAsDegrees()
        {
            Double expectedValue = -310.0;
            Deflection defl = new Deflection(5.41052068118, -1);
            Double actualValue = defl.getAsDegreesDouble();
            Assert.AreEqual(expected: expectedValue, actual: actualValue, delta: 0.00001);
        }

        [TestMethod]
        public void Deflection_positiveLessThan180_getAsRadians()
        {
            Double expectedValue = 0.785398164;
            Deflection defl = new Deflection(0.785398164, 1);
            Double actualValue = defl.getAsRadians();
            Assert.AreEqual(expected: expectedValue, actual: actualValue, delta: 0.00001);
        }

        [TestMethod]
        public void Deflection_positiveGreaterThan180_getAsRadians()
        {
            Double expectedValue = 5.41052068118;
            Deflection defl = new Deflection(5.41052068118, 1);
            Double actualValue = defl.getAsRadians();
            Assert.AreEqual(expected: expectedValue, actual: actualValue, delta: 0.00001);
        }

        [TestMethod]
        public void Deflection_negativeLessThan180_getAsRadians()
        {
            Double expectedValue = -0.39479111970;
            Azimuth begAz = new Azimuth(new Point(0.0, 0.0, 0.0), new Point(10.0, 50.0, 0.0));
            Azimuth endAz = new Azimuth(new Point(10.0, 50.0, 0.0), new Point(0.0, 100.0, 0.0));
            Deflection defl = new Deflection(endAz, begAz, true);
            Double actualValue = defl.getAsRadians();
            Assert.AreEqual(expected: expectedValue, actual: actualValue, delta: 0.0000001);
        }

        [TestMethod]
        public void Deflection_negativeGreaterThan180_getAsRadians()
        {
            Double expectedValue = -5.88839418748;
            Azimuth endAz = new Azimuth(new Point(0.0, 0.0, 0.0), new Point(10.0, 50.0, 0.0));
            Azimuth begAz = new Azimuth(new Point(10.0, 50.0, 0.0), new Point(0.0, 100.0, 0.0));
            Deflection defl = new Deflection(endAz, begAz, false);
            Double actualValue = defl.getAsRadians();
            Assert.AreEqual(expected: expectedValue, actual: actualValue, delta: 0.00001);
        }

        [TestMethod]
        public void GenericAlignment_instantiateWithBeginStationOnly()
        {
            testList = new List<Double>();
            testList.Add(1000.0);
            GenericAlignment align = new GenericAlignment(testList);

            double actualDbl = align.BeginStation;
            double expectedDbl = 1000.0;

            Assert.AreEqual(expected: expectedDbl, actual: actualDbl, delta: 0.0000001);
        }

        [TestMethod]
        public void GenericAlignment_instantiateWithBeginAndEndStation()
        {
            testList = new List<Double>();
            testList.Add(1000.0);
            testList.Add(2000.0);
            GenericAlignment align = new GenericAlignment(testList);

            bool actualBool = true;
            actualBool = actualBool && (0 == utilFunctions.tolerantCompare(align.BeginStation, 1000.0, 0.00001));
            actualBool = actualBool && (0 == utilFunctions.tolerantCompare(align.EndStation, 2000.0, 0.00001));

            Assert.AreEqual(expected: true, actual: actualBool);
        }

        //[TestMethod]
        public void HorizontalAlignment_instantiateSingleLine_fromNullFundamentalGeometry()
        {
            //  Assert.That(() => new HorizontalAlignment(fundamentalGeometryList: (List<IFundamentalGeometry>)null,
            //   Name: null, stationEquationing: null), Throws.Exception.TypeOf<NullReferenceException>());
        }

        private List<IFundamentalGeometry> createFundmGeoms1()
        {
            List<IFundamentalGeometry> fundmtlGeoms = new List<IFundamentalGeometry>();

            //MockFundamentalGeometry mockFG = new MockFundamentalGeometry();

            //List<Point> ptLst = new List<Point>();
            //ptLst.Add(new Point(10.0, 10.0, 0.0));
            //ptLst.Add(new Point(80.7106781188, 80.7106781188, 0.0));

            //mockFG.pointList = ptLst;
            //mockFG.expectedType = expectedType.LineSegment;

            //fundmtlGeoms.Add(mockFG);
            return fundmtlGeoms;
        }

        private List<IFundamentalGeometry> createFundmGeoms_arc1()
        {
            List<IFundamentalGeometry> fundmtlGeoms = new List<IFundamentalGeometry>();

            //MockFundamentalGeometry mockFG = new MockFundamentalGeometry();

            //List<Point> ptLst = new List<Point>();
            //ptLst.Add(new Point(443.176112, 569.321807, 0.0));
            //ptLst.Add(new Point(1211.097635, -23.605404, 0.0));
            //ptLst.Add(new Point(1186.397526, 946.268836, 0.0));

            //mockFG.pointList = ptLst;
            //mockFG.expectedType = expectedType.ArcSegmentInsideSolution;

            //fundmtlGeoms.Add(mockFG);
            return fundmtlGeoms;
        }

        private List<IFundamentalGeometry> createFundmGeoms_arc1_butExternalSolution()
        {
            List<IFundamentalGeometry> fundmtlGeoms = new List<IFundamentalGeometry>();

            //MockFundamentalGeometry mockFG2 = new MockFundamentalGeometry();

            //List<Point> ptLst = new List<Point>();
            //ptLst.Add(new Point(443.176112, 569.321807, 0.0));
            //ptLst.Add(new Point(1211.097635, -23.605404, 0.0));
            //ptLst.Add(new Point(1186.397526, 946.268836, 0.0));

            //mockFG2.pointList = ptLst;

            //mockFG2.expectedType = expectedType.ArcSegmentOutsideSoluion;
            //mockFG2.deflectionSign = -1;

            //fundmtlGeoms.Add(mockFG2);
            return fundmtlGeoms;
        }

        [TestMethod]
        public void HorizontalAlignment_ComputeDegreeOfCurveFromRadius()
        {
            Double Radius = 5729.58; Double LengthForDegreeOfCurve = 100.0;
            Angle DegreeOfCurve = new Angle(Radius, LengthForDegreeOfCurve);

            Double actualValue = DegreeOfCurve.getAsDegreesDouble();
            Double expectedValue = 1.00;

            Assert.AreEqual(expected: expectedValue, actual: actualValue, delta: 0.000001);
        }

        //[TestMethod]
        public void HorizontalAlignment_instantiateSingleLine_fromFundamentalGeometry()
        {
            List<IFundamentalGeometry> fundmtlGeoms = createFundmGeoms1();
            
            HorizontalAlignment HA = new HorizontalAlignment(
               fundamentalGeometryList: fundmtlGeoms,
               Name: null, stationEquationing: null);

            Assert.IsNotNull(HA);
        }

        //[TestMethod]
        public void HorizontalAlignment_instantiateSingleLine_fromFundamentalGeometry_HAlengthIs100()
        {
            List<IFundamentalGeometry> fundmtlGeoms = createFundmGeoms1();

            HorizontalAlignment HA = new HorizontalAlignment(
               fundamentalGeometryList: fundmtlGeoms,
               Name: null, stationEquationing: null);

            double actualLength = HA.EndStation - HA.BeginStation;
            double expectedLength = 100.0;

            Assert.AreEqual(expected: expectedLength, actual: actualLength, delta: 0.00001);

        }

        //[TestMethod]
        public void HorizontalAlignment_instantiateSingleArcInterior_fromFundamentalGeometry_HAlengthIs861()
        {
            List<IFundamentalGeometry> fundmtlGeoms = createFundmGeoms_arc1();

            HorizontalAlignment HA = new HorizontalAlignment(
               fundamentalGeometryList: fundmtlGeoms,
               Name: null, stationEquationing: null);

            double actualLength = HA.EndStation - HA.BeginStation;
            double expectedLength = 861.359280;

            Assert.AreEqual(expected: expectedLength, actual: actualLength, delta: 0.00001);

        }

        //[TestMethod]
        public void HorizontalAlignment_instantiateSingleArcExterior_fromFundamentalGeometry_HAlengthIs5235()
        {
            List<IFundamentalGeometry> fundmtlGeoms = createFundmGeoms_arc1_butExternalSolution();

            HorizontalAlignment HA = new HorizontalAlignment(
               fundamentalGeometryList: fundmtlGeoms,
               Name: null, stationEquationing: null);

            double actualLength = HA.EndStation - HA.BeginStation;
            double expectedLength = 5234.5162;

            Assert.AreEqual(expected: expectedLength, actual: actualLength, delta: 0.00001);

        }

        //[TestMethod]
        public void HorizontalAlignment_instantiateFromFundamentalGeometry_shouldNotThrowException()
        {
            var fgList = new List<IFundamentalGeometry>();
            //var funGeomItem = new MockFundamentalGeometry();

            //// Line 1, Item [0], geometric sequence 1
            //funGeomItem.pointList.Add(new Point(2078236.6862, 746878.8969, 0.0));
            //funGeomItem.pointList.Add(new Point(2078312.7571, 746940.1485, 0.0));
            //funGeomItem.expectedType = expectedType.LineSegment;
            //fgList.Add(funGeomItem);

            //// Line 2, Item [1], geometric sequence 3
            //funGeomItem = new MockFundamentalGeometry();
            //funGeomItem.pointList.Add(new Point(2078814.5136, 747096.7721, 0.0));
            //funGeomItem.pointList.Add(new Point(2079329.4845, 747059.2340, 0.0));
            //funGeomItem.expectedType = expectedType.LineSegment;
            //fgList.Add(funGeomItem);

            //// Arc 1, Item [2], geometric sequence 2, oriented backward
            //funGeomItem = new MockFundamentalGeometry();
            //funGeomItem.pointList.Add(new Point(2078814.5136, 747096.7721, 0.0)); // beg matches [0].end
            //funGeomItem.pointList.Add(new Point(2078762.3916, 746381.7284, 0.0)); // center pt
            //funGeomItem.pointList.Add(new Point(2078312.7571, 746940.1485, 0.0)); // end matches [1].begin
            //funGeomItem.expectedType = expectedType.ArcSegmentInsideSolution;
            //funGeomItem.deflectionSign = 1;
            //fgList.Add(funGeomItem);

            //try
            //{
            //    var HA = new HorizontalAlignment(
            //       fundamentalGeometryList: fgList,
            //       Name: null, stationEquationing: null);
            //}
            //catch(Exception e)
            //{ Assert.Fail("Exception not expected: \"" + e.Message + "\""); }
        }

        //[TestMethod]
        public void HorizontalAlignment_instantiate5ItemHA_fromFundamentalGeometry_HAlengthIs7155()
        {
            List<IFundamentalGeometry> fundmtlGeoms = createTestHA_fundGeom1();

            HorizontalAlignment HA = new HorizontalAlignment(
               fundamentalGeometryList: fundmtlGeoms,
               Name: null, stationEquationing: null);

            double actualLength = HA.EndStation - HA.BeginStation;
            double expectedLength = 7154.9385;

            Assert.AreEqual(expected: expectedLength, actual: actualLength, delta: 0.00015);

        }

        private List<IFundamentalGeometry> createTestHA_fundGeom1()
        {
            // Code editors Note:  Items 4 and 5 are deliberately swapped.
            // Please do not change this.  The point of swapping them is to
            //    test the process of putting the items in the correct order.
            var returnList = new List<IFundamentalGeometry>();
            /* var funGeomItem = new MockFundamentalGeometry(); */

            // Line 1, Item 1
            //funGeomItem.pointList.Add(new Point(3556.2226, 2526.6156, 0.0));
            //funGeomItem.pointList.Add(new Point(4932.6214, 4309.2396, 0.0));
            //funGeomItem.expectedType = expectedType.LineSegment;
            //returnList.Add(funGeomItem);

            //// Arc 1, Item 2
            //funGeomItem = new MockFundamentalGeometry();
            //funGeomItem.pointList.Add(new Point(4932.6214, 4309.2396, 0.0));
            //funGeomItem.pointList.Add(new Point(5700.5429, 3716.3124, 0.0));
            //funGeomItem.pointList.Add(new Point(5675.8428, 4686.1866, 0.0));
            //funGeomItem.expectedType = expectedType.ArcSegmentInsideSolution;
            //funGeomItem.deflectionSign = 1;
            //returnList.Add(funGeomItem);

            //// Line 2, Item 3
            //funGeomItem = new MockFundamentalGeometry();
            //funGeomItem.pointList.Add(new Point(5675.8428, 4686.1866, 0.0));
            //funGeomItem.pointList.Add(new Point(6624.6701, 4710.3507, 0.0));
            //funGeomItem.expectedType = expectedType.LineSegment;
            //returnList.Add(funGeomItem);

            //// Line 3, Item 5
            //funGeomItem = new MockFundamentalGeometry();
            //funGeomItem.pointList.Add(new Point(7738.3259, 5168.4199, 0.0));
            //funGeomItem.pointList.Add(new Point(9093.6332, 6443.7163, 0.0));
            //funGeomItem.expectedType = expectedType.LineSegment;
            //returnList.Add(funGeomItem);

            //// Arc 2, Item 4
            //funGeomItem = new MockFundamentalGeometry();
            //funGeomItem.pointList.Add(new Point(6624.6701, 4710.3507, 0.0));
            //funGeomItem.pointList.Add(new Point(6581.7001, 6397.6113, 0.0));
            //funGeomItem.pointList.Add(new Point(7738.3259, 5168.4199, 0.0));
            //funGeomItem.expectedType = expectedType.ArcSegmentInsideSolution;
            //funGeomItem.deflectionSign = -1;
            //returnList.Add(funGeomItem);

            return returnList;
        }

        //[TestMethod]
        public void HorizontalAlignment_singleArcHAinsideSolutionRight_fromFundamentalGeometry_HAlengthIs666()
        {
            //var funGeomItem = new MockFundamentalGeometry();
            //funGeomItem.pointList.Add(new Point(6903.1384, 3830.6151, 0.0));
            //funGeomItem.pointList.Add(new Point(7458.9796, 3830.6151, 0.0));
            //funGeomItem.pointList.Add(new Point(7257.2446, 4348.5557, 0.0));
            //funGeomItem.expectedType = expectedType.ArcSegmentInsideSolution;
            //funGeomItem.deflectionSign = 1;
            //var fGeomList = new List<IFundamentalGeometry>();
            //fGeomList.Add(funGeomItem);


            //HorizontalAlignment HA = new HorizontalAlignment(
            //   fundamentalGeometryList: fGeomList,
            //   Name: null, stationEquationing: null);

            //double actualLength = HA.EndStation - HA.BeginStation;
            //double expectedLength = 666.6644;

            //Assert.AreEqual(expected: expectedLength, actual: actualLength, delta: 0.00015);

        }

        //[TestMethod]
        public void HorizontalAlignment_singleArcHAOutsideSolutionRight_fromFundamentalGeometry_HAlengthIs2439()
        {
            //var funGeomItem = new MockFundamentalGeometry();
            //funGeomItem.pointList.Add(new Point(7415.7202, 4384.7704, 0.0));
            //funGeomItem.pointList.Add(new Point(7458.9796, 3830.6151, 0.0));
            //funGeomItem.pointList.Add(new Point(6947.3437, 3613.3867, 0.0));
            //funGeomItem.expectedType = expectedType.ArcSegmentOutsideSoluion;
            //funGeomItem.deflectionSign = 1;
            //var fGeomList = new List<IFundamentalGeometry>();
            //fGeomList.Add(funGeomItem);


            //HorizontalAlignment HA = new HorizontalAlignment(
            //   fundamentalGeometryList: fGeomList,
            //   Name: null, stationEquationing: null);

            //double actualLength = HA.EndStation - HA.BeginStation;
            //double expectedLength = 2439.4665;

            //Assert.AreEqual(expected: expectedLength, actual: actualLength, delta: 0.00015);

        }

        //[TestMethod]
        public void HorizontalAlignment_singleArcHAInsideSolutionLeft_fromFundamentalGeometry_HAlengthIs1051()
        {
            //var funGeomItem = new MockFundamentalGeometry();
            //funGeomItem.pointList.Add(new Point(4003.3849, 4491.7185, 0.0));
            //funGeomItem.pointList.Add(new Point(3995.0953, 5346.2102, 0.0));
            //funGeomItem.pointList.Add(new Point(4803.1466, 5068.2214, 0.0));
            //funGeomItem.expectedType = expectedType.ArcSegmentInsideSolution;
            //funGeomItem.deflectionSign = -1;
            //var fGeomList = new List<IFundamentalGeometry>();
            //fGeomList.Add(funGeomItem);

            //HorizontalAlignment HA = new HorizontalAlignment(
            //   fundamentalGeometryList: fGeomList,
            //   Name: null, stationEquationing: null);

            //double actualLength = HA.EndStation - HA.BeginStation;
            //double expectedLength = 1050.8644;

            //Assert.AreEqual(expected: expectedLength, actual: actualLength, delta: 0.00015);

        }

        //[TestMethod]
        public void HorizontalAlignment_singleArcHAOutsideSolutionLeft_fromFundamentalGeometry_HAlengthIs4038()
        {

        }

        //[TestMethod]
        public void HorizontalAlignment_singleArcHAOutsideSolutionLeft_fromFundamentalGeometry_HAlengthIs3396()
        {
            //var funGeomItem = new MockFundamentalGeometry();
            //funGeomItem.pointList.Add(new Point(6925.6663, 6218.7689, 0.0));
            //funGeomItem.pointList.Add(new Point(6540.7903, 5615.8802, 0.0));
            //funGeomItem.pointList.Add(new Point(5952.2191, 6022.3138, 0.0));
            //funGeomItem.expectedType = expectedType.ArcSegmentOutsideSoluion;
            //funGeomItem.deflectionSign = 1;
            //var fGeomList = new List<IFundamentalGeometry>();
            //fGeomList.Add(funGeomItem);


            //HorizontalAlignment HA = new HorizontalAlignment(
            //   fundamentalGeometryList: fGeomList,
            //   Name: null, stationEquationing: null);

            //double actualLength = HA.EndStation - HA.BeginStation;
            //double expectedLength = 3396.4881;

            //Assert.AreEqual(expected: expectedLength, actual: actualLength, delta: 0.0045);

        }

        [TestMethod]
        public void angleNormalization_withinPlusOrMinus2Pi_OverPositive2PI()
        {
            Double angleNeedingToBeNormalized = 2 * Math.PI * 4.56;
            Double expectedAfterNormalized = 2 * Math.PI * 0.56;
            Angle anAngle = new Angle();
            Double actualAfterNormalization =
               Angle.ComputeRemainderScaledByDenominator(angleNeedingToBeNormalized, 2 * Math.PI);

            Assert.AreEqual(expected: expectedAfterNormalized,
               actual: actualAfterNormalization, delta: 0.0000001);
        }

        [TestMethod]
        public void angleNormalization_withinPlusOrMinus2Pi_UnderNegative2PI()
        {
            Double angleNeedingToBeNormalized = -710.0;
            Double expectedAfterNormalized = -350.0;
            Angle anAngle = new Angle();
            Double actualAfterNormalization =
               Angle.ComputeRemainderScaledByDenominator(angleNeedingToBeNormalized, 360.0);

            Assert.AreEqual(expected: expectedAfterNormalized,
               actual: actualAfterNormalization, delta: 0.0000001);
        }

        //[TestMethod]
        public void HorizontalAlignment_givenStationOffsetValues_getXYvalues()
        {
            bool allValuesAgree = true;
            List<IFundamentalGeometry> fundmtlGeoms = createTestHA_fundGeom1();

            HorizontalAlignment HA = new HorizontalAlignment(
               fundamentalGeometryList: fundmtlGeoms,
               Name: null, stationEquationing: null);

            StationOffsetElevation anSOE = new StationOffsetElevation();

            // test a point right of the second line segment (the third segment)
            anSOE.station = 3611.75; anSOE.offset.OFST = 238.949;
            Point anXYpoint = HA.getXYZcoordinates(anSOE);
            if (anXYpoint != null)
            {
                allValuesAgree &= anXYpoint.x.tolerantEquals(6180.0, 0.014);
                allValuesAgree &= anXYpoint.y.tolerantEquals(4460.0, 0.014);
                allValuesAgree &= anXYpoint.z.tolerantEquals(0.0, 0.000001);
            }
            else
            {
                allValuesAgree = false;
            }

            // test a point right of the second arc (the fourth segment)
            anSOE.station = 4469.2978; anSOE.offset.OFST = 138.1336;
            anXYpoint = HA.getXYZcoordinates(anSOE);
            if (anXYpoint != null)
            {
                allValuesAgree &= anXYpoint.x.tolerantEquals(7062.3839, 0.014);
                allValuesAgree &= anXYpoint.y.tolerantEquals(4636.0766, 0.014);
                allValuesAgree &= anXYpoint.z.tolerantEquals(0.0, 0.000001);
            }
            else
            {
                allValuesAgree = false;
            }

            Assert.IsTrue(allValuesAgree);
        }

        //[TestMethod]
        public void HorizontalAlignment_givenBeginStationAndLeftOffset_getXYcoordinates()
        {
            var HA = new HorizontalAlignment(
               fundamentalGeometryList: createTestHA_fundGeom1(),
               Name: null, stationEquationing: null);

            var actual = HA.getXYZcoordinates(0.0, -20.0, 0.0);
            Assert.AreEqual(expected: 3540.3922, actual: actual.x, delta: 0.00015);
            Assert.AreEqual(expected: 2538.8385, actual: actual.y, delta: 0.00015);

        }

        [TestMethod]
        public void ArcSegment_ctor1_correct()
        {
            var begPt = new Point(1293788.6543, 963246.6113);
            var endPt = new Point(1293807.3081, 963594.4737);
            var inAz = Azimuth.fromDegreesDouble(337.39838);
            var radius = 402.0756;

            //var anArc = new HorArc(begPt, endPt, inAz, radius);
            var anArc = HorArcSegment.Create(begPt, endPt, inAz, radius);

            var actualDeflection = anArc.Deflection.getAsDegreesDouble();
            Assert.AreEqual(expected: 51.34223, actual: actualDeflection, delta: 0.00015);

            var actualLength = anArc.Length;
            Assert.AreEqual(expected: 360.2963, actual: actualLength, delta: 0.00015);
        }

        //[TestMethod]
        public void HorizontalAlignment_givenXYvalues_getStationOffsetValues()
        {
            List<IFundamentalGeometry> fundmtlGeoms = createTestHA_fundGeom1();

            HorizontalAlignment HA = new HorizontalAlignment(
               fundamentalGeometryList: fundmtlGeoms,
               Name: null, stationEquationing: null);

            StationOffsetElevation anSOE = null;
            bool allValuesAgree = true;

            // test point on first tangent
            Point somePoint = new Point(4046.2915, 3161.3216, 0.0);
            var soePoints = HA.getStationOffsetElevation(somePoint);
            if (soePoints != null && soePoints.Count > 0)
            {
                anSOE = soePoints.FirstOrDefault();
                allValuesAgree &= anSOE.station.tolerantEquals(801.8849, 0.00014);
                allValuesAgree &= anSOE.offset.OFST.tolerantEquals(0.0, 0.00014);
                allValuesAgree &= anSOE.elevation.EL.tolerantEquals(0.0, 0.00000001);
            }
            else
            {
                allValuesAgree = false;
            }

            // test point which is before the beginning of the HA
            somePoint = new Point(2500.0, 1000.0, 0.0);
            soePoints = HA.getStationOffsetElevation(somePoint);
            allValuesAgree &= soePoints.Count == 0;

            // test point which is beyond the end of the HA
            somePoint = new Point(9554.0, 9000.0, 0.0);
            soePoints = HA.getStationOffsetElevation(somePoint);
            allValuesAgree &= soePoints.Count == 0;

            // test point offset from first tangent
            somePoint.x = 4516.0; somePoint.y = 3404.0;
            soePoints = HA.getStationOffsetElevation(somePoint);
            if (soePoints != null && soePoints.Count > 0)
            {
                anSOE = soePoints.FirstOrDefault();
                allValuesAgree &= anSOE.station.tolerantEquals(1281.0297, 0.00014);
                allValuesAgree &= anSOE.offset.OFST.tolerantEquals(223.4706, 0.00014);
                allValuesAgree &= anSOE.elevation.EL.tolerantEquals(0.0, 0.000001);
            }
            else
            {
                allValuesAgree = false;
            }

            // test point on first arc
            somePoint.x = 5494.3772; somePoint.y = 4664.3429;
            soePoints = HA.getStationOffsetElevation(somePoint);
            if (soePoints != null && soePoints.Count > 0)
            {
                anSOE = soePoints.FirstOrDefault();
                allValuesAgree &= anSOE.station.tolerantEquals(2930.4718, 0.00014);
                allValuesAgree &= anSOE.offset.OFST.tolerantEquals(0.0, 0.00014);
                allValuesAgree &= anSOE.elevation.EL.tolerantEquals(0.0, 0.000001);
            }
            else
            {
                allValuesAgree = false;
            }

            // test point offset right of second arc
            somePoint.x = 6918.0; somePoint.y = 4557.0;
            soePoints = HA.getStationOffsetElevation(somePoint);
            if (soePoints != null && soePoints.Count > 0)
            {
                anSOE = soePoints.FirstOrDefault();
                allValuesAgree &= anSOE.station.tolerantEquals(4324.6956, 0.00014);
                allValuesAgree &= anSOE.offset.OFST.tolerantEquals(183.2743, 0.00014);
                allValuesAgree &= anSOE.elevation.EL.tolerantEquals(0.0, 0.000001);
            }
            else
            {
                allValuesAgree = false;
            }

            // test point offset left of second arc
            somePoint.x = 7103.0; somePoint.y = 4979.0;
            soePoints = HA.getStationOffsetElevation(somePoint);
            if (soePoints != null && soePoints.Count > 0)
            {
                anSOE = soePoints.FirstOrDefault();
                allValuesAgree &= anSOE.station.tolerantEquals(4614.0481, 0.00014);
                allValuesAgree &= anSOE.offset.OFST.tolerantEquals(-176.4468, 0.00014);
                allValuesAgree &= anSOE.elevation.EL.tolerantEquals(0.0, 0.000001);
            }
            else
            {
                allValuesAgree = false;
            }

            // test point offset left of third line (fifth segment
            somePoint.x = 8071.7032; somePoint.y = 5913.8278;
            soePoints = HA.getStationOffsetElevation(somePoint);
            if (soePoints != null && soePoints.Count > 0)
            {
                anSOE = soePoints.FirstOrDefault();
                allValuesAgree &= anSOE.station.tolerantEquals(6047.5668, 0.00014);
                allValuesAgree &= anSOE.offset.OFST.tolerantEquals(-314.4057, 0.00014);
                allValuesAgree &= anSOE.elevation.EL.tolerantEquals(0.0, 0.000001);
            }
            else
            {
                allValuesAgree = false;
            }

            // test point offset at center of first arc
            somePoint.x = 5700.5429; somePoint.y = 3716.3124;
            soePoints = HA.getStationOffsetElevation(somePoint);
            if (soePoints != null && soePoints.Count > 0)
            {
                anSOE = soePoints.FirstOrDefault();
                allValuesAgree &= anSOE.station.tolerantEquals(2252.1594, 0.00014);
                allValuesAgree &= anSOE.offset.OFST.tolerantEquals(970.1887, 0.00014);
                allValuesAgree &= anSOE.elevation.EL.tolerantEquals(0.0, 0.000001);
            }
            else
            {
                allValuesAgree = false;
            }

            // test point offset right that returns 2 SOE instances for the same point
            somePoint.x = 5940.0; somePoint.y = 3310.0;
            soePoints = HA.getStationOffsetElevation(somePoint);
            if (soePoints != null && soePoints.Count > 0)
            {
                allValuesAgree &= (soePoints.Count == 2);
                anSOE = soePoints.FirstOrDefault();
                allValuesAgree &= anSOE.station.tolerantEquals(3342.5539, 0.00014);
                allValuesAgree &= anSOE.offset.OFST.tolerantEquals(1382.4657, 0.00014);
                allValuesAgree &= anSOE.elevation.EL.tolerantEquals(0.0, 0.000001);
            }
            else
            {
                allValuesAgree = false;
            }

            Assert.IsTrue(allValuesAgree);
        }

        private HorizontalAlignment createSingleTangentHA()
        {
            var newHA = new HorizontalAlignment();
            newHA.reset(new Point(2082268.0907, 740846.3249, 0.0),
               new Point(2082339.9608, 740834.3849, 0.0));
            return newHA;
        }

        [TestMethod]
        public void HorizontalAlignment_from2points_producesCorrectValues()
        {
            HorizontalAlignment HA = createSingleTangentHA();

            double expectedDbl = 72.8552;
            double actualDbl = (Double)HA.EndStation;
            Assert.AreEqual(expected: expectedDbl, actual: actualDbl, delta: 0.0001);

            expectedDbl = 99.43257281767;
            Azimuth az = HA.EndAzimuth;
            actualDbl = az.getAsDegreesDouble();
            Assert.AreEqual(expected: expectedDbl, actual: actualDbl, delta: 0.0001);

            HA.reset(new Point(100.0, 100.0, 0.0), new Point(80.0, 150.0, 0.0));

            expectedDbl = 53.8516480713;
            actualDbl = (Double)HA.EndStation;
            Assert.AreEqual(expected: expectedDbl, actual: actualDbl, delta: 0.0001);

            expectedDbl = 338.198590514;
            actualDbl = HA.EndAzimuth.getAsDegreesDouble();
            Assert.AreEqual(expected: expectedDbl, actual: actualDbl, delta: 0.0001);

        }

        [TestMethod]
        public void HorizontalAlignment_appendSimpleArcToTangent_DPonAlignment_EndStationIs391p78()
        {
            Double radius = 1170.0;
            var HA = createSingleTangentHA();
            HA.appendArc(ArcEndPoint: new Point(2082657.7727, 740825.3769, 0.0),
               radius: radius);

            Double expectedDbl = 391.7812;
            Double actualDbl = (Double)HA.EndStation;
            Assert.AreEqual(expected: expectedDbl, actual: actualDbl, delta: 0.0001);

            expectedDbl = -15.61806;
            actualDbl = HA.GetElementByStation(390.0).Deflection.getAsDegreesDouble();
            Assert.AreEqual(expected: expectedDbl, actual: actualDbl, delta: 0.0001);
        }

        [TestMethod]
        public void HorizontalAlignment_appendSimpleArcToTangent_DPoffAlignment_EndStationIs391p78()
        {
            Double radius = 1170.0;
            var HA = createSingleTangentHA();
            HA.appendArc(ArcEndPoint: new Point(2082652.9833, 740869.5684, 0.0),
               radius: radius);

            Double expectedDbl = 391.7812;
            Double actualDbl = (Double)HA.EndStation;
            Assert.AreEqual(expected: expectedDbl, actual: actualDbl, delta: 0.0001);

            expectedDbl = -15.61806;
            actualDbl = HA.GetElementByStation(390.0).Deflection.getAsDegreesDouble();
            Assert.AreEqual(expected: expectedDbl, actual: actualDbl, delta: 0.0001);
        }

        [TestMethod]
        public void HorizontalAlignment_appendTangentToSimpleArc_EndStationIs481p78()
        {
            Double radius = 1170.0;
            var HA = createSingleTangentHA();
            HA.appendArc(ArcEndPoint: new Point(2082660.0, 740870.0, 0.0),
               radius: radius);

            Double expectedDbl = -15.618046;
            Double actualDbl = HA.GetElementByStation(390.0).Deflection.getAsDegreesDouble();
            Assert.AreNotEqual(notExpected: expectedDbl, actual: actualDbl);

            HA.appendTangent(TangentEndPoint: new Point(2082747.242780, 740835.073448, 0.0));

            expectedDbl = -15.61806;
            actualDbl = HA.GetElementByStation(390.0).Deflection.getAsDegreesDouble();
            Assert.AreEqual(expected: expectedDbl, actual: actualDbl, delta: 0.000045);
            // Note: The error is 1 inch over 20 miles -- acceptable accuracy.
        }

        [TestMethod]
        public void HorizontalAlignment_appendTangentToSimpleArc_ArcEndPtAtRightLocation()
        {
            Double radius = 1170.0;
            var HA = createSingleTangentHA();
            HA.appendArc(ArcEndPoint: new Point(2082660.0, 740870.0, 0.0),
               radius: radius);

            Double expectedDbl = -15.618046;
            Double actualDbl = HA.GetElementByStation(390.0).Deflection.getAsDegreesDouble();
            Assert.AreNotEqual(notExpected: expectedDbl, actual: actualDbl);

            HA.appendTangent(TangentEndPoint: new Point(2082747.242780, 740835.073448, 0.0));

            Point arcEndPt = HA.GetElementByStation(390.0).EndPoint;
            expectedDbl = 2082657.772726;
            actualDbl = arcEndPt.x;
            Assert.AreEqual(expected: expectedDbl, actual: actualDbl, delta: 0.001);

            expectedDbl = 740825.376857;
            actualDbl = arcEndPt.y;
            Assert.AreEqual(expected: expectedDbl, actual: actualDbl, delta: 0.001);
        }

        [TestMethod]
        public void HorizontalAlignment_appendTangentToSimpleArc_DeflectionIsToTheRight()
        {
            var HA = new HorizontalAlignment();
            HA.reset(new Point(2083115.2982, 740931.8698, 0.0),
               new Point(X: 2083652.8068, Y: 741163.1806, Z: 0.0));

            HA.appendArc(new Point(X: 2084073.2987, Y: 741255.4852, Z: 0.0), 1138.0);
            HA.appendTangent(new Point(X: 2084518.1573, Y: 741266.9617, Z: 0.0));

            Double expectedDbl = 21.806287;
            Double actualDbl = HA.GetElementByStation(1010.0).Deflection.getAsDegreesDouble();
            Assert.AreEqual(expected: expectedDbl, actual: actualDbl, delta: 0.000045);
        }

        private HorizontalAlignment buildFrehandHAforTesting()
        {
            var retHA = new HorizontalAlignment();
            retHA.reset(new Point(X: 2082268.0907, Y: 740846.3249, Z: 0.0),
               new Point(X: 2082339.9608, Y: 740834.3849, Z: 0.0));

            retHA.appendArc(new Point(X: 2082657.7727, Y: 740825.3769, Z: 0.0), 1170.0);
            retHA.appendTangent(new Point(X: 2082747.2428, Y: 740835.0734, Z: 0.0));

            retHA.appendArc(new Point(X: 2083115.2982, Y: 740931.8698, Z: 0.0), 1280.0);
            retHA.appendTangent(new Point(X: 2083652.8068, Y: 741163.1806, Z: 0.0));

            retHA.appendArc(new Point(X: 2084073.2987, Y: 741255.4852, Z: 0.0), 1138.0);
            retHA.appendTangent(new Point(X: 2084518.1573, Y: 741266.9617, Z: 0.0));

            return retHA;
        }

        [TestMethod]
        public void HorizontalAlignment_buildFreehand_EndStationIs2327p0486()
        {
            var HA = buildFrehandHAforTesting();
            Assert.IsNotNull(HA);

            Double expected = 2327.0486;
            Double actual = HA.Length;
            Assert.AreEqual(expected: expected, actual: actual, delta: 0.0025);
        }

        [TestMethod]
        public void HorizontalAlignment_buildFreehand_PointIs8LeftOf2000()
        {
            var HA = buildFrehandHAforTesting();
            Assert.IsNotNull(HA);

            StationOffsetElevation soe = new StationOffsetElevation(2000.0, -8.0, 0.0);
            var pt = HA.getXYZcoordinates(soe);

            Double expectedX = 2084191.011134;
            Double actualX = pt.x;
            Assert.AreEqual(expected: expectedX, actual: actualX, delta: 0.0025);
            Double expectedY = 741266.52458;
            Double actualY = pt.y;
            Assert.AreEqual(expected: expectedY, actual: actualY, delta: 0.0025);
        }

        [Ignore]
        [TestMethod]
        public void HorizontalAlignment_buildFreehand_5LeftOf2200isPoint()
        {
            var HA = buildFrehandHAforTesting();
            Assert.IsNotNull(HA);

            var soe = HA.getStationOffsetElevation(new Point(2084391.021982, 741268.683499));
            Assert.IsNotNull(soe);
            Assert.AreEqual(expected: 1, actual: soe.Count);

            Double expectedStation = 2200.0;
            Double actualStation = soe[0].station;
            Assert.AreEqual(expected: expectedStation, actual: actualStation, delta: 0.0025);
            Double expectedOffset = -5.0;
            Double actualOffset = soe[0].offset;
            Assert.AreEqual(expected: expectedOffset, actual: actualOffset, delta: 0.0025);
        }

        [TestMethod]
        public void HorizontalAlignment_instantiateArcSegment_RayForm_isCorrect()
        {
            Azimuth startAz = 77.5488;
            var beginRay = new Ray(
                new Point(2152973.8702, 735330.7239),
                startAz
                );
            var expectedEndRay = new Ray(
                new Point(2153151.3148, 735350.0147),
                Azimuth.fromDegreesDouble(90.0421)
                );
            double arcLength = 178.8443;
            double radius = 820.2083;
            double Dc = radius.dblDegreeFromRadius();
            HorArcSegment newArcRight = (HorArcSegment)HorizontalAlignment.newSegment(beginRay, Dc, arcLength, Dc);

            var expCenterPt = new Point(2153150.7135, 734529.8066);
            Assert.AreEqual(expected: expCenterPt, actual: newArcRight.ArcCenterPt);

            Azimuth expAheadAz = 90.0421;
            Assert.AreEqual(expected: expAheadAz, actual: newArcRight.EndAzimuth);

            double expLongChordLength = 178.4902;
            Assert.AreEqual(expected: expLongChordLength, actual: newArcRight.LongChordVector.Length, delta: 0.0005);

            Assert.AreEqual(expected: arcLength, actual: newArcRight.Length, delta: 0.0005);

            int expectedDeflSign = 1;
            int actualDeflSign = newArcRight.Deflection.deflectionDirection;
            Assert.AreEqual(expected: expectedDeflSign, actual: actualDeflSign);

            // now flip it to be curving left and recheck.
            Deflection arcDef = newArcRight.Deflection * -1.0;
            Dc *= -1.0;
            HorArcSegment newArcLeft = (HorArcSegment)HorizontalAlignment.newSegment(beginRay, Dc, arcLength, Dc);

            expCenterPt = new Point(2152797.0270, 736131.6405);
            //Assert.AreEqual(expected: expCenterPt, actual: newArcLeft.ArcCenterPt);

            expAheadAz = 65.0556;
            Assert.AreEqual(expected: expAheadAz, actual: newArcLeft.EndAzimuth);

            expLongChordLength = 178.4902;
            Assert.AreEqual(expected: expLongChordLength, actual: newArcLeft.LongChordVector.Length, delta: 0.0005);

            Assert.AreEqual(expected: arcLength, actual: newArcLeft.Length, delta: 0.0005);

            expectedDeflSign = -1;
            actualDeflSign = newArcLeft.Deflection.deflectionDirection;
            Assert.AreEqual(expected: expectedDeflSign, actual: actualDeflSign);
        }

        [TestMethod]
        public void HorizontalAlignment_instantiateTangent_RayForm_isCorrect()
        {
            Azimuth startAz = 110.4920;
            var beginRay = new Ray(
                new Point(2139755.8215, 735223.4531),
                startAz
                );
            var expectedEndRay = new Ray(
                new Point(2139970.4859, 735143.2274),
                Azimuth.fromDegreesDouble(110.4920)
                );

            double tangentLength = 229.1658;
            var newTangent = HorizontalAlignment.newSegment(beginRay, 0.0, tangentLength, 0.0);

            Assert.AreEqual(expected: beginRay, actual: newTangent.BeginRay);
            Assert.AreEqual(expected: expectedEndRay, actual: newTangent.EndRay);

        }

        //[TestMethod]
        public void HorizontalAlignment_outputControlPointsToCSV()
        {
            var directory = new DirectoryManager();
            directory.CdUp(2).CdDown("Datasets").CdDown("Cogo");
            string testFile = directory.GetPathAndAppendFilename("R2100_L1.CogoDN.csv");

            HorizontalAlignment AccRev = HorizontalAlignment.createFromCsvFile(testFile);
            Assert.IsNotNull(AccRev);

            var outDirectory = new DirectoryManager();
            outDirectory.CdUp(2).CdDown("Datasets").CdDown("Cogo").CdDown("outputs");
            outDirectory.EnsureExists();
            string outFile = outDirectory.GetPathAndAppendFilename("ACC_REV_events.csv");

            AccRev.WriteEventPointsToCSV(outFile);

            bool fileExists = File.Exists(outFile);
            Assert.IsTrue(fileExists);
        }

        [TestMethod]
        public void HorizontalAlignment_outputToDxf()
        {
            var directory = new DirectoryManager();
            directory.CdUp(2).CdDown("Datasets").CdDown("Cogo");
            string testFile = directory.GetPathAndAppendFilename("R2100_L1.CogoDN.csv");

            HorizontalAlignment AccRev = HorizontalAlignment.createFromCsvFile(testFile);
            Assert.IsNotNull(AccRev);

            var outDirectory = new DirectoryManager();
            outDirectory.CdUp(2).CdDown("Datasets").CdDown("Cogo").CdDown("outputs");
            outDirectory.EnsureExists();
            string outFile = outDirectory.GetPathAndAppendFilename("R2100b_L1.dxf");

            AccRev.WriteToDxf(outFile);

            bool fileExists = File.Exists(outFile);
            Assert.IsTrue(fileExists);
        }

        [TestMethod]
        public void HorizontalAlignment_instantiates_fromCSV_noSpirals()
        {
            var directory = new DirectoryManager();
            directory.CdUp(2).CdDown("Datasets").CdDown("Cogo").CdDown("R2547");
            string testFile = directory.GetPathAndAppendFilename("ACC_REV.csv");

            HorizontalAlignment AccRev = HorizontalAlignment.createFromCsvFile(testFile);
            Assert.IsNotNull(AccRev);

            var actualItemCount = AccRev.childCount();
            Assert.AreEqual(expected: 17, actual: actualItemCount);

            Assert.AreEqual(expected: new Point(2139755.8215, 735223.4531),
                actual: AccRev.BeginPoint);

            Assert.AreEqual(expected: new Point(2142363.2412, 734302.9399),
                actual: AccRev.EndPoint);

            var actualStation = AccRev.BeginStation;
            double expectedStation = 1000.0;
            Assert.AreEqual(expected: expectedStation,
                actual: actualStation, delta: stdDelta);

            expectedStation = 3895.032;
            Assert.AreEqual(expected: expectedStation,
                actual: AccRev.EndStation, delta: 0.00011);
        }

        // Todo: Add test for instantiate from csv file in which the stationing
        // includes at least one equality

        private void SpiralType2_LeftTurning_proof(HorSpiralSegment aSpiral)
        {
            Assert.AreEqual(expected: 2, actual: aSpiral.SpiralType);

            var expectedLongChordVector = new Vector(x_: -117.5485, y_: 23.2791);
            var actualLongChordVector = aSpiral.LongChordVector;
            Assert.AreEqual(expected: expectedLongChordVector.Azimuth.getAsDegreesDouble(),
                actual: actualLongChordVector.Azimuth.getAsDegreesDouble(),
                delta: 0.0001);
            Assert.AreEqual(expected: expectedLongChordVector.Length,
                actual: actualLongChordVector.Length,
                delta: 0.001);
            //Assert.AreEqual(expected: expectedLongChordVector,
            //    actual: actualLongChordVector);

            // Expected value source: Microstation file for NCDOT project R-2100B.
            var expectedEndRay = new Ray(1295257.0660, 962242.6880, 277.80329);
            var actualEndRay = aSpiral.EndRay;
            var pointDist = (expectedEndRay.StartPoint - actualEndRay.StartPoint).Length;
            Assert.AreEqual(expected: 0.0,
                actual: pointDist,
                delta: 0.004);
        }

        private void SpiralType2_RightTurning_proof(HorSpiralSegment aSpiral)
        {
            Assert.AreEqual(expected: 2, actual: aSpiral.SpiralType);

            var expectedLongChordVector = new Vector(x_: -58.8425, y_: 104.4418);
            var expectAz = expectedLongChordVector.Azimuth.getAsDegreesDouble();
            var actualLongChordVector = aSpiral.LongChordVector;
            var actualAz = actualLongChordVector.Azimuth.getAsDegreesDouble();

            Assert.AreEqual(expected: expectAz,
                actual: actualAz,
                delta: 0.0001);
            Assert.AreEqual(expected: expectedLongChordVector.Length,
                actual: actualLongChordVector.Length,
                delta: 0.001);
            //Assert.AreEqual(expected: expectedLongChordVector,
            //    actual: actualLongChordVector);

            // Expected value source: Microstation file for NCDOT project R-2100B.
            var expectedEndRay = new Ray(1294823.8430, 962526.7940, 333.50256);
            var actualEndRay = aSpiral.EndRay;
            var pointDist = (expectedEndRay.StartPoint - actualEndRay.StartPoint).Length;
            Assert.AreEqual(expected: 0.0,
                actual: pointDist,
                delta: 0.005);
        }

        private void SpiralType1_RightTurning_proof(HorSpiralSegment aSpiral)
        {
            Assert.AreEqual(expected: 1, actual: aSpiral.SpiralType);
            //for right-turning type 1 spirals, spiralY is being added in the wrong direction.
            var expectedLongChordVector = new Vector(x_: -117.7917, y_: 22.2628);
            var actualLongChordVector = aSpiral.LongChordVector;
            Assert.AreEqual(expected: expectedLongChordVector.Azimuth.getAsDegreesDouble(),
                actual: actualLongChordVector.Azimuth.getAsDegreesDouble(),
                delta: 0.001);
            Assert.AreEqual(expected: expectedLongChordVector.Length,
                actual: actualLongChordVector.Length,
                delta: 0.001);
            //Assert.AreEqual(expected: expectedLongChordVector,
            //    actual: actualLongChordVector);

            // Expected value source: Microstation file for NCDOT project R-2100B.
            var expectedEndRay = new Ray(1295093.3392, 962271.2450, 41.39871);
            var actualEndRay = aSpiral.EndRay;
            var pointDist = (expectedEndRay.StartPoint - actualEndRay.StartPoint).Length;
            Assert.AreEqual(expected: 0.0,
                actual: pointDist,
                delta: 0.005);
        }

        [TestMethod]
        public void Type2Spiral_instantiatesCorrectly_fromCSV()
        {
            var directory = new DirectoryManager();
            directory.CdUp(2).CdDown("Datasets").CdDown("Cogo");
            //string testFile = directory.GetPathAndAppendFilename("R2100_L1.CogoDN.csv");
            string testFile = directory.GetPathAndAppendFilename("R2100_L1_workingPart.CogoDN.csv");

            HorizontalAlignment L1 = HorizontalAlignment.createFromCsvFile(testFile);
            Assert.IsNotNull(L1);

            var aT2Spiral = L1.getChildBySequenceNumber(3) as HorSpiralSegment;
            Assert.IsNotNull(aT2Spiral.AnchorPoint);
        }

        [TestMethod]
        public void HorizontalAlignment_instantiates_fromCSV_withSpirals()
        {
            var directory = new DirectoryManager();
            directory.CdUp(2).CdDown("Datasets").CdDown("Cogo");
            //string testFile = directory.GetPathAndAppendFilename("R2100_L1.CogoDN.csv");
            string testFile = directory.GetPathAndAppendFilename("R2100_L1_workingPart.CogoDN.csv");

            HorizontalAlignment L1 = HorizontalAlignment.createFromCsvFile(testFile);
            Assert.IsNotNull(L1);

            var actualItemCount = L1.childCount();
            // Assert.AreEqual(expected: 45, actual: actualItemCount);
            //Assert.AreEqual(expected: 21, actual: actualItemCount);

            SpiralType1_LeftTurning_proof(L1.getChildBySequenceNumber(1) as HorSpiralSegment);
            SpiralType2_LeftTurning_proof(L1.getChildBySequenceNumber(7) as HorSpiralSegment);
            SpiralType1_RightTurning_proof(L1.getChildBySequenceNumber(9) as HorSpiralSegment);
            SpiralType2_RightTurning_proof(L1.getChildBySequenceNumber(11) as HorSpiralSegment);

            Assert.AreEqual(expected: new Point(1296205.4529, 960387.001),
                actual: L1.BeginPoint);

            // Test point on the first element, a line segment
            var aPoint = L1.getXYZcoordinates(new StationOffsetElevation(25400.00, 0.0));
            Assert.AreEqual(expected: new Point(1296178.105, 960483.189),
                actual: aPoint);

            // Test point on the second element, a left turning Type I spiral
            aPoint = L1.getXYZcoordinates(new StationOffsetElevation(25500.00, 0.0));
            Assert.AreEqual(expected: new Point(1296150.757, 960579.375),
                actual: aPoint);

            // Test point on the third element, a left turning arc
            aPoint = L1.getXYZcoordinates(new StationOffsetElevation(25600.00, 0.0));
            Assert.AreEqual(expected: new Point(1296122.927, 960675.425),
                actual: aPoint);

            // Test point on the fourth element, a left turning type 2 spiral
            // Type 2 spirals are not complete, so this is temporarily removed.
            //aPoint = L1.getXYZcoordinates(new StationOffsetElevation(26790.00, 0.0));
            //Assert.AreEqual(expected: new Point(1295651.085, 961885.208),
            //    actual: aPoint);
            // All following point will be wrong.

            // Test point on the fifth element, a line segment
            //aPoint = L1.getXYZcoordinates(new StationOffsetElevation(26900.00, 0.0));
            //Assert.AreEqual(expected: new Point(1295651.085, 961885.208),
            //    actual: aPoint);

            // Test point on the eighth element, a line segment
            //aPoint = L1.getXYZcoordinates(new StationOffsetElevation(27500.00, 0.0));
            //Assert.AreEqual(expected: new Point(1295227.119, 962246.792),
            //    actual: aPoint);

            // Test point on the tenth element, an right-turning arc
            //aPoint = L1.getXYZcoordinates(new StationOffsetElevation(27700.00, 0.0));
            //Assert.AreEqual(expected: new Point(1295033.836, 962294.245),
            //    actual: aPoint);

            // Test point on the tenth element, line segment
            //aPoint = L1.getXYZcoordinates(new StationOffsetElevation(28100.00, 0.0));
            //Assert.AreEqual(expected: new Point(1294788.272, 962598.146),
            //    actual: aPoint);

            // Test point on the tenth element, a line segment
            //aPoint = L1.getXYZcoordinates(new StationOffsetElevation(28100.00, 0.0));
            //Assert.AreEqual(expected: new Point(1294788.272, 962598.146),
            //    actual: aPoint);

            // Test point on the eighteenth element, a line segment
            //aPoint = L1.getXYZcoordinates(new StationOffsetElevation(29800.00, 0.0));
            //Assert.AreEqual(expected: new Point(1293789.463, 963557.593),
            //    actual: aPoint);

            // Test point on the twentieth element, a line segment
            //aPoint = L1.getXYZcoordinates(new StationOffsetElevation(30000.00, 0.0));
            //Assert.AreEqual(expected: new Point(1293898.743, 963724.377),
            //    actual: aPoint);

            // Test point on the 42th element, a line segment
            //aPoint = L1.getXYZcoordinates(new StationOffsetElevation(37200.00, 0.0));
            //Assert.AreEqual(expected: new Point(1291899.844, 970381.041),
            //    actual: aPoint);

            // Test point on the 44th element, a line segment
            //aPoint = L1.getXYZcoordinates(new StationOffsetElevation(38200.00, 0.0));
            //Assert.AreEqual(expected: new Point(1291648.733, 971348.367),
            //    actual: aPoint);

            // End point for the full alignment, but it doesn't work yet.
            //Assert.AreEqual(expected: new Point(1291644.4100, 971366.237),
            //    actual: L1.EndPoint);

            // end point for R2100_L1_workingPart.CogoDN.csv
            //var expected = new Point(1293915.761, 963746.724);
            //var deltaVector = L1.EndPoint - expected;
            //Assert.AreEqual(expected: deltaVector.Length, actual: 0.0, delta: 0.015);

            var actualStation = L1.BeginStation;
            double expectedStation = 25300.0;
            Assert.AreEqual(expected: expectedStation,
                actual: actualStation, delta: stdDelta);

            // expectedStation = 38218.3855;
            //expectedStation = 30028.09;
            //Assert.AreEqual(expected: expectedStation,
            //    actual: L1.EndStation, delta: 0.0099);
        }

        private void SpiralType1_LeftTurning_proof(HorSpiralSegment aSpiral)
        {
            Assert.AreEqual(expected: 1,
                actual: aSpiral.SpiralType);

            Assert.AreEqual(expected: new Point(1296158.5690, 960551.8990),
                actual: aSpiral.BeginPoint);

            Assert.AreEqual(expected: 344.12846,
                actual: aSpiral.BeginAzimuth.getAsDegreesDouble(),
                delta: 0.0001);

            Assert.AreEqual(expected: 25471.4333,
                actual: aSpiral.BeginStation,
                delta: 0.0005);

            Assert.AreEqual(expected: 0.0,
                actual: aSpiral.BeginDegreeOfCurve.getAsDegreesDouble(),
                delta: 0.0001);

            Assert.AreEqual(expected: -0.3375,
                actual: aSpiral.Deflection.getAsDegreesDouble(),
                delta: 0.0001);

            Assert.AreEqual(expected: 90.0,
                actual: aSpiral.Length,
                delta: 0.0001);

            Assert.AreEqual(expected: 0.1767,
                actual: aSpiral.spiralDY.Length,
                delta: 0.0005);

            Assert.AreEqual(expected: 89.9997,
                actual: aSpiral.spiralDX.Length,
                delta: 0.0005);

            Assert.AreEqual(expected: 60.0001,
                actual: aSpiral.spiralU.Length,
                delta: 0.0002);

            Assert.AreEqual(expected: 30.0001,
                actual: aSpiral.spiralV.Length,
                delta: 0.0002);

            // Next: Check EndStation, Bearing, Point.
            Assert.AreEqual(expected: 25561.4342,
                actual: aSpiral.EndStation,
                delta: 0.002);

            Assert.AreEqual(expected: 343.79103,
                actual: aSpiral.EndAzimuth.getAsDegreesDouble(),
                delta: 0.0005);

            var expectedEndPt = new Point(1296133.7860, 960638.4200);
            var actualEndPt = aSpiral.EndPoint;
            Assert.AreEqual(expected: expectedEndPt,
                actual: actualEndPt);

            var expectedLongChordVectorLength = 90.0001;
            var expectedLongChordVectorAzimuth = 344.01619;
            var actualLongChordVector = aSpiral.LongChordVector;
            Assert.IsNotNull(actualLongChordVector);
            Assert.AreEqual(expected: expectedLongChordVectorLength,
                actual: actualLongChordVector.Length,
                delta: 0.001);
            Assert.AreEqual(expected: expectedLongChordVectorAzimuth,
                actual: actualLongChordVector.Azimuth.getAsDegreesDouble(),
                delta: 0.0005);

        }

        /// <summary>
        /// Note: At this time (5 Jan 2018), functionality for getStationOffsetElevation()
        /// has not been implemented for Type 1, 2, 3, or 4 spirals.
        /// Otherwise, Type 1 Spirals are complete.
        /// </summary>
        [Ignore]
        [TestMethod]
        public void HorizontalAlignment_instantiates_fromCSV_withType1SpiralOnly()
        {
            var directory = new DirectoryManager();
            directory.CdUp(2).CdDown("Datasets").CdDown("Cogo");
            string testFile = directory.GetPathAndAppendFilename("R2100_L1part.CogoDN.csv");

            HorizontalAlignment L1 = HorizontalAlignment.createFromCsvFile(testFile);
            Assert.IsNotNull(L1);

            var actualItemCount = L1.childCount();
            Assert.AreEqual(expected: 3, actual: actualItemCount);

            SpiralType1_LeftTurning_proof((L1.getChildBySequenceNumber(1)) as HorSpiralSegment);

            Assert.AreEqual(expected: new Point(1296205.4529, 960387.001),
                actual: L1.BeginPoint);

            Assert.AreEqual(expected: new Point(1295733.5145, 961709.039),
                actual: L1.EndPoint);

            var actualStation = L1.BeginStation;
            double expectedStation = 25300.0;
            Assert.AreEqual(expected: expectedStation,
                actual: actualStation, delta: stdDelta);

            expectedStation = 26705.50;
            Assert.AreEqual(expected: expectedStation,
                actual: L1.EndStation, delta: 0.001);

            var expectedEndAzimuthDeg = 335.21053;
            Assert.AreEqual(expected: expectedEndAzimuthDeg,
                actual: L1.EndAzimuth.getAsDegreesDouble(),
                delta: 0.0002);

            var expectedStationPoint = new Point(1296150.7510, 960579.375);
            var testStation = 25500.0; // On the Type 1 Spiral
            var actualStationPoint = L1.getXYZcoordinates(testStation);
            var P2Pdist = (expectedStationPoint - actualStationPoint).Length;
            Assert.AreEqual(expected: 0.0,
                actual: P2Pdist,
                delta: 0.0005);

        }

        [TestMethod]
        public void HorizontalAlignment_fromCSV_stationOffsets_mapCorrectly()
        {
            var directory = new DirectoryManager();
            directory.CdUp(2).CdDown("Datasets").CdDown("Cogo").CdDown("R2547");
            string testFile = directory.GetPathAndAppendFilename("Y15A.csv");

            HorizontalAlignment Y15A = HorizontalAlignment.createFromCsvFile(testFile);
            Assert.IsNotNull(Y15A);

            var soeList = Y15A.getStationOffsetElevation(
                new Point(655966.5667, 224111.1887));

            Assert.IsNotNull(soeList);
            Assert.AreEqual(expected: 1, actual: soeList.Count);
            var actualSOE = soeList.FirstOrDefault();
            StationOffsetElevation expectedSOE = new StationOffsetElevation(1120.0, 0.0, 0.0);
            Assert.AreEqual(expected: expectedSOE.station, actual: actualSOE.station, delta: stdDelta);

            soeList = Y15A.getStationOffsetElevation(
                new Point(655896.9056, 224002.7434));
            Assert.AreEqual(expected: 2, actual: soeList.Count);

            Point actualPoint = Y15A.getXYZcoordinates(1500.0, -2.0, 0.0);
            Point expectedPoint = new Point(656344.166, 224137.087);
            Assert.AreEqual(expected: expectedPoint, actual: actualPoint);

        }

        [TestMethod]
        public void HorizontalAlignment_GetCoordinatesByStation_multiple()
        {
            var directory = new DirectoryManager();
            directory.CdUp(2).CdDown("Datasets").CdDown("Cogo").CdDown("R2547");
            string testFile = directory.GetPathAndAppendFilename("Y15A.csv");

            HorizontalAlignment Y15A = HorizontalAlignment.createFromCsvFile(testFile);

            var allPoints = Y15A.getXYZcoordinateList(10.0);
            int actualCountOfStations = allPoints.Count;
            int expectedCount = 75;
            Assert.AreEqual(expected: expectedCount,
                actual: actualCountOfStations);
        }

        [Ignore]
        [TestMethod]
        public void EulerSpiral_createType1__correct()
        {
            var inRay = new Ray(new Point(0.0, 0.0), Azimuth.fromDegreesDouble(90.0));
            var aSpiral = HorSpiralSegment.Create(inRay,
                length: 60.0, degreeIn: 0.0, degreeOut: -95.4929659);

            Assert.IsNotNull(aSpiral);

            Double expectedThetaS = -28.64789;
            Double actualThetaS = aSpiral.Deflection.getAsDegreesDouble();
            Assert.AreEqual(expected: expectedThetaS, actual: actualThetaS, delta: 0.00001);

            Double expectedX = 58.517;
            Double expectedY = 9.823;
            Double actualX = aSpiral.spiralDX.Length;
            Double actualY = aSpiral.spiralDY.Length;
            Assert.AreEqual(expectedX, actualX, 0.0015);
            Assert.AreEqual(expectedY, actualY, 0.0015);

            var expectedPt = new Point(19.994, 0.370);
            var sta20 = new StationOffsetElevation(20.0, 0.0, 0.0);
            var actualPt = aSpiral.getXYZcoordinates(sta20);
            Assert.IsTrue(expectedPt.Equals(actualPt));
            Assert.AreEqual(expected: 176.8178,
                    actual: aSpiral.getPerpandicularAzimuth(sta20.station).getAsDegreesDouble(),
                    delta: 0.001);

            expectedPt = new Point(39.803, 2.953);
            var sta40 = new StationOffsetElevation(40.0, 0.0, 0.0);
            actualPt = aSpiral.getXYZcoordinates(sta40);
            Assert.IsTrue(expectedPt.Equals(actualPt));
            Assert.AreEqual(expected: 167.268,
                    actual: aSpiral.getPerpandicularAzimuth(sta40.station).getAsDegreesDouble(),
                    delta: 0.001);

            expectedPt = new Point(19.938, 1.369);
            actualPt = aSpiral.getXYZcoordinates(new StationOffsetElevation(20.0, -1.0, 0.0));
            Assert.IsTrue(expectedPt.Equals(actualPt));

            expectedPt = new Point(40.244, 1.002);
            actualPt = aSpiral.getXYZcoordinates(new StationOffsetElevation(40.0, 2.0, 0.0));
            Assert.IsTrue(expectedPt.Equals(actualPt));

            // Get end point and verify
            // Get Anchor point and verify

            //var expecteStationOffset = new StationOffsetElevation(0020.0, 0.0);
            //var testPoint = new Point(X: 19.9938, Y: 0.3703);
            //var actualStationOffset = aSpiral.getStationOffsetElevation(testPoint);
        }

        [TestMethod]
        public void HorizontalAlignments_fromDXF_LoadCorrectly()
        {
            var directory = new DirectoryManager();
            directory.CdUp(2).CdDown("Datasets").CdDown("Cogo");
            string testFile = directory.GetPathAndAppendFilename("SmallLidar_StreamAlignment.dxf");

            IList<HorizontalAlignment> PerryCreekAlignments =
                HorizontalAlignment.createFromDXFfile(testFile);
            Assert.IsNotNull(PerryCreekAlignments);
            Assert.AreEqual(expected: 2, actual: PerryCreekAlignments.Count);
            var secondAlignment = PerryCreekAlignments[1];
            var lastCurve = secondAlignment.getChildBySequenceNumber(7);

            Assert.AreEqual(expected: 83.2166, actual: lastCurve.Length, delta: 0.001);
            Assert.AreEqual(expected: 292.8904, actual: lastCurve.Radius, delta: 0.001);

            Assert.AreEqual(expected: 61.26707,
                actual: secondAlignment.EndAzimuth.getAsDegreesDouble(), delta: 0.001);

        }

        [TestMethod]
        public void Arc_BoundingBox_IsCorrect()
        { // Note: This needs 4 more test cases before it is confirmed correst.
            // Another quadrant. An interior solution crossing two cardinal.
            // At least one that deflects left. At least one external solution.

            // Case 1: Interior solution crosses north quad 2 to 1
            var startPt = new Point(12.0926, 9.4402);
            var endPt = new Point(18.3603, 8.2049);
            Azimuth azimuth = Azimuth.fromDegreesDouble(63.13259);
            double radius = 5.1863;
            HorArcSegment arc = new HorArcSegment(startPt, endPt, azimuth, radius);
            Assert.AreEqual(expected: 76.033,
                actual: arc.Deflection.getAsDegreesDouble(), delta: 0.001);

            BoundingBox bb = arc.BoundingBox;
            var bbEquality =
                bb.lowerLeftPt.GetHorizontalDistanceTo(new Point(12.0926, 8.2049)) +
                bb.upperRightPt.GetHorizontalDistanceTo(new Point(18.3603, 10.0));
            Assert.AreEqual(expected: 0d, actual: bbEquality, delta: 0.0025);


            // Case 2: Interior solution crosses north quad 1 to 2
            azimuth = Azimuth.fromDegreesDouble(319.16549);
            arc = new HorArcSegment(endPt, startPt, azimuth, radius);
            Assert.AreEqual(expected: -76.032,
                actual: arc.Deflection.getAsDegreesDouble(), delta: 0.001);

            bb = arc.BoundingBox;
            bbEquality =
                bb.lowerLeftPt.GetHorizontalDistanceTo(new Point(12.0926, 8.2049)) +
                bb.upperRightPt.GetHorizontalDistanceTo(new Point(18.3603, 10.0));
            Assert.AreEqual(expected: 0d, actual: bbEquality, delta: 0.0025);

            // Case 3: Interior solution crosses east quad 2 to 4
            startPt = new Point(27.4552, 7.9725);
            endPt = new Point(28.2832, 0.1134);
            azimuth = Azimuth.fromDegreesDouble(124.3559);
            radius = 5.1863;
            arc = new HorArcSegment(startPt, endPt, azimuth, radius);
            Assert.AreEqual(expected: 99.25908,
                actual: arc.Deflection.getAsDegreesDouble(), delta: 0.001);

            bb = arc.BoundingBox;
            bbEquality =
                bb.lowerLeftPt.GetHorizontalDistanceTo(new Point(27.4552, 0.1134)) +
                bb.upperRightPt.GetHorizontalDistanceTo(new Point(29.7146, 7.9725));
            Assert.AreEqual(expected: 0d, actual: bbEquality, delta: 0.0025);

            // Case 4: Interior solution crosses south and west quad 2 to 4
            // Note: The computed deflection of the curve is wrong, so this whole
            // subtest is deactivated for now. (I have to move on.)
            //startPt = new Point(36.8732, 9.2531);
            //endPt = new Point(42.7014, 1.2551);
            //azimuth = Azimuth.fromDegreesDouble(216.48909);
            //radius = 5.1863;
            //arc = new HorArc(startPt, endPt, azimuth, radius);
            //double actualDefl = arc.Deflection.getAsDegreesDouble();
            //Assert.AreEqual(expected: -145.14014,
            //    actual: actualDefl, delta: 0.001);

            //bb = arc.BoundingBox;
            //bbEquality =
            //    bb.lowerLeftPt.GetHorizontalDistanceTo(new Point(35.8565, 0.9827)) +
            //    bb.upperRightPt.GetHorizontalDistanceTo(new Point(42.7014, 9.2531));
            //Assert.AreEqual(expected: 0d, actual: bbEquality, delta: 0.0025);

            // Case 5 Exterior solution crosses all four corners
            // Not implemented at this time.

        }

        ////////////////////////////////////////////////
        /// Unit tests and support for Nonlinear Regression
        ////////////////////////////////////////////////

        private string hyperbolaData1string =
@"0.000,0.000,
1.501,-0.246,
4.501,-1.249,
7.501,-2.595,
10.501,-4.122,
13.501,-5.707,
16.501,-7.538,
19.501,-9.365,
22.501,-11.382,
25.501,-13.371,
28.501,-15.471,
31.501,-17.285,
34.501,-19.280,
37.501,-21.221,
40.501,-23.046,
43.501,-24.659,
46.501,-26.261,
49.501,-27.882,
52.501,-29.458,
55.501,-30.959,
58.501,-32.311,
";

        List<double[]> HyperbolaData1 = new List<double[]>();
        Func<double, double, double, double, double> HyperbolaFunction =
            (zeroElevation, a, aSlope, x) => zeroElevation + aSlope * (Math.Sqrt((a * a) + (x * x)) - a);

        bool setupHasBeenCalled = false;

        private void setupLinearRegressionTests()
        {
            if (setupHasBeenCalled == true)
                return;

            var sumpin = hyperbolaData1string.Split(",").Select(t => t.Trim()).ToList();
            for(int idx=0; idx<sumpin.Count()-1; idx+=2)
            {
                string xstr = sumpin[idx];
                string ystr = sumpin[idx + 1];

                double x = Convert.ToDouble(sumpin[idx]);
                double y = Convert.ToDouble(sumpin[idx + 1]);

                double[] newArray = new double[2] { x, y };
                HyperbolaData1.Add(newArray);
            }

            setupHasBeenCalled = true;
        }

        /// <summary>
        ///
        /// </summary>
    //    [TestMethod]
    //    public void NonlinearRegression_Test1()
    //    {
    //        setupLinearRegressionTests();

    //        double aGuess = 6.25;
    //        double SlopeGuess = -0.678;
    //        double errorTolerance = 0.001;
    //        var solver = new NonLinearGoodFitter(HyperbolaFunction,
    //            HyperbolaData1.Take(15),
    //            aGuess, SlopeGuess, errorTolerance);
    //        solver.param1PercentRange = solver.param2PercentRange = 0.75;
    //        solver.param1Partitions = solver.param2Partitions = 40;

    //        GoodFitParameters bestFit = solver.solve();
    //        double a = bestFit.parameter1;
    //        double asymptoticSlope = bestFit.parameter2;
    //        double aveError = bestFit.averageError;

    //        Assert.IsTrue(true);
    //    }

    //    [Ignore]
    //    [TestMethod]
    //    public void NonlinearRegression_Test2()
    //    {
    //        var solver = new NonLinearGoodFitter(HyperbolaFunction, HyperbolaData1);
    //        var values = solver.getValuesOverRange(10d, 10, 0.1);
    //        Assert.AreEqual(expected: 10, actual: values.Count);
    //        Assert.AreEqual(expected: 9d, values[0], 0.00001);
    //        Assert.AreEqual(expected: 9.2d, values[1], 0.00001);
    //    }

    }


    internal class pointRadiusPair
    {
        public Point point { get; set; }
        public Double radius { get; set; }
    }



}
