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
using Surfaces.TIN.Support;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
        private double decimationRemainingPercent { get; set; }
        public double runSpanMinutes { get; private set; }

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

        protected static TextMessagePump messagePump = new TextMessagePump();
        public static TextMessagePump GetMessagePump(IObserver<String> observer)
        {
            messagePump.Register(observer);
            return messagePump;
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
        { // ToDo: Throw file not found exception
            var stopwatch = new Stopwatch(); stopwatch.Start();
            messagePump.BroadcastMessage($"Creating Tin in memory from {lidarFileName}.");
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
            messagePump.BroadcastMessage($"{lasFile.AllPoints.Count:N0} LAS points loaded.");
            lasFile.ClearAllPoints();  // Because I have them now.

            for (indexCount = 0; indexCount < returnObject.allUsedPoints.Count; indexCount++)
            {
                var aPoint = returnObject.allUsedPoints[indexCount];
                aPoint.myIndex = indexCount;
                returnObject.allUsedPoints[indexCount] = aPoint;
            }

            messagePump.BroadcastMessage("Tesselating points to triangles.");
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
                if (!(newTriangle is null))
                    returnObject.allTriangles.Add(newTriangle);
            }
            messagePump.BroadcastMessage("Tin created. Final processing ...");
            returnObject.finalProcessing();
            messagePump.BroadcastMessage("Final processing complete. " +
                $"{returnObject.allTriangles.Count:N0} Triangles, " +
                $"{returnObject.allLines.Count:N0} Lines.");
            messagePump.BroadcastMessage
                ($"In {stopwatch.ElapsedMilliseconds / 1000.0:0.000} seconds" +
                $"( {stopwatch.ElapsedMilliseconds / 60000.0:0.000} minutes).");

            return returnObject;
        }

        /// <summary>
        /// Decimates a given TINsurface using the Smart Decimation algorithm.
        /// 1. Retain all hull points.
        /// 2. Retain points associated with lines of high rollover.
        /// 3. Retain randomly selected points via score based on curvature and sparsity.
        /// 4. Create a new TINsurface from the retained points.
        /// </summary>
        /// <param name="sourceSurface">Instance of TINsurface with all points. This is the dataset that is to be decimated.</param>
        /// <param name="decimationRemainingPercent"></param>
        /// <returns></returns>
        public static TINsurface CreateByDecimation
            (TINsurface sourceSurface, 
            double decimationRemainingPercent,
            double dihedral_CurvatureSplit=0.50)
        {
            messagePump.BroadcastMessage(
                $"Smart Decimation to {decimationRemainingPercent * 100.0:F2}% -- Started.");
            return CreateByReductionAlgorithm(sourceSurface, decimationRemainingPercent,
                computeLikelihoodsSmart, dihedral_CurvatureSplit);
        }

        private static void adjustLiklihoodsToTargetMean(
            Dictionary<int, tinPointParameters> dataset, 
            double targetMean, double pretransformMean=-1.0)
        {
            //double preTransMean = pretransformMean;
            if(pretransformMean < 0.0)
            {
                pretransformMean = (new DescriptiveStatistics
                (dataset.Values.Select(v => v.retainProbability))).Mean;
            }

            // Equation taken from Weilinga 2007, pp 6, 7
            // https://www.mwsug.org/proceedings/2007/saspres/MWSUG-2007-SAS01.pdf
            // Weilinga, D., 2007, Identifying and Overcoming Common Data Mining Mistakes
            // SAS Global Forum. Orlando, FL, p 7. I use Weilinga's symbols here.
            double p1 = pretransformMean;
            double p0 = 1.0 - p1;
            double tau1 = targetMean;
            double tau0 = 1.0 - tau1;

            foreach(var item in dataset.Values)
            {
                var P = item.retainProbability;

                var Padj = (P * tau1 * p0) /
                    ((P * tau1 * p0)+((1.0 - P) * tau0 * p1));

                item.retainProbability = Padj;
            }
        }

        protected SamplingGrid samplingGrid { get; set; } = null;
        public void SetSampleGrid(double desiredSamplePointDensity)
        {
            if(desiredSamplePointDensity == 0.0)
            {
                samplingGrid = null;
                return;
            }
            samplingGrid = new SamplingGrid(this.myBoundingBox, desiredSamplePointDensity);
            samplingGrid.SetSourceElevationValues(this);
        }

        /// <summary>
        /// For each point in the dictionary (keys), computes the point density,
        /// Aggregate Cross Slope, and the Retain Probability.
        /// This is a side-effect function. It modifies each point pool index
        /// retainProbability in-place to bring the dataset mean to the 
        /// desired percent within a tolerance. Tolerance is hard-coded.
        /// </summary>
        /// <param name="sourceSurface">The undecimted TINsurface upon which to based computations.</param>
        /// <param name="pointPoolIndices">The collection of values to be modified in-place.</param>
        /// <param name="decimationRemainingPercent">Target value for the collection mean retain probability.</param>
        /// <returns>Descriptive Statistics of the final state of the retain probabilities. This is for verification only and may be ignored.</returns>
        private static DescriptiveStatistics ComputePointRetentionLikelihood
            (TINsurface sourceSurface,
            Dictionary<int, tinPointParameters> pointPoolIndices,
            double decimationRemainingPercent)
        {
            double tolerance = 0.00001;

            // populate line and triangle references for each point
            foreach (var line in sourceSurface.allLines.Values)
            {
                TINpoint firstPoint = line.firstPoint;
                int idx = firstPoint.myIndex;
                if(pointPoolIndices.ContainsKey(idx))
                {
                    pointPoolIndices[idx].myLines.Add(line);
                }

                TINpoint secondPoint = line.secondPoint;
                idx = secondPoint.myIndex;
                if (pointPoolIndices.ContainsKey(idx))
                {
                    pointPoolIndices[idx].myLines.Add(line);
                }
            }
            foreach (var triangle in sourceSurface.allTriangles)
            {
                int idx = triangle.point1.myIndex;
                if(pointPoolIndices.ContainsKey(idx))
                    pointPoolIndices[idx].myTriangles.Add(triangle);

                idx = triangle.point2.myIndex;
                if (pointPoolIndices.ContainsKey(idx))
                    pointPoolIndices[idx].myTriangles.Add(triangle);

                idx = triangle.point3.myIndex;
                if (pointPoolIndices.ContainsKey(idx))
                    pointPoolIndices[idx].myTriangles.Add(triangle);
            }
            // end: populate line and triangle references for each point

            // Compute Gaussian curvature for each point.
            foreach(var idx in pointPoolIndices.Keys)
            {
                var ptParams = pointPoolIndices[idx];
                ptParams.gaussianCurvature = 0.0;
                foreach(var tri in ptParams.myTriangles)
                {
                    ptParams.gaussianCurvature += tri.getFaceAngleForPoint(idx);
                }
                ptParams.aggregateCrossSlope =
                    ptParams.myLines
                    .Where(L => null != L.DeltaCrossSlopeAsAngleRad)
                    .Select(L => 
                        Math.Abs((double)L.DeltaCrossSlopeAsAngleRad) / L.Length2d)
                    .Sum() / ptParams.myLines.Count;

                ptParams.pointSparsity = ptParams.myTriangles
                    .Select(t => t.Area2d).Sum() / 3.0;

                ptParams.retainProbability = 
                    ptParams.pointSparsity * ptParams.aggregateCrossSlope;
            }
            // Compute Gaussian curvature for each point.

            int removedCount = 0;
            foreach (var idx in pointPoolIndices.Keys)
            {
                if (Double.IsNaN(pointPoolIndices[idx].retainProbability))
                {
                    pointPoolIndices.Remove(idx);
                    removedCount++;
                }
            }
            var maxVal = pointPoolIndices.Values.Select(v => v.retainProbability).Max();
            foreach(var poolItem in pointPoolIndices.Values)
            {
                poolItem.retainProbability /= maxVal;
            }
            foreach(var poolItem in pointPoolIndices.Values)
            {
                double retainProb = poolItem.retainProbability;
                double oneComplement = 1.0 - retainProb;
            }
            var stats = new DescriptiveStatistics
                (pointPoolIndices.Values.Select(v => v.retainProbability));

            double popMean = stats.Mean;
            while(Math.Abs(popMean - decimationRemainingPercent) > tolerance)
            {
                adjustLiklihoodsToTargetMean(pointPoolIndices,
                    decimationRemainingPercent, popMean);

                stats = new DescriptiveStatistics
                    (pointPoolIndices.Values.Select(v => v.retainProbability));

                popMean = stats.Mean;
            }

            return stats;
        }

        private static List<TINpoint> computeLikelihoodsSmart
            (TINsurface sourceSurface, double decimationPercent, 
            double dihedral_CurvatureSplit)
        { 
            int usedPoints = 0;
            var tempAllPoints = (sourceSurface.allUsedPoints
                .Concat(sourceSurface.allUnusedPoints))
                .Select(pt => new TINpoint(pt))
                .ToList();

            // Force hull points to be always included.
            //tempAllPoints.ForEach(
            Parallel.ForEach(tempAllPoints,
                pt =>
                {
                    pt.hasBeenSkipped = true;
                    if (pt.isOnHull)
                    {
                        pt.retainProbability = 1.0;
                        usedPoints++;
                    }
                    else
                        pt.retainProbability = 0.0;
                });

            int hullPointCount = usedPoints;
            int pointCountSoFar = hullPointCount;
            int nonHullPointCount = tempAllPoints.Count;
            int targetPointCount = (int)(decimationPercent * tempAllPoints.Count);
            int remainingPointsToGetCount = targetPointCount - hullPointCount;
            double decimationRemainingPercent = (double)remainingPointsToGetCount /
                (double)tempAllPoints.Count;

            // Assign retain probability to all other points based on smart decimation.
            //    For half of available points, retain based on line cross slope

            //      Variable "usedIndices" holds indices into the original source surface.
            //      We added indices of used points as we go along.
            var usedIndices = new HashSet<int>(
                sourceSurface.allUsedPoints
                .Where(pt => pt.isOnHull)
                .Select(pt => pt.myIndex).ToList());

            //      Variable "pointPoolIndices" is points available to move over to usedIndices
            //      We will use a dictionary where each entry refers to a point in the
            //      original point collection. This is a way to add "extension properties"
            //      to each point without bloating the point class with luggage that is
            //      used only for this process.
            //      Variable pointPoolIndices is the indices of points that have not
            //      yet been selected to retain in the used-points dataset.
            //      When a point is selected for retention, it is moved from the pool to
            //      the variable usedIndices.
            var pointPoolIndices = new Dictionary<int, tinPointParameters>();
            foreach (var anInt in
                sourceSurface.allUsedPoints
                .Where(pt => !pt.isOnHull)
                .Select(pt => pt.myIndex))
            {
                pointPoolIndices.Add(anInt, new tinPointParameters());
            }

            var sourceLines = sourceSurface.allLines
                .OrderByDescending(line => line.Value.DeltaCrossSlopeAsAngleRad)
                .Select(line => line.Value)
                .ToList();

            remainingPointsToGetCount -= usedIndices.Count;
            int pointsToGet =
                (int)(decimationRemainingPercent * 
                (dihedral_CurvatureSplit * remainingPointsToGetCount));

            // Get points based on highest line cross slope until half of the
            // available points in the Pool have been taken. In other words, take
            // points until usedIndices is full.
            int lineIndex = 0;
            int ptIdx = 0;
            while (pointsToGet >= 0)
            {
                var aLine = sourceLines[lineIndex++];
                var firstPoint = aLine.firstPoint;
                if (!usedIndices.Contains(firstPoint.myIndex))
                {
                    ptIdx = firstPoint.myIndex;
                    tempAllPoints[ptIdx].retainProbability = 1.0;
                    usedIndices.Add(ptIdx);
                    pointPoolIndices.Remove(ptIdx);
                    pointsToGet--;
                    remainingPointsToGetCount--;
                    pointCountSoFar++;
                }
                var secondPoint = aLine.secondPoint;
                if (!usedIndices.Contains(secondPoint.myIndex))
                {
                    ptIdx = secondPoint.myIndex;
                    tempAllPoints[ptIdx].retainProbability = 1.0;
                    usedIndices.Add(ptIdx);
                    pointPoolIndices.Remove(ptIdx);
                    pointsToGet--;
                    remainingPointsToGetCount--;
                    pointCountSoFar++;
                }
            }
            //    end of "For half of available points, retain based on line cross slope"

            //    For half, assign retain prob based on curvature/sparsity score
            int pointsNowNeeded = targetPointCount - pointCountSoFar;
            double percentOfRemaining = (double)pointsNowNeeded /
                (double)pointPoolIndices.Count;

            ComputePointRetentionLikelihood(sourceSurface,
                pointPoolIndices, percentOfRemaining);

            Parallel.ForEach(pointPoolIndices,
                kvp =>
            //foreach (var kvp in pointPoolIndices)
            {
                ptIdx = kvp.Key;
                tempAllPoints[ptIdx].retainProbability = 
                    kvp.Value.retainProbability;
            }
            );
            //    end of "For half, assign retain prob based on curvature/sparsity score"

            // end of "Assign retain probability to all other points"

            return tempAllPoints;
        }

        /// <summary>
        /// Implements random decimation
        /// </summary>
        /// <param name="sourceSurface"></param>
        /// <param name="decimationPercent"></param>
        private static List<TINpoint> computeLikelihoodsRandom(TINsurface sourceSurface, 
            double decimationPercent, double notUsed=0.0)
        {
            int usedPoints = 0;
            var tempAllPoints = (sourceSurface.allUsedPoints
                .Concat(sourceSurface.allUnusedPoints))
                .Select(pt => new TINpoint(pt))
                .ToList();

            // Force hull points to be always included.
            //tempAllPoints.ForEach(
            Parallel.ForEach(tempAllPoints,
                pt =>
                {
                    pt.hasBeenSkipped = true;
                    if (pt.isOnHull)
                    {
                        pt.retainProbability = 1.0;
                        usedPoints++;
                    }
                    else
                        pt.retainProbability = 0.0;
                });

            int hullPointCount = usedPoints;
            int nonHullPointCount = tempAllPoints.Count;
            int targetPointCount = (int) (decimationPercent * tempAllPoints.Count);
            int remainingPointsToGetCount = targetPointCount - hullPointCount;
            double adjustedRetainProbability = (double)remainingPointsToGetCount /
                (double)tempAllPoints.Count;

            // Assign random retain probability values to all other points.
            Parallel.ForEach(tempAllPoints,
                pt =>
                {
                    pt.hasBeenSkipped = true;
                    if (!pt.isOnHull)
                        pt.retainProbability = adjustedRetainProbability;
                });

            return tempAllPoints;
        }

        public static TINsurface CreateByRandomDecimation(TINsurface sourceSurface,
            double decimationRemainingPercent)
        {
            messagePump.BroadcastMessage(
                $"Random Decimation to {decimationRemainingPercent * 100.0:F2}% -- Started.");
            return CreateByReductionAlgorithm(sourceSurface, decimationRemainingPercent,
                computeLikelihoodsRandom, 0.0);
        }

        protected static TINsurface CreateByReductionAlgorithm(TINsurface sourceSurface,
            double decimationRemainingPercent,
            Func<TINsurface, double, double, List<TINpoint>> likelihoodFunction,
            double dihedral_CurvatureSplit)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            var returnObject = new TINsurface();

            var tempAllPoints =
                likelihoodFunction(sourceSurface, decimationRemainingPercent, 
                dihedral_CurvatureSplit);

            returnObject.SourceData = sourceSurface.SourceData +
                $"Randomly Decimated {decimationRemainingPercent:0.000}";
            returnObject.decimationRemainingPercent = decimationRemainingPercent;

            var gridIndexer = new Dictionary<Tuple<int, int>, int>();
            //foreach(var pt in tempAllPoints)
            Parallel.ForEach(tempAllPoints, pt => 
            {
                Random drm = new Random();
                double diceRoll = drm.NextDouble();
                if (diceRoll <= pt.retainProbability)
                    pt.hasBeenSkipped = false;
            }
            );

            returnObject.allUsedPoints =
                tempAllPoints
                .Where(pt => pt.hasBeenSkipped == false)
                .ToList();
            returnObject.unused_points = tempAllPoints
                .Where(pt => pt.hasBeenSkipped == true).ToList();

            int indexCount = 0;
            foreach (var aPoint in returnObject.allUsedPoints)
            {
                aPoint.myIndex = indexCount;
                gridIndexer[aPoint.GridCoordinates] = indexCount;
                indexCount++;
            }
            var runSpanMinutes = stopwatch.ElapsedMilliseconds / 60000.0;

            tempAllPoints = null;
            GC.Collect();
            setBoundingBox(returnObject);

            messagePump.BroadcastMessage
                ($"Creating Decimated Tin Surface from {returnObject.allUsedPoints.Count:N0} points.");

            var VoronoiMesh = MIConvexHull.VoronoiMesh
                .Create<TINpoint, ConvexFaceTriangle>(returnObject.allUsedPoints);

            returnObject.allTriangles = new List<TINtriangle>(2 * returnObject.allUsedPoints.Count);
            foreach (var vTriangle in VoronoiMesh.Triangles)
            {
                var point1 = gridIndexer[vTriangle.Vertices[0].GridCoordinates];
                var point2 = gridIndexer[vTriangle.Vertices[1].GridCoordinates];
                var point3 = gridIndexer[vTriangle.Vertices[2].GridCoordinates];
                TINtriangle newTriangle = null;
                newTriangle = TINtriangle.CreateTriangle(
                    returnObject.allUsedPoints, point1, point2, point3);
                if (!(newTriangle is null))
                    returnObject.allTriangles.Add(newTriangle);
            }
            messagePump.BroadcastMessage("Final processing.");
            returnObject.finalProcessing();
            messagePump.BroadcastMessage("Created Tin by random decimation, complete:");
            messagePump.BroadcastMessage(
                $"{returnObject.allUsedPoints.Count:N0} Points, " + 
                $"{returnObject.allTriangles.Count:N0} Triangles, {returnObject.allLines.Count:N0} Lines.");
            stopwatch.Stop();
            messagePump.BroadcastMessage
                ($"In {stopwatch.ElapsedMilliseconds / 1000.0:0.000} seconds" +
                $"( {stopwatch.ElapsedMilliseconds / 60000.0:0.000} minutes).");

            returnObject.runSpanMinutes = runSpanMinutes;

            return returnObject;
        }

        protected void correctUpsidedownTriangles()
        {
            List<TINtriangle> upsideDownTriangles = this.allTriangles.Where(t => t.normalVec.z < 0.0).ToList();            
            foreach (var tri in upsideDownTriangles)
                tri.SwapPoint1And2();
            
            upsideDownTriangles = null;
            GC.Collect();
        }

        protected void finalProcessing()
        {
            this.pruneTinHull();
            this.IndexTriangles();
            if (this.allLines.Count <= 1)
                populateAllLines();
            this.DetermineEdgePoints();
            this.correctUpsidedownTriangles();
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

        public void ComputeErrorStatistics(string v, TINsurface mainSurface=null)
        {
            SamplingGrid samplingGrid = null;
            if (null != mainSurface)
                samplingGrid = mainSurface.samplingGrid;

            if (allUnusedPoints.Count == 0)
                return;

            // ToDo: Verify path exists; throw if not.

            if (!File.Exists(v))
                System.IO.File.WriteAllText(v,
                    "DecimationPercent,PointCount," +
                    "p25,AbsMean,p75,p95,Max,RMSE,\r\n");

            randomIndices rdmIdc = new randomIndices(allUnusedPoints.Count, 1000);
            var squaredErrorsBag = new ConcurrentBag<double?>();
            var errorsBag = new ConcurrentBag<double>();

            Console.WriteLine();
            Console.WriteLine("Starting Elevation Computation");
            
            var sw = Stopwatch.StartNew();
            if(samplingGrid == null)
            {
                //foreach(var pt in allUnusedPoints)
                Parallel.ForEach(allUnusedPoints, pt =>
                {
                    var error = pt.z - getElevation(pt);
                    squaredErrorsBag.Add(error * error);
                    if (error != null)
                        errorsBag.Add(Math.Abs((double)error));
                }
                );
                //;
            }
            else
            {
                int columns = samplingGrid.columns;
                int rows = samplingGrid.rows;
                var x = samplingGrid.x;
                var y = samplingGrid.y;
                var z = samplingGrid.z;
                Parallel.For(0, columns,
                    i =>
                //for (int i = 0; i < columns; i++)
                {
                        for (int j = 0; j < rows; j++)
                        {
                            var xCoord = x[(int)i, j];
                            var yCoord = y[(int)i, j];
                            double? elevation;
                            try  // this try block is necessary because of an apparent bug
                            {    // .Net Core.
                                elevation = (double) getElevation(
                                    xCoord, yCoord);
                            }
                            catch (Exception e)
                            {
                                elevation = null;
                            }
                            if(elevation != null)
                            {
                                var error = z[(int)i, j] - (double) elevation;
                                squaredErrorsBag.Add(error * error);
                                errorsBag.Add(Math.Abs(error));
                            }
                        }
                    }
                );
            }

            double absoluteMean = errorsBag.Mean();

            List<double?> squaredErrors = new List<double?>(squaredErrorsBag);
            squaredErrors.Sort();
            squaredErrorsBag.Clear();
            squaredErrorsBag = null;
            var stats = new DescriptiveStatistics(squaredErrors);
            //var rootMaxSquared = Math.Sqrt(stats.Maximum);
            var rootMeanSquared = Math.Sqrt(stats.Mean);
            //var rootVarianceSquared = Math.Sqrt(stats.Variance);
            int idxP25 = (int)(0.25 * (double)errorsBag.Count);
            int idxP75 = (int)(0.75 * (double)errorsBag.Count);
            int idxP95 = (int)(0.95 * (double)errorsBag.Count);
            var errorsList = errorsBag.ToList();
            errorsList.Sort();
            var p25 = (double)errorsList[idxP25];
            var p75 = (double)errorsList[idxP75];
            var p95 = (double)errorsList[idxP95];
            var maxVal = (double)errorsList.Last();
            sw.Stop();
            Console.Write(sw.Elapsed);
            //var msPerQuery = (double)sw.ElapsedMilliseconds / rdmIdc.SampleCount;
            var msPerQuery = (double)sw.ElapsedMilliseconds / allUnusedPoints.Count;
            Console.WriteLine($"   {msPerQuery} milliseconds per query.");

            String outRow = $"{decimationRemainingPercent * 100.0:F2},"
                + $"{this.allUsedPoints.Count},{p25:F5},{absoluteMean:F5}," +
                $"{p75:F5},{p95:F5},{maxVal:F5},{rootMeanSquared:F5}";
            using (StreamWriter csvFile = new StreamWriter(v, true))
            {
                csvFile.WriteLine(outRow);
            }

            return;
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
        {   // To do: fix where lines are not computed in random decimation 
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
            double maxSlopeDegrees = 79.5, double maxLineCrossSlopeChange=175.0)
        {
            var markNotValided = this.getExteriorTriangles()
                .Where(tr => tr.shouldRemove()).ToList();

            var activeLevel = new List<TINtriangle>();
            var nextLevel = new List<TINtriangle>();
            var visitedList = new HashSet<TINtriangle>();

            foreach (var tri in markNotValided)
            {
                tri.IsValid = false;
                activeLevel.Add(tri);
            }

            while(activeLevel.Count > 0)
            {
                foreach(var aTri in activeLevel)
                {
                    visitedList.Add(aTri);
                    if (aTri.HasBeenVisited) continue;
                    aTri.HasBeenVisited = true;
                    if(aTri.shouldRemove())
                    {
                        aTri.IsValid = false;
                        var nextTriangles = aTri.myLines
                            .Where(Line => Line.GetOtherTriangle(aTri) != null)
                            .Select(Line => Line.GetOtherTriangle(aTri))
                            .Where(t => !t.HasBeenVisited);
                        foreach (var nextTri in nextTriangles)
                            nextLevel.Add(nextTri);
                    }
                }

                activeLevel.Clear();
                activeLevel.AddRange(nextLevel);
                nextLevel.Clear();
            }

            foreach (var triangle in visitedList)
                triangle.HasBeenVisited = false;

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

        public void writeLinesToDxf(string outFile, Func<TINtriangleLine, bool> filter=null)
        {
            var dxf = new DxfDocument();
            dxf.DrawingVariables.AcadVer = netDxf.Header.DxfVersion.AutoCad2013;
            int counter = 0;

            foreach(var aLine in this.allLines.Values.Where(L => filter(L)))
            {
                var pt1 = aLine.firstPoint;
                var startVec = new netDxf.Vector3(pt1.x, pt1.y, pt1.z);
                var pt2 = aLine.secondPoint;
                var endVec = new netDxf.Vector3(pt2.x, pt2.y, pt2.z);
                Line dxfLine = new Line(startVec, endVec);
                dxf.AddEntity(dxfLine);
                counter++;
            }


            if (counter > 0)
                dxf.Save(outFile);
            else
                throw new Exception("Empty dxf file not created.");

        }

        public void WritePointsToDxf(string outFile, bool shouldZip=false)
        {
            var dxf = new DxfDocument();
            dxf.DrawingVariables.AcadVer = netDxf.Header.DxfVersion.AutoCad2013;

            messagePump.BroadcastMessage($"Iterating over {allUsedPoints.Count:N0} points.");
            foreach (var item in this.allUsedPoints)
            {
                item.AddToDxf(dxf);
            }

            messagePump.BroadcastMessage($"Saving points to dxf.");
            dxf.Save(outFile);

            if(shouldZip)
            {
                messagePump.BroadcastMessage(
                    $"Compressing dxf file to zip file {outFile + ".zip"}");
                zipAndDelete(outFile);
                messagePump.BroadcastMessage("Save to dxf.zip complete.");
            }
            else
            {
                messagePump.BroadcastMessage("Save to dxf complete.");
            }

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

        public void WriteToWaveFront(string outfile, bool translateTo0 = true, bool shouldZip=false)
        {
            var affineXform = setAffineTransformToZeroCenter(translateTo0);
            var aString = convertArrayToString(affineXform.ToArray(), 4, 4);
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(outfile))
            {
                file.WriteLine("# Created by CogoDN: Tin Mesh");
                file.WriteLine(aString.ToString());
                foreach (var aPt in this.allUsedPoints)
                    file.WriteLine("v " + aPt.ToString(affineXform));

                foreach (var aTriangle in this.ValidTriangles)
                    file.WriteLine(aTriangle.IndicesToWavefrontString());
            }

            if (shouldZip)
            {
                messagePump.BroadcastMessage(
                    $"Compressing obj file to zip file {outfile + ".zip"}");
                zipAndDelete(outfile);
                messagePump.BroadcastMessage("Save to obj.zip complete.");
            }
            else
            {
                messagePump.BroadcastMessage("Save to obj complete.");
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
            //generateTriangleLineIndex();
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

        private void zipAndDelete(string outfileName)
        {
            var zipFile = outfileName + ".zip";
            using (FileStream zipToOpen = new FileStream(zipFile, FileMode.Create))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                {
                    ZipArchiveEntry tinFile = archive.CreateEntryFromFile(outfileName,
                        System.IO.Path.GetFileName(outfileName));
                }
            }

            System.IO.File.Delete(outfileName);
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
                    aDTM.correctUpsidedownTriangles();
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

        internal void setPointsTriangleIndices()
        {
            var testPt = this.allUsedPoints.First();
            if (null != testPt.myTriangles)
                return;

            Parallel.ForEach(allUsedPoints,
                pt =>
                {
                    pt.myTriangles = new ConcurrentBag<TINtriangle>();
                });

            //Parallel.ForEach(allTriangles,
              //  aTriangle =>
            foreach(var aTriangle in allTriangles)
            {
                aTriangle.point1.myTriangles.Add(aTriangle);
                aTriangle.point2.myTriangles.Add(aTriangle);
                aTriangle.point3.myTriangles.Add(aTriangle);
            } 
            //);

        }

        internal void clearPointsTriangleIndices()
        {
            Parallel.ForEach(allUsedPoints,
                pt =>
                {
                    pt.myTriangles = null;
                });
            GC.Collect();
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

    internal class tinPointParameters
    {
        /// <summary>
        /// Point Sparsity is the reciprocal of point density. The higher the value,
        /// the lower the number of points per area unit. In other words, a low sparsity
        /// indicates a high point density.
        /// </summary>
        public double pointSparsity;
        
        /// <summary>
        /// The weighted sum of the cross slope of each line associated with the
        /// given point. This value is not currently computed.
        /// </summary>
        //public double aggregateCrossSlope;

        /// <summary>
        /// The gaussian curvature of the point, computed by summing all connected
        /// triangle angles per the Gauss-Bonnet Equation.  Ref:
        /// Crane, Keenan, 2013, Digital Geometry Processing with Discrete Exterior
        ///    Calculus, In: ACM SIGGRAPH 2013 courses, SIGGRAPH ’13. ACM, New York, 
        ///    NY, USA (2013), p 56 (Se Exercise 7)
        /// </summary>
        public double gaussianCurvature;


        public double retainProbability;
        public HashSet<TINtriangleLine> myLines = new HashSet<TINtriangleLine>();
        public HashSet<TINtriangle> myTriangles = new HashSet<TINtriangle>();

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
            HullPointCount = dtm.allUsedPoints.Where(pt => pt.isOnHull)
                .Count();
            HullPointPercent = HullPointCount / (double) PointCount;

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

            var sortedInZ = dtm.allUsedPoints.OrderBy(pt => pt.z);
            MinZ = sortedInZ.First().z;
            MaxZ = sortedInZ.Last().z;
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
        public int HullPointCount { get; set; }
        public double HullPointPercent { get; set; }
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
            sb.AppendLine($"Points: {PointCount:n0}");
            sb.AppendLine($"Hull Points: {HullPointCount:n0}   " +
                $"{HullPointPercent * 100.0:n4}%");
            sb.AppendLine($"Width (in x): {WidthX:F2}    Depth (in y): {WidthY:F2}   EL: {HeightZ:F2}");

            sb.AppendLine();
            sb.AppendLine($"Lines: {LineCount:n0}");
            sb.AppendLine($"Planar Lengths: Min: {ShortestLinePlane:F5}  Median: {MedianLinePlane:F2}   Max: {LongestLinePlane:F2}");

            sb.AppendLine();
            sb.AppendLine($"Triangles: {TriangleCount:n0}");
            sb.AppendLine($"Planar Areas: Min: ?   Median: ?   Max: ?");

            sb.AppendLine();
            sb.AppendLine($"Bounding Box Area: {PlanarArea:n2}");
            sb.AppendLine($"Points per Square Unit: {PointsPerSquareUnit:n4}");
            sb.AppendLine($"Square Units per Point: {1.0 / PointsPerSquareUnit:n4}");

            return sb.ToString();
        }

        public static IReadOnlyList<binCell> 
            GetPointSparsityHistogram(TINsurface aSurface,
            int binCount, double capRatio=double.PositiveInfinity)
        {
            ConcurrentBag<double> allSparsities = new ConcurrentBag<double>();

            aSurface.setPointsTriangleIndices();

            //foreach (var pt in aSurface.allUsedPoints)
            Parallel.ForEach(aSurface.allUsedPoints,
                pt =>
            {
                double aSparsity = pt.Sparsity(aSurface);
                    allSparsities.Add(aSparsity);
                } );

            ConcurrentDictionary<int, int> counts = new ConcurrentDictionary<int, int>();
            for (int i = 0; i < binCount; i++)
                counts[i] = 0;

            var minSparsity = allSparsities.AsParallel().Min();
            var maxSparsity = allSparsities.AsParallel().Max();
            var aveSparsity = allSparsities.AsParallel().Average();
            var cap = aveSparsity * capRatio;
            var range = maxSparsity - minSparsity;
            if (maxSparsity > cap)
            {
                binCount -= 1;
                range = cap - minSparsity;
            }

            foreach (var aSparsity in allSparsities)
            //Parallel.ForEach(sparsities,
            //    aSparsity =>
                {
                int index = 0;
                index = (int)(binCount * (aSparsity - minSparsity) / range);
                try
                {
                    counts[index]++;
                }
                catch (Exception e)
                {
                    counts[binCount-1]++;
                }
            }
            //);

            double binSpan = range / binCount;
            double binMin = minSparsity;
            List<binCell> returnList = new List<binCell>();
            for (int i = 0; i < binCount; i++)
            {
                returnList.Add(new binCell(
                    binMin,
                    binSpan,
                    counts[i]
                    ));
                binMin += binSpan;
            }
            if(maxSparsity > cap)
            {
                var lastBin = returnList.Last();
                lastBin.binMaxX = maxSparsity;
            }

            // Get the mode of the histogram.
            int iMax = -1;
            int countMax = -1;
            for(int i=0; i<binCount; i++)
            {
                if(counts[i] > countMax)
                {
                    iMax = i;
                    countMax = counts[i];
                }
            }
            var modeBin = returnList[iMax];
            
            TINstatistics.messagePump.BroadcastMessage
            ($"Sparsity mode value is bin number {iMax}: {modeBin}.");

            // debugging diagnostics only
            var totalOfCounts = returnList.Aggregate(0, (acc, b) => acc + b.binCount);
            var finalBin = returnList.Last();

            return returnList;
        }

        protected static TextMessagePump messagePump = new TextMessagePump();
        public static TextMessagePump GetMessagePump(IObserver<String> observer)
        {
            messagePump.Register(observer);
            return messagePump;
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
