using CadFoundation.Coordinates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NonLinearBestFit.ParameterEstimates
{
    /// <summary>
    /// Base class for things common to all parameter estimators for non-linear good-fitting.
    /// </summary>
    public abstract class ParamEstimatorBase
    {
        public IReadOnlyList<double> xValues { get; set; } = null;
        public IReadOnlyList<double> yValues { get; set; } = null;

        // public BoundingBox boundingBox { get; protected set; } = null;

        public ParamEstimatorBase(IReadOnlyList<double> xVals, IReadOnlyList<double> yVals)
        {
            xValues = xVals;
            yValues = yVals;

            //boundingBox = new BoundingBox()
        }

        public void swapValuesLeftToRight()
        {
            var tmpXvalues = xValues.Select(x => -x).Reverse().ToArray();
            xValues = tmpXvalues;
        }
    }

    public enum TerrainFeatureType
    {
        Crest = -1,
        Sag = 1
    };
    public class DataTooIrregularException : NotImplementedException
    {

    }
}
