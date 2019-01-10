using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.LunaticAstroEQ.Core.Geometry
{
   public struct Vector
   {
      double[] _data;

      public Vector(int size)
      {
         _data = new double[size];
      }

      public Vector(double l, double m, double n)
      {
         _data = new double[] { l, m, n };
      }

      public double this[int index]
      {
         get
         {
            return _data[index];
         }
         set
         {
            _data[index] = value;
         }
      }
   }

}
