﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CadFoundation.Coordinates;
using CadFoundation;
using Surfaces.TIN;
using System.IO;
using System.Text;

namespace TinConsole
{
    class Program
    {
        static DirectoryManager pwd = DirectoryManager.FromPwd();
        static string commandFileName = "TinConsoleCommands.txt";
        static Queue<string> commandList = null;
        static string logFileName = "TinDN Processing Log.log";
        static StreamWriter logFile = null;
        static string hcSource =
            @"D:\Research\Datasets\Lidar\Tilley Creek\decimation research\Tilley Creek Small.las";
        static TINsurface surface = null;

        static StringWriter intercept = new StringWriter();
        static TextWriter stdOut = Console.Out;

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += 
                new UnhandledExceptionEventHandler(OnUnhandledException);
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnAppExit);
            if (args.Length == 0)
            {
                if(pwd.ConfirmExists(commandFileName))
                {
                    var fileToRead = pwd.GetPathAndAppendFilename(commandFileName);
                    commandList = new Queue<string>(File.ReadAllLines(fileToRead));
                }
                repl();
            }

            if(args.Length == 1 && args[0].ToLower() == "pwd")
            {
                System.Console.WriteLine("Pwd is " + pwd);
                Environment.Exit(0);
            }

            System.Console.WriteLine($"Source: ", args);
            //System.Console.WriteLine($"Source: {args[1]}");
            //System.Console.WriteLine($"Source: {args[2]}");
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

        static void OnAppExit(object sender, EventArgs e)
        {
            mirrorLogPrint("Application exiting.");
            logFile?.Dispose();
        }

        static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var stackTrace = e.ExceptionObject.ToString();
            mirrorLogPrint(stackTrace);
            Console.WriteLine();
            Console.WriteLine("Press a key to exit");
            Environment.Exit(-1);
        }

        private static List<string> parseLine(string str)
        {
            StringBuilder insideQuote = null;
            List<string> s = new List<string>();
            foreach (var item in str.Split(" "))
            {
                if (item[0] == '\"')
                {
                    insideQuote = new StringBuilder(item);
                    continue;
                }
                if (item[^1] == '\"')
                {
                    insideQuote.Append(" ").Append(item);
                    s.Add(insideQuote.ToString());
                    insideQuote = null;
                    continue;
                }

                if (insideQuote != null)
                {
                    insideQuote.Append(" ").Append(item);
                }
                else
                    s.Add(item);
            }
            return s;
        }

        private static void repl()
        {
            var commands = new Dictionary<string, Action<List<string>>>
            {
                ["exit"] = ls => System.Environment.Exit(0),
                ["quit"] = ls => System.Environment.Exit(0),
                ["log"] = ls => SetupLogging(ls),
                ["load"] = ls => Load(ls),
                ["summarize"] = ls => summarize(ls),
                ["reload"] = ls => reload(ls),
                ["decimate_multiple"] = ls => decimate_multiple(),
                ["performance_test"] = ls => performance_test(ls),
                ["output_lines"] = ls => output_lines(ls),
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
                catch (KeyNotFoundException knfe)
                {
                    //System.Console.WriteLine("command not found");
                    //continue;
                    commandLine = new List<string>() { "decimate_multiple" };
                    command = commands[commandLine[0]];
                }
                command(commandLine);
            }
        }

        private static void Load(List<string> commandItems)
        {
            var openFileStr = pwd.GetPathAndAppendFilename(commandItems[1]);
            surface = TINsurface.CreateFromLAS(openFileStr);
        }

        private static void SetupLogging(List<string> commandItems)
        {
            string localLogFName = logFileName;
            Func<string, StreamWriter> openMethod = s => File.AppendText(s);
            string somevar = null; 
            var txt = commandItems.RetrieveByIndex(1);
            switch (txt)
            {
                case "reset":
                    {
                        openMethod = s => new StreamWriter(s);
                        localLogFName = commandItems.RetrieveByIndex(2) ?? logFileName;
                        break;
                    }
                case string s when s.Contains("."):
                    {
                        localLogFName = s;
                        break;
                    }
                case "stop":
                    {
                        mirrorLogPrint("Stopping log file without exiting.");
                        logFile?.Flush();
                        logFile?.Dispose();
                        //Console.setOut(stdOut);
                        break;
                    }
                default: break;
            }

            localLogFName = pwd.GetPathAndAppendFilename(localLogFName);
            try
            {
                logFile = openMethod(localLogFName);
                mirrorLogPrint("Log file opened.");
                //Console.setOut(intercept);
            }
            catch (IOException ioe)
            {
                if (ioe.Message.Contains("it is being used"))
                    mirrorLogPrint("Log open attempt failed because it is already open.");
                else
                    throw;
            }
            logFile?.Flush();
        }

        static bool logMessageGiven = false;
        static bool priorWasNewline = true;
        static void mirrorLogPrint(string message, bool newline=true)
        {
            String msg;
            if (priorWasNewline)
                msg = DateTime.Now.ToString("dddd, d MMM yyyy 'at' HH:mm:ss.fff: ") + message;
            else
                msg = message;

            if(null == logFile && !logMessageGiven)
            {
                System.Console.WriteLine("Logging requested but could not be started.");
                logMessageGiven = true;
            }
            if (newline)
            {
                logFile?.WriteLine(msg);
                //Console.setOut(stdOut);
                System.Console.WriteLine(message);
                //Console.setOut(intercept);
            }
            else
            {
                logFile?.Write(msg);
                //Console.setOut(stdOut);
                System.Console.WriteLine(message);
                //Console.setOut(intercept);
            }

            priorWasNewline = newline;
        }

        private static void summarize(List<string> commandItems)
        {
            if (surface is null)
            {
                System.Console.WriteLine("No file has been loaded. Nothing to summarize.");
                return;
            }
            System.Console.WriteLine("Summarizing ...");
            System.Console.Write(surface.Statistics.ToString());
        }

        private static void output_lines(List<string> commandItems)
        {
            var outfile = @"D:\Research\Datasets\Lidar\Tilley Creek\decimation research\smartDecResults\linesOnly.dxf";
            surface.writeLinesToDxf(outfile, (Line => Line.DeltaCrossSlopeAsAngleRad > 3.05));
        }

        private static void reload(List<string> commandItems)
        {
            surface = null;
            GC.Collect();
            var skipPoints = 0;
            if (commandItems.Count > 1)
                skipPoints = Convert.ToInt32(commandItems[1]);
            mirrorLogPrint("Reloading primary tin model.");
            surface = TINsurface.CreateFromLAS(hcSource, skipPoints: skipPoints);
            mirrorLogPrint("Primary tin model reloaded.");
        }

        private static void performance_test(List<string> commandItems)
        {
            if (surface is null)
            {
                System.Console.WriteLine("No file has been loaded. Nothing to summarize.");
                return;
            }
            
            var skipPoints = 1;
            if (commandItems.Count > 1)
                skipPoints = Convert.ToInt32(commandItems[1]);

            if (surface != null && hcSource != surface.SourceData)
                surface = TINsurface.CreateFromLAS(hcSource, skipPoints: skipPoints);

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

        static string researchOutpath = @"D:\Research\Datasets\Lidar\Tilley Creek\decimation research\simpleResults\";
        static string summaryFile = "summary.csv";
        public static void decimate_multiple(int start = 1, int count = 21, int step = 1)
        {
            Stopwatch sw = Stopwatch.StartNew();
            var counts = Enumerable.Range(start, count).ToList();
            counts.AddRange(new List<int>() { 25, 50, 75, 100, 200, 300, 400, 500, 1000 });
            foreach (var pointsToSkip in counts)
            {
                if (!((pointsToSkip % step) == 0))
                    continue;
                decimate_single(pointsToSkip, "TestTin");
            }
            sw.Stop();
            System.Console.WriteLine("Done. " + sw.Elapsed.TotalSeconds + " seconds.");
        }

        private static void decimate_single(int skipPoints, string outputBaseName)
        {
            string outname = outputBaseName + $"{skipPoints:D2}";
            Console.Write($"Processing {outname}   ");
            surface = TINsurface.CreateFromLAS(hcSource, skipPoints: skipPoints);
            surface.IndexTriangles();
            Console.Write("Created.   ");
            surface.saveAsBinary(researchOutpath + outname + ".TinDN");
            Console.Write("Saved.   ");
            surface.ComputeErrorStatistics(researchOutpath + summaryFile);
            Console.WriteLine("Stats computed, written.");
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

        public static TINpoint NextPoint(
            this Random random,
            BoundingBox bb)
        {
            double x = random.NextDouble(bb.lowerLeftPt.x, bb.upperRightPt.x);
            double y = random.NextDouble(bb.lowerLeftPt.y, bb.upperRightPt.y);
            return new Surfaces.TIN.TINpoint(x, y);
        }
    }
}
