using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Surfaces.TIN
{
    public class LasFile
    {
        public List<ILidarPoint> AllPoints { get; private set; } = null;

        public int VersionMajor { get; private set; } = 0;
        public int VersionMinor { get; private set; } = 0;

        public int FileCreationYear { get; private set; } = 0;
        public int FileCreationDay { get; private set; } = 0;

        public int HeaderSize { get; private set; } = 0;
        public int OffsetToPointData { get; private set; } = 0;
        public long NumberVarLenRecords { get; private set; } = 0;
        public int PointDataRecordFormat { get; private set; } = 0;
        public int PointDataRecordLength { get; private set; } = 0;
        public double XscaleFactor { get; private set; } = 0;
        public double YscaleFactor { get; private set; } = 0;
        public double ZscaleFactor { get; private set; } = 0;
        public double Xoffset { get; private set; } = 0;
        public double Yoffset { get; private set; } = 0;

        public double Zoffset { get; private set; } = 0;
        public double MaxX { get; private set; } = 0;
        public double MinX { get; private set; } = 0;
        public double MaxY { get; private set; } = 0;
        public double MinY { get; private set; } = 0;
        public double MaxZ { get; private set; } = 0;
        public double MinZ { get; private set; } = 0;
        public int NumberOfPointRecords { get; private set; } = 0;

        public LasFile(string LasFilename,
            List<int> classificationFilter = null)
        {
            this.classificationFilter = new List<int> { 2, 13 };
            if (null != classificationFilter)
                this.classificationFilter = classificationFilter;

            int hdrLen = 375;
            using (BinaryReader reader = new BinaryReader(File.Open(LasFilename, FileMode.Open)))
            {
                byte[] memoryData = new byte[hdrLen];
                reader.Read(memoryData, 0, hdrLen);
                //var aseg = new ArraySegment<byte>(memoryData, 0, 4);
                //var hdrStr = String.Concat(aseg.Select(c => (char) c));
                var hdrStr = memoryData.getString(0, 4);
                if (!hdrStr.Equals("LASF"))
                    throw new IOException("File is not a .las file.");

                this.VersionMajor = memoryData.getChar(24);
                this.VersionMinor = memoryData.getChar(25);

                if (this.VersionMajor != 1 && this.VersionMinor != 4)
                    throw new IOException("Las file is not Version 1.4. Unable to process.");

                this.FileCreationDay = memoryData.getInt(90);
                this.FileCreationYear = memoryData.getInt(92);

                this.HeaderSize = memoryData.getInt(94);
                this.OffsetToPointData = (int)memoryData.getLong(96);
                this.NumberVarLenRecords = memoryData.getLong(100);
                this.PointDataRecordFormat = memoryData.getChar(104);
                this.PointDataRecordLength = memoryData.getInt(105);

                this.XscaleFactor = memoryData.getDouble(131);
                this.YscaleFactor = memoryData.getDouble(139);
                this.ZscaleFactor = memoryData.getDouble(147);
                this.Xoffset = memoryData.getDouble(155);
                this.Yoffset = memoryData.getDouble(163);
                this.Zoffset = memoryData.getDouble(171);

                this.MaxX = memoryData.getDouble(179);
                this.MinX = memoryData.getDouble(187);
                this.MaxY = memoryData.getDouble(195);
                this.MinY = memoryData.getDouble(203);
                this.MaxZ = memoryData.getDouble(211);
                this.MinZ = memoryData.getDouble(219);

                this.NumberOfPointRecords = (int)memoryData.getLongLong(247);

                this.populateAllPoints(reader);

            }
        }

        internal void ClearAllPoints()
        {
            this.AllPoints = null;
        }
        private int skipPoints { get; set; }
        private void populateAllPoints(BinaryReader reader)
        {
            int pointCounter = 0;
            int sequenceCounter = 0; // Counts all points in the file.
            this.AllPoints = new List<ILidarPoint>();
            foreach (int recNo in Enumerable.Range(1, this.NumberOfPointRecords))
            {
                int offset = recNo * this.PointDataRecordLength;
                int address = offset + this.OffsetToPointData;
                TINpoint aPoint = this.readPoint(reader, address);
                sequenceCounter++;
                if (!this.classificationFilter.Contains(aPoint.lidarClassification))
                    continue;
                pointCounter++;
                aPoint.originalSequenceNumber = sequenceCounter;

                if (pointCounter % (skipPoints + 1) == 0)
                    this.AllPoints.Add(aPoint);
            }
        }

        private List<int> classificationFilter { get; set; }
        private TINpoint readPoint(BinaryReader reader, int address)
        {
            byte[] pointData = new byte[this.PointDataRecordLength];
            reader.Read(pointData, 0, this.PointDataRecordLength);



            var retPoint = new TINpoint();
            retPoint.lidarClassification = pointData.getChar(16);

            if (!(this.classificationFilter.Contains(retPoint.lidarClassification)))
                return retPoint;

            int xCoord = (int)pointData.getLong(0);
            int yCoord = (int)pointData.getLong(4);
            int zCoord = (int)pointData.getLong(8);

            retPoint.x = xCoord * this.XscaleFactor + this.Xoffset;
            retPoint.y = yCoord * this.YscaleFactor + this.Yoffset;
            retPoint.z = zCoord * this.YscaleFactor + this.Zoffset;

            return retPoint;
        }

        public static void CreateLasFromLas(string filename, string sourceFilename,
            IReadOnlyList<int> rowsToWrite)
        {
            int totalPointsToWrite = rowsToWrite.Count;
            int hdrLen = 375;
            byte[] headerByteArray = new byte[hdrLen];
            using (BinaryReader reader = new BinaryReader(File.Open(sourceFilename, FileMode.Open)))
            {
                reader.Read(headerByteArray, 0, hdrLen);
                //var aseg = new ArraySegment<byte>(memoryData, 0, 4);
                //var hdrStr = String.Concat(aseg.Select(c => (char) c));
                var hdrStr = headerByteArray.getString(0, 4);
                if (!hdrStr.Equals("LASF"))
                    throw new IOException("File is not a .las file.");

                int VersionMajor = headerByteArray.getChar(24);
                int VersionMinor = headerByteArray.getChar(25);

                if (VersionMajor != 1 && VersionMinor != 4)
                    throw new IOException("Las file is not Version 1.4. Unable to process.");

                var OffsetToPointData = (int)headerByteArray.getLong(96);
                var PointDataRecordLength = headerByteArray.getInt(105);
                byte[] pointData = new byte[PointDataRecordLength];
                int totalSourcePointRow = (int)headerByteArray.getLongLong(247);
                headerByteArray.setLongLong(247, (long)totalPointsToWrite);

                using(var writer = new BinaryWriter(File.Open(filename, FileMode.Create)))
                {
                    writer.Write(headerByteArray);
                    int rowCounter = 0;
                    foreach(int idx in rowsToWrite.OrderBy(i => i))
                    {
                        while(rowCounter < idx)
                        {
                            rowCounter++;
                            if (rowCounter >= totalSourcePointRow)
                                return;
                        }
                        int offset = rowCounter * PointDataRecordLength;
                        int address = offset + OffsetToPointData;
                        reader.Read(pointData, 0, PointDataRecordLength);
                        writer.Write(pointData);
                    }
                }
            }

        }
    }

    public static class byteArrayExtensions
    {
        public static string getString(this byte[] byteArray, int start, int stop)
        {
            var aSegment = new ArraySegment<byte>(byteArray, start, stop);
            return String.Concat(aSegment.Select(c => (char)c));
        }

        public static char getChar(this byte[] byteArray, int index)
        {
            return (char)byteArray[index];
        }

        public static ushort getInt(this byte[] byteArray, int index)
        {
            return BitConverter.ToUInt16(byteArray, index);
        }

        public static long getLong(this byte[] byteArray, int index)
        {
            return BitConverter.ToUInt32(byteArray, index);
        }

        public static ulong getLongLong(this byte[] byteArray, int index)
        {
            return BitConverter.ToUInt64(byteArray, index);
        }

        public static void setLongLong(this byte[] byteArray, int index, long newValue)
        {
            int byteCount = 8; // for long long.
            var bytes = BitConverter.GetBytes(newValue);
            for(int i=0; i<byteCount; i++)
            {
                byteArray[index + i] = bytes[i];
            }
        }

        public static double getDouble(this byte[] byteArray, int index)
        {
            return BitConverter.ToDouble(byteArray, index);
        }

    }


    public class LasPoint
    {
        public Double x { get; set; }
        public Double y { get; set; }
        public Double z { get; set; }

        public LasPoint(double newX, double newY, double newZ)
        { x = newX; y = newY; z = newZ; }
    }
}
