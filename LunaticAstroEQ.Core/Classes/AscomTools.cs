using System;
using ASCOM.Astrometry.AstroUtils;
using ASCOM.Astrometry.Transform;
using ASCOM.LunaticAstroEQ.Core.Geometry;
using ASCOM.Utilities;

namespace ASCOM.LunaticAstroEQ.Core
{
   public class AscomTools : IDisposable
   {
      private Transform _Transform;
      public Transform Transform
      {
         get
         {
            return _Transform;
         }
      }

      private Util _Util;
      public Util Util
      {
         get
         {
            return _Util;
         }
      }

      /// <summary>
      /// Returns the local julian time corrected for daylight saving and a minor adjustment
      /// to get a more accurate result when converting from RA/Dec to AltAz using the Transform instance.
      /// </summary>
      public double LocalJulianTimeUTC
      {
         get
         {
            double localTimeZoneOffset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalHours;   // Taken from Util.GetTimeZoneOffset()
            DateTime testTime = DateTime.Now.AddHours(localTimeZoneOffset).AddSeconds(Constants.UTIL_LOCAL2JULIAN_TIME_CORRECTION);     // Fix for daylight saving 0.2 seconds
            return this._Util.DateLocalToJulian(testTime);
         }
      }

      private AstroUtils _AstroUtils;
      public AstroUtils AstroUtils
      {
         get
         {
            return _AstroUtils;
         }
      }

      ///// <summary>
      ///// Returns the local julian time corrected for daylight saving and a minor adjustment
      ///// to get a more accurate result when converting from RA/Dec to AltAz using the Transform instance.
      ///// </summary>
      //public double LocalJulianTimeUTC
      //{
      //   get
      //   {
      //      double localTimeZoneOffset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalHours;   // Taken from Util.GetTimeZoneOffset()
      //      DateTime testTime = DateTime.Now.AddHours(localTimeZoneOffset).AddSeconds(Constants.UTIL_LOCAL2JULIAN_TIME_CORRECTION);     // Fix for daylight saving 0.2 seconds
      //      return this._Util.DateLocalToJulian(testTime);
      //   }
      //}

      public AscomTools()
      {
         _Util = new Util();
         _Transform = new Transform();
         _AstroUtils = new AstroUtils();
      }

      public void Dispose()
      {
         Dispose(true);
         GC.SuppressFinalize(this);
      }

      protected virtual void Dispose(bool disposing)
      {
         if (disposing)
         {
            if (_Util != null)
            {
               _Util.Dispose();
               _Util = null;
            }
            if (_Transform != null)
            {
               _Transform.Dispose();
               _Transform = null;
            }
            if (_AstroUtils != null)
            {
               _AstroUtils.Dispose();
               _AstroUtils = null;
            }
         }
      }

      public AltAzCoordinate GetAltAz(double rightAscension, double declination, DateTime when)
      {
         _Transform.JulianDateTT = _Util.DateLocalToJulian(when);
         _Transform.SetTopocentric(rightAscension, declination);
         return new AltAzCoordinate(_Transform.ElevationTopocentric, AstroConvert.RangeAzimuth(_Transform.AzimuthTopocentric));

      }

      public EquatorialCoordinate GetEquatorial(double altitude, double azimuth, DateTime when)
      {
         _Transform.JulianDateTT = _Util.DateLocalToJulian(when);
         _Transform.SetAzimuthElevation(azimuth, altitude);
         return new EquatorialCoordinate(_Transform.RATopocentric, _Transform.DECTopocentric);
      }
   }
}
