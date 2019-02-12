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
      public void LASTTest()
      {
         DateTime now = DateTime.Now;
         MountCoordinate mount = new MountCoordinate(new AxisPosition(0.0, 0.0), _Tools, now);
         double ASCOMLast = (18.697374558 + 24.065709824419081 * (_Tools.Util.DateLocalToJulian(now) - 2451545.0) + (_Tools.Transform.SiteLongitude / 15.0)) % 24.0;
         Assert.AreEqual(ASCOMLast, mount.LocalApparentSiderialTime.Value, 1/3600.00);

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





      [DataRow(0, -90, 329.919511756115, 180, false)]
      [DataRow(0, -60, 329.919511756115, 210, false)]
      [DataRow(0, -30, 329.919511756115, 240, false)]
      [DataRow(0, 0, 329.919511756115, 270, false)]
      [DataRow(0, 30, 329.919511756115, 300, false)]
      [DataRow(0, 60, 329.919511756115, 330, false)]
      [DataRow(0, 90, 329.919511756115, 0, false)]
      [DataRow(3, -90, 14.9195117561147, 180, false)]
      [DataRow(3, -60, 14.9195117561147, 210, false)]
      [DataRow(3, -30, 14.9195117561147, 240, false)]
      [DataRow(3, 0, 14.9195117561147, 270, false)]
      [DataRow(3, 30, 14.9195117561147, 300, false)]
      [DataRow(3, 60, 14.9195117561147, 330, false)]
      [DataRow(3, 90, 14.9195117561147, 0, false)]
      [DataRow(6, -90, 59.9195117561147, 180, false)]
      [DataRow(6, -60, 59.9195117561147, 210, false)]
      [DataRow(6, -30, 59.9195117561147, 240, false)]
      [DataRow(6, 0, 59.9195117561147, 270, false)]
      [DataRow(6, 30, 59.9195117561147, 300, false)]
      [DataRow(6, 60, 59.9195117561147, 330, false)]
      [DataRow(6, 90, 59.9195117561147, 0, false)]
      [DataRow(9, -90, 284.919511756115, 180, true)]
      [DataRow(9, -60, 284.919511756115, 150, true)]
      [DataRow(9, -30, 284.919511756115, 120, true)]
      [DataRow(9, 0, 284.919511756115, 90, true)]
      [DataRow(9, 30, 284.919511756115, 60, true)]
      [DataRow(9, 60, 284.919511756115, 30, true)]
      [DataRow(9, 90, 284.919511756115, 0, true)]
      [DataRow(12, -90, 329.919511756115, 180, true)]
      [DataRow(12, -60, 329.919511756115, 150, true)]
      [DataRow(12, -30, 329.919511756115, 120, true)]
      [DataRow(12, 0, 329.919511756115, 90, true)]
      [DataRow(12, 30, 329.919511756115, 60, true)]
      [DataRow(12, 60, 329.919511756115, 30, true)]
      [DataRow(12, 90, 329.919511756115, 0, true)]
      [DataRow(15, -90, 14.9195117561147, 180, true)]
      [DataRow(15, -60, 14.9195117561147, 150, true)]
      [DataRow(15, -30, 14.9195117561147, 120, true)]
      [DataRow(15, 0, 14.9195117561147, 90, true)]
      [DataRow(15, 30, 14.9195117561147, 60, true)]
      [DataRow(15, 60, 14.9195117561147, 30, true)]
      [DataRow(15, 90, 14.9195117561147, 0, true)]
      [DataRow(18, -90, 59.9195117561147, 180, true)]
      [DataRow(18, -60, 59.9195117561147, 150, true)]
      [DataRow(18, -30, 59.9195117561147, 120, true)]
      [DataRow(18, 0, 59.9195117561147, 90, true)]
      [DataRow(18, 30, 59.9195117561147, 60, true)]
      [DataRow(18, 60, 59.9195117561147, 30, true)]
      [DataRow(18, 90, 59.9195117561147, 0, true)]
      [DataRow(21, -90, 284.919511756115, 180, false)]
      [DataRow(21, -60, 284.919511756115, 210, false)]
      [DataRow(21, -30, 284.919511756115, 240, false)]
      [DataRow(21, 0, 284.919511756115, 270, false)]
      [DataRow(21, 30, 284.919511756115, 300, false)]
      [DataRow(21, 60, 284.919511756115, 330, false)]
      [DataRow(21, 90, 284.919511756115, 0, false)]
      [DataRow(8, 90, 89.9195117561147, 0, false)]
      [DataRow(8, 45, 89.9195117561147, 315, false)]
      [DataRow(8, 0, 89.9195117561147, 270, false)]
      [DataRow(8, -45, 89.9195117561147, 225, false)]
      [DataRow(8, -90, 89.9195117561147, 180, false)]
      [DataRow(9, 90, 284.919511756115, 0, true)]
      [DataRow(9, 45, 284.919511756115, 45, true)]
      [DataRow(9, 0, 284.919511756115, 90, true)]
      [DataRow(9, -45, 284.919511756115, 135, true)]
      [DataRow(9, -90, 284.919511756115, 180, true)]
      [DataRow(14, 90, 359.919511756115, 0, true)]
      [DataRow(14, 45, 359.919511756115, 45, true)]
      [DataRow(14, 0, 359.919511756115, 90, true)]
      [DataRow(14, -45, 359.919511756115, 135, true)]
      [DataRow(14, -90, 359.919511756115, 180, true)]
      [DataRow(21, 90, 284.919511756115, 0, false)]
      [DataRow(21, 45, 284.919511756115, 315, false)]
      [DataRow(21, 0, 284.919511756115, 270, false)]
      [DataRow(21, -45, 284.919511756115, 225, false)]
      [DataRow(21, -90, 284.919511756115, 180, false)]
      // ASCOM_SOP Tests
      [DataRow(17, 15, 44.9195117561147, 75, true)]     // Point A (SE)
      [DataRow(5, 60, 44.9195117561147, 330, false)]     // Point B (NW)
      [DataRow(11, 60, 314.919511756115, 30, true)]     // Point C (NE)
      [DataRow(23, 15, 314.919511756115, 285, false)]     // Point D (SW)
      [DataTestMethod]
      public void TestCalculateTargetAxis(double targetRa, double targetDec, double expectedAxisRa, double expectedAxisDec, bool decFlipped)
      {
         EquatorialCoordinate target = _Tools.GetEquatorial(0.0, 0.0, _Now);
         MountCoordinate mount = new MountCoordinate(new AxisPosition(0.0, 0.0), _Tools, _Now);
         AxisPosition axisPosition = mount.GetAxisPositionForRADec(targetRa, targetDec, _Tools);
         Assert.AreEqual(expectedAxisRa, axisPosition.RAAxis, 0.000001, "RA Axis");
         Assert.AreEqual(expectedAxisDec, axisPosition.DecAxis, 0.000001, "Dec Axis");
         Assert.AreEqual(decFlipped, axisPosition.DecFlipped, "Dec flipped");

      }


      [DataRow(0, -90, 329.919511756115, 180, false)]
      [DataRow(0, -60, 329.919511756115, 210, false)]
      [DataRow(0, -30, 329.919511756115, 240, false)]
      [DataRow(0, 0, 329.919511756115, 270, false)]
      [DataRow(0, 30, 329.919511756115, 300, false)]
      [DataRow(0, 60, 329.919511756115, 330, false)]
      [DataRow(0, 90, 329.919511756115, 0, false)]
      [DataRow(3, -90, 14.9195117561147, 180, false)]
      [DataRow(3, -60, 14.9195117561147, 210, false)]
      [DataRow(3, -30, 14.9195117561147, 240, false)]
      [DataRow(3, 0, 14.9195117561147, 270, false)]
      [DataRow(3, 30, 14.9195117561147, 300, false)]
      [DataRow(3, 60, 14.9195117561147, 330, false)]
      [DataRow(3, 90, 14.9195117561147, 0, false)]
      [DataRow(6, -90, 59.9195117561147, 180, false)]
      [DataRow(6, -60, 59.9195117561147, 210, false)]
      [DataRow(6, -30, 59.9195117561147, 240, false)]
      [DataRow(6, 0, 59.9195117561147, 270, false)]
      [DataRow(6, 30, 59.9195117561147, 300, false)]
      [DataRow(6, 60, 59.9195117561147, 330, false)]
      [DataRow(6, 90, 59.9195117561147, 0, false)]
      [DataRow(9, -90, 284.919511756115, 180, true)]
      [DataRow(9, -60, 284.919511756115, 150, true)]
      [DataRow(9, -30, 284.919511756115, 120, true)]
      [DataRow(9, 0, 284.919511756115, 90, true)]
      [DataRow(9, 30, 284.919511756115, 60, true)]
      [DataRow(9, 60, 284.919511756115, 30, true)]
      [DataRow(9, 90, 284.919511756115, 0, true)]
      [DataRow(12, -90, 329.919511756115, 180, true)]
      [DataRow(12, -60, 329.919511756115, 150, true)]
      [DataRow(12, -30, 329.919511756115, 120, true)]
      [DataRow(12, 0, 329.919511756115, 90, true)]
      [DataRow(12, 30, 329.919511756115, 60, true)]
      [DataRow(12, 60, 329.919511756115, 30, true)]
      [DataRow(12, 90, 329.919511756115, 0, true)]
      [DataRow(15, -90, 14.9195117561147, 180, true)]
      [DataRow(15, -60, 14.9195117561147, 150, true)]
      [DataRow(15, -30, 14.9195117561147, 120, true)]
      [DataRow(15, 0, 14.9195117561147, 90, true)]
      [DataRow(15, 30, 14.9195117561147, 60, true)]
      [DataRow(15, 60, 14.9195117561147, 30, true)]
      [DataRow(15, 90, 14.9195117561147, 0, true)]
      [DataRow(18, -90, 59.9195117561147, 180, true)]
      [DataRow(18, -60, 59.9195117561147, 150, true)]
      [DataRow(18, -30, 59.9195117561147, 120, true)]
      [DataRow(18, 0, 59.9195117561147, 90, true)]
      [DataRow(18, 30, 59.9195117561147, 60, true)]
      [DataRow(18, 60, 59.9195117561147, 30, true)]
      [DataRow(18, 90, 59.9195117561147, 0, true)]
      [DataRow(21, -90, 284.919511756115, 180, false)]
      [DataRow(21, -60, 284.919511756115, 210, false)]
      [DataRow(21, -30, 284.919511756115, 240, false)]
      [DataRow(21, 0, 284.919511756115, 270, false)]
      [DataRow(21, 30, 284.919511756115, 300, false)]
      [DataRow(21, 60, 284.919511756115, 330, false)]
      [DataRow(21, 90, 284.919511756115, 0, false)]
      [DataRow(8, 90, 89.9195117561147, 0, false)]
      [DataRow(8, 45, 89.9195117561147, 315, false)]
      [DataRow(8, 0, 89.9195117561147, 270, false)]
      [DataRow(8, -45, 89.9195117561147, 225, false)]
      [DataRow(8, -90, 89.9195117561147, 180, false)]
      [DataRow(9, 90, 284.919511756115, 0, true)]
      [DataRow(9, 45, 284.919511756115, 45, true)]
      [DataRow(9, 0, 284.919511756115, 90, true)]
      [DataRow(9, -45, 284.919511756115, 135, true)]
      [DataRow(9, -90, 284.919511756115, 180, true)]
      [DataRow(14, 90, 359.919511756115, 0, true)]
      [DataRow(14, 45, 359.919511756115, 45, true)]
      [DataRow(14, 0, 359.919511756115, 90, true)]
      [DataRow(14, -45, 359.919511756115, 135, true)]
      [DataRow(14, -90, 359.919511756115, 180, true)]
      [DataRow(21, 90, 284.919511756115, 0, false)]
      [DataRow(21, 45, 284.919511756115, 315, false)]
      [DataRow(21, 0, 284.919511756115, 270, false)]
      [DataRow(21, -45, 284.919511756115, 225, false)]
      [DataRow(21, -90, 284.919511756115, 180, false)]
      // ASCOM_SOP Tests
      [DataRow(17, 15, 44.9195117561147, 75, true)]     // Point A (SE)
      [DataRow(5, 60, 44.9195117561147, 330, false)]     // Point B (NW)
      [DataRow(11, 60, 314.919511756115, 30, true)]     // Point C (NE)
      [DataRow(23, 15, 314.919511756115, 285, false)]     // Point D (SW)
      [DataTestMethod]
      public void TestGetRA(double expectedRa, double expectedDec, double RAAxis, double DecAxis, bool flippedDec)
      {
         EquatorialCoordinate target = _Tools.GetEquatorial(0.0, 0.0, _Now);
         MountCoordinate mount = new MountCoordinate(
               new AltAzCoordinate(_Tools.Transform.SiteLatitude, 0.0),
               new AxisPosition(0.0, 0.0),
               _Tools,
               _Now);
         AxisPosition axisPosition = new AxisPosition(RAAxis, DecAxis, flippedDec);
         System.Diagnostics.Debug.Write($"\nFlipped:{(flippedDec ? "Y" : "N")} Expecting: {expectedRa} ->");
         HourAngle testRa = mount.GetRA(axisPosition);
         //if (Math.Abs(expectedRa-testRa)> 0.000001)
         //{
         //   System.Diagnostics.Debug.Write(" - FAIL");
         //}
         Assert.AreEqual(expectedRa, testRa, 0.000001, "RA Value");
      }

      [DataRow(0, -90, 329.919511756115, 180, false)]
      [DataRow(0, -60, 329.919511756115, 210, false)]
      [DataRow(0, -30, 329.919511756115, 240, false)]
      [DataRow(0, 0, 329.919511756115, 270, false)]
      [DataRow(0, 30, 329.919511756115, 300, false)]
      [DataRow(0, 60, 329.919511756115, 330, false)]
      [DataRow(0, 90, 329.919511756115, 0, false)]
      [DataRow(3, -90, 14.9195117561147, 180, false)]
      [DataRow(3, -60, 14.9195117561147, 210, false)]
      [DataRow(3, -30, 14.9195117561147, 240, false)]
      [DataRow(3, 0, 14.9195117561147, 270, false)]
      [DataRow(3, 30, 14.9195117561147, 300, false)]
      [DataRow(3, 60, 14.9195117561147, 330, false)]
      [DataRow(3, 90, 14.9195117561147, 0, false)]
      [DataRow(6, -90, 59.9195117561147, 180, false)]
      [DataRow(6, -60, 59.9195117561147, 210, false)]
      [DataRow(6, -30, 59.9195117561147, 240, false)]
      [DataRow(6, 0, 59.9195117561147, 270, false)]
      [DataRow(6, 30, 59.9195117561147, 300, false)]
      [DataRow(6, 60, 59.9195117561147, 330, false)]
      [DataRow(6, 90, 59.9195117561147, 0, false)]
      [DataRow(9, -90, 284.919511756115, 180, true)]
      [DataRow(9, -60, 284.919511756115, 150, true)]
      [DataRow(9, -30, 284.919511756115, 120, true)]
      [DataRow(9, 0, 284.919511756115, 90, true)]
      [DataRow(9, 30, 284.919511756115, 60, true)]
      [DataRow(9, 60, 284.919511756115, 30, true)]
      [DataRow(9, 90, 284.919511756115, 0, true)]
      [DataRow(12, -90, 329.919511756115, 180, true)]
      [DataRow(12, -60, 329.919511756115, 150, true)]
      [DataRow(12, -30, 329.919511756115, 120, true)]
      [DataRow(12, 0, 329.919511756115, 90, true)]
      [DataRow(12, 30, 329.919511756115, 60, true)]
      [DataRow(12, 60, 329.919511756115, 30, true)]
      [DataRow(12, 90, 329.919511756115, 0, true)]
      [DataRow(15, -90, 14.9195117561147, 180, true)]
      [DataRow(15, -60, 14.9195117561147, 150, true)]
      [DataRow(15, -30, 14.9195117561147, 120, true)]
      [DataRow(15, 0, 14.9195117561147, 90, true)]
      [DataRow(15, 30, 14.9195117561147, 60, true)]
      [DataRow(15, 60, 14.9195117561147, 30, true)]
      [DataRow(15, 90, 14.9195117561147, 0, true)]
      [DataRow(18, -90, 59.9195117561147, 180, true)]
      [DataRow(18, -60, 59.9195117561147, 150, true)]
      [DataRow(18, -30, 59.9195117561147, 120, true)]
      [DataRow(18, 0, 59.9195117561147, 90, true)]
      [DataRow(18, 30, 59.9195117561147, 60, true)]
      [DataRow(18, 60, 59.9195117561147, 30, true)]
      [DataRow(18, 90, 59.9195117561147, 0, true)]
      [DataRow(21, -90, 284.919511756115, 180, false)]
      [DataRow(21, -60, 284.919511756115, 210, false)]
      [DataRow(21, -30, 284.919511756115, 240, false)]
      [DataRow(21, 0, 284.919511756115, 270, false)]
      [DataRow(21, 30, 284.919511756115, 300, false)]
      [DataRow(21, 60, 284.919511756115, 330, false)]
      [DataRow(21, 90, 284.919511756115, 0, false)]
      [DataRow(8, 90, 89.9195117561147, 0, false)]
      [DataRow(8, 45, 89.9195117561147, 315, false)]
      [DataRow(8, 0, 89.9195117561147, 270, false)]
      [DataRow(8, -45, 89.9195117561147, 225, false)]
      [DataRow(8, -90, 89.9195117561147, 180, false)]
      [DataRow(9, 90, 284.919511756115, 0, true)]
      [DataRow(9, 45, 284.919511756115, 45, true)]
      [DataRow(9, 0, 284.919511756115, 90, true)]
      [DataRow(9, -45, 284.919511756115, 135, true)]
      [DataRow(9, -90, 284.919511756115, 180, true)]
      [DataRow(14, 90, 359.919511756115, 0, true)]
      [DataRow(14, 45, 359.919511756115, 45, true)]
      [DataRow(14, 0, 359.919511756115, 90, true)]
      [DataRow(14, -45, 359.919511756115, 135, true)]
      [DataRow(14, -90, 359.919511756115, 180, true)]
      [DataRow(21, 90, 284.919511756115, 0, false)]
      [DataRow(21, 45, 284.919511756115, 315, false)]
      [DataRow(21, 0, 284.919511756115, 270, false)]
      [DataRow(21, -45, 284.919511756115, 225, false)]
      [DataRow(21, -90, 284.919511756115, 180, false)]
      // ASCOM_SOP Tests
      [DataRow(17, 15, 44.9195117561147, 75, true)]     // Point A (SE)
      [DataRow(5, 60, 44.9195117561147, 330, false)]     // Point B (NW)
      [DataRow(11, 60, 314.919511756115, 30, true)]     // Point C (NE)
      [DataRow(23, 15, 314.919511756115, 285, false)]     // Point D (SW)
      [DataTestMethod]
      public void TestGetDec(double expectedRA, double expectedDec, double RAAxis, double DecAxis, bool decFlipped)
      {
         EquatorialCoordinate target = _Tools.GetEquatorial(0.0, 0.0, _Now);
         MountCoordinate mount = new MountCoordinate(
               new AltAzCoordinate(_Tools.Transform.SiteLatitude, 0.0),
               new AxisPosition(0.0, 0.0),
               _Tools,
               _Now);
         AxisPosition axisPosition = new AxisPosition(RAAxis, DecAxis);
         Angle testDec = mount.GetDec(axisPosition);
         Assert.AreEqual(expectedDec, testDec, 0.000001, "Dec Value");
      }

      /// <summary>
      /// Not really a test method. Just used to generate test input data.
      /// </summary>
      [TestMethod]
      public void GenerateTestData()
      {
         MountCoordinate mount = new MountCoordinate(
               new AltAzCoordinate(_Tools.Transform.SiteLatitude, 0.0),
               new AxisPosition(0.0, 0.0),
               _Tools,
               _Now);
         AxisPosition axes = mount.GetAxisPositionForRADec(17.0, 15, _Tools);

         for (double ra = 0.0; ra < 24.0; ra = ra + 3.0)
         {
            for (double dec = -90.0; dec <= 90.0; dec = dec + 30.0)
            {
               axes = mount.GetAxisPositionForRADec(ra, dec, _Tools);
               System.Diagnostics.Debug.WriteLine($"[DataRow({ra}, {dec}, {axes.RAAxis.Value}, {axes.DecAxis.Value}, {(axes.DecFlipped ? "true":"false")})]");
            }
         }

         foreach (double ra in new double[] { 8.0, 9.0, 14.0, 21.0 })
         {
            foreach (double dec in new double[] { 90.0, 45.0, 0.0, -45.0, -90.0 })
            {
               axes = mount.GetAxisPositionForRADec(ra, dec, _Tools);
               System.Diagnostics.Debug.WriteLine($"[DataRow({ra}, {dec}, {axes.RAAxis.Value}, {axes.DecAxis.Value}, {(axes.DecFlipped ? "true" : "false")})]");
            }
         }

         System.Diagnostics.Debug.WriteLine("// ASCOM_SOP Tests");

         double sopRA = 23.0;
         double sopDec = 15.0;
         axes = mount.GetAxisPositionForRADec(sopRA, sopDec, _Tools);
         System.Diagnostics.Debug.WriteLine($"[DataRow({sopRA}, {sopDec}, {axes.RAAxis.Value}, {axes.DecAxis.Value}, {(axes.DecFlipped ? "true" : "false")})]     // Point A (SE)");

         sopRA = 11.0;
         sopDec = 60.0;
         axes = mount.GetAxisPositionForRADec(sopRA, sopDec, _Tools);
         System.Diagnostics.Debug.WriteLine($"[DataRow({sopRA}, {sopDec}, {axes.RAAxis.Value}, {axes.DecAxis.Value}, {(axes.DecFlipped ? "true" : "false")})]     // Point B (NW)");

         sopRA = 5.0;
         sopDec = 60.0;
         axes = mount.GetAxisPositionForRADec(sopRA, sopDec, _Tools);
         System.Diagnostics.Debug.WriteLine($"[DataRow({sopRA}, {sopDec}, {axes.RAAxis.Value}, {axes.DecAxis.Value}, {(axes.DecFlipped ? "true" : "false")})]     // Point C (NE)");

         sopRA = 17.0;
         sopDec = 15.0;
         axes = mount.GetAxisPositionForRADec(sopRA, sopDec, _Tools);
         System.Diagnostics.Debug.WriteLine($"[DataRow({sopRA}, {sopDec}, {axes.RAAxis.Value}, {axes.DecAxis.Value}, {(axes.DecFlipped ? "true" : "false")})]     // Point D (SW)");

         Assert.IsTrue(true);
      }

   }
}
