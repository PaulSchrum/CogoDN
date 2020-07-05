using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using CadFoundation;
using CadFoundation.Angles;
using CadFoundation.Coordinates;
using CadFoundation.Coordinates.Indexing;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace Unit_Tests
{
    [TestClass]
    public class FoundationTests
    {
        private Double delta = 0.0000001;

        [TestMethod]
        public void Vector_additonVpV_returnsCorrect()
        {
            Vector v1 = new Vector(0.9, 0.1, 0.0);
            Vector v2 = new Vector(0.1, 0.9, 0.0);
            var vResult = v1 + v2;
            Double actualLength = vResult.Length;
            Double expectedLength = 1.41421356237;
            Assert.AreEqual(expected: expectedLength,
                actual: actualLength, delta: delta);
        }

        [TestMethod]
        public void Angle_division_isCorrect()
        {
            var anAngle = Angle.DEGREE * 180.0;
            var angleOverTwo = anAngle / 2.0;

            var expectedDegrees = 90.0;
            var actualDegrees = angleOverTwo.getAsDegreesDouble();
            Assert.AreEqual(expected: expectedDegrees, actual: actualDegrees, delta: 0.0001);
        }

        [TestMethod]
        public void Angle_verify_customRangeNormalization()
        {
            // Test basis 0 -- 360
            double inputVal = 360.0 + 20.0;
            double actual = Angle.normalizeToCustomRange(inputVal, 0.0, 360.0);
            Assert.AreEqual(expected: 20.0, actual: actual, delta: 0.001);

            inputVal = 180.0;
            actual = Angle.normalizeToCustomRange(inputVal, 0.0, 360.0);
            Assert.AreEqual(expected: 180.0, actual: actual, delta: 0.001);

            inputVal = -20.0;
            actual = Angle.normalizeToCustomRange(inputVal, 0.0, 360.0);
            Assert.AreEqual(expected: 340.0, actual: actual, delta: 0.001);

            inputVal = -360.0 - 20.0;
            actual = Angle.normalizeToCustomRange(inputVal, 0.0, 360.0);
            Assert.AreEqual(expected: 340.0, actual: actual, delta: 0.001);

            // Test basis -180 -- +180
            inputVal = 180.0 + 20.0;
            actual = Angle.normalizeToCustomRange(inputVal, -180.0, 180.0);
            Assert.AreEqual(expected: -160.0, actual: actual, delta: 0.001);

            inputVal = 90.0;
            actual = Angle.normalizeToCustomRange(inputVal, -180.0, 180.0);
            Assert.AreEqual(expected: 90.0, actual: actual, delta: 0.001);

            inputVal = -20.0;
            actual = Angle.normalizeToCustomRange(inputVal, -180.0, 180.0);
            Assert.AreEqual(expected: -20.0, actual: actual, delta: 0.001);

            inputVal = -180.0 - 20.0;
            actual = Angle.normalizeToCustomRange(inputVal, -180.0, 180.0);
            Assert.AreEqual(expected: 160.0, actual: actual, delta: 0.001);

        }

        [TestMethod]
        public void DegreeOfCurve_sin90_returns1p0()
        {
            DegreeOfCurve deg = 90.0;
            Double expectedDbl = 1.0;
            Double actualDbl = DegreeOfCurve.Sin(deg);
            Assert.AreEqual(expected: expectedDbl, actual: actualDbl, delta: delta);
        }

        [TestMethod]
        public void DegreeOfCurve_fromRadius5730_isOneDegree()
        {
            var radius = 5729.5779513;
            double expect = 1.0;
            double actual = DegreeOfCurve.AsDblFromRadius(radius);
            Assert.AreEqual(expected: expect, actual: actual, delta: 0.00001);
        }

        [TestMethod]
        public void DegreeOfCurve_Atan2Of10And0_returns90degrees()
        {
            DegreeOfCurve deg = DegreeOfCurve.Atan2(10.0, 0.0);
            Double expectedDbl = 90.0;
            Double actualDbl = deg.getAsDouble();
            Assert.AreEqual(expected: expectedDbl, actual: actualDbl, delta: delta);
        }

        [TestMethod]
        public void DegreeOfCurve_AsinOf1overSqrt2_shouldEqual45degrees()
        {
            DegreeOfCurve deg = DegreeOfCurve.Asin(1.0 / Math.Sqrt(2.0));
            Double expectedDbl = 45.0;
            Double actualDbl = deg.getAsDouble();
            Assert.AreEqual(expected: expectedDbl, actual: actualDbl, delta: delta);
        }

        [TestMethod]
        public void AzimuthAddition_Az189PlusDeflNeg15_shouldEqual174()
        {
            Double expectedDbl = 174.0;
            Azimuth az = new Azimuth(); az.setFromDegreesDouble(189.0);
            Deflection defl = new Deflection(); defl.setFromDegreesDouble(-15.0);
            Azimuth newAz = az + defl;
            Double actualDbl = newAz.getAsDegreesDouble();
            Assert.AreEqual(expected: expectedDbl, actual: actualDbl, delta: delta);
        }

        [TestMethod]
        public void Angle_settingTo1_shouldResultIn_equals57_2957795Degrees()
        {
            Angle angle = 1.0;
            Double expected = 57.2957795;
            Double actual = angle.getAsDegreesDouble();
            Assert.AreEqual(expected: expected, actual: actual, delta: delta);
        }

        [TestMethod]
        public void Azimuth_setToDMS183__29__29_5_shouldResultIn_Angle()
        {
            Azimuth anAzimuth = new Azimuth();
            anAzimuth.setFromDegreesMinutesSeconds(183, 29, 29.5);
            Double expected = 183.4915277778;
            Double actual = anAzimuth.getAsDegreesDouble();
            Assert.AreEqual(expected: expected, actual: actual, delta: delta);
        }

        [TestMethod]
        public void Deflection_setTo_Pos1Rad_shouldBe_Pos1Rad()
        {
            Deflection aDefl = new Deflection();
            aDefl = (Deflection)1.0;
            Double expected = 1.0;
            Double actual = aDefl.getAsRadians();
            Assert.AreEqual(expected: expected, actual: actual, delta: delta);
        }

        [TestMethod]
        public void Deflection_setTo_Neg1Rad_shouldBe_Neg1Rad()
        {
            Deflection aDefl = new Deflection();
            aDefl = (Deflection)(-1.0);
            Double expected = -1.0;
            Double actual = aDefl.getAsRadians();
            Assert.AreEqual(expected: expected, actual: actual, delta: delta);
        }

        [TestMethod]
        public void Deflection_setTo_Pos6Rad_shouldBe_Pos6Rad()
        {
            Deflection aDefl = new Deflection();
            aDefl = (Deflection)6.0;
            Double expected = 6.0;
            Double actual = aDefl.getAsRadians();
            Assert.AreEqual(expected: expected, actual: actual, delta: delta);
        }

        [TestMethod]
        public void Deflection_setTo_Pos2_shouldBe_Pos2Degrees()
        {
            Deflection defl = new Deflection();
            defl.setFromDegreesDouble(2.0);

            Double expected = 2.0;
            Double actual = defl.getAsDegreesDouble();
            Assert.AreEqual(expected: expected, actual: actual, delta: delta);
        }

        [TestMethod]
        public void Deflection_setTo_neg5__18__29_5()
        {
            Deflection aDeflection = new Deflection();
            aDeflection.setFromDegreesMinutesSeconds(-5, 18, 29.5);
            Double expected = -5.308194444444;
            Double actual = aDeflection.getAsDegreesDouble();
            Assert.AreEqual(expected: expected, actual: actual, delta: delta);
        }

        [TestMethod]
        public void Azimuth1_30_addDeflection_Pos2_15_shouldYieldNewAzimuth_3_45()
        {
            Azimuth anAzimuth = new Azimuth();
            anAzimuth.setFromDegreesMinutesSeconds(1, 30, 0);
            Deflection aDefl = new Deflection();
            aDefl.setFromDegreesMinutesSeconds(2, 15, 0);

            Double expected = 3.75;
            Azimuth newAz = anAzimuth + aDefl;
            Double actual = newAz.getAsDegreesDouble();
            Assert.AreEqual(expected: expected, actual: actual, delta: delta);
        }

        [TestMethod]
        public void Azimuth_createFromDouble_byAssignmentEquals()
        {
            double expected = 123.456;
            Azimuth anAz = expected;
            double actual = anAz.getAsDegreesDouble();
            Assert.AreEqual(expected: expected, actual: actual, delta: 0.0001);

            expected = 123.456;
            anAz = expected;
            actual = anAz.getAsDegreesDouble();
            Assert.AreEqual(expected: expected, actual: actual, delta: 0.0001);

            expected = 123.456;
            anAz = expected;
            actual = anAz.getAsDegreesDouble();
            Assert.AreEqual(expected: expected, actual: actual, delta: 0.0001);
        }

        [DataTestMethod]
        [DataRow(10, 2, 78.690067526)]
        [DataRow(10, -2, 101.309932474)]
        [DataRow(-10, 2, 281.309932474)]
        [DataRow(-10, -2, 258.690067526)]
        public void Azimuth_setFromXY(Double x, Double y, Double expectedDegrees)
        {
            Azimuth anAzimuth = new Azimuth();
            anAzimuth.setFromXY(x, y);
            Double actualDegrees = anAzimuth.getAsDegreesDouble();

            Assert.AreEqual(expected: expectedDegrees, actual: actualDegrees, delta: delta);
        }

        [DataTestMethod]
        //[DataRow(45.0, 1)]
        //[DataRow(135.0, 2)]
        //[DataRow(225.0, 3)]
        [DataRow(315.0, 4)]
        public void Azimuth_quadrant_isCorrect(Double azim, int expected)
        {
            Azimuth azimuth = new Azimuth(); azimuth.setFromDegreesDouble(azim);
            int actual = azimuth.quadrant;
            Assert.AreEqual(expected: expected, actual: actual);
        }

        [DataTestMethod]
        [DataRow(270.0 + 5.0, 270.0 - 5.0, -10.0)]
        [DataRow(250.0 + 5.0, 250.0 - 5.0, -10.0)]
        [DataRow(185.0, 175.0, -10.0)]
        [DataRow(95.0, 85.0, -10.0)]
        [DataRow(20.0, 10.0, -10.0)]
        [DataRow(340.0, 350.0, 10.0)]
        [DataRow(20.0, 340.0, -40.0)]
        [DataRow(340.0, 20.0, 40.0)]
        public void AzimuthArithmatic_subtraction(Double Az1Dbl, Double Az2Dbl, Double expectedDeflection)
        {
            Azimuth Az1 = new Azimuth(); Az1.setFromDegreesDouble(Az1Dbl);
            Azimuth Az2 = new Azimuth(); Az2.setFromDegreesDouble(Az2Dbl);

            var actualDefl = Az2.minus(Az1);
            Double actualDeflection = actualDefl.getAsDegreesDouble();

            var diff = expectedDeflection - actualDeflection;
            var ratio = expectedDeflection / actualDeflection;

            Assert.AreEqual(expected: expectedDeflection, actual: actualDeflection, delta: 0.00000001);
        }

        [DataTestMethod]
        [DataRow(20.0, 10.0, -10.0)]
        [DataRow(340.0, 350.0, 10.0)]
        [DataRow(20.0, 340.0, -40.0)]
        [DataRow(340.0, 20.0, 40.0)]
        [DataRow(189.4326, 173.8145, -15.6181)]
        public void AzimuthArithmatic_addition(Double Az1Dbl, Double ExpectedAz2Dbl, Double DeflectionDbl)
        {
            Azimuth Az1 = new Azimuth(); Az1.setFromDegreesDouble(Az1Dbl);
            Deflection defl = DeflectionDbl.AsDegreeOfCurve();
            Azimuth Az2 = Az1 + defl;

            Double actualAzimuth = Az2.getAsDegreesDouble();

            Assert.AreEqual(expected: ExpectedAz2Dbl, actual: actualAzimuth, delta: 0.00000001);
        }

        [TestMethod]
        public void AzimuthArithmatic_GetNormal()
        {
            Azimuth Az1 = new Azimuth(); Az1.setFromDegreesDouble(350.0);
            Azimuth Az1Right = Az1.RightNormal();
            Assert.AreEqual(
                expected: 80.0,
                actual: Az1Right.getAsDegreesDouble(),
                delta: 0.00001
                );
            Assert.AreEqual(
                expected: 260.0,
                actual: Az1.RightNormal(-1.0).getAsDegreesDouble(),
                delta: 0.00001
                );
        }

        [DataTestMethod]
        [DataRow(5.0, 10.0, 5.0)]
        [DataRow(15.0, 10.0, 5.0)]
        [DataRow(-5.0, 10.0, -5.0)]
        [DataRow(-15.0, 10.0, -5.0)]
        public void ComputeRemainder_ScaledByDenominator(Double numerator, Double Denominator, Double expectedDbl)
        {
            Double actualDbl = Angle.ComputeRemainderScaledByDenominator(numerator, Denominator);
            Assert.AreEqual(expected: expectedDbl, actual: actualDbl, delta: 0.00000001);
        }

        [TestMethod]
        public void AzimuthArithmatic_Subtraction_Correct()
        {
            Azimuth Az1 = new Azimuth(); Az1.setFromDegreesDouble(299);
            Azimuth Az2 = new Azimuth(); Az2.setFromDegreesDouble(247);
            var defl = Az2.minus(Az1);  //start here. This value is wrong.
            Assert.AreEqual(expected: -52.0, actual: defl.getAsDegreesDouble(), delta: 0.001);
        }

        [TestMethod]
        public void RayRay_Intersect_ReturnsCorrectPoint()
        {
            Point pt1 = new Point(0d, 0d);
            Ray ray1 = new Ray(pt1.x, pt1.y, 30.0);
            Ray ray2 = new Ray(3d, 11d, 120.0);
            Point resultingPoint = ray1.IntersectWith_2D(ray2);
            double expectedX = 5.5131;
            double expectedY = 9.5490;
            Assert.AreEqual(expected: expectedX, actual: resultingPoint.x, 0.0002);
            Assert.AreEqual(expected: expectedY, actual: resultingPoint.y, 0.0002);

            Point ptOnRay1 = pt1 + new Vector(ray1, 100.0);
            Ray ray1identical = new Ray(ptOnRay1, 30.0);

            Exception except = null;
            resultingPoint = null;
            try { resultingPoint = ray1.IntersectWith_2D(ray1identical); }
            catch (Exception e)
            { except = e; }
            if (!(except is null))
            {
                if (except.Message != "Two rays are colinear. They intersect at all points.")
                    throw new Exception
                        ("Wrong kind of exception thrown. Should have been colinear.");
            }

            Ray ray1ParallelButOff = new Ray(3d, 11d, 30.0);
            try { resultingPoint = ray1.IntersectWith_2D(ray1ParallelButOff); }
            catch (Exception e)
            { except = e; }
            if (!(except is null))
            {
                if (except.Message != "Two rays with identical horizontal direction never intersect.")
                    throw new Exception
                        ("Wrong kind of exception thrown. Should have been parallel but not colinear.");
            }

        }


        [TestMethod]
        public void GridIndexer_worksRight()
        {
            var bb = new BoundingBox(0.0, 0.0, 1000000.0, 1000000.0);
            var testInstance = new testBB();
            testInstance.BoundingBox = bb;
            var indexingGrid = new GridIndexer(1000000, testInstance);
            Assert.IsNotNull(indexingGrid);

            var aCell = indexingGrid.AllCells[2][2];
            Assert.AreEqual(actual: aCell.lowerLeftPt.x, expected: 31250.0*2.0);
            Assert.AreEqual(actual: aCell.lowerLeftPt.y, expected: 31250.0*2.0);
            Assert.AreEqual(actual: aCell.upperRightPt.x, expected: 31250.0*3.0);
            Assert.AreEqual(actual: aCell.upperRightPt.y, expected: 31250.0*3.0);

            var smallBB1 = new BoundingBox(113670.0, 54170.0, 114610.0, 54960.0);
            var smallBB2 = new BoundingBox(115670.0, 60770.0, 119990.0, 63320.0);
            var smallBB3 = new BoundingBox(123240.0, 61430.0, 125910.0, 63360.0);
            var smallBB4 = new BoundingBox(123690.0, 55170.0, 126820.0, 56120.0);
            var small1 = new testBB(); small1.BoundingBox = smallBB1;
            var small2 = new testBB(); small2.BoundingBox = smallBB2;
            var small3 = new testBB(); small3.BoundingBox = smallBB3;
            var small4 = new testBB(); small4.BoundingBox = smallBB4;

            indexingGrid.AssignObjectsToCells(new List<testBB>() { small1, small2, small3, small4 });

            var objects = indexingGrid.FindObjectsAt(119330.0, 58190.0);
            Assert.AreEqual(actual: objects.Count, expected: 0);

            objects = indexingGrid.FindObjectsAt(114170.0, 54690.0);
            objects = indexingGrid.FindObjectsAt(117870.0, 62690.0);
            objects = indexingGrid.FindObjectsAt(125560.0, 62940.0);
            objects = indexingGrid.FindObjectsAt(125930.0, 55880.0);
            Assert.AreEqual(actual: objects.Count, expected: 1);

        }

        [TestMethod]
        public void Vector_Theta_isCorrect()
        {
            Vector v = new Vector(-20, -20, 100);
            double expected = 15.7931690483;
            var theta = v.Theta;
            double actual = v.Theta.getAsDegreesDouble();
            Assert.AreEqual(expected: expected, actual: actual, delta: 0.00001);
        }
    }

    internal class testBB : IBoxBounded
    {
        public BoundingBox BoundingBox { get; set; }
    }

    [TestClass]
    public class DirectoryManagerTests
    {
        [TestMethod]
        public void DirectoryNode_TestBasics()
        {
            var cwd = new DirectoryManager();
            var depth = cwd.pathAsList.Count;
            var dir = new DirectoryNode(cwd);
            dir.PopulateAll();

            var rootDir = new DirectoryNode(cwd);
            rootDir.CdRoot();
            rootDir.PopulateAll();
        }

        [TestMethod]
        public void DirectoryNode_SearchByFilter()
        {
            var aDrive = DirectoryNode.SetAtDriveLetterRoot("C");
            Assert.IsNotNull(aDrive);
            aDrive.CdDown("Windows");
            
            var results = aDrive.FindAll(".exe");
            Assert.IsTrue(results.Count > 1);
        }
    }
}
