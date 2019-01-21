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
         _RA.Value = AstroConvert.RangeRA(rightAscension);
         _Dec.Value = AstroConvert.RangeDEC(declination);
      }


      public EquatorialCoordinate(HourAngle rightAscension, Angle declination):this()   // , Angle longitude, DateTime observedTime)
      {
         _RA.Value = AstroConvert.RangeRA(rightAscension.Value);
         _Dec = AstroConvert.RangeDEC(declination.Value);
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

      public AxisPosition GetRelativeAxisPositionOf(EquatorialCoordinate target)
      {
         double newRa = target.RightAscension.Radians - this.RightAscension.Radians;
         double newDec = target.Declination.Radians - this.Declination.Radians;
         System.Diagnostics.Debug.WriteLine($"New Dec = {Angle.RadiansToDegrees(newDec)} degrees");
         AxisPosition targetAxisPosition = new AxisPosition(newRa, newDec);
         return targetAxisPosition;
      }

   }

}
