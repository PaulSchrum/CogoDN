using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CadFoundation.Coordinates;
using Surfaces.TIN;

namespace TinConsole
{
    class Program
    {
        static string hcSource = 
            @"D:\Research\Datasets\Lidar\Tilley Creek\decimation research\Tilley Creek Small.las";
        static TINsurface surface = null;

        static void Main(string[] args)
        {
            System.Console.WriteLine($"Source: {args[0]}");
            System.Console.WriteLine($"Source: {args[1]}");
            System.Console.WriteLine($"Source: {args[2]}");
            bool useHardCodes = false;
            

            if ((args.Length > 1 && args[1].ToLower() == "repl") || useHardCodes)
            {
                if (useHardCodes)
                {
                    System.Console.WriteLine("Loading.");
                    surface = TINsurface.CreateFromLAS(hcSource);
                    System.Console.WriteLine("Loaded. Ready.");
                }

                repl();

                Environment.Exit(0);
            }

            var tinModel = TINsurface.CreateFromLAS(args[0],
                skipPoints: 8,
                classificationFilter: new List<int> { 2, 6, 13 });  // Add 6 to get roofs.
            var pointCount = tinModel.allUsedPoints.Count;
            var triangleCount = tinModel.TriangleCount;
            Console.Write($"Successfully loaded tin Model: {pointCount} points and ");
            Console.WriteLine($"{triangleCount} triangles.");

            if (args.Length == 1) return;

            if (args[1].ToLower() == "get_elevation")
            { // inputfile get_elevation 2133966 775569
                var x = Double.Parse(args[2]);
                var y = Double.Parse(args[3]);
                var z = tinModel.getElevation(x, y);
                Console.WriteLine($"Elevation: {z}");
            }
            else if (args[1].ToLower() == "to_obj")
            { // inputfile to_obj outputfile
                var outfileName = args[2];
                tinModel.WriteToWaveFront(outfileName);
                Console.WriteLine($"Wavefront obj file written: {outfileName}");
            }
        }

        private static List<string> parseLine(string str)
        {
            List<string> s = new List<string>();
            foreach (var item in str.Split(" "))
                s.Add(item);
            return s;
        }

        private static void repl()
        {
            var commands = new Dictionary<string, Action<List<string>>>
            {
                ["exit"] = ls => System.Environment.Exit(0),
                ["quit"] = ls => System.Environment.Exit(0),
                ["load"] = ls => surface = TINsurface.CreateFromLAS(ls[1]),
                ["summarize"] = ls => summarize(ls),
                ["reload"] = ls => reload(ls),
                ["decimate_multiple"] = ls => decimate_multiple(),
            };

            Action<List<String>> command = null;
            while (true)
            {
                System.Console.Write("> ");
                var commandLine = parseLine(Console.ReadLine());
                try
                {
                    command = commands[commandLine[0]];
                }
                catch(KeyNotFoundException knfe)
                {
                    //System.Console.WriteLine("command not found");
                    //continue;
                    commandLine = new List<string>(){ "decimate_multiple" };
                    command = commands[commandLine[0]];
                }
                command(commandLine);
            }
        }

        private static void summarize(List<string> commandItems)
        {
            if(surface is null)
            {
                System.Console.WriteLine("No file has been loaded. Nothing to summarize.");
                return;
            }
            System.Console.WriteLine("Summarizing ...");
            System.Console.Write(surface.Statistics.ToString());
        }

        private static void reload(List<string> commandItems)
        {
            surface = null;
            GC.Collect();
            var skipPoints = 0;
            if (commandItems.Count > 1)
                skipPoints = Convert.ToInt32(commandItems[1]);
            surface = TINsurface.CreateFromLAS(hcSource, skipPoints: skipPoints);
        }

        private static void performance_test(List<string> commandItems)
        {
            if (surface is null)
            {
                System.Console.WriteLine("No file has been loaded. Nothing to summarize.");
                return;
            }
            //GC.Collect();
            var skipPoints = 1;
            if (commandItems.Count > 1)
                skipPoints = Convert.ToInt32(commandItems[1]);

            if(surface != null && hcSource != surface.SourceData)
                surface = TINsurface.CreateFromLAS(hcSource, skipPoints: skipPoints);

            surface.IndexTriangles();
            var bb = surface.BoundingBox;
            var rnd = new Random(12345);
            var samples = 10_000_000;
            var testCount = samples;
            Console.Write("Initiating run:   ");
            Stopwatch sw = Stopwatch.StartNew();
            //foreach(var i in Enumerable.Range(0,samples))
            Parallel.For(0, testCount, num =>
            {
                var el = surface.getElevation(rnd.NextPoint(bb));
            }
            );

            sw.Stop();
            
            double cps = (double)sw.ElapsedMilliseconds / testCount;
            double spc = testCount / sw.Elapsed.TotalSeconds;
            Console.WriteLine($"{testCount} points in {sw.Elapsed.TotalSeconds:F1} Seconds.   "
                + $"{cps:F1} milliseconds per call.     {spc:F2} calls per second");
        }

    }

    public static class RandomExtensions
    {
        public static double NextDouble(
            this Random random,
            double minValue,
            double maxValue)
        {
            return random.NextDouble() * (maxValue - minValue) + minValue;
        }

        static string researchOutpath = @"D:\Research\Datasets\Lidar\Tilley Creek\decimation research\simpleResults\";
        static string summaryFile = "summary.csv";
        public static void decimate_multiple(int start=1, int count=21, int step=1)
        {

            foreach (var pointsToSkip in Enumerable.Range(1, 1))
            //foreach (var pointsToSkip in Enumerable.Range(start, count))
            //foreach (var pointsToSkip in new List<int>() { 25, 50, 75, 100, 200, 300, 400, 500, 1000})
            {
                if (!((pointsToSkip % step) == 0))
                    continue;
                decimate_single(pointsToSkip, "TestTin");
            }
        }

        private static void decimate_single(int skipPoints, string outputBaseName)
        {
            string outname = outputBaseName + $"{skipPoints:D2}";
            Console.Write($"Processing {outname}   ");
            surface = TINsurface.CreateFromLAS(hcSource, skipPoints: skipPoints);
            Console.Write("Created.   ");
            surface.saveAsBinary(researchOutpath + outname + ".TinDN");
            Console.Write("Saved.   ");
            surface.ComputeErrorStatistics(researchOutpath + summaryFile);
            Console.WriteLine("Stats computed, written.");
        }
    }
}
