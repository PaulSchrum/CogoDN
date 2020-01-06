using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleToAttribute("Unit Tests")]
namespace CadFoundation.Coordinates.Indexing
{
    /// <summary>
    /// 
    /// </summary>
    public class GridIndexer
    {
        BoundingBox myBB;
        int CellCount;
        int RowCount;
        int ColumnCount { get{ return RowCount; } }
        internal double CellWidth { get; private set; }
        internal double CellLength { get; private set; }

        // Row Major 2D grid
        List<List<GridIndexCell>> cells;
        public IReadOnlyList<List<GridIndexCell>> AllCells { get { return cells.ToList(); } }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        public GridIndexer(int itemCount, IBoxBounded rootBoundingBox, IEnumerable<IBoxBounded> objects=null)
        {
            if (null == rootBoundingBox) throw new ArgumentException("Parameter 2: BoxBounded, cannot be null.");
            myBB = rootBoundingBox.BoundingBox;
            var cellCount = Math.Sqrt(itemCount);
            var rowCount = Math.Sqrt(cellCount);
            RowCount = (int) Math.Ceiling(rowCount);
            CellCount = RowCount * RowCount;
            CellWidth = myBB.Width / ColumnCount;   // East-West
            CellLength = myBB.Depth / RowCount;      // North-South
            cells = new List<List<GridIndexCell>>(RowCount);
            for (int row=0; row<RowCount; row++)
            {
                var newRow = new List<GridIndexCell>(ColumnCount);
                for(int col=0; col<ColumnCount; col++)
                {
                    var newCell = new GridIndexCell(GetCoordinatesFromIndex(row, col));
                    newRow.Add(newCell);
                }
                cells.Add(newRow);
            }
            if (objects != null)
                this.AssignObjectsToCells(objects);
        }

        protected (double, double, double, double) GetCoordinatesFromIndex(int row, int column)
        {
            // start here. Why does (25, 31) give me the wrong coordinates?
            double LLx = this.myBB.lowerLeftPt.x + column * CellWidth;
            double LLy = this.myBB.lowerLeftPt.y + row * CellLength;
            double URy = LLy + CellLength;
            double URx = LLx + CellWidth;
            return (LLx, LLy, URx, URy);
        }

        protected (int, int) GetIndicesFromCoordinates(double x, double y)
        {
            int xInt = (int) ((x - myBB.lowerLeftPt.x) / CellWidth);
            int yInt = (int)((y - myBB.lowerLeftPt.y) / CellLength);

            if (xInt >= this.RowCount) xInt = this.RowCount - 1;
            if (yInt >= this.ColumnCount) yInt = this.ColumnCount - 1;
            if (xInt < 0) xInt = 0;
            if (yInt <0) xInt = 0;

            return (xInt, yInt);
        }

        public void AssignObjectsToCells(IEnumerable<IBoxBounded> objects)
        {
            foreach(var anObject in objects)
            {
                int LLrowIndex; int LLcolumnIndex; int URrowIndex; int URcolumnIndex;

                // compute the cell indices from the Center Point, then give anObject to it
                (LLrowIndex, LLcolumnIndex) =
                    GetIndicesFromCoordinates(anObject.BoundingBox.lowerLeftPt.x, anObject.BoundingBox.lowerLeftPt.y);
                (URrowIndex, URcolumnIndex) =
                    GetIndicesFromCoordinates(anObject.BoundingBox.upperRightPt.x, anObject.BoundingBox.upperRightPt.y);

                if (LLrowIndex != URrowIndex || LLcolumnIndex != URcolumnIndex)
                {
                    var bdg = 1.0;
                }


                for (int r=LLrowIndex; r<=URrowIndex;r++)
                {
                    for(int c=LLcolumnIndex; c<=URcolumnIndex;c++)
                    {
                        var cell = cells[r][c];
                        cell.items.Add(anObject);
                    }
                }
            }
        }

        public IReadOnlyList<IBoxBounded> FindObjectsAt(double x, double y)
        {
            //// indexing is top to bottom, left to right
            //var dx = x - this.myBB.lowerLeftPt.x; var dy = y - this.myBB.upperRightPt.y;
            //var dxWhole = Math.Truncate(dx); var dyWhole = Math.Truncate(dy);
            //var dxPart = dx - dxWhole; var dyPart = dy - dyWhole;

            int xIndex , yIndex;
            (xIndex, yIndex) = GetIndicesFromCoordinates(x, y);
            var theGridCell = cells[xIndex][yIndex];
            return new List<IBoxBounded>(theGridCell.GetAllCovering(x, y));
        }

    }

    public class GridIndexCell : BoundingBox
    {
        internal HashSet<IBoxBounded> items = new HashSet<IBoxBounded>();
        internal GridIndexCell((double, double, double, double) p) : base(p.Item1, p.Item2, p.Item3, p.Item4)
        { }

        internal IEnumerable<IBoxBounded> GetAllCovering(double x, double y)
        {
            var allHaveThisPoint = new List<IBoxBounded>();
            foreach(var item in items)
            {
                var itis = item.BoundingBox.isPointInsideBB2d(x, y);
                if(itis)
                {
                    allHaveThisPoint.Add(item);
                }
            }
            return items.Where(item => item.BoundingBox.isPointInsideBB2d(x, y));
        }
    }

    public interface IBoxBounded
    {
        BoundingBox BoundingBox { get; }
    }
}
