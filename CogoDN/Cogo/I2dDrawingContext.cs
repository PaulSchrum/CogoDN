using System;
using System.Collections.Generic;
using System.Text;

namespace Cogo
{
    public interface I2dDrawingContext
    {
        void resetDashArray();
        void addToDashArray(double dashLength);
        void setElementLevel(string LevelName);
        // void setElementColor(Color Color);
        void setElementWeight(double Weight);
        void Draw(double X1, double Y1, double X2, double Y2);
        void Draw(string TextContent, double X1, double Y1, double rotationAngle);
        double getAheadOrientationAngle();
    }
}
