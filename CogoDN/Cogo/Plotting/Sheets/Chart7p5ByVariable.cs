using Cogo.Plotting.Details;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cogo.Plotting.Sheets
{
    public class Chart7p5ByVariable : Sheet
    {
        private Chart7p5ByVariable()
        {

        }

        public static Chart7p5ByVariable Create()
        {
            Chart7p5ByVariable sheet = new Chart7p5ByVariable();
            sheet.height = new DecimalUnits(0, pUnit.Inch);
            sheet.width = new DecimalUnits(7.5m, pUnit.Inch);
            //sheet.scale = PlotScale
            

            return sheet;
        }

    }
}
