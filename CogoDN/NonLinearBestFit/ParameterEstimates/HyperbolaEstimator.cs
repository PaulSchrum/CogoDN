using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CadFoundation.Coordinates;
using CadFoundation.Coordinates.Curvilinear;
using Cogo;
using Cogo.Analysis;

namespace NonLinearBestFit.ParameterEstimates
{
    public class HyperbolaEstimator : ParamEstimatorBase
    {
        public HyperbolaEstimator(IReadOnlyList<double> xVals, IReadOnlyList<double> yVals) :
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

        /// <summary>
        /// Makes reasonable guesses at the values from a, Sa, and xDistance from a Profile.
        /// Assumptions: First station is 0 or very close to 0.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="Sa"></param>
        /// <param name="xDistance"></param>
        public void EstimateHyperbolaParameters(out double a, out double Sa, out double xDistance)
        {
            tmpCounter++;

            var dataAsProfile = Profile.CreateFromXYlists(xValues, yValues);

            var startingSlopeTrend = ProfileAnalysis.GetStartingTrend(dataAsProfile);
            // -1 is a ridge. // +1 is a valley

            var allLocalExtrema = ProfileAnalysis.GetLocalExtrema(dataAsProfile);
            var startStation = allLocalExtrema[0].station;
            var startElevation = allLocalExtrema[0].elevation;
            double endStation = 0.0;

            if (tmpCounter == 4)
            {
                int stopHere = 100;
            }

            var extremeSoe = new StationOffsetElevation(startStation, startElevation, 0.0);
            if(allLocalExtrema.Count == 2)
            {
                extremeSoe.station = allLocalExtrema[1].station;
                extremeSoe.elevation = allLocalExtrema[1].elevation;
            }
            else
            {
                if(startingSlopeTrend <= -1)
                {
                    extremeSoe.elevation = Double.PositiveInfinity;
                    foreach(var vpi in dataAsProfile.VpiList.theVPIs)
                    {
                        if(vpi.Elevation < extremeSoe.elevation)
                        {
                            extremeSoe.station = (double) vpi.Station;
                            extremeSoe.elevation = vpi.Elevation;   ////  ?????
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

            for(double sta=startStation+increment; sta<= extremeSoe.station; sta+=increment)
            {
                double elev = (double) dataAsProfile.getElevation(sta);

                double intermediateStation = (sta + prevStation) / 2.0;
                double slope = (elev - prevElevation) / (sta - prevStation);

                slopes.Add(new localStationSlope { station = intermediateStation, slope = slope });

                prevStation = sta;
                prevElevation = elev;
            }


            // 3. Get steeptest slope and its location
            localStationSlope maxSlopeAndStation = null;
            if(startingSlopeTrend <= 1) // ridge
            {
                maxSlopeAndStation = slopes.OrderBy(val => val.slope).First();
            }
            else // valley
            {
                maxSlopeAndStation = slopes.OrderByDescending(val => val.slope).First();
            }

            Sa = maxSlopeAndStation.slope;
            xDistance = maxSlopeAndStation.station * 1.1;

            // 4. Set a as being the approximate station where slope = Sa / sqrt(2).
            var slopeAtA = Sa * 0.707106781;

            prevStation = startStation;  double nextStation = 0;
            double prevSlope = 0;  // By definition, slope is 0% at x = 0
            double nextSlope = 0;
            foreach(var segment in slopes)
            {
                nextStation = segment.station;
                nextSlope = segment.slope;
                if (startingSlopeTrend >= 1) // ridge
                {
                    if (nextSlope <= slopeAtA)
                        break;
                }
                else // valley
                {
                    if (nextSlope >= slopeAtA)
                        break;
                }

                prevStation = nextStation;
                prevSlope = nextSlope;
            }

            a = SimpleLerp.LERP(prevSlope, prevStation, nextSlope, nextStation, slopeAtA);

        }
    }

    public class DataTooIrregularException : NotImplementedException
    {

    }
}
