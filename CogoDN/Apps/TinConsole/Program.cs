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
        static DirectoryManager outDir = pwd;
        static string commandFileName = "TinConsoleCommands.txt";
        static Queue<string> commandList = null;
        static string logFileName = "TinDN Processing Log.log";
        static StreamWriter logFile = null;
        static List<int> classificationFilter = new List<int> { 2, 11 }; // standard bare ground (ground, road)
        static string hcSource =
            @"D:\Research\Datasets\Lidar\Tilley Creek\decimation research\Tilley Creek Small.las";
        static TINsurface mainSurface = null;
        static TINsurface derivedSurface = null;

        static StringWriter intercept = new StringWriter();
        static TextWriter stdOut = Console.Out;
        static MessageObserver msgObs = new MessageObserver();

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += 
                new UnhandledExceptionEventHandler(OnUnhandledException);
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnAppExit);

            TINsurface.GetMessagePump(msgObs);

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
                mirrorLogPrint("Pwd is " + pwd);
                Environment.Exit(0);
            }

            mirrorLogPrint($"Source: {args}");
            //mirrorLogPrint($"Source: {args[1]}");
            //mirrorLogPrint($"Source: {args[2]}");
            bool useHardCodes = false;


            if ((args.Length > 1 && args[1].ToLower() == "repl") || useHardCodes)
            {
                if (useHardCodes)
                {
                    mirrorLogPrint("Loading.");
                    mainSurface = TINsurface.CreateFromLAS(hcSource);
                    mirrorLogPrint("Loaded. Ready.");
                }

                repl();

                Environment.Exit(0);
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
            str = str.Split("#").FirstOrDefault().Trim();
            StringBuilder insideQuote = null;
            List<string> s = new List<string>();
            foreach (var item in str.Split(" "))
            {
                if (item == string.Empty)
                    continue;
                if (item[0] == '\"')
                {
                    insideQuote = new StringBuilder(item);
                    if (item[^1] == '\"')
                    {
                        s.Add(item.Trim('"'));
                    }
                    continue;
                }
                if (item[^1] == '\"')
                {
                    insideQuote.Append(" ").Append(item);
                    var insideQstring = insideQuote.ToString().Trim('"');
                    s.Add(insideQstring);
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

        /// <summary>
        /// Hand-rolled Read/Evaluate/Print/Loop functionality.
        /// </summary>
        private static void repl()
        {
            var commands = new Dictionary<string, Action<List<string>>>
            {
                ["exit"] = ls => System.Environment.Exit(0),
                ["quit"] = ls => System.Environment.Exit(0),
                ["log"] = ls => SetupLogging(ls),
                ["set_dir"] = ls => SetDirectories(ls[1]),
                ["set_outdir"] = ls => SetDirectories(outDr: ls[1]),
                ["load"] = ls => Load(ls),
                ["summarize"] = ls => summarize(ls),
                ["reload"] = ls => reload(ls),
                ["decimate_multiple"] = ls => decimate_multiple(),
                ["performance_test"] = ls => performance_test(ls),
                ["output_lines"] = ls => output_lines(ls),
                ["set_filter"] = ls => set_filter(ls),
                ["points_to_dxf"] = ls => points_to_dxf(ls),
                ["to_obj"] = ls => to_obj(ls),
                ["decimate_random"] = ls => decimate_random(ls),
                // to do: add to_obj, but remember, we will have two surfaces by then.
            };

            Action<List<String>> command = null;
            List<String> commandLine;
            while (true)
            {
                if(commandList.Count >= 1)
                {
                    commandLine = parseLine(commandList.Dequeue());
                    if (commandLine.Count == 0) // This is an empty line (happens on a comment line).
                        continue;
                }
                else
                {
                    System.Console.Write("> ");
                    commandLine = parseLine(Console.ReadLine());
                }
                try
                {
                    command = commands[commandLine[0]];
                }
                catch (KeyNotFoundException knfe)
                {
                    System.Console
                        .WriteLine($"{commandLine[0]}: command not found");

                    continue;
                }
                command(commandLine);
            }
        }

        private static string GetCorrectOutputFilename(string filenameIn)
        {
            if (filenameIn.Contains(":"))
                return filenameIn;
            return outDir.GetPathAndAppendFilename(filenameIn);
        }

        private static void decimate_random(List<string> commandItems)
        {
            if (commandItems.Count != 2)
                throw new ArgumentException(
                    "Error: decimate_random requires one parameter only.");

            var decimation = Convert.ToDouble(commandItems.Skip(1).FirstOrDefault());

            derivedSurface = TINsurface.CreateByRandomDecimation(mainSurface, decimation);
        }

        private static void points_to_dxf(List<string> commandItems)
        {
            bool shouldZip = commandItems.Skip(1).Where(arg => arg.Equals("-zipped")).Any();
            var outFilename = commandItems.Skip(1).Where(arg => arg.Contains(".dxf")).FirstOrDefault();
            if(null == outFilename)
            {
                mirrorLogPrint("Output file name not specified. Dxf output files must have a \".dxf\" extension.");
                return;
            }
            if(shouldZip)
                mirrorLogPrint("points_to_dxf: Output filename will have \".zip\" extension appended.");

            var writeFileName = GetCorrectOutputFilename(outFilename);
            mirrorLogPrint($"Creating points dxf file: {writeFileName}");
            if(null != derivedSurface)
            {

            }
            else if(null != mainSurface)
            {
                mainSurface.WritePointsToDxf(writeFileName, shouldZip);
            }
        }

        private static void to_obj(List<string> commandItems)
        {
            bool shouldZip = commandItems.Skip(1).Where(arg => arg.Equals("-zipped")).Any();
            bool shouldTransform = commandItems.Skip(1).Where(arg => arg.Equals("-transformed")).Any() ||
                commandItems.Skip(1).Where(arg => arg.Equals("-trans")).Any();
            var outFilename = commandItems.Skip(1).Where(arg => arg.Contains(".obj")).FirstOrDefault();
            if (null == outFilename)
            {
                mirrorLogPrint("Output file name not specified. Wavefront obj output files must have a \".obj\" extension.");
                return;
            }
            if (shouldZip)
                mirrorLogPrint("to_obj: Output filename will have \".zip\" extension appended.");

            var writeFileName = GetCorrectOutputFilename(outFilename);
            mirrorLogPrint($"Creating surface obj file: {writeFileName}");
            if (null != derivedSurface)
            {
                derivedSurface.WriteToWaveFront(writeFileName, shouldTransform, shouldZip);
            }
            else if (null != mainSurface)
            {
                mainSurface.WriteToWaveFront(writeFileName, shouldTransform, shouldZip);
            }
        }

        private static void set_filter(List<string> commandItems)
        {
            classificationFilter.Clear();
            foreach(string argument in commandItems.Skip(1))
            {
                classificationFilter.Add(Convert.ToInt32(argument));
            }
            mirrorLogPrint($"Classification filter set to: {String.Join(", ", classificationFilter)}.");
        }

        private static void SetDirectories(string inDir=null, string outDr=null)
        {
            if(null != inDir)
            {
                pwd = DirectoryManager.FromPathString(inDir);
                mirrorLogPrint($"Input directory set to {pwd}");
            }
            if(null != outDr)
            {
                outDir = DirectoryManager.FromPathString(outDr);
                mirrorLogPrint($"Output directory set to {outDir}");
            }
        }

        private static void Load(List<string> commandItems)
        {
            var openFileStr = pwd.GetPathAndAppendFilename(commandItems[1]);
            mainSurface = TINsurface.CreateFromLAS(openFileStr, classificationFilter: classificationFilter);
        }

        private static void SetupLogging(List<string> commandItems)
        {
            string localLogFName = logFileName;
            Func<string, StreamWriter> openMethod = s => File.AppendText(s);

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
                        break;
                    }
                default: break;
            }

            if(!localLogFName.Contains(":"))
                localLogFName = pwd.GetPathAndAppendFilename(localLogFName);
            try
            {
                logFile = openMethod(localLogFName);
                mirrorLogPrint("Log file opened.");
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
        public static void mirrorLogPrint(string message, bool newline=true)
        {
            String msg;
            if (priorWasNewline)
                msg = DateTime.Now.ToString("dddd, d MMM yyyy 'at' HH:mm:ss.fff: ") + message;
            else
                msg = message;

            if(null == logFile && !logMessageGiven)
            {
                Console.WriteLine("Logging requested but could not be started.");
                logMessageGiven = true;
            }
            if (newline)
            {
                logFile?.WriteLine(msg);
                Console.WriteLine(message);
            }
            else
            {
                logFile?.Write(msg);
                Console.Write(message);
            }

            priorWasNewline = newline;
        }

        private static void summarize(List<string> commandItems)
        {
            if (mainSurface is null)
            {
                mirrorLogPrint("No file has been loaded. Nothing to summarize.");
                return;
            }
            mirrorLogPrint("Summarizing ...");
            mirrorLogPrint(mainSurface.Statistics.ToString());
        }

        private static void output_lines(List<string> commandItems)
        {
            var outfile = @"D:\Research\Datasets\Lidar\Tilley Creek\decimation research\smartDecResults\linesOnly.dxf";
            mainSurface.writeLinesToDxf(outfile, (Line => Line.DeltaCrossSlopeAsAngleRad > 3.05));
        }

        private static void reload(List<string> commandItems)
        {
            mainSurface = null;
            GC.Collect();
            var skipPoints = 0;
            if (commandItems.Count > 1)
                skipPoints = Convert.ToInt32(commandItems[1]);
            mirrorLogPrint("Reloading primary tin model.");
            mainSurface = TINsurface.CreateFromLAS(hcSource, skipPoints: skipPoints);
            mirrorLogPrint("Primary tin model reloaded.");
        }

        private static void performance_test(List<string> commandItems)
        {
            if (mainSurface is null)
            {
                mirrorLogPrint("No file has been loaded. Nothing to summarize.");
                return;
            }
            
            var skipPoints = 1;
            if (commandItems.Count > 1)
                skipPoints = Convert.ToInt32(commandItems[1]);

            if (mainSurface != null && hcSource != mainSurface.SourceData)
                mainSurface = TINsurface.CreateFromLAS(hcSource, skipPoints: skipPoints);

            var bb = mainSurface.BoundingBox;
            var rnd = new Random(12345);
            var samples = 10_000_000;
            var testCount = samples;
            Console.Write("Initiating run:   ");
            Stopwatch sw = Stopwatch.StartNew();
            //foreach(var i in Enumerable.Range(0,samples))
            Parallel.For(0, testCount, num =>
            {
                var el = mainSurface.getElevation(rnd.NextPoint(bb));
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
            mirrorLogPrint("Done. " + sw.Elapsed.TotalSeconds + " seconds.");
        }

        private static void decimate_single(int skipPoints, string outputBaseName)
        {
            string outname = outputBaseName + $"{skipPoints:D2}";
            Console.Write($"Processing {outname}   ");
            mainSurface = TINsurface.CreateFromLAS(hcSource, skipPoints: skipPoints);
            mainSurface.IndexTriangles();
            Console.Write("Created.   ");
            mainSurface.saveAsBinary(researchOutpath + outname + ".TinDN");
            Console.Write("Saved.   ");
            mainSurface.ComputeErrorStatistics(researchOutpath + summaryFile);
            Console.WriteLine("Stats computed, written.");
        }

        /// <summary>
        /// Accepts messages generated by 
        /// </summary>
        class MessageObserver : IObserver<String>
        {
            public void OnCompleted()
            {
                return;
            }

            public void OnError(Exception error)
            {
                throw error;
            }

            public void OnNext(string value)
            {
                Program.mirrorLogPrint(value);
            }
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
