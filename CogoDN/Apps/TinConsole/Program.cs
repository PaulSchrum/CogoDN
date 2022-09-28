using System;
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
using Cogo.Horizontal;
using System.Collections.Concurrent;
using NonLinearBestFit.CsvManager;

namespace TinConsole
{
    class Program
    {

        static DirectoryManager pwd = DirectoryManager.FromPwd();
        static DirectoryManager outDir = pwd;
        static string commandFileName = "TinConsoleCommands.txt";
        static string replPrompt = "TinConsole REPL > ";
        static Queue<string> commandList = new Queue<string>();
        static string logFileName = "TinDN Processing Log.log";
        static StreamWriter logFile = null;
        static List<int> classificationFilter = new List<int> { 2, 11 }; // standard bare ground (ground, road)
        static string hcSource =
            @"D:\Research\Datasets\Lidar\Tilley Creek\decimation research\Tilley Creek Small.las";
        static TINsurface mainSurface = null;
        static TINsurface derivedSurface = null;
        static BoundingBox filterBB = null;

        static HorizontalAlignment activeAlignment = null;
        static Cogo.Profile activeGroundProfile = null;
        static ConcurrentBag<HorizontalAlignment> allHorAlignments =
            new ConcurrentBag<HorizontalAlignment>();
        static BoundingBox allHAsBB = null;

        static string StatisticsCsvFile = null;
        static MessageObserver msgObs = new MessageObserver();
        static Stopwatch overallSW = new Stopwatch();
        static Dictionary<string, Action<List<string>>> commands =
            new Dictionary<string, Action<List<string>>>
            {
                ["exit"] = ls => exit(ls),
                ["quit"] = ls => exit(ls),
                ["log"] = ls => SetupLogging(ls),
                ["set_dir"] = ls => SetDirectories(ls[1]),
                ["set_outdir"] = ls => SetDirectories(outDr: ls[1]),
                ["set_bounding_box"] = ls => set_bounding_box(ls),
                ["point_count"] = ls => point_count(ls),
                ["load"] = ls => Load(ls),
                ["load_las"] = ls => Load(ls),
                ["load_raster"] = ls => Load_raster(ls),
                ["load_rasters"] = ls => Load_rasters(ls),
                ["to_las"] = ls => ToLas(ls),
                ["to_xyz"] = ls => ToXYZ(ls),
                ["summarize"] = ls => summarize(ls),
                ["reload"] = ls => reload(ls),
                ["repl"] = ls => repl_cmd(ls),
                ["print"] = ls => print(ls),
                ["decimate_multiple"] = ls => decimate_multiple(), // must remove  // undocumented
                ["performance_test"] = ls => performance_test(ls), // must remove  // undocumented
                ["output_lines"] = ls => output_lines(ls),
                ["set_filter"] = ls => set_filter(ls),
                ["transform_to_zero"] = ls => transform_to_zero(ls),
                ["points_to_dxf"] = ls => points_to_dxf(ls),
                ["to_obj"] = ls => to_obj(ls),
                ["save_stats"] = ls => save_stats(ls), // undocumented
                ["decimate_random"] = ls => decimate_random(ls), // undocumented

                ["decimate"] = ls => decimate(ls),

                ["histogram"] = ls => histogram(ls), // undocumented
                ["set_sample_grid"] = ls => set_sample_grid(ls),
                ["save_grid_to_raster"] = ls => save_grid_to_raster(ls),

                ["polyline_to_geojson"] = ls => polyline_to_geojson(ls),
                ["load_alignment"] = ls => load_alignment(ls),
                ["load_alignments"] = ls => load_alignments(ls),
                ["profile_to_csv"] = ls => profile_to_csv(ls),
                ["profiles_to_csvs"] = ls => profiles_to_csvs(ls),
                ["good_fit_hyperbolas"] = ls => good_fit_hyperbolas(ls),
                ["plot_csv"] = ls => plot_csv(ls),  // Currently inoperable.
                ["alignment_to_3d_dxf"] = ls => alignment_to_3d_dxf(ls), // undocumented
            };

        static void Main(string[] args)
        {
            overallSW.Start();
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
                    mirrorLogPrint($"Loaded commands from {fileToRead}.");
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

            if (args.Length > 0)
            {
                commandFileName = args[0];
                if (pwd.ConfirmExists(commandFileName))
                {
                    var fileToRead = pwd.PrependPWDifNotAlreadyFullPath(commandFileName);
                    commandList = new Queue<string>(File.ReadAllLines(fileToRead));
                    mirrorLogPrint($"Loaded commands from {fileToRead}.");
                    repl();
                }

                Environment.Exit(0);
            }
        }

        static void OnAppExit(object sender, EventArgs e)
        {
            overallSW.Stop();
            mirrorLogPrint($"Application exiting. Runtime = {overallSW.Elapsed}");
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
            Action<List<String>> command = null;
            List<String> commandLine;
            while (true)
            {
                if(commandList.Count >= 1)
                {
                    var rawCommandLine = commandList.Dequeue();
                    commandLine = parseLine(rawCommandLine);
                    if (commandLine.Count == 0) // This is an empty line (happens on a comment line).
                    {
                        mirrorLogPrint(rawCommandLine);
                        continue;
                    }
                }
                else
                {
                    System.Console.Write(replPrompt);
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
                catch (ArgumentOutOfRangeException aoore)
                {
                    System.Environment.Exit(0);
                }
                command(commandLine);
            }
        }

        private static void save_stats(List<string> commandItems)
        {
            if(commandItems.Count == 2)
                StatisticsCsvFile = outDir.GetPathAndAppendFilename(commandItems[1]);
            if(commandItems.Count == 3)
            {
                if(commandItems[1] == "reset")
                {
                    outDir.DeleteFile(commandItems[2]);
                    StatisticsCsvFile = outDir.GetPathAndAppendFilename(commandItems[2]);
                }
                else if(commandItems[2] == "reset")
                {
                    outDir.DeleteFile(commandItems[1]);
                    StatisticsCsvFile = outDir.GetPathAndAppendFilename(commandItems[1]);
                }
            }
        }
        private static string GetCorrectOutputFilename(string filenameIn)
        {
            if (filenameIn.Contains(":"))
                return filenameIn;
            return outDir.GetPathAndAppendFilename(filenameIn);
        }

        /// <summary>
        /// Creates a new tin model derived from the source tin using Smart Decimation.
        /// > decimate !valuePercent! -x!runTimes! -s!methodSplit!
        /// valuePercent: Percent of points to be retained.
        /// runTimes: Number of times to run the decimation. Used for experimental runs.
        /// dihedralCurvatureSplit: Percent of points retained by dihedral angle method
        /// </summary>
        /// <param name="commandItems">List of all string items in the command line.</param>
        private static void decimate(List<string> commandItems)
        {
            var dihedralCurvatureSplit = 0.50;
            var runTimes = 1;
            for(int i=2; i<commandItems.Count;i++)
            {
                var param = commandItems[i];
                if (param.StartsWith("-x"))
                    runTimes = Convert.ToInt32(param.Substring(2));
                if (param.StartsWith("-s"))
                    dihedralCurvatureSplit = Convert.ToDouble(param.Substring(2));
            }

            var decimation = Convert.ToDouble(commandItems.Skip(1).FirstOrDefault());

            for (int counter = 0; counter < runTimes; counter++)
            {
                mirrorLogPrint($"Decimation run {counter + 1} of {runTimes}.");
                derivedSurface = TINsurface.CreateByDecimation(mainSurface, decimation,
                    dihedralCurvatureSplit);

                if (null != StatisticsCsvFile)
                    derivedSurface.ComputeErrorStatistics(StatisticsCsvFile, mainSurface);
            }
        }

        /// <summary>
        /// Random decimates a tin one or more times.
        /// Arg 1 (req) must be the decimation value (percent points remaining).
        /// Arg 2 (optional) -xnn is the number of times to decimate (for research purposes).
        /// </summary>
        /// <param name="commandItems"></param>
        private static void decimate_random(List<string> commandItems)
        {
            mirrorLogPrint(
                "Random Decimation is deprecated. Decimate is the recommended command.");
            var runTimes = 1;
            if(commandItems.Count == 3)
            {
                var param2 = commandItems.Skip(2).FirstOrDefault();
                if (param2.StartsWith("-x"))
                    runTimes = Convert.ToInt32(param2.Substring(2));
            }

            var decimation = Convert.ToDouble(commandItems.Skip(1).FirstOrDefault());

            for(int counter=0; counter<runTimes; counter++)
            {
                mirrorLogPrint($"Random Decimation run {counter+1} of {runTimes}.");
                derivedSurface = TINsurface.CreateByRandomDecimation(mainSurface,
                    decimation);

                if (null != StatisticsCsvFile)
                    derivedSurface.ComputeErrorStatistics(StatisticsCsvFile);
            }
        }

        private static void set_sample_grid(List<string> commandItems)
        {
            double desiredSamplePointDensity = Convert.ToDouble(commandItems[1]);
            if (null == mainSurface)
                return;

            mirrorLogPrint("Sample Grid creation in progress.");
            if(null == mainSurface.samplingGrid)
                mainSurface.SetSampleGrid(desiredSamplePointDensity);
            if (null != derivedSurface && null != derivedSurface.samplingGrid)
                derivedSurface.SetSampleGrid(desiredSamplePointDensity);
            mirrorLogPrint("Sample Grid Created.");
        }

        private static void save_grid_to_raster(List<string> commandItems)
        {
            var outFileName = commandItems[1];
            var writeFileName = GetCorrectOutputFilename(outFileName);

            if(null == mainSurface)
            {
                mirrorLogPrint("Save Grid To Raster: No TIN Surface has been defined yet.");
                mirrorLogPrint("   No raster has been written.");
            }
            var grid = mainSurface.samplingGrid;
            if (null != derivedSurface && null != derivedSurface.samplingGrid)
                grid = derivedSurface.samplingGrid;

            mirrorLogPrint("Converting TIN model to Raster.");
            var tinAsRaster = grid.GetAsRaster();
            mirrorLogPrint($"Saving Raster as {outFileName}.");
            tinAsRaster.WriteToFile(writeFileName);
            if (File.Exists(writeFileName))
                mirrorLogPrint("Raster file saved.");
            else
                mirrorLogPrint("Unable to save raster file.");
        }

        private static void polyline_to_geojson(List<string> commandItems)
        {
            mirrorLogPrint("The command polyline_to_geojson has not been implemented yet.");
        }

        /// <summary>
        /// Loads a horizontal alignment into memory for use in working with the terrain
        /// surface.
        /// 
        /// 1 required Command line parameter: file name of the alignment to load.
        /// </summary>
        /// <param name="commandItems"></param>
        private static void load_alignment(List<string> commandItems)
        {
            string openFileStr = commandItems[1];
            if(!File.Exists(openFileStr))
                openFileStr = pwd.GetPathAndAppendFilename(openFileStr);
            activeAlignment = HorizontalAlignment.createFromCsvFile(openFileStr);
            if(null == allHorAlignments)
            {
                allHorAlignments = new ConcurrentBag<HorizontalAlignment>();
            }

            if(!allHorAlignments.Contains(activeAlignment))
            {
                allHorAlignments.Add(activeAlignment);
            }

            if (activeAlignment != null)
                mirrorLogPrint("Alignment loaded.");
            else
                mirrorLogPrint("Alignment not loaded.");
        }


        /// <summary>
        /// Loads all alignments in a given ___ into memory for later use.
        /// Implemented: ___ = geojson file
        /// Future: ___ = directory
        /// </summary>
        /// <param name="commandItems"></param>
        private static void load_alignments(List<string> commandItems)
        {
            string openFileStr = commandItems[1];
            if (!File.Exists(openFileStr))
                openFileStr = pwd.GetPathAndAppendFilename(openFileStr);
            allHorAlignments = HorizontalAlignment.createMultipleFromGeojsonFile(openFileStr);

            if (allHorAlignments == null)
            {
                mirrorLogPrint("Alignments not loaded.");
                return;
            }
            else if(allHorAlignments.Count == 0)
            {
                mirrorLogPrint("Alignments not loaded.");
                return;
            }
            else
                mirrorLogPrint($"{allHorAlignments.Count} Alignments loaded.");

            if (commandItems.Count > 2)
            {
                if(commandItems.Skip(2).Contains("-list"))
                {
                    foreach(var alignment in allHorAlignments.OrderBy(a => a.Name))
                    {
                        mirrorLogPrint($"{alignment.Name}");
                    }
                }
            }

        }

        /// <summary>
        /// For all profiles in memory (really all alignments in allHorAlignments), create a new
        /// csv file of the profile. Example usage:
        /// 
        /// > set_dir "E:\Research\My Papers\Hyperbola Analysis of Terrain\GIS\Studies\Plot Balsams North Slope"
        /// > set_outdir "E:\Research\My Papers\Hyperbola Analysis of Terrain\GIS\Studies\Plot Balsams North Slope\Outputs\XS"
        /// >
        /// > load_alignments "Plot Balsams XS HAs.json" -list
        /// > load_rasters "E:\Research\My Papers\Hyperbola Analysis of Terrain\GIS\Studies\Plot Balsams North Slope" -bb
        /// > profiles_to_csvs -truestation
        /// > 
        /// > exit
        ///
        /// Outputs go to outdir. No output arguments are accepted.
        /// 
        /// Arguments:
        /// : -inc=value : only sample terrain at inc distance increments along alignment. If not set, compute elevation at
        ///                at every alignment-triangle line intersection.
        /// : -truestation : Set x-values to true stations. Recommended for cross section profiles.
        /// : -nameInInfix=value : insert string "value" in the generated csv file name.
        /// </summary>
        /// <param name="commandItems"></param>
        private static void profiles_to_csvs(List<string> commandItems)
        {
            var aSurface = mainSurface;
            if (null == mainSurface)
            {
                mirrorLogPrint("Unable to create profile as no surface has been loaded.");
                return;
            }
            if (derivedSurface != null)
                aSurface = derivedSurface;

            double incrementDistance = 0d;
            if (commandItems.Count > 2)
            {
                var parmString = commandItems.Where(s => s.ToLower().Contains("-inc")).FirstOrDefault();
                if (null != parmString)
                {
                    incrementDistance = Convert.ToDouble(parmString.Split("=")[1]);
                }
            }
            // Technical debt: Unimplemented: If a derived (decimated) surface exists, use that for
            //      for the intersecting surface.
            // code here.

            bool useTrueStations = false;
            int trueStationIndex = commandItems.FirstIndexMatchLeft("-truestation");
            if (trueStationIndex > 0)
                useTrueStations = true;

            if (null == allHorAlignments)
            {
                mirrorLogPrint("Unable to create profile as no alignments have been loaded.");
                return;
            }

            foreach(var anAlignment in allHorAlignments)
            {
                mirrorLogPrint(
                    $"Creating profile from intersection of terrain and alignment {anAlignment.Name}.");

                try
                {
                    activeGroundProfile = aSurface.getIntersectingProfile(anAlignment, incrementDistance);
                }
                catch(ArgumentOutOfRangeException e)
                {
                    mirrorLogPrint(
                        $"   Unable to create profile from intersection of terrain and alignment {anAlignment.Name}.");
                    continue;
                }
                string nameInfix = string.Empty;
                int infixIndex = commandItems.FirstIndexMatchLeft("-nameInfix");
                if (infixIndex > 0)
                {
                    string infixValue = commandItems[infixIndex].Split("=")[1];
                    nameInfix = " " + infixValue;
                }

                var outputCSVfileName = anAlignment.Name + nameInfix + " pfl.csv";
                outputCSVfileName = outDir.ToString() + "\\" + outputCSVfileName;

                activeGroundProfile.WriteToCSV(outputCSVfileName, useTrueStations);
                mirrorLogPrint($"   Created {outputCSVfileName}");
            }
        }

        /// <summary>
        /// For all csv files in a directory, loads the csv, reads station and elevation, computes a good fit hyperbola
        /// for the left and the right, then writes the hyperbola elevation values to column "hyperbola".
        /// Example usage:
        /// 
        /// > set_dir "E:\Research\My Papers\Hyperbola Analysis of Terrain\GIS\Studies\Plot Balsams North Slope"
        /// > set_outdir "E:\Research\My Papers\Hyperbola Analysis of Terrain\GIS\Studies\Plot Balsams North Slope\Outputs\XS"
        /// >
        /// > load_alignments "Plot Balsams XS HAs.json" -list
        /// > load_rasters "E:\Research\My Papers\Hyperbola Analysis of Terrain\GIS\Studies\Plot Balsams North Slope" -bb
        /// > profiles_to_csvs -truestation
        /// > good_fit_hyperbolas "E:\Research\My Papers\Hyperbola Analysis of Terrain\GIS\Studies\Plot Balsams North Slope\Outputs\XS"
        /// > 
        /// > exit
        ///
        /// The input directory is a required input parameter.
        /// 
        /// Another file is added to the directory (or updated if it exists) recording the a-value and Sa-value for each
        ///    
        /// 
        /// Arguments:
        /// : path : the path to read csv files from (and write revised csv files to, name unchanged). Required.
        /// 
        /// </summary>
        /// <param name="commandItems"></param>
        private static void good_fit_hyperbolas(List<string> commandItems)
        {
            DirectoryManager workDir = null;
            // Get the path to find csv files. Refuse to process anything if not provided.
            if (commandItems.Count == 2)
            {
                workDir = DirectoryManager.FromPathString(commandItems[1]);
                var well = workDir.Exists();
                if(!workDir.Exists())
                {
                    mirrorLogPrint($"good_fit_hyperbola command not processed as the path does not exist:");
                    mirrorLogPrint(workDir.ToString());
                    mirrorLogPrint("");
                    return;
                }
            }
            else
            {
                mirrorLogPrint("Format error for good_fit_hyperbola. Unable to process this command.");
                return;
            }

            var allFileInDir = workDir.ListFiles(prependPath: true);
            foreach(var aFile in allFileInDir)
            {
                var df = GoodFitDataFrame.Create(aFile);
            }
        }

        /// <summary>
        /// Arguments:
        ///     .csv -- csv file to load profile info from
        ///     .pdf -- pdf file to output profile to
        /// </summary>
        /// <param name="commandItems"></param>
        private static void plot_csv(List<string> commandItems)
        {
            // This effort, Cogo.Plotting, is suspended indefinitely pending 
            // any future need to restart it, which may never happen.
            throw new NotImplementedException();
            var csvInputFile = "";
            var parmString = commandItems.Where(s => s.ToLower().Contains(".csv")).FirstOrDefault();
            if (null != parmString)
            {
                csvInputFile =parmString;
            }
            csvInputFile = GetCorrectOutputFilename(csvInputFile);

            var pdfFileName = "";
            parmString = commandItems.Where(s => s.ToLower().Contains(".pdf")).FirstOrDefault();
            if (null != parmString)
            {
                pdfFileName = parmString;
            }
            pdfFileName = GetCorrectOutputFilename(pdfFileName);

            // Invoke plotter.py

            // From https://www.youtube.com/watch?v=g1VWGdHRkHs&t=172s
            var psi = new ProcessStartInfo();
            psi.FileName = @"C:\Users\pauls\AppData\Local\Programs\Python\Python38\python.exe";
            var script = @"E:\Research\My Papers\Hyperbola Analysis of Terrain\GIS\Studies\daysBetweenDates.py";
            var v = DirectoryManager.FromPathString(script);
            var x = v.Exists();
            var startDate = "2019-1-1";
            var endDate = "2019-1-22"; 
            psi.Arguments = $"\"{script}\" \"{startDate}\" \"{endDate}\"";
            mirrorLogPrint(psi.Arguments);

            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;

            var errors = "";
            var results = "";
            using (var process = Process.Start(psi))
            {
                errors = process.StandardError.ReadToEnd();
                results = process.StandardOutput.ReadToEnd();

            }

            mirrorLogPrint("");
            mirrorLogPrint("Errors: " + errors);
            mirrorLogPrint("Results: " + results);
            mirrorLogPrint("");
            int i = 99;

            // ////////////////////////////////////////////////////////////////////////////////////////////////

            //var profileToPlot = Cogo.Profile.LoadFromCsv(csvInputFile);
            //var pflAsPoints = profileToPlot.GetVClistAsPoints();
            //DataSeries profileAsSeries = new DataSeries(pflAsPoints, pUnit.USfoot);
            //profileAsSeries.SetPenProperties(new ProfilePenLibrary(), "Existing Terrain");
            //PlotScale plotScale = new PlotScale(new DecimalUnits(50, pUnit.Foot),
            //    new DecimalUnits(1, pUnit.Inch));

            //var simpleList = new List<Point>
            //{
            //    new Point(-50.0, -10.0),
            //    new Point(0.0, 0.0),
            //    new Point(5, 1.1),
            //    new Point(0.9, 0.65),
            //    new Point(2.5, 1)
            //};
            //DataSeries testSeries = new DataSeries(simpleList, pUnit.USfoot);

            ////PDFplotting.CreateSheetFromProfiles(new[] { profileAsSeries }.ToList(), 
            ////    pdfFileName, plotScale);
            //var sheetType = Chart7p5ByVariable.Create();
            //sheetType.PlotSheetToPdfFile(new[] { testSeries }.ToList(), pdfFileName, 0.0);
            ////PDFplotting.PlotSheetFromProfiles(new[] { testSeries }.ToList(), sheetType,pdfFileName, plotScale);
            //mirrorLogPrint($"Create file {pdfFileName}.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="commandItems"></param>
        private static void profile_to_csv(List<string> commandItems)
        {
            if (null == mainSurface)
            {
                mirrorLogPrint("Unable to create profile as no surface has been loaded.");
                return;
            }

            if (null == activeAlignment)
            {
                mirrorLogPrint("Unable to create profile as no alignment has been loaded.");
                return;
            }

            mirrorLogPrint("Creating profile from intersection of terrain and alignment.");

            double incrementDistance = 0d;
            if(commandItems.Count > 2)
            {
                var parmString = commandItems.Where(s => s.ToLower().Contains("-inc")).FirstOrDefault();
                if(null != parmString)
                {
                    incrementDistance = Convert.ToDouble(parmString.Split("=")[1]);
                }
            }

            activeGroundProfile = mainSurface.getIntersectingProfile(activeAlignment, incrementDistance);
            var outputCSVfileName = commandItems[1];
            bool useTrueStations = false;
            if(commandItems.Count > 2)
            {
                if (commandItems[2].ToLower().Contains("-truestation"))
                    useTrueStations = true;
            }

            activeGroundProfile.WriteToCSV(outputCSVfileName, useTrueStations);
            mirrorLogPrint($"Created {outputCSVfileName}");

        }

        /// <summary>
        /// Create a histogram of the last created surface.
        /// Pattern: histogram [type] [binCount] [outputFileName] [ratioCap]
        /// type "Sparsity" - 2d sparisty of each point
        /// binCount is an integer.
        /// outputFileName is the name of the csv file created without path.
        /// ratioCap, optional, is the ratio over the dataset mean at which to cap bins 1 : n-1
        /// </summary>
        /// <param name="commandItems"></param>
        private static void histogram(List<string> commandItems)
        {
            string parameterType = commandItems.Skip(1).FirstOrDefault();
            int binCount = Convert.ToInt32(commandItems.Skip(2).FirstOrDefault());
            string fileName = commandItems.Skip(3).FirstOrDefault();
            string capString = commandItems.Skip(4).FirstOrDefault();
            double capRatio = double.PositiveInfinity;
            if (null != capString)
                capRatio = Convert.ToDouble(capString);
            TINsurface surfaceToUse = (null == derivedSurface) ? mainSurface
                : derivedSurface;

            TINstatistics.GetMessagePump(msgObs);
            IReadOnlyList<binCell> counts;

            switch(parameterType.ToLower())
            {
                case "sparsity":
                    {
                        mirrorLogPrint("Creating point sparsities histogram in " +
                            $"{binCount} bins");
                        counts = TINstatistics.GetPointSparsityHistogram(surfaceToUse,
                            binCount, capRatio);
                        break;
                    }
                default:
                    {
                        mirrorLogPrint($"The histogram type \"{parameterType}\" is not " +
                            "available.\n  Continuing without creating the histogram.");
                        return;
                    }
            }

            string pathFileName = outDir.GetPathAndAppendFilename(fileName);
            File.WriteAllLines(pathFileName,
                counts.Select(count => count.ToString()));
            mirrorLogPrint($"Operation complete. File created: {pathFileName}.");
        }

        /// <summary>
        /// Sets the affine transform for the export operations of points_to_dxf and
        ///    to_obj. Invoke this command if loading in Unity3d or Blender will be
        ///    used.
        /// One argument: true or false. Setting true invokes the transform. Setting 
        ///    false deactivates it.
        /// When invoked, cooridnates of output elements are changed so that the tin model's
        ///     center is at 0,0,0. This is necessary to view them in Blender and Unity.
        /// The command is only effective after a TIN surface has been created.
        /// </summary>
        /// <param name="commandItems"></param>
        private static void transform_to_zero(List<string> commandItems)
        {
            if(null == mainSurface)
            {
                mirrorLogPrint("The transform_to_zero command cannot be invoked " +
                    "until the main surface has been created.");
                mirrorLogPrint("   No transform created.");
            }
            if(commandItems.Count > 1)
            {
                if(commandItems[1].ToLower() == "false")
                {
                    TINsurface.setAffineTransformToZeroCenter(mainSurface, false);
                    mirrorLogPrint("Transform cleared.");
                }
            }
            TINsurface.setAffineTransformToZeroCenter(mainSurface, true);
            mirrorLogPrint("Transform to zero now set.");
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
                derivedSurface.WritePointsToDxf(writeFileName, shouldZip);
            }
            else if(null != mainSurface)
            {
                mainSurface.WritePointsToDxf(writeFileName, shouldZip);
            }
        }

        private static void alignment_to_3d_dxf(List<string> commandItems)
        {
            throw new NotImplementedException();
        }

        private static void ToXYZ(List<string> commandItems)
        {
            var outFilename = commandItems.Skip(1).Where(arg => arg.Contains(".xyz")).FirstOrDefault();
            if (null == outFilename)
            {
                mirrorLogPrint("Output file name not specified. xyz output files must have a \".xyz\" extension.");
                return;
            }

            var writeFileName = GetCorrectOutputFilename(outFilename);
            mirrorLogPrint($"Creating xyz file: {writeFileName}");
            if (null != derivedSurface)
            {
                derivedSurface.ExportToXYZ(writeFileName);
            }
            else if (null != mainSurface)
            {
                mainSurface.ExportToXYZ(writeFileName);
            }
        }


        /// <summary>
        /// Output the active TIN surface as a Wavefront Object. Example:
        /// to_obj "c:\temp folder\my.obj"
        /// </summary>
        /// <param name="commandItems"></param>
        private static void to_obj(List<string> commandItems)
        {
            bool shouldZip = commandItems.Skip(1).Where(arg => arg.Equals("-zipped")).Any();
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
                derivedSurface.WriteToWaveFront(writeFileName, shouldZip);
            }
            else if (null != mainSurface)
            {
                mainSurface.WriteToWaveFront(writeFileName, shouldZip);
            }
        }

        private static void set_filter(List<string> commandItems)
        {
            classificationFilter.Clear();
            foreach(string argument in commandItems.Skip(1))
            {
                var arg = argument.Replace(",", "");
                classificationFilter.Add(Convert.ToInt32(arg));
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
        
        /// <summary>
        /// Sets bounding box coordinates for all following operations.
        /// Arguments:
        /// No arguments = clears the bounding box.
        /// 4 arguments, all numbers, are
        ///    Lower Left x (easting), Lower Left y (northing)
        ///    Upper Right x, Upper Right y
        /// </summary>
        /// <param name="commandItems"></param>
        private static void set_bounding_box(List<string> commandItems)
        {
            if(commandItems.Count != 5)
            {
                filterBB = null;
                return;
            }
            var llx = Convert.ToDouble(commandItems[1]);
            var lly = Convert.ToDouble(commandItems[2]);
            var urx = Convert.ToDouble(commandItems[3]);
            var ury = Convert.ToDouble(commandItems[4]);
            filterBB = new BoundingBox(llx, lly, urx, ury);
        }

        private static void point_count(List<string> commandItems)
        {
            var panelId = commandItems[1];
            var openFileStr = pwd.GetPathAndAppendFilename(panelId);
            int pointCount = TINsurface.PointCountFromLAS(openFileStr, 
                classificationFilter: classificationFilter);
            mirrorLogPrint($"File {panelId} contains {pointCount:n0}.");
        }

        private static void Load(List<string> commandItems)
        {
            var openFileStr = pwd.PrependPWDifNotAlreadyFullPath(commandItems[1]);
            if(null == filterBB)
                mainSurface = TINsurface.CreateFromLAS(openFileStr,
                classificationFilter: classificationFilter);
            else
                mainSurface = TINsurface.CreateFromLAS(openFileStr, 
                    filterBB.lowerLeftPt.x, filterBB.lowerLeftPt.y,
                    filterBB.upperRightPt.x, filterBB.upperRightPt.y, 
                    classificationFilter: classificationFilter);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="commandItems"></param>
        private static void Load_raster(List<string> commandItems)
        {
            var openFileStr = pwd.GetPathAndAppendFilename(commandItems[1]);
            mainSurface = TINsurface.CreateFromRaster(openFileStr);

        }

        private static void Load_rasters(List<string> commandItems)
        {
            var directoryToRead = commandItems[1];

            var trimToBB = false;

            // check to see if Trim parameter passed in commandItems
            if (commandItems.Count > 2 && commandItems[2].ToLower() == "-bb")
                trimToBB = true;

            if(trimToBB && allHorAlignments != null)
            {
                allHAsBB = allHorAlignments.FirstOrDefault().BoundingBox;
                foreach (var ha in allHorAlignments.Skip(1))
                    allHAsBB.expandByOtherBB(ha.BoundingBox);
            }

            mainSurface = TINsurface.CreateFromRasters(directoryToRead, allHAsBB);

            /* Test values for Plot Balsam rasters. * /
            var el = mainSurface.getElevation(new Point(749150.0, 645500.0));
            el = mainSurface.getElevation(new Point(750140.66, 644449.64));
            el = mainSurface.getElevation(new Point(754975.04, 644971.36));
            el = mainSurface.getElevation(new Point(750428.46, 640353.02));
            el = mainSurface.getElevation(new Point(753706.71, 641483.01));
            el = mainSurface.getElevation(new Point(754401.23, 640515.46));
            el = mainSurface.getElevation(new Point(752501.47, 643149.84));
            /* end of "Test values for Plot Balsam rasters" */
        }


        private static void ToLas(List<string> commandItems)
        {
            TINsurface sourceSurface = derivedSurface;
            if (null == derivedSurface)
                sourceSurface = mainSurface;

            if (commandItems.Count < 2)
            {
                mirrorLogPrint("Las file not created. No output file name given.");
                return;
            }

            var outFileName = GetCorrectOutputFilename(commandItems[1]);
            if (outFileName == sourceSurface.SourceData)
            {
                mirrorLogPrint("Warning: It is not possible to overwrite the original, source Las file.");
                mirrorLogPrint("Save_las command not executed.");
                return;
            }

            sourceSurface.ExportToLas(outFileName, mainSurface);
        }

        private static void repl_cmd(List<string> commandItems)
        {
            repl();
        }

        private static void print(List<string> commandItems)
        {
            foreach (var item in commandItems.Skip(1))
            {
                mirrorLogPrint(item);
            }
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
                        localLogFName = commandItems.RetrieveByIndex(2) ?? logFileName;
                        if (outDir.ConfirmExists(localLogFName))
                        {
                            outDir.DeleteFile(localLogFName);
                            outDir.CreateTextFile(localLogFName);
                        }
                        openMethod = s => new StreamWriter(s);
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

        static int attemptCount = 0;
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
                attemptCount++;
                if(attemptCount > 2000) Environment.Exit(-1);
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
            mainSurface.writeLinesToDxf(outfile, (Line => Line.DihedralAngleAsRad > 3.05));
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

        public static void exit(List<string> commandItems)
        {
            overallSW.Stop();
            Console.WriteLine($"Exiting normally.  {overallSW.Elapsed} time ellapsed.");
            System.Environment.Exit(0);
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

        public static int FirstIndexMatchLeft(this IEnumerable<string> items, string matchString=null)
        { // adapted from https://stackoverflow.com/a/4075382/1339950
            if (null == matchString) return -1;
            var strLen = matchString.Length;
            matchString = matchString.ToLower();

            var index = 0;
            foreach (var item in items)
            {
                var testString = item.Substring(0, strLen);
                if (testString.ToLower() == matchString)
                {
                    return index;
                }
                index++;
            }
            return -1;
        }
    }

}
