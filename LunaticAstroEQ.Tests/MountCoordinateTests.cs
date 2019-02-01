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

      private AscomTools _Tools;
      private DateTime _Now;

      private DateTime _localTime = new DateTime(2019, 1, 8, 9, 47, 0);


      [TestInitialize]
      public void Initialize()
      {
         _Now = new DateTime(2019, 1, 25, 11, 47, 40);      // Gives an hourangle of approximately 20
         _Tools = new AscomTools();
         _Tools.Transform.SiteElevation = 192;
         _Tools.Transform.SiteLatitude = 52.667;
         _Tools.Transform.SiteLongitude = -1.333;
         _Tools.Transform.SiteTemperature = 15.0;
      }

      [TestCleanup]
      public void Cleanup()
      {
         _Tools.Dispose();
      }

      [TestMethod]
      public void MountCoordinateRAToAltAz()
      {
         //double localTimeZoneOffset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalHours;   // Taken from Util.GetTimeZoneOffset()
         //DateTime testTime = _localTime.AddHours(localTimeZoneOffset).AddSeconds(Constants.UTIL_LOCAL2JULIAN_TIME_CORRECTION);     // Fix for daylight saving 0.2 seconds
         //using (AscomTools tools = new AscomTools()) {
         //   Angle longitude = new Angle("-1°20'20.54\"");
         //   double last = AstroConvert.LocalApparentSiderealTime(longitude, testTime);
         //   double myJulian = AstroConvert.DateLocalToJulianUTC(_localTime);
         //   System.Diagnostics.Debug.WriteLine(string.Format("Local Sidereal Time = {0}, Expecting 20:44:51.7", tools.Util.HoursToHMS(last, ":", ":", "", 1)));
         //   tools.Transform.SiteLatitude = new Angle("52°40'6.38\"");
         //   tools.Transform.SiteLongitude = longitude;
         //   tools.Transform.SiteElevation = 175.0;
         //   MountCoordinate deneb = new MountCoordinate(new EquatorialCoordinate("+20h42m5.66s", "+45°21'03.8\""), new AltAzCoordinate("+52°39'10.5\"", "+77°33'0.8\""));
         //   AltAzCoordinate suggestedAltAz = deneb.UpdateAltAzimuth(tools, _localTime);

         //   System.Diagnostics.Debug.WriteLine(string.Format("{0} (Suggested), Expecting {1}",
         //      suggestedAltAz,
         //      deneb.AltAzimuth));
         //   double tolerance = 5.0 / 3600; // 5 seconds.
         //   bool testResult = ((Math.Abs(suggestedAltAz.Altitude.Value - deneb.AltAzimuth.Altitude.Value) < tolerance)
         //         && (Math.Abs(suggestedAltAz.Azimuth.Value - deneb.AltAzimuth.Azimuth.Value) < tolerance));
         //   Assert.IsTrue(testResult);
         //}
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


      [TestMethod]
      public void TestGetAngleFromDecDegrees()
      {
         EquatorialCoordinate target = _Tools.GetEquatorial(0.0, 0.0, _Now);
         MountCoordinate mount = new MountCoordinate(
               new AltAzCoordinate(_Tools.Transform.SiteLatitude, 0.0),
               new AxisPosition(0.0, 0.0),
               _Tools,
               _Now);
         System.Diagnostics.Debug.WriteLine("DEC\t\t\tAngle\t\tAngle flipped");
         System.Diagnostics.Debug.WriteLine("===\t\t\t=====\t\t=============");
         for (double dec = -90.0; dec <= 90.0; dec = dec + 10)
         {
            System.Diagnostics.Debug.WriteLine($"{dec}\t\t\t{mount.GetAngleFromDecDegrees(dec, false)}\t\t\t{mount.GetAngleFromDecDegrees(dec, true)}");
         }
         Assert.IsTrue(true);
      }


      [TestMethod]
      public void TestGetAngleFromHours()
      {
         EquatorialCoordinate target = _Tools.GetEquatorial(0.0, 0.0, _Now);
         MountCoordinate mount = new MountCoordinate(
               new AltAzCoordinate(_Tools.Transform.SiteLatitude, 0.0),
               new AxisPosition(0.0, 0.0),
               _Tools,
               _Now);
         System.Diagnostics.Debug.WriteLine("RA\t\t\tAngle");
         System.Diagnostics.Debug.WriteLine("===\t\t\t=====");
         for (double ra = 0.0; ra < 24.0; ra++)
         {
            System.Diagnostics.Debug.WriteLine($"{ra}\t\t\t{mount.GetAngleFromHours(ra)}");
         }
         Assert.IsTrue(true);
      }

      [TestMethod]
      public void TestGetAxisPositionForRA()
      {
         EquatorialCoordinate target = _Tools.GetEquatorial(0.0, 0.0, _Now);
         MountCoordinate mount = new MountCoordinate(
               new AltAzCoordinate(_Tools.Transform.SiteLatitude, 0.0),
               new AxisPosition(0.0, 0.0),
               _Tools,
               _Now);
         System.Diagnostics.Debug.WriteLine("RA\t\t\tAxis1");
         System.Diagnostics.Debug.WriteLine("==\t\t\t=====");
         for (double ra = -0.0; ra < 24.0; ra++)
         {
            System.Diagnostics.Debug.WriteLine($"{ra}\t\t\t{mount.GetAxisPositionForRA(ra, 0.0)}");
         }
         Assert.IsTrue(true);
      }

      [TestMethod]
      public void TestGetAxisPositionForDec()
      {
         EquatorialCoordinate target = _Tools.GetEquatorial(0.0, 0.0, _Now);
         MountCoordinate mount = new MountCoordinate(
               new AltAzCoordinate(_Tools.Transform.SiteLatitude, 0.0),
               new AxisPosition(0.0, 0.0),
               _Tools,
               _Now);
         System.Diagnostics.Debug.WriteLine("DEC\t\t\tAxis\t\tAxis flipped");
         System.Diagnostics.Debug.WriteLine("===\t\t\t=====\t\t=============");
         for (double dec = -90.0; dec <= 90.0; dec = dec + 10)
         {
            System.Diagnostics.Debug.WriteLine($"{dec}\t\t\t{mount.GetAxisPositionForDec(dec, false)}\t\t\t{mount.GetAxisPositionForDec(dec, true)}");
         }
         Assert.IsTrue(true);
      }


      [TestMethod]
      public void TestCalculateTargetAxis()
      {
         EquatorialCoordinate target = _Tools.GetEquatorial(0.0, 0.0, _Now);
         MountCoordinate mount = new MountCoordinate(
               new AltAzCoordinate(_Tools.Transform.SiteLatitude, 0.0),
               new AxisPosition(0.0, 0.0),
               _Tools,
               _Now);
         System.Diagnostics.Debug.Write($"\nDEC");
         for (double dec = 90.0; dec >= -90; dec = dec - 15.0)
         {
            System.Diagnostics.Debug.Write($"\t{dec}");
         }
         System.Diagnostics.Debug.Write($"\nRA");
         for (double ra = 0.0; ra < 24.0; ra = ra + 1)
         {
            System.Diagnostics.Debug.Write($"\n{ra}");
            for (double dec = 90.0; dec >= -90; dec = dec - 15.0)
            {
               AxisPosition axisPosition = mount.CalculateTargetAxes(ra, dec, _Tools);
               System.Diagnostics.Debug.Write($"\t{Math.Round(axisPosition.RAAxis, 0)}/{Math.Round(axisPosition.DecAxis, 0)}");
            }
         }
         System.Diagnostics.Debug.WriteLine("");
         Assert.IsTrue(true);
      }

      [TestMethod]
      public void TestRAAdjustment()
      {
         EquatorialCoordinate target = _Tools.GetEquatorial(0.0, 0.0, _Now);
         MountCoordinate mount = new MountCoordinate(
               new AltAzCoordinate(_Tools.Transform.SiteLatitude, 0.0),
               new AxisPosition(0.0, 0.0),
               _Tools,
               _Now);
         //System.Diagnostics.Debug.Write($"\nDEC");
         //for (double dec = 90.0; dec >= -90; dec = dec - 15.0)
         //{
         //   System.Diagnostics.Debug.Write($"\t{dec}");
         //}
         //System.Diagnostics.Debug.Write($"\nRA");
         for (double ra = 0.0; ra < 24.0; ra = ra + 1)
         {
            // System.Diagnostics.Debug.Write($"\n{ra}");
            //for (double dec = 90.0; dec >= -90; dec = dec - 15.0)
            //{
               mount.TestCalculateTargetAxes(ra, 90.0, _Tools);
            //}
         }
         System.Diagnostics.Debug.WriteLine("");
         Assert.IsTrue(true);
      }


   }
}
