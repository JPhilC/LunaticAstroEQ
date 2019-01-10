using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.LunaticAstroEQ.Core.Geometry
{
   /// <summary>
   /// A structure to represent an Altitude Azimuth coordinate
   /// </summary>
   public class AltAzCoordinate
   {
      private Angle _Alt;
      private Angle _Az;
      private double _X;
      private double _Y;
      private bool _Flag;
      public const double ALT_OFFSET = 180;  // Used to allow us to encode altitudes of when determing equivalent cartesean coordinates.

      public Angle Altitude
      {
         get
         {
            return _Alt;
         }
      }


      public Angle Azimuth
      {
         get
         {
            return _Az;
         }
      }

      /// <summary>
      /// Returns the cartesean X component of the coordinate
      /// Cos(Az)*Alt
      /// </summary>
      public double X
      {
         get
         {
            return _X;
         }
      }

      /// <summary>
      /// Returns the cartesean Y component of the coordinate
      /// Sin(Az)*Alt
      /// </summary>
      public double Y
      {
         get
         {
            return _Y;
         }
      }

      /// <summary>
      /// A flag to single a true of false.
      /// </summary>
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

      public AltAzCoordinate()
      {
         _Alt = new Angle(0.0);
         _Az = new Angle(0.0);
      }

      public AltAzCoordinate(string altitude, string azimuth)
      {

         _Alt = new Angle(altitude);
         _Az = new Angle(azimuth);
         _X = Math.Cos(_Az.Radians) * (_Alt + ALT_OFFSET);
         _Y = Math.Sin(_Az.Radians) * (_Alt + ALT_OFFSET);
         _Flag = false;
      }
      public AltAzCoordinate(double altitude,double azimuth):this()
      {
         if (azimuth < 0 || azimuth >= 360) {
            throw new ArgumentOutOfRangeException("Azimuth must be >= 0 and < 360");
         }
         if (altitude < -90 || altitude > 90) {
            throw new ArgumentOutOfRangeException("Altitude must be between -90 and 90.");
         }
         _Alt.Value = altitude;
         _Az.Value = azimuth;
         _X = Math.Cos(_Az.Radians) * (_Alt + ALT_OFFSET);
         _Y = Math.Sin(_Az.Radians) * (_Alt + ALT_OFFSET);
         _Flag = false;
      }

      public AltAzCoordinate(Angle altitude, Angle azimuth)
      {
         if (azimuth.Value < 0 || azimuth.Value >= 360) {
            throw new ArgumentOutOfRangeException("Azimuth must be >= 0 and < 360");
         }
         if (altitude.Value < -90 || altitude.Value > 90) {
            throw new ArgumentOutOfRangeException("Altitude must be between -90 and 90.");
         }
         _Alt = altitude;
         _Az = azimuth;
         _X = Math.Cos(_Az.Radians) * (_Alt + ALT_OFFSET);
         _Y = Math.Sin(_Az.Radians) * (_Alt + ALT_OFFSET);
         _Flag = false;
      }

      /// <summary>
      /// Index used during Affine transformations
      /// </summary>
      /// <param name="index"></param>
      /// <returns></returns>
      public double this[int index]
      {
         get
         {
            if (index < 0 || index > 1) {
               throw new ArgumentOutOfRangeException();
            }
            return (index == 0 ? _X : _Y);
         }
         //set
         //{
         //   if (index < 0 || index > 1) {
         //      throw new ArgumentOutOfRangeException();
         //   }
         //   if (index == 0) {
         //      _RAAxis.Radians = value;
         //   }
         //   else {
         //      _DecAxis.Radians = value;
         //   }
         //}
      }

      /// <summary>
      /// Compares the two specified sets of Axis positions.
      /// </summary>
      public static bool operator ==(AltAzCoordinate pos1, AltAzCoordinate pos2)
      {
         return (pos1.Altitude.Value == pos2.Altitude.Value && pos1.Azimuth.Value == pos2.Azimuth.Value);
      }

      public static bool operator !=(AltAzCoordinate pos1, AltAzCoordinate pos2)
      {
         return !(pos1 == pos2);
      }

      public override int GetHashCode()
      {
         unchecked // Overflow is fine, just wrap
         {
            int hash = 17;
            // Suitable nullity checks etc, of course :)
            hash = hash * 23 + _Az.GetHashCode();
            hash = hash * 23 + _Alt.GetHashCode();
            return hash;
         }
      }

      public override bool Equals(object obj)
      {
         return (obj is AltAzCoordinate
                 && this == (AltAzCoordinate)obj);
      }
      public static AltAzCoordinate operator -(AltAzCoordinate pos1, AltAzCoordinate pos2)
      {
         return new AltAzCoordinate(pos1.Altitude - pos2.Altitude, pos1.Azimuth - pos2.Azimuth);
      }

      public static AltAzCoordinate operator +(AltAzCoordinate pos1, AltAzCoordinate pos2)
      {
         return new AltAzCoordinate(pos1.Altitude + pos2.Altitude, pos1.Azimuth + pos2.Azimuth);
      }

      public override string ToString()
      {
         return string.Format("Alt/Az = {0}/{1}", 
            Altitude.ToString(AngularFormat.DegreesMinutesSeconds, false), 
            Azimuth.ToString(AngularFormat.DegreesMinutesSeconds, false));
      }

      /// <summary>
      /// Decodes an AzAlt Coordinate from it's cartesean equivalent
      /// Note: This method should ONLY be used to decode cartesean coordinates
      /// that were originally generated from an AzAltCoordinate of from values
      /// interpolated from those originally generated from AzAltCoordinates.
      /// </summary>
      /// <param name="x"></param>
      /// <param name="y"></param>
      /// <returns></returns>
      public static AltAzCoordinate FromCartesean(double x, double y)
      {
         double az = 0.0;
         double alt = Math.Sqrt((x * x) + (y * y));
         if (x > 0) {
            az = Math.Atan(y / x);
         }

         if (x < 0) {
            if (y >= 0) {
               az = Math.Atan(y / x) + Math.PI;
            }
            else {
               az = Math.Atan(y / x) - Math.PI;
            }
         }
         if (x == 0) {
            if (y > 0) {
               az = Math.PI / 2.0;
            }
            else {
               az = -1 * (Math.PI / 2.0);
            }
         }
         return new AltAzCoordinate((alt - ALT_OFFSET), AstroConvert.Range360(AstroConvert.RadToDeg(az)));
      }

      public CarteseanCoordinate ToCartesean()
      {
         double radius = this.Altitude + ALT_OFFSET;
         CarteseanCoordinate cartCoord = new CarteseanCoordinate(Math.Cos(this.Azimuth.Radians), Math.Sin(this.Azimuth.Radians), 1.0);
         return cartCoord;
      }

      /// <summary>
      /// Calculates the great circle distance to another point assuming
      /// both points at a given radial distance.
      /// </summary>
      /// <param name="toCoordinate"></param>
      /// <returns></returns>
      public double DistanceTo(AltAzCoordinate to, double radius)
      {
         /*
            Taken from: http://www.movable-type.co.uk/scripts/latlong.html
            Haversine
            formula:	a = sin²(Δφ/2) + cos φ1 ⋅ cos φ2 ⋅ sin²(Δλ/2)
            c = 2 ⋅ atan2( √a, √(1−a) )
            d = R ⋅ c
            where	φ is latitude, λ is longitude, R is earth’s radius (mean radius = 6,371km);
            note that angles need to be in radians to pass to trig functions!
            JavaScript:	
            var R = 6371e3; // metres
            var φ1 = lat1.toRadians();
            var φ2 = lat2.toRadians();
            var Δφ = (lat2-lat1).toRadians();
            var Δλ = (lon2-lon1).toRadians();

            var a = Math.sin(Δφ/2) * Math.sin(Δφ/2) +
                    Math.cos(φ1) * Math.cos(φ2) *
                    Math.sin(Δλ/2) * Math.sin(Δλ/2);
            var c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));

            var d = R * c;
          */
         double theta1 = this.Altitude.Radians;  // Equivalent to φ1
         double theta2 = to.Altitude.Radians;    // Equivalent to φ2
         double deltaTheta = theta2 - theta1; // eqivalent to Δφ
         double deltaGamma = to.Azimuth.Radians - this.Azimuth.Radians;    // equivalent to Δλ
         double a = Math.Pow(Math.Sin(deltaTheta / 2), 2) +
            Math.Cos(theta1) * Math.Cos(theta2) *
            Math.Pow(Math.Sin(deltaGamma), 2);
         double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
         double d = radius * c;
         return d;
      }

      /// <summary>
      /// Equirectangular approximation of distance between two points with a given
      /// radial distance.
      /// </summary>
      /// <param name="to"></param>
      /// <param name="radius"></param>
      /// <returns></returns>
      public double ApproxDistanceTo(AltAzCoordinate to, double radius)
      {
         /*
             Taken from: http://www.movable-type.co.uk/scripts/latlong.html
             Formula	x = Δλ ⋅ cos φm
             y = Δφ
             d = R ⋅ √x² + y²
             JavaScript:	
             var x = (λ2-λ1) * Math.cos((φ1+φ2)/2);
             var y = (φ2-φ1);
             var d = Math.sqrt(x*x + y*y) * R;         
         */
         double theta1 = this.Altitude.Radians;  // Equivalent to φ1
         double theta2 = to.Altitude.Radians;    // Equivalent to φ2
         double deltaTheta = theta2 - theta1; // eqivalent to Δφ
         double deltaGamma = to.Azimuth.Radians - this.Azimuth.Radians;    // equivalent to Δλ
         double x = deltaGamma * Math.Cos((theta1 + theta2) / 2);
         double d = Math.Sqrt(Math.Pow(x, 2) + Math.Pow(deltaTheta, 2)) * radius;
         return d;
      }

      /// <summary>
      /// Provides a quick calculation of a pseudo distance to another 
      /// point that can be used when find nearest positions.
      /// Based on Equirrectangular approximation but without the radius and Sqrt calculation.
      /// </summary>
      /// <param name="to"></param>
      /// <param name="radius"></param>
      /// <returns></returns>
      public double OrderingDistanceTo(AltAzCoordinate to)
      {
         /*
             Taken from: http://www.movable-type.co.uk/scripts/latlong.html
             Formula	x = Δλ ⋅ cos φm
             y = Δφ
             d = R ⋅ √x² + y²
             JavaScript:	
             var x = (λ2-λ1) * Math.cos((φ1+φ2)/2);
             var y = (φ2-φ1);
             var d = Math.sqrt(x*x + y*y) * R;         
         */
         double theta1 = this.Altitude.Radians;  // Equivalent to φ1
         double theta2 = to.Altitude.Radians;    // Equivalent to φ2
         double deltaTheta = theta2 - theta1; // eqivalent to Δφ
         double deltaGamma = to.Azimuth.Radians - this.Azimuth.Radians;    // equivalent to Δλ
         double x = deltaGamma * Math.Cos((theta1 + theta2) / 2);
         double d = Math.Pow(x, 2) + Math.Pow(deltaTheta, 2);
         return d;
      }

   }


}
