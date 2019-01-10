using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.LunaticAstroEQ.Core.Geometry
{
   public enum Quadrant
   {
      [Description("Norteast")]
      NE,
      [Description("Southeast")]
      SE,
      [Description("Southwest")]
      SW,
      [Description("Northwest")]
      NW
   }
   /// <summary>
   /// A structure to represent an EquatorialCoordinate
   /// </summary>
   public struct CarteseanCoordinate
   {
      private double _X;
      private double _Y;
      private double _Z;
      private double _R;        // Radius Sign
      private double _RA;       // Radius Alpha
      private bool _Flag;        // Was .F in VB6 seems to be a flag used to indicate whether Taki transform worked         

      public double X
      {
         get
         {
            return _X;
         }
         set
         {
            _X = value;
         }
      }
      public double Y
      {
         get
         {
            return _Y;
         }
         set
         {
            _Y = value;
         }
      }
      public double Z
      {
         get
         {
            return _Z;
         }
         set
         {
            _Z = value;
         }
      }

      public double R
      {
         get
         {
            return _R;
         }
         set
         {
            _R = value;
         }
      }
      public double RA
      {
         get
         {
            return _RA;
         }
         set
         {
            _RA = value;
         }
      }
      public bool Flag
      {
         get
         {
            return _Flag;
         }
         set
         {
            _Flag = value;
         }
      }

      public Quadrant Quadrant
      {
         get
         {
            if (X >= 0) {
               if (Y >= 0) {
                  return Quadrant.NE;
               }
               else {
                  return Quadrant.SE;
               }
            }
            else {
               if (Y < 0) {
                  return Quadrant.SW;
               }
               else {
                  return Quadrant.NW;
               }
            }
         }
      }

      public CarteseanCoordinate(double x, double y) :this(x, y, 0.0)
      {
      }
      public CarteseanCoordinate(double x, double y, double z)
      {
         _X = x;
         _Y = y;
         _Z = z;
         //_Longitude = new Angle(longitude);
         //_ObservedWhen = observedTime;
         _R = 0.0;
         _RA = 0.0;
         _Flag = false;
      }

      public double this[int index]
      {
         get
         {
            if (index < 0 || index > 1) {
               throw new ArgumentOutOfRangeException();
            }
            return (index == 0 ? _X: _Y);
         }
         set
         {
            if (index < 0 || index > 1) {
               throw new ArgumentOutOfRangeException();
            }
            if (index == 0) {
               _X = value;
            }
            else {
               _Y = value;
            }
         }
      }


      #region Operator overloads ...
      /// <summary>
      /// Compares the two specified sets of Axis positions.
      /// </summary>
      public static bool operator ==(CarteseanCoordinate pos1, CarteseanCoordinate pos2)
      {
         return (pos1.X == pos2.X && pos1.Y == pos2.Y && pos1.Z == pos2.Z);
      }

      public static bool operator !=(CarteseanCoordinate pos1, CarteseanCoordinate pos2)
      {
         return !(pos1 == pos2);
      }

      public override int GetHashCode()
      {
         unchecked // Overflow is fine, just wrap
         {
            int hash = 17;
            // Suitable nullity checks etc, of course :)
            hash = hash * 23 + _X.GetHashCode();
            hash = hash * 23 + _Y.GetHashCode();
            hash = hash * 23 + _Z.GetHashCode();
            return hash;
         }
      }

      public override bool Equals(object obj)
      {
         return (obj is CarteseanCoordinate
                 && this == (CarteseanCoordinate)obj);
      }

      public static CarteseanCoordinate operator -(CarteseanCoordinate pos1, CarteseanCoordinate pos2)
      {
         return new CarteseanCoordinate(pos1.X - pos2.X, pos1.Y - pos2.Y, pos1.Z - pos2.Z);
      }

      public static CarteseanCoordinate operator +(CarteseanCoordinate pos1, CarteseanCoordinate pos2)
      {
         return new CarteseanCoordinate(pos1.X + pos2.X, pos1.Y + pos2.Y, pos1.Z + pos2.Z);
      }

      public override string ToString()
      {
         return string.Format("({0},{1})", _X, _Y);
      }
      #endregion
   }

}
