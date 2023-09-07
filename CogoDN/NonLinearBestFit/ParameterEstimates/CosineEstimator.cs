﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CadFoundation.Coordinates;
using CadFoundation.Coordinates.Curvilinear;
using Cogo;
using Cogo.Analysis;


namespace NonLinearBestFit.ParameterEstimates
{
    public class CosineEstimator : ParamEstimatorBase
    {
        public CosineEstimator(IReadOnlyList<double> xVals, IReadOnlyList<double> yVals) :
            base(xVals, yVals)
        {

        }

        private class localStationSlope
        {
            public double station;
            public double slope;

            public override string ToString()
            {
                return $"{station:0.##}, {slope:0.###}";
            }
        }

        internal static int tmpCounter = 0;


        public Profile EstimateCosineParameterRanges(double percentMaxSlope, out Tuple<double, double> distanceRanges)
        {
            tmpCounter++;
            distanceRanges = new Tuple<double, double>(0.0, 0.0);

            var dataAsProfile = Profile.CreateFromXYlists(xValues, yValues);

            var startingSlopeTrend = ProfileAnalysis.GetStartingTrend(dataAsProfile);
            // -1 is a ridge. // +1 is a valley

            var allLocalExtrema = ProfileAnalysis.GetLocalExtrema(dataAsProfile);
            var startStation = allLocalExtrema[0].station;
            var startElevation = allLocalExtrema[0].elevation;
            double endStation = allLocalExtrema.Last().station;

            var extremeSoe = new StationOffsetElevation(startStation, startElevation, 0.0);
            if (allLocalExtrema.Count == 2)
            {
                extremeSoe.station = allLocalExtrema[1].station;
                extremeSoe.elevation = allLocalExtrema[1].elevation;
            }
            else
            {
                if (startingSlopeTrend <= -1)
                {
                    extremeSoe.elevation = Double.PositiveInfinity;
                    foreach (var vpi in dataAsProfile.VpiList.theVPIs)
                    {
                        if (vpi.Elevation < extremeSoe.elevation)
                        {
                            extremeSoe.station = (double)vpi.Station;
                            extremeSoe.elevation = vpi.Elevation;
                        }
                    }
                }
                else
                {
                    extremeSoe.elevation = Double.NegativeInfinity;
                    foreach (var vpi in dataAsProfile.VpiList.theVPIs)
                    {
                        if (vpi.Elevation > extremeSoe.elevation)
                        {
                            extremeSoe.station = (double)vpi.Station;
                            extremeSoe.elevation = vpi.Elevation;
                        }
                    }
                }
            }


            // 2. Get slopes at intervals
            double increment = 12;
            double prevStation = startStation;
            double prevElevation = startElevation;
            List<localStationSlope> slopes = new List<localStationSlope>();

            for (double sta = startStation + increment; sta <= extremeSoe.station; sta += increment)
            {
                double elev = (double)dataAsProfile.getElevation(sta);

                double intermediateStation = (sta + prevStation) / 2.0;
                double slope = (elev - prevElevation) / (sta - prevStation);

                slopes.Add(new localStationSlope { station = intermediateStation, slope = slope });

                prevStation = sta;
                prevElevation = elev;
            }
            localStationSlope maxSlopeAndStation = null;
            if (startingSlopeTrend <= -1) // ridge
            {
                maxSlopeAndStation = slopes.OrderBy(val => val.slope).First();
            }
            else // valley
            {
                maxSlopeAndStation = slopes.OrderByDescending(val => val.slope).First();
            }

            var slopeCutoff = Math.Abs(percentMaxSlope * maxSlopeAndStation.slope);
            var rangeStart = slopes.Where(s => Math.Abs(s.slope) >= slopeCutoff).First().station;
            var rangeEnd = slopes.Where(s => Math.Abs(s.slope) >= slopeCutoff).Last().station;
            distanceRanges = new Tuple<double, double>(rangeStart, rangeEnd);

            return dataAsProfile;

        }
    }

}
