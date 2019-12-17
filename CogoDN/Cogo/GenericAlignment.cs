using System;
using System.Collections.Generic;
using System.Text;

namespace Cogo
{
    /// <summary>
    /// GenericAlignment is the parent class for all subclasses which have stations:
    /// Horizontal Alignment (and its members) and Vertical Alignment.  
    /// So GenericAlignment exists basically for three purposes
    /// 1.  As a way to provide station arithmetic for all kinds of alignments
    ///      (including arithmatic across equalities)
    /// 2.  As a way to keep track of when station arithmatic puts you past
    ///       the begining or end of an alignment.
    /// 3.  As a way to make it easy for a profile automatically to use the
    ///       regions/equalities of a governing horizontal alignment by holding
    ///       a reference to that governing HA without reduplicating the
    ///       stationing.
    /// </summary>
    [Serializable]
    public class GenericAlignment
    {
        public string Name { get; set; }

        public GenericAlignment Parent
        {
            get { return anAlignment; }
            set { if (value != this) anAlignment = value; }
        }

        private List<Region> allRegions;
        public double BeginStation { get; set; }
        public double EndStation { get; set; }
        private GenericAlignment anAlignment;

        public GenericAlignment() { Parent = null; }
        public GenericAlignment(double beginSta, double endSta) : this()
        {
            allRegions = new List<Region>();
            allRegions.Add(new Region(beginSta, endSta, 1));
            BeginStation = beginSta;
            EndStation = endSta;
        }

        public GenericAlignment(GenericAlignment anAlignment)
        {
            Parent = anAlignment;
        }

        public GenericAlignment(List<Double> stationEquationingList)
           : this()
        {
            if (null == stationEquationingList)
            {
                BeginStation = 0.0;
                EndStation = Double.NegativeInfinity;
                return;
            }

            allRegions = new List<Region>();
            BeginStation = stationEquationingList[0];
            EndStation = Double.PositiveInfinity;

            if (stationEquationingList.Count > 1)
                EndStation = stationEquationingList[1];

            if (stationEquationingList.Count > 2)
            {
                throw new NotImplementedException();
            }

            int i;
            for (i = 0; i < stationEquationingList.Count; i++)
            {

            }
        }

        /// <summary>
        /// Side effects only. Created so Spirals can do some essential cleanup
        /// after a restationing operation, but it can be used as needed.
        /// </summary>
        internal virtual void restationFollowOnOperation()
        {

        }

        public void addRegion(double beginSta, double endSta)
        {
            if (Parent != null)
                throw new cantAddRegionToChildAlignment();

            Region aNewRegion = new Region(beginSta, endSta, allRegions.Count);
            aNewRegion.trueBeginStation_ = allRegions[allRegions.Count - 1].getEndTrueStation();
            EndStation = EndStation + (endSta - beginSta);
            allRegions.Add(aNewRegion);
        }

        public CogoStation newStation()
        {
            var aNewStation = new CogoStation(this);
            setToBeginStation(ref aNewStation);
            return aNewStation;
        }

        public CogoStation newStation(double station, int region)
        {
            if (null == allRegions)
            {
                return Parent.newStation(station, region);
            }
            var aNewStation = new CogoStation(this);
            setStation(aNewStation, station, region, 0.0);
            return aNewStation;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stationVariable"></param>
        /// <param name="station"></param>
        /// <param name="region"></param>
        /// <param name="extended">Currently not implemented</param>
        public void setStation(CogoStation stationVariable, double station, int region, double extended)
        {
            int maxRegion = allRegions.Count;
            int regionToUse = region <= maxRegion ? region : maxRegion;
            if (regionToUse < 1) regionToUse = 1;

            stationVariable.region = regionToUse;

            Region aRegion = allRegions[regionToUse - 1];
            double tempTrueStation = (station - aRegion.beginStationDbl) + aRegion.trueBeginStation_ + extended;
            setStation(stationVariable, tempTrueStation);
        }

        public void setStation(CogoStation stationVariable, double station, int region)
        {
            setStation(stationVariable, station, region, 0.0);
        }

        public void setStation(CogoStation aStationRef, double trueStation)
        {
            aStationRef.trueStation = trueStation;
            aStationRef.region = getRegionFromTrueStation(trueStation);
            Region aRegion = allRegions[aStationRef.region - 1];
            if (trueStation > EndStation)
            {
                aStationRef.extended = trueStation - EndStation;
                aStationRef.station = aRegion.endStationDbl;
            }
            else if (trueStation < BeginStation)
            {
                aStationRef.extended = trueStation - BeginStation;
                aStationRef.region = 1;
                aStationRef.station = aRegion.beginStationDbl;
            }
            else
                aStationRef.station = trueStation - aRegion.trueBeginStation_ + aRegion.beginStationDbl;
        }

        internal int getRegionFromTrueStation(double trueStation)
        {
            if (trueStation < BeginStation)
                return 1;
            else if (trueStation > EndStation)
                return allRegions.Count;
            else
            {

                for (int i = 0; i < allRegions.Count; i++)
                {
                    if (trueStation < allRegions[i].trueBeginStation_)
                        return i;
                }
            }
            return allRegions.Count;
        }

        public void setToBeginStation(ref CogoStation aStation)
        {
            aStation.region = 1;
            aStation.station = allRegions[0].beginStationDbl;
            aStation.trueStation = aStation.station;
            aStation.extended = 0.0;
        }

        public void setToEndStation(ref CogoStation aStation)
        {
            aStation.region = allRegions.Count - 1;
            Region finalRegion = allRegions[aStation.region];
            aStation.station = finalRegion.endStationDbl;
            aStation.trueStation = finalRegion.getEndTrueStation();
            aStation.extended = 0.0;
        }

        [Serializable]
        private class Region
        {
            internal double trueBeginStation_ { get; set; }

            public double beginStationDbl { get; set; }
            public double endStationDbl { get; set; }
            public int region { get; set; }

            public Region(double bSta, double eSta, int regionNum)
            {
                beginStationDbl = bSta;
                endStationDbl = eSta;
                region = regionNum;
                trueBeginStation_ = beginStationDbl;
            }

            public double getEndTrueStation()
            {
                return trueBeginStation_ + (endStationDbl - beginStationDbl);
            }

        }


        public class cantAddRegionToChildAlignment : Exception
        {

        }

        /// Desired features not yet implemented
        /// None currently known (3 May 2012)

    }
}

