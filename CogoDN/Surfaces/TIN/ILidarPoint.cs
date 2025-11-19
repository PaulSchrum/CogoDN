using MIConvexHull;
using System;

namespace Surfaces.TIN
{
    public interface ILidarPoint : IVertex
    {
        public Double x { get; set; }
        public Double y { get; set; }
        public Double z { get; set; }

        public int lidarClassification { get; set; }
        public int myIndex { get; set; }
        public int originalSequenceNumber {  get; set; }
    }
}
