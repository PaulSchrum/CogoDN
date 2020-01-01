using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        internal double CellDepth { get; private set; }

        // Row Major 2D grid
        List<List<GridIndexCell>> cells;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        public GridIndexer(int itemCount, IBoxBounded BoxBounded)
        {
            if (null == BoxBounded) throw new ArgumentException("Parameter 2: BoxBounded, cannot be null.");
            myBB = BoxBounded.BoundingBox;
            var cellCount = Math.Sqrt(itemCount);
            var rowCount = Math.Sqrt(cellCount);
            RowCount = (int) Math.Ceiling(0.0);
            CellCount = RowCount * RowCount;
            CellWidth = myBB.Width / CellCount;
            CellDepth = myBB.Depth;
            for(int row=0; row<RowCount; row++)
            {
                var newRow = new List<GridIndexCell>();
                for(int col=0; col<ColumnCount; col++)
                {
                    var newCell = new GridIndexCell();
                    newRow.Add(newCell);
                }

            }
        }

        protected (int, int) GetIndicesFromCoordinates(double x, double y)
        {
            int xInt = (int) ((x - myBB.lowerLeftPt.x) / CellWidth);
            int yInt = (int)((myBB.upperRightPt.y - y) / CellDepth);
            return (xInt, yInt);
        }

        public void AssignObjectsToCells(IEnumerable<IBoxBounded> objects)
        {
            int rowIndex; int columnIndex;
            foreach(var anObject in objects)
            {
                // compute the cell indices from the Center Point, then give anObject to it
                (rowIndex, columnIndex) =
                    GetIndicesFromCoordinates(anObject.BoundingBox.Center.x, anObject.BoundingBox.Center.y);
                var cell = cells[rowIndex][columnIndex];
                cell.items.Add(anObject);
            }
        }

        public IReadOnlyCollection<IBoxBounded> FindObjectsAt(double x, double y)
        {
            //// indexing is top to bottom, left to right
            //var dx = x - this.myBB.lowerLeftPt.x; var dy = y - this.myBB.upperRightPt.y;
            //var dxWhole = Math.Truncate(dx); var dyWhole = Math.Truncate(dy);
            //var dxPart = dx - dxWhole; var dyPart = dy - dyWhole;

            int xIndex , yIndex;
            (xIndex, yIndex) = GetIndicesFromCoordinates(x, y);
            var theGridCell = cells[yIndex][xIndex];
            return new HashSet<IBoxBounded>(theGridCell.GetAllCovering(x, y));
        }

    }

    internal class GridIndexCell : BoundingBox
    {
        internal List<IBoxBounded> items = new List<IBoxBounded>();
        internal GridIndexCell()
        {

        }

        internal IEnumerable<IBoxBounded> GetAllCovering(double x, double y)
        {
            return items.Where(item => item.BoundingBox.isPointInsideBB2d(x, y));
        }
    }

    public interface IBoxBounded
    {
        BoundingBox BoundingBox { get; }
    }
}
