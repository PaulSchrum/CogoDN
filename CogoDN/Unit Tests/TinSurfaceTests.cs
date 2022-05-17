using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Surfaces;
using System.IO;
using Cogo.Horizontal;
using Surfaces.TIN;
using CadFoundation;
using System.Diagnostics;

namespace Unit_Tests
{

    /// <summary>
    /// Attempts to read a .las file to get point data from it.
    /// Developed for .las 1.4.   See
    /// https://www.asprs.org/wp-content/uploads/2010/12/LAS_1_4_r13.pdf
    /// 
    ///     From that, Table 4.9 indicates point classes:
    /// 0 Created, never classified
    /// 1 Unclassified1
    /// 2 Ground
    /// 3 Low Vegetation
    /// 4 Medium Vegetation
    /// 5 High Vegetation
    /// 6 Building
    /// 7 Low Point(“low noise”)
    /// 8 High Point(typically “high noise”). Note that this value was previously
    ///         used for Model Key Points.Bit 1 of the Classification Flag must now
    /// 
    /// be used to indicate Model Key Points.This allows the model key point 
    /// class to be preserved.
    /// 9 Water
    /// 10 Rail
    /// 11 Road Surface
    /// 12 Bridge Deck
    /// 13 Wire - Guard
    /// 14 Wire – Conductor (Phase)
    /// 15 Transmission Tower
    /// 16 Wire-structure Connector (e.g.Insulator)
    /// 17 Reserved
    /// 18-63 Reserved
    /// 64-255 User definable – The specific use of these classes 
    ///     should be encoded in the Classification lookup VLR
    /// </summary>
    [TestClass]
    public class TinTests
    {
        string lidarFileName;
        string geotiffFileName;

        TINsurface tinFromLidar = null;
        TINsurface tinFromGeotiff = null;

        private void setupDatasetFileNames()
        {
            var directory = new DirectoryManager();
            directory.CdUp(2).CdDown("Datasets").CdDown("Surfaces").CdDown("Lidar");
            this.lidarFileName = directory.GetPathAndAppendFilename("Raleigh WRAL Soccer.las");
            directory.CdUp(1).CdDown("GeoTiff");
            this.geotiffFileName = directory.GetPathAndAppendFilename("Rippetoe DEM03.tif");
        }

        private void InitializeLidarTests()
        {
            setupDatasetFileNames();
            if (this.tinFromLidar == null)
                tinFromLidar = TINsurface.CreateFromLAS(lidarFileName);
        }

        private void InitializeGeoTiffTests()
        {
            setupDatasetFileNames();
            tinFromGeotiff ??= TINsurface.CreateFromGeoTiff(geotiffFileName);
        }

        [TestMethod]
        public void TinFromLidar_isNotNull()
        {
            this.InitializeLidarTests();
            Assert.IsNotNull(this.tinFromLidar);
        }

        [TestMethod]
        public void TinFromLidar_ElevationSlopeAspect_correctForTriangleZero()
        {
            this.InitializeLidarTests();
            TinFromLidar_isNotNull();
            var ElSlopeAspect = this.tinFromLidar
                .getElevationSlopeAzimuth(new TINpoint(2133760.0, 775765.59, 0.0));
            ElSlopeAspect.AssertDerivedValuesAreEqual(202.63, 2.6, 24.775);
        }

        [TestMethod]
        public void TinFromLidar_ElevationSlopeAspect_OnSameTriangleOfAroadwayFillSlope()
        {
            this.InitializeLidarTests();
            TinFromLidar_isNotNull();
            var ElSlopeAspect = this.tinFromLidar
                .getElevationSlopeAzimuth(new TINpoint(2133835.08, 775629.79));
            ElSlopeAspect.AssertDerivedValuesAreEqual(200.0, 44.0, 226.48);

            ElSlopeAspect = this.tinFromLidar
                .getElevationSlopeAzimuth(new TINpoint(2133836.27, 775629.41));
            ElSlopeAspect.AssertDerivedValuesAreEqual(200.24, 44.0, 226.48);
        }

        [TestMethod]
        public void TinFromLidar_Statistics_AreCorrect()
        {  // To do: complete this. It is currently not really checking anything.
            getPrunedTin();
            var stats = this.aTin.Statistics;
            Assert.IsNotNull(stats);

            var pointCount = stats.PointCount;
            var expected = this.aTin.allUsedPoints.Count();
            Assert.AreEqual(expected: expected, actual: pointCount);

        }

        [Ignore]
        [TestMethod]
        public void TinFromLidar_Decimation_WorksCorrectly()
        {
            getPrunedTin();
            var undecimatedStats = this.aTin.Statistics;

            var decimatedTin = TINsurface.CreateFromLAS(lidarFileName, skipPoints: 1);
            var decimatedSt = decimatedTin.Statistics;

            var halfUndicimated = undecimatedStats.PointCount / 2;
            var nearness = Math.Abs(decimatedSt.PointCount - halfUndicimated);
            Assert.IsTrue(nearness < 5);

        }

        [TestMethod]
        public void TinFromLidar_ElevationSlopeAspect_OnSameTriangleOfAbridgeEndBentSlope()
        {
            this.InitializeLidarTests();
            TinFromLidar_isNotNull();
            var ElSlopeAspect = this.tinFromLidar
                .getElevationSlopeAzimuth(new TINpoint(2133952.01, 775539.31));
            ElSlopeAspect.AssertDerivedValuesAreEqual(190.0, 41.3, 317.291);

            ElSlopeAspect = this.tinFromLidar
                .getElevationSlopeAzimuth(new TINpoint(2133987.65, 775577.91));
            ElSlopeAspect.AssertDerivedValuesAreEqual(190.0, 41.3, 317.291);
        }

        [TestMethod]
        public void TinFromLidar_GettingEdgePoint_Correct()
        {
            this.InitializeLidarTests();
            var edgeLines = this.tinFromLidar.OuterEdgeLines.ToList();
            Assert.AreEqual(expected: 538, actual: edgeLines.Count);
            var perimeter = edgeLines.Select(L => L.Length2d).Sum();
            Assert.IsTrue(perimeter > 2300 && perimeter < 2575);
        }

        [Ignore]
        [TestMethod]
        public void TinFromLidar_SavePointsAsDxf()
        {
            this.InitializeLidarTests();
            TinFromLidar_isNotNull();

            var outDirectory = new DirectoryManager();
            outDirectory.CdUp(2).CdDown("CogoTests", true).CdDown("outputs", true);
            outDirectory.EnsureExists();
            string outFile = outDirectory.GetPathAndAppendFilename("SmallLidar_Points.dxf");

            this.tinFromLidar.WritePointsToDxf(outFile);

            bool fileExists = File.Exists(outFile);
            Assert.IsTrue(fileExists);
        }

        //[Ignore]
        [TestMethod]
        public void TinFromLidar_SaveAsWavefrontObj()
        {
            Trace.Write("Testing.\n");
            this.InitializeLidarTests();
            TinFromLidar_isNotNull();

            var outDirectory = new DirectoryManager();
            outDirectory.CdUp(2).CdDown("CogoTests").CdDown("outputs");
            outDirectory.EnsureExists();
            string outFile = outDirectory.GetPathAndAppendFilename("SmallLidar_wfront.obj");

            TINsurface.setAffineTransformToZeroCenter(this.tinFromLidar, false);
            this.tinFromLidar.WriteToWaveFront(outFile);

            bool fileExists = File.Exists(outFile);
            Assert.IsTrue(fileExists);
        }

        [Ignore]  // We don't need this to run every time we run all tests.
        [TestMethod]
        public void TinFromLidar_SaveTinsAsDxfTriangleShapes()
        {
            this.InitializeLidarTests();
            TinFromLidar_isNotNull();

            var outDirectory = new DirectoryManager();
            outDirectory.CdUp(2).CdDown("CogoTests").CdDown("outputs");
            outDirectory.EnsureExists();
            string outFile = outDirectory.GetPathAndAppendFilename("SmallLidar_Triangles.dxf");

            this.tinFromLidar.WriteTinToDxf(outFile);

            bool fileExists = File.Exists(outFile);
            Assert.IsTrue(fileExists);
        }

        [Ignore]
        [TestMethod]
        public void TinFromLidar_IntersectsAlignment_CreatesProfile()
        {
            var aTin = TINsurface.CreateFromLAS(lidarFileName);
            aTin.pruneTinHull();

            var directory = new DirectoryManager();
            directory.CdUp(2).CdDown("CogoTests");
            string testFile = directory.GetPathAndAppendFilename("SmallLidar_StreamAlignment.dxf");

            IList<HorizontalAlignment> PerryCreekAlignments =
                HorizontalAlignment.createFromDXFfile(testFile);
            Assert.IsNotNull(PerryCreekAlignments);
            Assert.AreEqual(expected: 2, actual: PerryCreekAlignments.Count);
            var secondAlignment = PerryCreekAlignments[1];

            // Cogo.Profile groundProfile = secondAlignment.ProfileFromSurface(aTin);
        }

        private TINsurface aTin = null;
        private void getPrunedTin()
        {
            if (null == aTin)
            {
                setupDatasetFileNames();
                this.aTin = TINsurface.CreateFromLAS(lidarFileName);
            }
        }

        //[Ignore]
        [TestMethod]
        public void TinFromLidar_CompareTriangleCount()
        {
            this.InitializeLidarTests();
            //getPrunedTin();
            var aTin = this.aTin;
            var triangleCount = this.tinFromLidar.TriangleCount;
            int expected = 150192;
            Assert.AreEqual(expected: expected, actual: triangleCount);
        }

        [Ignore]
        [TestMethod]
        public void TinFromLidar_saveRead_hasSameValues()
        {
            var subDir = "temp";
            var outfile = "testTin.TinDN";
            var directory = new DirectoryManager();
            directory.CdUp(2).CdDown("Datasets").CdDown("Surfaces").CdDown("Lidar");
            var fileName = directory.GetPathAndAppendFilename("Raleigh WRAL Soccer.las");
            var tinFromLidar = TINsurface.CreateFromLAS(fileName);
            var stats = tinFromLidar.Statistics;
            Assert.IsNotNull(tinFromLidar);
            try
            {
                directory.CdDown(subDir, createIfNeeded: true);
                directory.EnsureExists();
                outfile = directory.GetPathAndAppendFilename(outfile);
                tinFromLidar.saveAsBinary(outfile, compress: false);
                if (!directory.ConfirmExists(outfile))
                    throw new IOException("Tin model binary file was not created.");

                var tinFromSavedFile = TINsurface.loadFromBinary(outfile);
                Assert.IsNotNull(tinFromSavedFile);
                assessTwoTinsForEquivalence(tinFromLidar, tinFromSavedFile);
                System.IO.File.Delete(outfile);

                tinFromLidar.saveAsBinary(outfile, compress: true);
                if (!directory.ConfirmExists(outfile))
                    throw new IOException("Tin model binary file was not created.");

                tinFromSavedFile = TINsurface.loadFromBinary(outfile);
                Assert.IsNotNull(tinFromSavedFile);
                assessTwoTinsForEquivalence(tinFromLidar, tinFromSavedFile);

            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                directory.ForceRemove(subDir);
            }
        }

        private void assessTwoTinsForEquivalence(TINsurface tinFromLidar, TINsurface tinFromSavedFile)
        {
            Assert.AreEqual(
                    expected: tinFromLidar.allUsedPoints.Count,
                    actual: tinFromSavedFile.allUsedPoints.Count);
            Assert.AreEqual(
                expected: tinFromLidar.TrianglesReadOnly.Count,
                actual: tinFromSavedFile.TrianglesReadOnly.Count);

            var centerPt = tinFromLidar.BoundingBox.Center;
            var x = centerPt.x + 0.25;
            var y = centerPt.y - 0.45;
            var testPointFromLidar = tinFromLidar.getElevationSlopeAzimuth(x, y);
            var testPointFromSavedFile = tinFromSavedFile.getElevationSlopeAzimuth(x, y);

            Assert.IsTrue(testPointFromLidar.Equals(testPointFromSavedFile));
        }

        [TestMethod]
        public void TINtriangle_hasCorrect_NormalVector()
        {
            TINtriangle testTriangle = new TINtriangle(new List<TINpoint>
                { new TINpoint(0, 0, 0), 
                    new TINpoint(20, 30, 40), 
                    new TINpoint(45, 70, 80) },
                "1 2 3"
            );
            var normalVec = testTriangle.normalVec;
            Assert.AreEqual(expected: -400.0, actual: normalVec.x, delta: 0.0001);
            Assert.AreEqual(expected: 200.0, actual: normalVec.y, delta: 0.0001);
            Assert.AreEqual(expected: 50.0, actual: normalVec.z, delta: 0.0001);

            var theta = normalVec.Theta.getAsDegreesDouble();
            Assert.AreEqual(expected: 83.6206297916, actual: theta, delta: 0.00001);

            var triangleSlopePercent = testTriangle.SlopeDouble;
            Assert.AreEqual(expected: 0.111803399, actual: triangleSlopePercent,
                delta: 0.00001);

        }


        [TestMethod]
        public void TinFromGeoTiff_isNotNull()
        {
            InitializeGeoTiffTests();
            Assert.IsNotNull(tinFromGeotiff);
        }

        [TestMethod]
        public void TinFromGeoTiff_ElevationSlopeAspect_correctForCertainPoint()
        {
            InitializeGeoTiffTests();
            TinFromGeoTiff_isNotNull();
            // 776297.7 1246760.7 1606.78
            var ElSlopeAspect = this.tinFromGeotiff
                .getElevationSlopeAzimuth(new TINpoint(1246760.7, 776297.7, 0.0));
            ElSlopeAspect.AssertDerivedValuesAreEqual(1607.02, 4.8, 8.4457);
        }

    }
}
