using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
            bool useHardCodes = true;
            

            if ((args.Length == 2 && args[0].ToLower() == "repl") || useHardCodes)
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
                classificationFilter: new List<int> { 2, 13 });  // Add 6 to get roofs.
            var pointCount = tinModel.allPoints.Count;
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
            var parse1 = str.Split("\"");
            foreach(var x in parse1)
            {
                s.Add(x);
            }
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
                    continue;
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

    }
}
