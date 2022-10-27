using CadFoundation.Coordinates.Curvilinear;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cogo.Analysis
{
    public static class ProfileAnalysis
    {
        public static int GetStartingTrend(Profile pfl)
        {
            double zeroElevation = (double) pfl.getElevation(pfl.BeginProfTrueStation);
            double quarterWidth = (pfl.EndProfTrueStation - pfl.BeginProfTrueStation) / 4.0;
            double increment = quarterWidth / 10;
            var elevations = new List<double>();
            for (double aStation = pfl.BeginProfTrueStation + increment;
                aStation <= quarterWidth;
                aStation += increment)
            {
                elevations.Add((double) pfl.getElevation(aStation));
            }
            double averageElevationFirstQuarter = elevations.Average();

            //IReadOnlyList<StationOffsetElevation> extremePoints = ProfileAnalysis.GetLocalExtrema(pfl);
            //var soe1 = extremePoints[0];
            //var soe2 = extremePoints[1];
            //if(soe2.station - soe1.station < 15.0)
            //{
            //    soe2.station = soe1.station + 15.0;
            //    soe2.elevation = pfl.getElevation(soe2.station);
            //}

            //if (soe1.elevation < soe2.elevation)
            //    return -1;

            double deltaElevationZeroToAverage = averageElevationFirstQuarter - zeroElevation;
            if (deltaElevationZeroToAverage > 0.0)  // valley
                return +1;

            return -1;  // else ridge
        }

        /// <summary>
        /// Get list of all Extrema stations over the profile.
        /// Offset value is use to indicate high point (+1) or low point (-1)
        /// Technical debt: Function only works for no-vc profiles. This must be fixed
        /// at some point.
        /// </summary>
        /// <param name="pfl"></param>
        /// <returns></returns>
        public static List<StationOffsetElevation> GetLocalExtrema(Profile pfl)
        {
            var returnList = new List<StationOffsetElevation>();
            var vpis = pfl.to_vpiList();
            foreach(rawVPI vpi in vpis.theVPIs)
            {
                var leftSlope = pfl.getSlopeFromTheLeft(vpi.Station);
                var rightSlope = pfl.getSlopeFromTheRight(vpi.Station);

                var soe = new StationOffsetElevation();
                if (null == leftSlope)
                {
                    soe.station = vpi.Station.trueStation;
                    soe.elevation = vpi.Elevation;
                    soe.offset = rightSlope < 0.0 ? +1 : -1;
                    returnList.Add(soe);
                }
                else if (null == rightSlope)
                {
                    soe.station = vpi.Station.trueStation;
                    soe.elevation = vpi.Elevation;
                    soe.offset = leftSlope < 0.0 ? -1 : +1;
                    returnList.Add(soe);
                }
                else
                {
                    if(leftSlope.Value > 0.0 && rightSlope.Value < 0.0)
                    {
                        soe.station = vpi.Station.trueStation;
                        soe.elevation = vpi.Elevation;
                        soe.offset = +1;
                        returnList.Add(soe);
                    }
                    else if(leftSlope.Value > 0.0 && rightSlope.Value < 0.0)
                    {
                        soe.station = vpi.Station.trueStation;
                        soe.elevation = vpi.Elevation;
                        soe.offset = -1;
                        returnList.Add(soe);
                    }
                }


            }
            return returnList;
        }
    }
}
