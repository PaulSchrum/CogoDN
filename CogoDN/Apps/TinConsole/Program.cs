using System;
using System.Collections.Generic;
using Surfaces.TIN;

namespace TinConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var tinModel = TINsurface.CreateFromLAS(args[0],
                skipPoints: 2,
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
    }
}
