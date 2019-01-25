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
      // Equality tolerances for RA and DEC
      private const double DefaultDegreesDelta = 0.000001D;
      private HourAngle _RA;
      private Angle _DeclinationAxis;

      public HourAngle RightAscension
      {
         get
         {
            return _RA;
         }
      }

      /// <summary>
      /// Note the declination is held in the range 0-360.
      /// </summary>
      public Angle Declination
      {
         get
         {
            return new Angle(AstroConvert.RangeDEC(_DeclinationAxis));
         }
      }


      /// <summary>
      /// Declination axis position.
      /// </summary>
      public Angle DeclinationAxis
      {
         get
         {
            return _DeclinationAxis;
         }
      }

      public EquatorialCoordinate()   // , double longitude, DateTime observedTime)
      {
         _RA = new HourAngle(0.0);
         _DeclinationAxis = new Angle(0.0);
      }

      public EquatorialCoordinate(double rightAscension, double declinationAxis) 
      {
         _RA = new HourAngle(rightAscension);
         _DeclinationAxis = new Angle(Angle.Range360(declinationAxis));
      }



      public EquatorialCoordinate(HourAngle rightAscension, Angle declinationAxis):this()   // , Angle longitude, DateTime observedTime)
      {
         _RA.Value = new HourAngle(AstroConvert.RangeRA(rightAscension));
         _DeclinationAxis = new Angle(Angle.Range360(declinationAxis));
      }

      #region Operator overloads ...
      /// <summary>
      /// Compares the two specified sets of Axis positions.
      /// </summary>
      public static bool operator ==(EquatorialCoordinate pos1, EquatorialCoordinate pos2)
      {
         if (System.Object.ReferenceEquals(pos2, pos1))
         {
            return true;
         }

         // If one is null, but not both, return false.
         if (((object)pos1 == null) || ((object)pos2 == null))
         {
            return false;
         }
         double deltaRA = Math.Abs(pos2.RightAscension.Value - pos1.RightAscension.Value);
         double deltaDec = Math.Abs(pos2.DeclinationAxis.Value - pos1.DeclinationAxis.Value);

         return ( deltaRA< DefaultDegreesDelta && deltaDec < DefaultDegreesDelta);
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
            hash = hash * 23 + _DeclinationAxis.GetHashCode();
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
         return new EquatorialCoordinate(pos1.RightAscension - pos2.RightAscension, pos1.DeclinationAxis - pos2.DeclinationAxis);
      }

      public static EquatorialCoordinate operator +(EquatorialCoordinate pos1, EquatorialCoordinate pos2)
      {
         return new EquatorialCoordinate(pos1.RightAscension + pos2.RightAscension, pos1.DeclinationAxis + pos2.DeclinationAxis);
      }


      public override string ToString()
      {
         return string.Format("{0}/{1}", RightAscension, Declination);
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
            cartCoord = new CarteseanCoordinate(this.RightAscension.Radians, this.DeclinationAxis.Radians, 1.0);
         }
         return cartCoord;
      }

      public double[] GetAxisOffsetTo(EquatorialCoordinate target)
      {
         double deltaRa = HourAngle.HoursToDegrees(target.RightAscension) - HourAngle.HoursToDegrees(this.RightAscension);
         double deltaDec = target.DeclinationAxis - this.DeclinationAxis;
         return new double[]{ deltaRa, deltaDec};
      }

      /// <summary>
      /// Return an new equatorual coordinate with the RA and Dec eqivalent to moving the 
      /// current axes by amounts in a double array element 0 = RA, 1 = dec.
      /// </summary>
      /// <param name="delta">Slew distance in degrees</param>
      /// <returns></returns>
      public EquatorialCoordinate SlewBy(double[] delta)
      {
         double deltaRa = HourAngle.DegreesToHours(delta[0]);
         double newRa = HourAngle.Range24(RightAscension.Value + deltaRa);
         return new EquatorialCoordinate(newRa, DeclinationAxis.Value + delta[1]);
      }

   }

}
