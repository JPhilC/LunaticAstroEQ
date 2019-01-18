using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ASCOM.Utilities;
using ASCOM.Astrometry.Transform;
using ASCOM.LunaticAstroEQ.Core.Geometry;
using ASCOM.LunaticAstroEQ.Core;

namespace LunaticAstroEQ.Tests
{
   [TestClass]
   public class MountCoordinateTests
   {
      private DateTime _localTime = new DateTime(2019, 1, 8, 9, 47, 0);
      [TestMethod]
      public void MountCoordinateRAToAltAz()
      {
         double localTimeZoneOffset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalHours;   // Taken from Util.GetTimeZoneOffset()
         DateTime testTime = _localTime.AddHours(localTimeZoneOffset).AddSeconds(Constants.UTIL_LOCAL2JULIAN_TIME_CORRECTION);     // Fix for daylight saving 0.2 seconds
         using (AscomTools tools = new AscomTools()) {
            Angle longitude = new Angle("-1°20'20.54\"");
            double last = AstroConvert.LocalApparentSiderealTime(longitude, testTime);
            double myJulian = AstroConvert.DateLocalToJulianUTC(_localTime);
            System.Diagnostics.Debug.WriteLine(string.Format("Local Sidereal Time = {0}, Expecting 20:44:51.7", tools.Util.HoursToHMS(last, ":", ":", "", 1)));
            tools.Transform.SiteLatitude = new Angle("52°40'6.38\"");
            tools.Transform.SiteLongitude = longitude;
            tools.Transform.SiteElevation = 175.0;
            MountCoordinate deneb = new MountCoordinate("+20h42m5.66s", "+45°21'03.8\"")
            {
               AltAzimuth = new AltAzCoordinate("+52°39'10.5\"", "+77°33'0.8\"")
            };
            AltAzCoordinate suggestedAltAz = deneb.GetAltAzimuth(tools, _localTime);

            System.Diagnostics.Debug.WriteLine(string.Format("{0} (Suggested), Expecting {1}",
               suggestedAltAz,
               deneb.AltAzimuth));
            double tolerance = 5.0 / 3600; // 5 seconds.
            bool testResult = ((Math.Abs(suggestedAltAz.Altitude.Value - deneb.AltAzimuth.Altitude.Value) < tolerance)
                  && (Math.Abs(suggestedAltAz.Azimuth.Value - deneb.AltAzimuth.Azimuth.Value) < tolerance));
            Assert.IsTrue(testResult);
         }
      }

      [TestMethod]
      public void TransformExample()
      {
         DateTime localTime = new DateTime(2019, 1, 8, 9, 47, 0);

         using (Util util = new Util())
         using (Transform transform = new Transform())
         {
            transform.SiteLatitude = 51.0;
            transform.SiteLongitude = -1.0; // West longitudes are negative, east are positive
            transform.SiteElevation = 80;
            transform.SiteTemperature = 15.0;
            DateTime testTime = localTime.ToUniversalTime();

            transform.JulianDateUTC = util.DateUTCToJulian(testTime);

            // Set Arcturus J2000 co-ordinates and read off the corresponding Topocentric co-ordinates 
            transform.SetJ2000(util.HMSToHours("14:15:38.37"), util.DMSToDegrees("19:10:14.8"));
            // transform.SetApparent(util.HMSToHours("14:16:32.66"), util.DMSToDegrees("19:04:56.7"));
            // transform.SetTopocentric(util.HMSToHours("14:16:32.66"), util.DMSToDegrees("19:04:56.7"));
            System.Diagnostics.Debug.WriteLine($"RA Topo: {util.HoursToHMS(transform.RATopocentric, ":", ":", "", 3)} DEC Topo: {util.DegreesToDMS(transform.DECTopocentric, ":", ":", "", 3)}");
            System.Diagnostics.Debug.WriteLine($"RA App: {util.HoursToHMS(transform.RAApparent, ":", ":", "", 3)} DEC App: {util.DegreesToDMS(transform.DECApparent, ":", ":", "", 3)}");
            System.Diagnostics.Debug.WriteLine($"Az Topo: {util.DegreesToDMS(transform.AzimuthTopocentric, ":", ":", "", 3)} Alt Topo: {util.DegreesToDMS(transform.ElevationTopocentric, ":", ":", "", 3)}");

         }
      }
   }
}
