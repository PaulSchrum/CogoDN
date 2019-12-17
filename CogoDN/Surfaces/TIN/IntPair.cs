using System;
using System.Collections.Generic;
using System.Text;

namespace Surfaces.TIN
{
    internal class IntPair
    {
        public int num1;
        public int num2;

        public override bool Equals(object obj)
        {
            IntPair other = obj as IntPair;
            if (other == null)
                return false;

            return ((this.num1 == other.num1) && (this.num2 == other.num2));
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
