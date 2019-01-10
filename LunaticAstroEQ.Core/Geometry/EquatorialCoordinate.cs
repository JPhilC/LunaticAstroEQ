using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.LunaticAstroEQ.Core.Geometry
{
   /// <summary>
   /// A structure to represent an EquatorialCoordinate
   /// </summary>
   public class EquatorialCoordinate
   {
      private HourAngle _RA;
      private Angle _Dec;
      //private DateTime _ObservedWhen;
      //private Angle _Longitude;

      public HourAngle RightAscension
      {
         get
         {
            return _RA;
         }
      }
      public Angle Declination
      {
         get
         {
            return _Dec;
         }
      }

      public EquatorialCoordinate()   // , double longitude, DateTime observedTime)
      {
         _RA = new HourAngle(0.0);
         _Dec = new Angle(0.0);
      }

      public EquatorialCoordinate(double rightAscension, double declination):this()   // , double longitude, DateTime observedTime)
      {
         if (rightAscension < 0 || rightAscension > 24.0) { throw new ArgumentOutOfRangeException("Right Ascension must be between 0 and 24."); }
         if (declination < -90 || declination > 90) { throw new ArgumentOutOfRangeException("Declination must be between -90 and 90."); }
         _RA.Value = rightAscension;
         _Dec.Value = declination;
      }


      public EquatorialCoordinate(HourAngle rightAscension, Angle declination)    // , Angle longitude, DateTime observedTime)
      {
         if (rightAscension.Value < 0 || rightAscension.Value > 24.0) { throw new ArgumentOutOfRangeException("Right Ascension must be between 0 and 24."); }
         if (declination.Value < -90 || declination.Value > 90) { throw new ArgumentOutOfRangeException("Declination must be between -90 and 90."); }
         _RA = rightAscension;
         _Dec = declination;
      }

      #region Operator overloads ...
      /// <summary>
      /// Compares the two specified sets of Axis positions.
      /// </summary>
      public static bool operator ==(EquatorialCoordinate pos1, EquatorialCoordinate pos2)
      {
         return (pos1.RightAscension.Value == pos2.RightAscension.Value && pos1.Declination.Value == pos2.Declination.Value);
      }

      public static bool operator !=(EquatorialCoordinate pos1, EquatorialCoordinate pos2)
      {
         return !(pos1 == pos2);
      }

      public override int GetHashCode()
      {
         unchecked // Overflow is fine, just wrap
         {
            int hash = 17;
            // Suitable nullity checks etc, of course :)
            hash = hash * 23 + _RA.GetHashCode();
            hash = hash * 23 + _Dec.GetHashCode();
            return hash;
         }
      }

      public override bool Equals(object obj)
      {
         return (obj is EquatorialCoordinate
                 && this == (EquatorialCoordinate)obj);
      }

      public static EquatorialCoordinate operator -(EquatorialCoordinate pos1, EquatorialCoordinate pos2)
      {
         return new EquatorialCoordinate(pos1.RightAscension - pos2.RightAscension, pos1.Declination - pos2.Declination);
      }

      public static EquatorialCoordinate operator +(EquatorialCoordinate pos1, EquatorialCoordinate pos2)
      {
         return new EquatorialCoordinate(pos1.RightAscension + pos2.RightAscension, pos1.Declination + pos2.Declination);
      }


      public override string ToString()
      {
         return string.Format("{0}/{1}", _RA, _Dec);
      }
      #endregion

      public CarteseanCoordinate ToCartesean(Angle latitude, bool affineTaki = true)
      {
         CarteseanCoordinate cartCoord;
         if (affineTaki) {
            // Get Polar (or should than be get AltAzimuth) from Equatorial coordinate (formerly call to EQ_SphericalPolar)
            AltAzCoordinate polar = AstroConvert.GetAltAz(this, latitude);
            // Get  Cartesean from Polar (formerly call to EQ_Polar2Cartes)
            cartCoord = polar.ToCartesean();
         }
         else {
            cartCoord = new CarteseanCoordinate(this.RightAscension.Radians, this.Declination.Radians, 1.0);
         }
         return cartCoord;
      }


   }

}
