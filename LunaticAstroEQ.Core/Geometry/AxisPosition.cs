using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.LunaticAstroEQ.Core.Geometry
{

   /// <summary>
   /// A structure to represent telecope mount axis positions
   /// </summary>
   public class AxisPosition
   {
      private Angle _RAAxis;
      private Angle _DecAxis;

      public Angle RAAxis
      {
         get
         {
            return _RAAxis;
         }
      }
      public Angle DecAxis
      {
         get
         {
            return _DecAxis;
         }
      }

      public int AxisCount
      {
         get
         {
            return 2;
         }
      }

      public AxisPosition()
      {
         _RAAxis = new Angle(0.0);
         _DecAxis = new Angle(0.0);
      }

      /// <summary>
      /// Initialise the Axis positions
      /// </summary>
      /// <param name="raPosition">RA Axis position in degrees</param>
      /// <param name="decPosition">Dec Axis position in degrees</param>
      public AxisPosition(string raPosition, string decPosition)
      {
         _RAAxis = new Angle(raPosition);
         _DecAxis = new Angle(decPosition);
      }
      public AxisPosition(double raRadians, double decRadians):this()
      {
         _RAAxis.Radians = raRadians ;
         _DecAxis.Radians = decRadians ;
      }


      public AxisPosition(string axisPositions):this()
      {
         string[] positions = axisPositions.Split(',');
         try {
            _RAAxis.Value = double.Parse(positions[0]);
            _DecAxis.Value = double.Parse(positions[1]);
         }
         catch  {
            throw new ArgumentException("Badly formed axis position string");
         }
      }

      public double this[int index]
      {
         get
         {
            if (index < 0 || index > 1) {
               throw new ArgumentOutOfRangeException();
            }
            return (index == 0 ? _RAAxis.Radians : _DecAxis.Radians);
         }
         set
         {
            if (index < 0 || index > 1) {
               throw new ArgumentOutOfRangeException();
            }
            if (index == 0) {
               _RAAxis.Radians = value;
            }
            else {
               _DecAxis.Radians = value;
            }
         }
      }

      /// <summary>
      /// Compares the two specified sets of Axis positions.
      /// </summary>
      public static bool operator ==(AxisPosition pos1, AxisPosition pos2)
      {
         return (pos1.RAAxis.Radians == pos2.RAAxis.Radians && pos1.DecAxis.Radians == pos2.DecAxis.Radians);
      }

      public static bool operator !=(AxisPosition pos1, AxisPosition pos2)
      {
         return !(pos1 == pos2);
      }

      public static AxisPosition operator -(AxisPosition pos1, AxisPosition pos2)
      {
         return new AxisPosition(pos1.RAAxis.Radians - pos2.RAAxis.Radians, pos1.DecAxis.Radians - pos2.DecAxis.Radians);
      }

      public static AxisPosition operator +(AxisPosition pos1, AxisPosition pos2)
      {
         return new AxisPosition(pos1.RAAxis.Radians + pos2.RAAxis.Radians, pos1.DecAxis.Radians + pos2.DecAxis.Radians);
      }

      public override int GetHashCode()
      {
         unchecked // Overflow is fine, just wrap
         {
            int hash = 17;
            // Suitable nullity checks etc, of course :)
            hash = hash * 23 + _RAAxis.GetHashCode();
            hash = hash * 23 + _DecAxis.GetHashCode();
            return hash;
         }
      }

      public override bool Equals(object obj)
      {
         return (obj is AxisPosition
                 && this == (AxisPosition)obj);
      }


      public bool Equals(AxisPosition obj, double toleranceRadians)
      {
         return ((Math.Abs(obj.RAAxis.Radians - this.RAAxis.Radians) < toleranceRadians)
            && (Math.Abs(obj.DecAxis.Radians - this.DecAxis.Radians) < toleranceRadians));
      }

      public override string ToString()
      {
         return string.Format("RAAxis = {0} Radians, DecAxis = {1} Radians", _RAAxis.Radians, _DecAxis.Radians);
      }
      public string ToDegreesString()
      {
         return string.Format("{0},{1}", _RAAxis.Value, _DecAxis.Value);
      }
      public string ToRadiansString()
      {
         return string.Format("{0},{1}", _RAAxis.Radians, _DecAxis.Radians);
      }

   }

}
