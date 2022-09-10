﻿using Cogo.Plotting.Details;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cogo.Plotting.Sheets
{
    public class ProfileSheet36x24 : Sheet
    {
        private ProfileSheet36x24()
        {

        }

        public override double? PlotSheetToPdfFile(
            IEnumerable<Object> dataToPlot,
            string printFileName,
            double leftXValue = 0.0,
            double? bottomYvalue = null)
        {
            throw new NotImplementedException();
        }

        public static ProfileSheet36x24 Create()
        {
            ProfileSheet36x24 sheet = new ProfileSheet36x24();
            sheet.height = new DecimalUnits(24, pUnit.Inch);
            sheet.width = new DecimalUnits(36, pUnit.Inch);           

            return sheet;
        }
    }
}
