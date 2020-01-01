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

        TINsurface tinFromLidar = null;

        private void setupDatasetFileNames()
        {
            var directory = new DirectoryManager();
            directory.CdUp(2).CdDown("Datasets").CdDown("Surfaces").CdDown("Lidar");
            this.lidarFileName = directory.GetPathAndAppendFilename("Raleigh WRAL Soccer.las");
        }

        private void Initialize()
        {
            setupDatasetFileNames();
            if (this.tinFromLidar == null)
                tinFromLidar = TINsurface.CreateFromLAS(lidarFileName);
        }

        [TestMethod]
        public void TinFromLidar_isNotNull()
        {
            this.Initialize();
            Assert.IsNotNull(this.tinFromLidar);
        }

        [TestMethod]
        public void TinFromLidar_ElevationSlopeAspect_correctForTriangleZero()
        {
            this.Initialize();
            TinFromLidar_isNotNull();
            var ElSlopeAspect = this.tinFromLidar
                .getElevationSlopeAzimuth(new TINpoint(2133760.0, 775765.59, 0.0));
            ElSlopeAspect.AssertDerivedValuesAreEqual(202.63, 2.6, 24.775);
        }

        [TestMethod]
        public void TinFromLidar_ElevationSlopeAspect_OnSameTriangleOfAroadwayFillSlope()
        {
            this.Initialize();
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

        [TestMethod]
        public void TinFromLidar_Decimation_WorksCorrectly()
        {
            getPrunedTin();
            var undecimatedStats = this.aTin.Statistics;

            var decimatedTin = TINsurface.CreateFromLAS(lidarFileName, skipPoints: 1);
            decimatedTin.pruneTinHull();
            var decimatedSt = decimatedTin.Statistics;

            var halfUndicimated = undecimatedStats.PointCount / 2;
            var nearness = Math.Abs(decimatedSt.PointCount - halfUndicimated);
            Assert.IsTrue(nearness < 5);

        }

        [TestMethod]
        public void TinFromLidar_ElevationSlopeAspect_OnSameTriangleOfAbridgeEndBentSlope()
        {
            this.Initialize();
            TinFromLidar_isNotNull();
            var ElSlopeAspect = this.tinFromLidar
                .getElevationSlopeAzimuth(new TINpoint(2133952.01, 775539.31));
            ElSlopeAspect.AssertDerivedValuesAreEqual(190.0, 41.3, 137.291);

            ElSlopeAspect = this.tinFromLidar
                .getElevationSlopeAzimuth(new TINpoint(2133987.65, 775577.91));
            ElSlopeAspect.AssertDerivedValuesAreEqual(190.0, 41.3, 137.291);
        }

        [Ignore]
        [TestMethod]
        public void TinFromLidar_SavePointsAsDxf()
        {
            this.Initialize();
            TinFromLidar_isNotNull();

            var outDirectory = new DirectoryManager();
            outDirectory.CdUp(2).CdDown("CogoTests").CdDown("outputs");
            outDirectory.EnsureExists();
            string outFile = outDirectory.GetPathAndAppendFilename("SmallLidar_Points.obj");

            this.tinFromLidar.WriteToWaveFront(outFile);

            bool fileExists = File.Exists(outFile);
            Assert.IsTrue(fileExists);
        }

        [Ignore]
        [TestMethod]
        public void TinFromLidar_SaveAsWavefrontObj()
        {
            this.Initialize();
            TinFromLidar_isNotNull();

            var outDirectory = new DirectoryManager();
            outDirectory.CdUp(2).CdDown("CogoTests").CdDown("outputs");
            outDirectory.EnsureExists();
            string outFile = outDirectory.GetPathAndAppendFilename("SmallLidar_Points.obj");

            this.tinFromLidar.WriteToWaveFront(outFile);

            bool fileExists = File.Exists(outFile);
            Assert.IsTrue(fileExists);
        }

        [Ignore]  // We don't need this to run every time we run all tests.
        [TestMethod]
        public void TinFromLidar_SaveTinsAsDxfTriangleShapes()
        {
            this.Initialize();
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
            this.Initialize();
            getPrunedTin();
            var aTin = this.aTin;
            var triangleCount = this.tinFromLidar.TriangleCount;
            int expected = 135368;
            Assert.AreEqual(expected: expected, actual: triangleCount);
        }

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

    }
}
