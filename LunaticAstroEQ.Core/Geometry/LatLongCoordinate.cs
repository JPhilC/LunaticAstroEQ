using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.LunaticAstroEQ.Core.Geometry
{
   /// <summary>
   /// A structure to represent an Latitude Longitude coordinate
   /// </summary>
   public class LatLongCoordinate
   {
      private Angle _Lat;
      private Angle _Long;
      public const double LAT_OFFSET = 180;  // Used to allow us to encode latitudes of when determing equivalent cartesean coordinates.

      public Angle Latitude
      {
         get
         {
            return _Lat;
         }
         private set
         {
            if (_Lat == value) {
               return;
            }
            _Lat = value;
         }
      }


      public Angle Longitude
      {
         get
         {
            return _Long;
         }
         private set
         {
            if (_Long == value)
            {
               return;
            }
         }
      }

      /// <summary>
      /// Returns the cartesean X component of the coordinate
      /// Cos(Long)*Lat
      /// </summary>
      [JsonIgnore]
      public double X
      {
         get
         {
            return Math.Cos(_Long.Radians) * (_Lat + LAT_OFFSET);
         }
      }

      /// <summary>
      /// Returns the cartesean Y component of the coordinate
      /// Sin(Long)*Lat
      /// </summary>
      [JsonIgnore]
      public double Y
      {
         get
         {
            return Math.Sin(_Long.Radians) * (_Lat + LAT_OFFSET);
         }
      }

      public LatLongCoordinate()
      {
         _Lat = new Angle(0.0);
         _Long = new Angle(0.0);
      }

      public LatLongCoordinate(string latitude, string longitude)
      {

         _Lat = new Angle(latitude);
         _Long = new Angle(longitude);
      }
      public LatLongCoordinate(double latitude,double longitude):this()
      {
         if (longitude < -180.0 || longitude > 180.0) {
            throw new ArgumentOutOfRangeException("Longitude must be >= -360 and < 360");
         }
         if (latitude < -90.0 || latitude > 90.0) {
            throw new ArgumentOutOfRangeException("Latitude must be between -90 and 90.");
         }
         _Lat.Value = latitude;
         _Long.Value = longitude;
      }

      public LatLongCoordinate(Angle latitude, Angle longitude)
      {
         if (longitude.Value < -360.0 || longitude.Value >= 360) {
            throw new ArgumentOutOfRangeException("Longitude must be >= 0 and < 360");
         }
         if (latitude.Value < -90 || latitude.Value > 90) {
            throw new ArgumentOutOfRangeException("Latitude must be between -90 and 90.");
         }
         _Lat = latitude;
         _Long = longitude;
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
            return (index == 0 ? X : Y);
         }
      }

      /// <summary>
      /// Compares the two specified sets of Axis positions.
      /// </summary>
      public static bool operator ==(LatLongCoordinate pos1, LatLongCoordinate pos2)
      {
         return (pos1.Latitude.Value == pos2.Latitude.Value && pos1.Longitude.Value == pos2.Longitude.Value);
      }

      public static bool operator !=(LatLongCoordinate pos1, LatLongCoordinate pos2)
      {
         return !(pos1 == pos2);
      }

      public override int GetHashCode()
      {
         unchecked // Overflow is fine, just wrap
         {
            int hash = 17;
            // Suitable nullity checks etc, of course :)
            hash = hash * 23 + _Long.GetHashCode();
            hash = hash * 23 + _Lat.GetHashCode();
            return hash;
         }
      }

      public override bool Equals(object obj)
      {
         return (obj is LatLongCoordinate
                 && this == (LatLongCoordinate)obj);
      }
      public static LatLongCoordinate operator -(LatLongCoordinate pos1, LatLongCoordinate pos2)
      {
         return new LatLongCoordinate(pos1.Latitude - pos2.Latitude, pos1.Longitude - pos2.Longitude);
      }

      public static LatLongCoordinate operator +(LatLongCoordinate pos1, LatLongCoordinate pos2)
      {
         return new LatLongCoordinate(pos1.Latitude + pos2.Latitude, pos1.Longitude + pos2.Longitude);
      }

      public override string ToString()
      {
         return string.Format("Lat/Long = {0}/{1}", 
            Latitude.ToString(AngularFormat.DegreesMinutesSeconds, false), 
            Longitude.ToString(AngularFormat.DegreesMinutesSeconds, false));
      }

      /// <summary>
      /// Decodes an LongLat Coordinate from it's cartesean equivalent
      /// Note: This method should ONLY be used to decode cartesean coordinates
      /// that were originally generated from an LongLatCoordinate of from values
      /// interpolated from those originally generated from LongLatCoordinates.
      /// </summary>
      /// <param name="x"></param>
      /// <param name="y"></param>
      /// <returns></returns>
      public static LatLongCoordinate FromCartesean(double x, double y)
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
         return new LatLongCoordinate((alt - LAT_OFFSET), AstroConvert.Range360(AstroConvert.RadToDeg(az)));
      }

      public CarteseanCoordinate ToCartesean()
      {
         double radius = this.Latitude + LAT_OFFSET;
         CarteseanCoordinate cartCoord = new CarteseanCoordinate(Math.Cos(this.Longitude.Radians), Math.Sin(this.Longitude.Radians), 1.0);
         return cartCoord;
      }

      /// <summary>
      /// Calculates the great circle distance to another point assuming
      /// both points at a given radial distance.
      /// </summary>
      /// <param name="toCoordinate"></param>
      /// <returns></returns>
      public double DistanceTo(LatLongCoordinate to, double radius)
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
         double theta1 = this.Latitude.Radians;  // Equivalent to φ1
         double theta2 = to.Latitude.Radians;    // Equivalent to φ2
         double deltaTheta = theta2 - theta1; // eqivalent to Δφ
         double deltaGamma = to.Longitude.Radians - this.Longitude.Radians;    // equivalent to Δλ
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
      public double ApproxDistanceTo(LatLongCoordinate to, double radius)
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
         double theta1 = this.Latitude.Radians;  // Equivalent to φ1
         double theta2 = to.Latitude.Radians;    // Equivalent to φ2
         double deltaTheta = theta2 - theta1; // eqivalent to Δφ
         double deltaGamma = to.Longitude.Radians - this.Longitude.Radians;    // equivalent to Δλ
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
      public double OrderingDistanceTo(LatLongCoordinate to)
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
         double theta1 = this.Latitude.Radians;  // Equivalent to φ1
         double theta2 = to.Latitude.Radians;    // Equivalent to φ2
         double deltaTheta = theta2 - theta1; // eqivalent to Δφ
         double deltaGamma = to.Longitude.Radians - this.Longitude.Radians;    // equivalent to Δλ
         double x = deltaGamma * Math.Cos((theta1 + theta2) / 2);
         double d = Math.Pow(x, 2) + Math.Pow(deltaTheta, 2);
         return d;
      }

   }


}
