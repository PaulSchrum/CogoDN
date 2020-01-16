using CadFoundation.Angles;
using CadFoundation.Coordinates;
using MathNet.Numerics.LinearAlgebra;
using netDxf;
using netDxf.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using CadFound = CadFoundation.Coordinates;

using MathNet.Numerics.Statistics;

using System.Runtime.CompilerServices;
using Cogo.Utils;
using CadFoundation;
using System.IO.Compression;
using CadFoundation.Coordinates.Indexing;

[assembly: InternalsVisibleTo("Unit Tests")]

namespace Surfaces.TIN
{
    [Serializable]
    public class TINsurface : IBoxBounded
    {
        // Substantive members - Do serialize
        public List<TINpoint> allUsedPoints { get; private set; }

        [NonSerialized]
        private List<TINpoint> unused_points = new List<TINpoint>();
        public List<TINpoint> allUnusedPoints { get { return unused_points; } }
        private int skippedPoints = 0;
        
        
        private Dictionary<Tuple<int, int>, TINtriangleLine> allLines { get; set; }
            = new Dictionary<Tuple<int, int>, TINtriangleLine>();
        private List<TINtriangle> allTriangles;
        private BoundingBox myBoundingBox;
        public BoundingBox BoundingBox
        {
            get { return myBoundingBox.Duplicate(); }
        }

        [NonSerialized]
        private LasFile lasfile_ = null;
        private LasFile lasFile 
        { 
            get { return lasfile_; }
            set { lasfile_ = value; } 
        }

        public string SourceData { get; private set; } = null;

        public int TriangleCount
        {
            get { return ValidTriangles.Count(); }
        }
        internal IEnumerable<TINtriangle> ValidTriangles
        {
            get { return this.allTriangles.Where(t => t.IsValid); }
        }

        private TINstatistics statistics_ = null;
        public TINstatistics Statistics
        {
            get
            {
                if (null == this.statistics_)
                    this.statistics_ = new TINstatistics(this);
                return this.statistics_;
            }
        }

        public IReadOnlyCollection<TINtriangleLine> ValidLines
        {
            get
            {
                return allLines.Values
                    .Where(line => !(line.theOtherTriangle is null) &&
                        (line.oneTriangle.IsValid ||
                        line.theOtherTriangle.IsValid))
                    .ToList();
            }
        }

        private static void setBoundingBox(TINsurface tin)
        {
            var points = tin.allUsedPoints;
            var minX = points.Select(p => p.x).Min();
            var maxX = points.Select(p => p.x).Max();
            var minY = points.Select(p => p.y).Min();
            var maxY = points.Select(p => p.y).Max();
            var minZ = points.Select(p => p.z).Min();
            var maxZ = points.Select(p => p.z).Max();
            tin.myBoundingBox = new BoundingBox
                (minX, minY, maxX, maxY, minZ, maxZ);

            return;
        }

        public static TINsurface CreateFromLAS(string lidarFileName, double minX, double maxX, double minY, double maxY,
            int skipPoints = 0, List<int> classificationFilter = null)
        {
            var bb = new BoundingBox(minX, minY, maxX, maxY);
            return CreateFromLAS(lidarFileName, skipPoints, bb, classificationFilter);
        }

        public static TINsurface CreateFromLAS(string lidarFileName,
            int skipPoints = 0,
            BoundingBox trimBB = null,
            List<int> classificationFilter = null)
        {
            LasFile lasFile = new LasFile(lidarFileName,
                classificationFilter: classificationFilter);
            TINsurface returnObject = new TINsurface();
            returnObject.SourceData = lidarFileName;
            int pointCounter = -1;
            int runningPointCount = -1;
            int indexCount = 0;
            var gridIndexer = new Dictionary<Tuple<int, int>, int>();
            returnObject.createAllpointsCollection();
            
            foreach (var point in lasFile.AllPoints)
            {
                if (null != trimBB && !trimBB.isPointInsideBB2d(point))
                    continue;

                pointCounter++;
                runningPointCount++;
                
                if (runningPointCount % (skipPoints + 1) == 0)
                {
                    returnObject.allUsedPoints.Add(point);
                    // Note this approach will occasionally skip over points that 
                    // are double-stamps. I am fine with that for now.
                    gridIndexer[point.GridCoordinates] = indexCount;
                }
                else
                {
                    point.hasBeenSkipped = true;
                    returnObject.allUnusedPoints.Add(point);
                    continue;
                }

                indexCount++;
            }

            setBoundingBox(returnObject);
            lasFile.ClearAllPoints();  // Because I have them now.

            for (indexCount = 0; indexCount < returnObject.allUsedPoints.Count; indexCount++)
            {
                var aPoint = returnObject.allUsedPoints[indexCount];
                aPoint.myIndex = indexCount;
                returnObject.allUsedPoints[indexCount] = aPoint;
            }

            var VoronoiMesh = MIConvexHull.VoronoiMesh
                .Create<TINpoint, ConvexFaceTriangle>(returnObject.allUsedPoints);

            returnObject.allTriangles = new List<TINtriangle>(2 * returnObject.allUsedPoints.Count);
            foreach (var vTriangle in VoronoiMesh.Triangles)
            {
                var point1 = gridIndexer[vTriangle.Vertices[0].GridCoordinates];
                var point2 = gridIndexer[vTriangle.Vertices[1].GridCoordinates];
                var point3 = gridIndexer[vTriangle.Vertices[2].GridCoordinates];
                var newTriangle = TINtriangle.CreateTriangle(
                    returnObject.allUsedPoints, point1, point2, point3);
                if(!(newTriangle is null))
                    returnObject.allTriangles.Add(newTriangle);
            }
            returnObject.finalProcessing();

            if (skipPoints == 0) 
                return returnObject;
            
            var baseTin = returnObject;
            returnObject = null;

            var tempAllPoints = (baseTin.allUsedPoints.Concat(baseTin.allUnusedPoints)).ToList();
            foreach (var pt in tempAllPoints)
            {
                if (pt.isOnHull)
                    pt.hasBeenSkipped = false;
                else
                    pt.hasBeenSkipped = true;
            }
                

            returnObject = new TINsurface();
            returnObject.SourceData = lidarFileName;
            pointCounter = -1;
            runningPointCount = -1;
            gridIndexer = new Dictionary<Tuple<int, int>, int>();
            returnObject.createAllpointsCollection();

            var hullIndices = tempAllPoints.Where(p => p.isOnHull).Select(p => p.myIndex).ToList();
            var nonHullIndices = tempAllPoints.Where(p => !p.isOnHull).Select(p => p.myIndex).ToList();
            var targetNonHullCount = (int)(nonHullIndices.Count / (skipPoints + 1));

            Random rdm = new Random();
            var pointsToIncludeByIndex = new HashSet<int>();
            for(int i=0; i<=targetNonHullCount; i++)
            {
                var proceed = false;
                while(!proceed)
                {
                    var nextIndex = rdm.Next(0, nonHullIndices.Count);
                    proceed = pointsToIncludeByIndex.Add(nextIndex);
                }
            }

            var pointsToUseIndexes = new List<int>(pointsToIncludeByIndex.Count);
            foreach(var idx in pointsToIncludeByIndex)
            {
                pointsToUseIndexes.Add(idx);
            }
            pointsToUseIndexes.AddRange(hullIndices);
            hullIndices = null;
            pointsToIncludeByIndex = null;


            foreach (var idx in pointsToUseIndexes)
            {
                var point = tempAllPoints[idx];
                pointCounter++;
                runningPointCount++;

                point.hasBeenSkipped = false;
                returnObject.allUsedPoints.Add(point);
                // Note this approach will occasionally skip over points that 
                // are double-stamps. I am fine with that for now.
                gridIndexer[point.GridCoordinates] = indexCount;

                indexCount++;
            }

            foreach (var pt in tempAllPoints.Where(pt => pt.hasBeenSkipped))
            {
                returnObject.allUnusedPoints.Add(pt);
                pt.myIndex = -1;
            }

            var usedCount = tempAllPoints.Where(p => !p.hasBeenSkipped).Count();
            var notUsedCount = tempAllPoints.Where(p => p.hasBeenSkipped).Count();
            setBoundingBox(returnObject);

            for (indexCount = 0; indexCount < returnObject.allUsedPoints.Count; indexCount++)
            {
                var aPoint = returnObject.allUsedPoints[indexCount];
                aPoint.myIndex = indexCount;
                gridIndexer[aPoint.GridCoordinates] = indexCount;
            }

            VoronoiMesh = MIConvexHull.VoronoiMesh
                .Create<TINpoint, ConvexFaceTriangle>(returnObject.allUsedPoints);

            returnObject.allTriangles = new List<TINtriangle>(2 * returnObject.allUsedPoints.Count);
            foreach (var vTriangle in VoronoiMesh.Triangles)
            {
                var point1 = gridIndexer[vTriangle.Vertices[0].GridCoordinates];
                var point2 = gridIndexer[vTriangle.Vertices[1].GridCoordinates];
                var point3 = gridIndexer[vTriangle.Vertices[2].GridCoordinates];
                var newTriangle = TINtriangle.CreateTriangle(
                    returnObject.allUsedPoints, point1, point2, point3);
                if (!(newTriangle is null))
                    returnObject.allTriangles.Add(newTriangle);
            }
            returnObject.skippedPoints = skipPoints;
            returnObject.finalProcessing();

            return returnObject;
        }

        protected void finalProcessing()
        {
            this.pruneTinHull();
            this.IndexTriangles();
            this.DetermineEdgePoints();
            var upsideDownTriangles = this.allTriangles.Where(t => t.normalVec.z < 0.0).ToList();
            foreach(var tri in upsideDownTriangles)
            {
                tri.SwapPoint1And2();
            }
            upsideDownTriangles = null;
            GC.Collect();
        }

        [NonSerialized]
        private GridIndexer validTrianglesIndexed_ = null;
        internal GridIndexer validTrianglesIndexed
        {
            get { return validTrianglesIndexed_; }
            private set { validTrianglesIndexed_ = value; }
        }
        public void IndexTriangles()
        {
            if (null == validTrianglesIndexed)
            {
                var validTris = ValidTriangles.ToList();
                validTrianglesIndexed = new GridIndexer(validTris.Count, this);
                validTrianglesIndexed.AssignObjectsToCells(validTris);
            }
        }

        public void ComputeErrorStatistics(string v)
        {
            if (allUnusedPoints.Count == 0)
                return;

            if(!File.Exists(v))
                System.IO.File.WriteAllText(v, "PointsSkipped,PointCount,RMSE,RMaxSE,Rp95SE,FileSize\r\n");

            randomIndices rdmIdc = new randomIndices(allUnusedPoints.Count, 1000);
            var squaredErrorsBag = new ConcurrentBag<double?>();


            Console.WriteLine();
            Console.WriteLine("Starting Elevation Sweep");
            var sw = Stopwatch.StartNew();
            //foreach(var idx in rdmIdc.indices)
            //Parallel.ForEach(rdmIdc.indices, idx =>
            Parallel.ForEach(allUnusedPoints, pt =>
            {
                //var pt = allUnusedPoints[idx];
                 var error = pt.z - getElevation(pt);
                 squaredErrorsBag.Add(error * error);
            }   );
            //allUnusedPoints.Select(p => p.z - getElevation(p)).ToList();
            //var squaredErrors = allUnusedPoints.Select(p => p.z - getElevation(p)).Select(e => e * e).ToList();
            List<double?> squaredErrors = new List<double?>(squaredErrorsBag);
            squaredErrors.Sort();
            squaredErrorsBag.Clear();
            squaredErrorsBag = null;
            var stats = new DescriptiveStatistics(squaredErrors);
            var rootMaxSquared = Math.Sqrt(stats.Maximum);
            var rootMeanSquared = Math.Sqrt(stats.Mean);
            var rootVarianceSquared = Math.Sqrt(stats.Variance);
            int idxP95 = (int)(0.95*(double)squaredErrors.Count);
            var rootP95Squared = Math.Sqrt((double)squaredErrors[idxP95]);
            sw.Stop();
            Console.Write(sw.Elapsed);
            //var msPerQuery = (double)sw.ElapsedMilliseconds / rdmIdc.SampleCount;
            var msPerQuery = (double)sw.ElapsedMilliseconds / allUnusedPoints.Count;
            Console.WriteLine($"   {msPerQuery} milliseconds per query.");

            String outRow = $"{skippedPoints},{this.allUsedPoints.Count},{rootMeanSquared:F5},{rootMaxSquared:F5},{rootP95Squared:F5},{rootVarianceSquared:F5}";
            using (StreamWriter csvFile = new StreamWriter(v, true))
            {
                csvFile.WriteLine(outRow);
            }

            return;
        }

        public void ComputeLineStatistics()
        {
            var binnedCounts = new Dictionary<Tuple<double, double>, int>();
            var LineCrossSlopes = this.allLines.Values
                .Select(L => (L.DeltaCrossSlopeAsAngleRad))
                .Where(slope => slope != null).Cast<double>()
                .ToArray();
            Array.Sort(LineCrossSlopes);

            double min = LineCrossSlopes[0];
            double max = LineCrossSlopes[^1];


            double binCount = 10.0;
            double binWidth = LineCrossSlopes.Count() / binCount;
            List<int> counts = new List<int>();
            int idx = 0;
            for(int i=0; i<(int)binCount; i++)
            {
                counts.Add(0);
                double cap = (max - min) * (double)(i + 1) / (double)binCount + min;
                while(LineCrossSlopes[idx] < cap)
                {
                    counts[i]++;
                    idx++;
                }
            }
        }

        [NonSerialized]
        private IEnumerable<TINtriangleLine> outerEdgeLines_ = null;
        public IEnumerable<TINtriangleLine> OuterEdgeLines
        {
            get
            {
                if (null == outerEdgeLines_)
                    DetermineEdgePoints();
                return outerEdgeLines_;
            }
        }

        protected void DetermineEdgePoints()
        {
            this.outerEdgeLines_ = this.ValidLines.Where(L => L.IsOnHull);
            foreach (var line in this.OuterEdgeLines)
            {
                line.firstPoint.isOnHull = line.secondPoint.isOnHull = true;
            }
        }

        // temp scratch pad members -- do not serialize
        [NonSerialized]
        private ConcurrentBag<TINtriangle> allTrianglesBag;
        [NonSerialized]
        private TINpoint scratchPoint;
        [NonSerialized]
        private TINtriangle scratchTriangle;
        [NonSerialized]
        private IntPair scratchUIntPair;


        [NonSerialized]
        private TINtriangleLine scratchTriangleLine;
        [NonSerialized]
        private Dictionary<IntPair, TINtriangleLine> triangleLines;
        [NonSerialized]
        private long memoryUsed = 0;

        [NonSerialized]
        private Dictionary<string, Stopwatch> stpWatches;
        [NonSerialized]
        private Stopwatch aStopwatch;
        [NonSerialized]
        static Stopwatch LoadTimeStopwatch = new Stopwatch();
        [NonSerialized]
        public static readonly String StandardExtension = ".TinDN";

        private void LoadTINfromVRML(string fileName)
        {
            string line;
            long lineCount = 0;
            if (!(String.Compare(Path.GetExtension(fileName), ".wrl", true) == 0))
            {
                throw new ArgumentException("Filename must have wrl extension.");
            }

            System.IO.StreamReader file = new System.IO.StreamReader(fileName);
            try
            {
                while ((line = file.ReadLine()) != null)
                {
                    if (false == validateVRMLfileHeader(line))
                        throw new System.IO.InvalidDataException("File not in VRML2 format.");
                    break;
                }

                lineCount++;
                while ((line = file.ReadLine()) != null)
                {
                    lineCount++;
                    if (line.Equals("IndexedFaceSet"))
                        break;
                }

                while ((line = file.ReadLine()) != null)
                {
                    lineCount++;
                    if (line.Equals("point"))
                    {
                        line = file.ReadLine();  // eat the open brace,  [
                        break;
                    }
                }

                ulong ptIndex = 0;
                while ((line = file.ReadLine()) != null)
                {
                    lineCount++;
                    // Read until the close brace,  [
                    if (line.Equals("]"))
                        break;
                    scratchPoint = convertLineOfDataToPoint(line);
                    if (allUsedPoints == null)
                    {
                        createAllpointsCollection();
                        myBoundingBox = new BoundingBox(scratchPoint.x, scratchPoint.y, scratchPoint.x, scratchPoint.y);
                    }
                    allUsedPoints.Add(scratchPoint);
                    ptIndex++;
                    myBoundingBox.expandByPoint(scratchPoint.x, scratchPoint.y, scratchPoint.z);
                }


                while ((line = file.ReadLine()) != null)
                {
                    lineCount++;
                    if (line.Equals("coordIndex"))
                    {
                        line = file.ReadLine();  // eat the open brace,  [
                        break;
                    }
                }

                allTriangles = new List<TINtriangle>();
                while ((line = file.ReadLine()) != null)
                {
                    lineCount++;
                    // Read until the close brace,  [
                    if (line.Equals("]"))
                        break;
                    scratchTriangle = convertLineOfDataToTriangle(line);
                    allTriangles.Add(scratchTriangle);
                }

                allTriangles.Sort();
                this.IndexTriangles();
            }
            finally
            {
                file.Close();
            }
        }

        private static Tuple<int, int> createOrderedTuple(int i1, int i2)
        {
            if (i1 < i2)
                return new Tuple<int, int>(i1, i2);
            return new Tuple<int, int>(i2, i1);
        }
        private void populateAllLines()
        {
            foreach (var aTriangle in this.allTriangles)
            {
                int point1 = aTriangle.point1.myIndex;
                int point2 = aTriangle.point2.myIndex;
                int point3 = aTriangle.point3.myIndex;
                TINtriangleLine aLine = null;
                var tuple = createOrderedTuple(point1, point2);
                if (this.allLines.ContainsKey(tuple))
                {
                    aLine = this.allLines[tuple];
                    aLine.theOtherTriangle = aTriangle;
                }
                else
                {
                    aLine =
                        new TINtriangleLine(aTriangle.point1, aTriangle.point2,
                        aTriangle);
                    allLines[tuple] = aLine;
                }
                aTriangle.myLine1 = aLine;

                tuple = createOrderedTuple(point2, point3);
                if (this.allLines.ContainsKey(tuple))
                {
                    aLine = this.allLines[tuple];
                    aLine.theOtherTriangle = aTriangle;
                }
                else
                {
                    aLine =
                        new TINtriangleLine(aTriangle.point2, aTriangle.point3,
                        aTriangle);
                    allLines[tuple] = aLine;
                }
                aTriangle.myLine2 = aLine;

                tuple = createOrderedTuple(point3, point1);
                if (this.allLines.ContainsKey(tuple))
                {
                    aLine = this.allLines[tuple];
                    aLine.theOtherTriangle = aTriangle;
                }
                else
                {
                    aLine =
                        new TINtriangleLine(aTriangle.point3, aTriangle.point1,
                        aTriangle);
                    allLines[tuple] = aLine;
                }
                aTriangle.myLine3 = aLine;


            }
        }

        private IEnumerable<TINtriangle> getExteriorTriangles()
        {
            if (this.allLines.Count == 0)
                populateAllLines();

            return this.allLines.Values
                .Where(line => line.TriangleCount == 1)
                .Select(line => line.oneTriangle);
        }

        /// <summary>
        /// Marks triangles invalid at the outside edge if they 
        /// meet certain geometric criteria. This is to avoid artifacts
        /// like a false sump filling in a stream where it crosses a
        /// tin boundary.
        /// </summary>
        /// <param name="maxInternalAngle"></param>
        /// <param name="maxSlopeDegrees"></param>
        internal void pruneTinHull(double maxInternalAngle = 157.0,
            double maxSlopeDegrees = 79.5)
        {
            var exteriorTriangles = this.getExteriorTriangles();
            var markNotValid = exteriorTriangles
                .Where(tr => tr.shouldRemove()).ToList();

            foreach (var triangle in markNotValid)
            {
                triangle.IsValid = false;
                triangle.walkNetwork();
            }

        }

        internal void WriteTinToDxf(string outFile)
        {
            // Adapted from https://github.com/haplokuon/netDxf/blob/master/TestDxfDocument/Program.cs
            // MeshEntity()
            // Note: The comments there say MeshEdges are optional.
            var dxf = new DxfDocument();
            dxf.DrawingVariables.AcadVer = netDxf.Header.DxfVersion.AutoCad2013;

            //List<Vector3> vertices = this.allPoints
            //    .Select(pt => new Vector3(pt.x, pt.y, pt.z)).ToList();

            //var faces = new List<int[]>();
            System.Diagnostics.Trace.WriteLine(this.allTriangles.Count + " triangles.");
            int counter = 0;
            foreach (var triangle in this.allTriangles)
            {
                if (counter++ % 1000 == 0) System.Diagnostics.Trace.WriteLine(counter);
                var aFace = new Face3d();
                aFace.FirstVertex = new Vector3(triangle.point1.x, triangle.point1.y, triangle.point1.z);
                aFace.SecondVertex = new Vector3(triangle.point2.x, triangle.point2.y, triangle.point2.z);
                aFace.ThirdVertex = new Vector3(triangle.point3.x, triangle.point3.y, triangle.point3.z);
                aFace.FourthVertex = new Vector3(triangle.point1.x, triangle.point1.y, triangle.point1.z);
                if (!triangle.IsValid)
                    aFace.Color = AciColor.Red;
                if (triangle.IsValid)
                    dxf.AddEntity(aFace);
            }

            dxf.Save(outFile);
        }

        public void WritePointsToDxf(string outFile)
        {
            var dxf = new DxfDocument();
            dxf.DrawingVariables.AcadVer = netDxf.Header.DxfVersion.AutoCad2013;

            foreach (var item in this.allUsedPoints)
            {
                item.AddToDxf(dxf);
            }

            dxf.Save(outFile);
        }

        protected Matrix<double> setAffineTransformToZeroCenter(bool translateTo0 = false)
        {
            double[,] m;
            if (translateTo0)
            {
                double[,] m1 = {{ 1.0, 0.0, 0.0, -1.0 * this.myBoundingBox.Center.x},
                           { 0.0, 1.0, 0.0, -1.0 * this.myBoundingBox.Center.y},
                           { 0.0, 0.0, 1.0, -1.0 * this.myBoundingBox.Center.z},
                           //{ 0.0, 0.0, 1.0, 1.0},
                           { 0.0, 0.0, 0.0, 1.0}};
                m = m1;
            }
            else
            {
                double[,] m2 = {{ 1.0, 0.0, 0.0, 0.0},
                           { 0.0, 1.0, 0.0, 0.0},
                           { 0.0, 0.0, 1.0, 0.0},
                           { 0.0, 0.0, 0.0, 1.0}};
                m = m2;
            }
            var M = Matrix<double>.Build.DenseOfArray(m);
            var mstr = M.ToMatrixString();

            return Matrix<double>.Build.DenseOfArray(m);
        }

        protected StringBuilder convertArrayToString(double[,] array, int colCount, int rowCount)
        {
            var str = new StringBuilder();
            int cols = colCount - 1;
            int rows = rowCount - 1;
            for (int row = 0; row <= rows; row++)
            {
                var s = new StringBuilder("# ");
                for (int col = 0; col <= cols; col++)
                {
                    s.Append(string.Format("{0:0.0######}", array[row, col]));
                    if (col < cols)
                        s.Append(", ");
                }
                str.Append(s.AppendLine());
            }
            return str;
        }

        public void WriteToWaveFront(string outfile, bool translateTo0 = true)
        {
            var affineXform = setAffineTransformToZeroCenter(translateTo0);
            var aString = convertArrayToString(affineXform.ToArray(), 4, 4);
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(outfile))
            {
                file.WriteLine("# Created by CogoDN: Tin Mesh");
                file.WriteLine(aString.ToString());
                foreach (var aPt in this.allUsedPoints)
                    file.WriteLine("v " + aPt.ToString(affineXform));

                foreach (var aTriangle in this.allTriangles)
                    file.WriteLine(aTriangle.IndicesToWavefrontString());
            }
        }

        private TINtriangle convertLineOfDataToTriangle(string line)
        {
            int ptIndex1, ptIndex2, ptIndex3;
            string[] parsed = line.Split(',');
            int correction = parsed.Length - 4;
            ptIndex1 = Convert.ToInt32(parsed[0 + correction]);
            ptIndex2 = Convert.ToInt32(parsed[1 + correction]);
            ptIndex3 = Convert.ToInt32(parsed[2 + correction]);
            TINtriangle triangle = TINtriangle.CreateTriangle(allUsedPoints, ptIndex1, ptIndex2, ptIndex3);
            return triangle;
        }

        private TINpoint convertLineOfDataToPoint(string line)
        {
            TINpoint newPt;
            string[] preParsedLine = line.Split(',');
            string[] parsedLine = preParsedLine[preParsedLine.Length - 1].Split(' ');

            newPt = new TINpoint(
               Convert.ToDouble(parsedLine[0]),
               Convert.ToDouble(parsedLine[1]),
               Convert.ToDouble(parsedLine[2]));

            return newPt;
        }

        private bool validateVRMLfileHeader(string line)
        {
            string[] words = line.Split(' ');
            if (words.Length < 2) return false;
            if (!(words[0].Equals("#VRML", StringComparison.OrdinalIgnoreCase))) return false;
            if (!(words[1].Equals("V2.0", StringComparison.OrdinalIgnoreCase))) return false;

            return true;
        }

        /// <summary>
        /// Creates a tin file
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static TINsurface CreateFromExistingFile(string fileName)
        {
            TINsurface returnTin = new TINsurface();

            if (!String.IsNullOrEmpty(fileName))
            {
                String ext = Path.GetExtension(fileName);
                if (ext.Equals(StandardExtension, StringComparison.OrdinalIgnoreCase))
                    returnTin = loadFromBinary(fileName);
                else
                    returnTin.LoadTextFile(fileName);
            }

            return returnTin;
        }

        /// <summary>
        /// Loads tin from either LandXML or VRML file, depending on the extension passed in.
        /// </summary>
        /// <param name="fileName">Use .xml extension for LandXML. Use .wrl extension for VRML.</param>
        public void LoadTextFile(string fileName)
        {
            string extension;
            if (false == File.Exists(fileName))
                throw new FileNotFoundException("File Not Found", fileName);

            extension = Path.GetExtension(fileName);
            if (extension.Equals(".xml", StringComparison.OrdinalIgnoreCase))
                LoadTINfromLandXML(fileName);
            else if (extension.Equals(".wrl", StringComparison.OrdinalIgnoreCase))
                LoadTINfromVRML(fileName);
            else
                throw new Exception("Filename must have xml or wrl extension.");
        }

        private void LoadTINfromLandXML(string fileName)
        {
            if (!(String.Compare(Path.GetExtension(fileName), ".xml", true) == 0))
            {
                throw new ArgumentException("Filename must have xml extension.");
            }

            memoryUsed = GC.GetTotalMemory(true);
            Stopwatch stopwatch = new Stopwatch();
            List<string> trianglesAsStrings;
            setupStopWatches();

            scratchUIntPair = new IntPair();

            System.Console.WriteLine("Load XML document took:");
            stopwatch.Reset(); stopwatch.Start();
            LoadTimeStopwatch.Reset(); LoadTimeStopwatch.Start();
            using (XmlTextReader reader = new XmlTextReader(fileName))
            {
                stopwatch.Stop(); consoleOutStopwatch(stopwatch);
                System.Console.WriteLine("Seeking Pnts collection took:");
                stopwatch.Reset(); stopwatch.Start();
                reader.MoveToContent();
                reader.ReadToDescendant("Surface");
                string astr = reader.GetAttribute("name");

                // Read Points
                reader.ReadToDescendant("Pnts");
                stopwatch.Stop(); consoleOutStopwatch(stopwatch);

                System.Console.WriteLine("Loading All Points took:");
                stopwatch.Reset(); stopwatch.Start();
                reader.Read();
                while (!(reader.Name.Equals("Pnts") && reader.NodeType.Equals(XmlNodeType.EndElement)))
                {
                    UInt64 id;
                    if (reader.NodeType.Equals(XmlNodeType.Element))
                    {
                        UInt64.TryParse(reader.GetAttribute("id"), out id);
                        reader.Read();
                        if (reader.NodeType.Equals(XmlNodeType.Text))
                        {
                            scratchPoint = new TINpoint(reader.Value, id);
                            if (allUsedPoints == null)
                            {
                                createAllpointsCollection();
                                myBoundingBox = new BoundingBox(scratchPoint.x, scratchPoint.y, scratchPoint.x, scratchPoint.y);
                            }
                            allUsedPoints.Add(scratchPoint);
                            myBoundingBox.expandByPoint(scratchPoint.x, scratchPoint.y, scratchPoint.z);
                        }
                    }
                    reader.Read();
                }


                // Read Triangles, but only as strings
                stopwatch.Stop(); consoleOutStopwatch(stopwatch);
                System.Console.WriteLine(allUsedPoints.Count.ToString() + " Points Total.");

                System.Console.WriteLine("Loading Triangle Reference Strings took:");
                stopwatch.Reset(); stopwatch.Start();
                trianglesAsStrings = new List<string>();
                if (!(reader.Name.Equals("Faces")))
                {
                    reader.ReadToFollowing("Faces");
                }
                reader.Read();
                while (!(reader.Name.Equals("Faces") && reader.NodeType.Equals(XmlNodeType.EndElement)))
                {
                    if (reader.NodeType.Equals(XmlNodeType.Text))
                    {
                        trianglesAsStrings.Add(reader.Value);
                    }
                    reader.Read();
                }
                reader.Close();
                this.IndexTriangles();
                stopwatch.Stop(); consoleOutStopwatch(stopwatch);

                System.Console.WriteLine("Generating Triangle Collection took:");
                stopwatch.Reset(); stopwatch.Start();
            }


            // assemble the allTriangles collection
            //allTriangles = new List<TINtriangle>(trianglesAsStrings.Count);
            allTrianglesBag = new ConcurrentBag<TINtriangle>();
            Parallel.ForEach(trianglesAsStrings, refString =>
            {
                allTrianglesBag.Add(new TINtriangle(allUsedPoints, refString));
            }
               );
            allTriangles = allTrianglesBag.OrderBy(triangle => triangle.point1.x).ToList();
            trianglesAsStrings = null; allTrianglesBag = null;
            GC.Collect(); GC.WaitForPendingFinalizers();
            memoryUsed = GC.GetTotalMemory(true) - memoryUsed;
            LoadTimeStopwatch.Stop();

            stopwatch.Stop();
            System.Console.WriteLine(allTriangles.Count.ToString() + " Total Triangles.");
            consoleOutStopwatch(stopwatch);

            //
            //System.Console.WriteLine("Indexing Triangles for adjacency took:");
            //stopwatch.Reset(); stopwatch.Start();
            //generateTriangleLineIndex();  start here
            //stopwatch.Stop(); consoleOutStopwatch(stopwatch);

        }

//        public void saveJustThePointsThenReadThemAgain()
//        {
//            String filenameToSaveTo = @"C:\Users\Paul\Documents\Visual Studio 2010\Projects\XML Files\Garden Parkway\allPoints.binary";

//            BinaryFormatter binFrmtr = new BinaryFormatter();
//            using
//            (Stream fstream =
//               new FileStream(filenameToSaveTo, FileMode.Create, FileAccess.Write, FileShare.None))
//            {
//                binFrmtr.Serialize(fstream, this.allPoints);
//            }

//            this.allPoints.Clear();
//            this.allTriangles.Clear();
//            GC.Collect();
//            Console.WriteLine("Pausing . . .");
//            Task.Delay(500);

//            BinaryFormatter binFrmtr2 = new BinaryFormatter();
//            using
//            (Stream fstream = File.OpenRead(filenameToSaveTo))
//            {
//                Dictionary<UInt64, TINpoint> testPts = new Dictionary<ulong, TINpoint>();
//                LoadTimeStopwatch = new Stopwatch();
//                LoadTimeStopwatch.Start();
//                try
//                {
//                    testPts = (Dictionary<UInt64, TINpoint>)binFrmtr.UnsafeDeserialize(fstream, null);
//                }
//#pragma warning disable 0168
//                catch (InvalidCastException e)
//#pragma warning restore 0168
//                { return; }
//                finally { LoadTimeStopwatch.Stop(); }

//            }

//        }

        /// <summary>
        /// Saves a tin model object to a file.
        /// </summary>
        /// <param name="filenameToSaveTo">Full path to save the file to. .tinDN extension is required.</param>
        /// <param name="compress">Defaul true. If true, the file is compressed while saving.</param>
        public void saveAsBinary(string filenameToSaveTo, bool compress=true, bool overwrite=true)
        {
            if (!Path.GetExtension(filenameToSaveTo).
               Equals(StandardExtension, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                 String.Format("Filename does not have extension: {0}.", StandardExtension));
            }

            var tempFname = filenameToSaveTo + "data";
            if (!compress)
                tempFname = filenameToSaveTo;

            BinaryFormatter binFrmtr = new BinaryFormatter();
            using (Stream fstream =
               new FileStream(tempFname, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
            {
                binFrmtr.Serialize(fstream, this);
                fstream.Flush();
            }
            if (!compress)
                return;

            var zipFile = filenameToSaveTo + ".zip";
            using (FileStream zipToOpen = new FileStream(zipFile, FileMode.Create))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                {
                    ZipArchiveEntry tinFile = archive.CreateEntryFromFile(tempFname,
                        System.IO.Path.GetFileName(tempFname));
                }
            }

            System.IO.File.Delete(tempFname);
            if (System.IO.File.Exists(filenameToSaveTo))
            {
                if(overwrite)
                {
                    System.IO.File.Delete(filenameToSaveTo);
                }
                else
                {
                    throw new IOException($"Can not overwrite {filenameToSaveTo}. Save operation failed.");
                }
            }
            System.IO.File.Move(zipFile, filenameToSaveTo);
        }

        static public TINsurface loadFromBinary(string filenameToLoad)
        {

            var fnameToLoad = filenameToLoad;
            try
            {
                using(ZipArchive arch = ZipFile.OpenRead(filenameToLoad))
                {
                    var entry = arch.Entries.FirstOrDefault();
                    var fname = entry.FullName;
                    var path = System.IO.Path.GetDirectoryName(filenameToLoad);
                    if (!path.EndsWith(Path.DirectorySeparatorChar.ToString(), 
                        StringComparison.Ordinal))
                        path += Path.DirectorySeparatorChar;
                    fnameToLoad = path + fname;
                    entry.ExtractToFile(fnameToLoad);
                }
            }
            catch(Exception e)
            {
                if(!(e.Message == "End of Central Directory record could not be found."))
                    throw e;
                fnameToLoad = filenameToLoad;
            }

            BinaryFormatter binFrmtr = new BinaryFormatter();
            TINsurface aDTM = null;
            using
            (Stream fstream = File.OpenRead(fnameToLoad))
            {
                aDTM = new TINsurface();
                LoadTimeStopwatch = new Stopwatch();
                LoadTimeStopwatch.Start();
                try
                {
                    aDTM = (TINsurface)binFrmtr.Deserialize(fstream);
                    LoadTimeStopwatch.Stop();
                }
#pragma warning disable 0168
                catch (InvalidCastException e)
#pragma warning restore 0168
                {
                    LoadTimeStopwatch.Stop();
                    return null;
                }
                LoadTimeStopwatch.Stop();

                Parallel.ForEach(aDTM.allTriangles
                   , new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }
                   , tri => tri.computeBoundingBox());

                aDTM.IndexTriangles();
            }
            if (!(fnameToLoad == filenameToLoad))
                System.IO.File.Delete(fnameToLoad);

            return aDTM;
        }

        private void setupStopWatches()
        {
            stpWatches = new Dictionary<string, Stopwatch>();
            stpWatches.Add("Process Points", new Stopwatch());
            stpWatches.Add("Process Triangles", new Stopwatch());
        }

        private void consoleOutStopwatch(Stopwatch anSW)
        {
            TimeSpan ts = anSW.Elapsed;

            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            Console.WriteLine("RunTime " + elapsedTime);
            Console.WriteLine();
        }

        private bool addTriangleLine(int ndx1, int ndx2, TINtriangle aTriangle)
        {
            if (ndx1 == 0 || ndx2 == 0 || aTriangle == null)
                return false;

            if (ndx1 < ndx2)
            {
                scratchUIntPair.num1 = ndx1;
                scratchUIntPair.num2 = ndx2;
            }
            else
            {
                scratchUIntPair.num1 = ndx2;
                scratchUIntPair.num2 = ndx1;
            }

            if (triangleLines == null)
            {
                triangleLines = new Dictionary<IntPair, TINtriangleLine>();
                scratchTriangleLine = new TINtriangleLine(allUsedPoints[ndx1], allUsedPoints[ndx2], aTriangle);
                triangleLines.Add(scratchUIntPair, scratchTriangleLine);

                return true;
            }

            bool tryGetSucces = triangleLines.TryGetValue(scratchUIntPair, out scratchTriangleLine);
            if (tryGetSucces == false)  // we must add this line to the collection
            {
                scratchTriangleLine = new TINtriangleLine(allUsedPoints[ndx1], allUsedPoints[ndx2], aTriangle);
                triangleLines.Add(scratchUIntPair, scratchTriangleLine);
                return true;
            }
            else
            {
                if (scratchTriangleLine.theOtherTriangle == null)
                {
                    scratchTriangleLine.theOtherTriangle = aTriangle;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        internal IReadOnlyCollection<TINtriangle> TrianglesReadOnly
        { get { return (IReadOnlyCollection<TINtriangle>)this.ValidTriangles.ToList(); } }

        public void testGetTriangles(TINpoint aPoint)
        {

            aStopwatch = new Stopwatch();
            System.Console.WriteLine("given a point, return triangles by BB:");
            aStopwatch.Reset(); aStopwatch.Start();

            List<TINtriangle> triangleSubset = getTrianglesForPointInBB(aPoint) as List<TINtriangle>;

            aStopwatch.Stop(); consoleOutStopwatch(aStopwatch);
        }

        internal List<TINtriangle> getTrianglesForPointInBB(TINpoint aPoint)
        {
            return (from TINtriangle triangle in allTriangles.AsParallel()
                    where triangle.isPointInBoundingBox(aPoint)
                    select triangle).ToList<TINtriangle>();
        }

        public void testGetTriangle(CadFound.Point aPoint)
        {
            aStopwatch = new Stopwatch();
            System.Console.WriteLine("given a point, return containing Triangle:");
            aStopwatch.Reset(); aStopwatch.Start();

            TINtriangle singleTriangle = getTriangleContaining((TINpoint)aPoint);

            aStopwatch.Stop(); consoleOutStopwatch(aStopwatch);
        }

        private List<TINtriangle> localGroupTriangles;
        internal TINtriangle getTriangleContaining(TINpoint aPoint)
        {
            //if (null == localGroupTriangles)
            //    localGroupTriangles = getTrianglesForPointInBB(aPoint).AsParallel().ToList();

            //TINtriangle theTriangle =
            //   localGroupTriangles.FirstOrDefault(aTrngl => aTrngl.contains(aPoint));
            var candidateTriangles = validTrianglesIndexed.FindObjectsAt(aPoint.x, aPoint.y).Cast<TINtriangle>();

            //if (null == theTriangle)
            //{
            //    localGroupTriangles = getTrianglesForPointInBB(aPoint).AsParallel().ToList();
            //    theTriangle =
            //       localGroupTriangles.FirstOrDefault(aTrngl => aTrngl.contains(aPoint));
            //}

            return candidateTriangles.Where(tri => tri.contains(aPoint)).FirstOrDefault();
        }

        public double? getElevation(double x, double y)
        {
            return getElevation(new TINpoint(x, y));
        }

        public double? getElevation(CadFoundation.Coordinates.Point aPoint)
        {
            return getElevation((TINpoint)aPoint);
        }
        public double? getElevation(TINpoint aPoint)
        {
            TINtriangle aTriangle = getTriangleContaining(aPoint);
            if (null == aTriangle)
                return null;

            return aTriangle.givenXYgetZ(aPoint);

        }

        public double? getSlope(CadFound.Point aPoint)
        {
            return getSlope((TINpoint)aPoint);
        }

        public double? getSlope(TINpoint aPoint)
        {
            TINtriangle aTriangle = getTriangleContaining(aPoint);
            if (null == aTriangle)
                return null;

            return aTriangle.givenXYgetSlopePercent(aPoint);

        }

        public Azimuth getAspect(CadFound.Point aPoint)
        {
            return getAspect((TINpoint)aPoint);
        }

        public Azimuth getAspect(TINpoint aPoint)
        {
            TINtriangle aTriangle = getTriangleContaining(aPoint);
            if (null == aTriangle)
                return null;

            return aTriangle.givenXYgetSlopeAzimuth(aPoint);

        }

        public PointSlopeAspect getElevationSlopeAzimuth(double x, double y)
        {
            return getElevationSlopeAzimuth(new TINpoint(x, y));
        }

        public PointSlopeAspect getElevationSlopeAzimuth(TINpoint aPoint)
        {
            return new PointSlopeAspect(
                aPoint,
                getElevation(aPoint),
                getSlope(aPoint),
                getAspect(aPoint)
                );
        }

        public void loadFromXYZtextFile(string fileToOpen)
        {
            //BoundingBox fileBB = new BoundingBox()
            using (var inputFile = new StreamReader(fileToOpen))
            {
                String line;
                String[] values;
                while ((line = inputFile.ReadLine()) != null)
                {
                    values = line.Split(',');
                    if (values.Length != 3) continue;
                    var newPt = new TINpoint(values[0], values[1], values[2]);
                    GridDTMhelper.addPoint(newPt);
                }
            }
        }

        private void createAllpointsCollection()
        {
            allUsedPoints = new List<TINpoint>();
        }

        public String GenerateSizeSummaryString()
        {
            StringBuilder returnString = new StringBuilder();
            returnString.AppendLine(String.Format(
               "Points: {0:n0} ", allUsedPoints.Count));
            returnString.AppendLine(String.Format("Triangles: {0:n0}", this.allTriangles.Count));
            returnString.AppendLine(String.Format("Total Memory Used: Approx. {0:n0} MBytes",
               memoryUsed / (1028 * 1028)));
            returnString.AppendLine(String.Format(
               "{0:f4} Average Points per Triangle.",
               (Double)((Double)allUsedPoints.Count / (Double)allTriangles.Count)));
            returnString.AppendLine(String.Format("Total Load Time: {0:f4} seconds",
               (Double)LoadTimeStopwatch.ElapsedMilliseconds / 1000.0));
            return returnString.ToString();
        }

        
    }

    internal class randomIndices
    {
        public int PopulationCount { get; private set; }
        public int SampleCount { get; private set; }
        public HashSet<int> indices = new HashSet<int>();
        internal randomIndices(int populationCount, int sampleCount)
        {
            PopulationCount = populationCount;
            SampleCount = sampleCount;
            var samples = 0;
            var rnd = new Random();
            while (true)
            {
                bool wasAdded = false;
                while (!wasAdded)
                {
                    wasAdded = indices.Add(rnd.Next(0, populationCount));
                }
                if (indices.Count >= sampleCount)
                    break;
            }
        }
    }



        internal static class GridDTMhelper
    {
        private const long GridSize = 500;
        public static Dictionary<XYtuple, List<TINpoint>> grid = new Dictionary<XYtuple, List<TINpoint>>();
        public static void addPoint(TINpoint pt)
        {
            long xGrid = (long)Math.Floor(pt.x / GridSize);
            long yGrid = (long)Math.Floor(pt.y / GridSize);
            addPoint_(new XYtuple(xGrid, yGrid), pt);
        }

        private static void addPoint_(XYtuple tupl, TINpoint pt)
        {
            if (false == grid.ContainsKey(tupl))
            {
                var ptList = new List<TINpoint>();
                ptList.Add(pt);
                grid.Add(tupl, ptList);
            }
            else
            {
                grid[tupl].Add(pt);
            }
        }
    }

    internal class XYtuple
    {
        public XYtuple(long x, long y)
        {
            X = x; Y = y;
        }
        public long X { get; set; }
        public long Y { get; set; }
    }

    public class PointSlopeAspect
    {
        public CadFound.Point Point { get; private set; }
        public double? Elevation { get; private set; }
        public double? Slope { get; private set; }
        public Azimuth Aspect { get; private set; }

        public PointSlopeAspect(TINpoint pt, double? el = null, double? sl = null, Azimuth aspect = null)
        {
            this.Point = new CadFound.Point(pt.x, pt.y);
            if (el != null) this.Point.z = (double)el;
            this.Elevation = el;
            this.Slope = sl;
            this.Aspect = aspect;
        }

        public override string ToString()
        {
            return $"EL: {Elevation:f2}, SL: {Slope:f1}%, AS: {Aspect}";
        }

        private static double tolerance = 0.15;
        private static double ntolerance = -tolerance;
        /// <summary>
        /// Compares only the derived values, elevation, slope, and aspect.
        /// </summary>
        /// <param name="el"></param>
        /// <param name="sl"></param>
        /// <param name="aspect"></param>
        public void AssertDerivedValuesAreEqual(double? el, double? sl, Azimuth aspect)
        {
            bool verifyNullStatus_AndIsItNull(Object n1, Object n2, string msg)
            {
                if (n1 == null && n2 == null) return true;
                if (n1 != null && n2 != null) return false;
                throw new Exception(msg);
            }

            if (verifyNullStatus_AndIsItNull(el, this.Elevation,
                "Elevation items not same null state."))
                return;

            double? diff = this.Elevation - el;
            if (diff > tolerance || diff < ntolerance)
                throw new Exception("Elevation values differ.");

            if (verifyNullStatus_AndIsItNull(sl, this.Slope,
                "Slope items not same null state."))
                return;

            diff = this.Slope - sl;
            if (diff > tolerance || diff < ntolerance)
                throw new Exception("Slope values differ.");

            if (verifyNullStatus_AndIsItNull(aspect, this.Aspect,
                "Aspect items not same null state."))
                return;

            diff = this.Aspect - aspect;
            if (diff > tolerance || diff < ntolerance)
                throw new Exception("Aspect values differ.");

        }

        public bool Equals(PointSlopeAspect other)
        {
            bool status = true;
            status &= this.Aspect == other.Aspect;
            status &= this.Slope == other.Slope;
            status &= this.Point.z.tolerantEquals(other.Point.z, 0.001);
            status &= this.Point.x.tolerantEquals(other.Point.x, 0.001);
            status &= this.Point.y.tolerantEquals(other.Point.y, 0.001);
            return status;
        }
    }

    [Serializable]
    public class TINstatistics
    {

        public TINstatistics(TINsurface dtm)
        {
            PointCount = dtm.allUsedPoints.Count;

            var sortedInX = dtm.allUsedPoints.OrderBy(pt => pt.x);
            MinX = sortedInX.First().x;
            MaxX = sortedInX.Last().x;
            WidthX = MaxX - MinX;
            CenterX = MinX + WidthX / 2.0;
            MedianX = Median(sortedInX.Select(pt => pt.x));
            sortedInX = null;
            AverageX = dtm.allUsedPoints.Average(pt => pt.x);

            var sortedInY = dtm.allUsedPoints.OrderBy(pt => pt.y);
            MinY = sortedInY.First().y;
            MaxY = sortedInY.Last().y;
            WidthY = MaxY - MinY;
            CenterY = MinY + WidthY / 2.0;
            MedianY = Median(sortedInY.Select(pt => pt.y));
            sortedInY = null;
            AverageY = dtm.allUsedPoints.Average(pt => pt.y);

            var sortedInZ = dtm.allUsedPoints.OrderBy(pt => pt.y);
            MinZ = sortedInZ.First().x;
            MaxZ = sortedInZ.Last().x;
            HeightZ = MaxZ - MinZ;
            CenterZ = MinZ + HeightZ / 2.0;
            MedianZ = Median(sortedInZ.Select(pt => pt.z));
            sortedInZ = null;
            AverageZ = dtm.allUsedPoints.Average(pt => pt.z);

            LineCount = dtm.ValidLines.Count;
            var lines = dtm.ValidLines.OrderBy(line => line.Length2d).ToList();
            ShortestLinePlane = lines.First().Length2d;
            LongestLinePlane = lines.Last().Length2d;
            MedianLinePlane = lines[LineCount / 2].Length2d;
            lines = null;


            var tris = dtm.ValidTriangles;
            TriangleCount = tris.Count();

            PlanarArea = dtm.BoundingBox.Area;

            // To do: Everything else

        }

        public int PointCount { get; set; }
        public double WidthX { get; set; }
        public double WidthY { get; set; }
        public double HeightZ { get; set; }
        public double MinX { get; set; }
        public double MinY { get; set; }
        public double MinZ { get; set; }
        public double MaxX { get; set; }
        public double MaxY { get; set; }
        public double MaxZ { get; set; }
        public double CenterX { get; set; }
        public double CenterY { get; set; }
        public double CenterZ { get; set; }
        public double AverageX { get; set; }
        public double AverageY { get; set; }
        public double AverageZ { get; set; }
        public double MedianX { get; set; }
        public double MedianY { get; set; }
        public double MedianZ { get; set; }
        public int LineCount { get; set; }
        public double ShortestLinePlane { get; set; }
        public double LongestLinePlane { get; set; }
        public double AverageLinePlane { get; set; }
        public double MedianLinePlane { get; set; }
        public double ShortestLine3d { get; set; }
        public double LongestLine3d { get; set; }
        public double AverageLine3d { get; set; }
        public double MedianLine3d { get; set; }
        public int TriangleCount { get; set; }
        public double SmallestTriangleAreaPlane { get; set; }
        public double LargestTriangleAreaPlane { get; set; }
        public double AverageTriangleAreaPlane { get; set; }
        public double MedianTriangleAreaPlane { get; set; }
        public double SmallestTriangleArea3d { get; set; }
        public double LargestTriangleArea3d { get; set; }
        public double AverageTriangleArea3d { get; set; }
        public double MedianTriangleArea3d { get; set; }
        public double MinTriangleSlope { get; set; }
        public double MaxTriangleSlope { get; set; }
        public double AverageTriangleSlopeWeighted { get; set; }
        public double MedianTriangleSlope { get; set; }
        public Vector AverageTriangleAspectWeighted { get; set; }

        public double PlanarArea { get; set; }
        public double PointsPerSquareUnit { get { return PointCount / PlanarArea; } }

        protected double Median(IEnumerable<double> values)
        {
            int midIndex = (values.Count() - 1) / 2;
            if (values.Count() % 2 == 0) // Even Number of items
            {
                var loc1 = values.Skip(midIndex);
                var loc2 = loc1.Skip(1);
                return (loc1.First() + loc2.First()) / 2.0;
            }
            else // Odd number of items
            {
                return values.Skip((midIndex)).First();
            }
        }

        public override string ToString()
        {
            // To Do:
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Points: {PointCount}");
            sb.AppendLine($"Width (in x): {WidthX:F2}    Depth (in y): {WidthY:F2}   EL: {HeightZ:F2}");

            sb.AppendLine();
            sb.AppendLine($"Lines: {LineCount}");
            sb.AppendLine($"Planar Lengths: Min: {ShortestLinePlane}  Median: {MedianLinePlane:F2}   Max: {LongestLinePlane:F2}");

            sb.AppendLine();
            sb.AppendLine($"Triangles: {TriangleCount}");
            sb.AppendLine($"Planar Areas: Min: ?   Median: ?   Max: ?");

            sb.AppendLine();
            sb.AppendLine($"Bounding Box: {PlanarArea}");
            sb.AppendLine($"Points per Square Unit: {PointsPerSquareUnit}");
            sb.AppendLine($"Square Units per Point: {1.0 / PointsPerSquareUnit}");

            return sb.ToString();
        }
    }


        public static class TupleExtensionMethods
    {
        public static bool Contains(this Tuple<int, int> tpl, int first, int second)
        {
            if (tpl.Item1 == first && tpl.Item2 == second)
                return true;
            if (tpl.Item1 == second && tpl.Item2 == first)
                return true;
            return false;
        }
    }
}
