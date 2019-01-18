using System;
using ASCOM.LunaticAstroEQ.Core;
using ASCOM.LunaticAstroEQ.Core.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LunaticAstroEQ.Tests
{
   [TestClass]
   public class AscomUtilityTests
   {
      [TestMethod]
      public void RawTransformTest()
      {
         using (AscomTools _AscomTools = new AscomTools())
         {
            double siteLongitude = -1.333333;
            double siteLatitude = 52.666667;
            _AscomTools.Transform.SiteLatitude = siteLatitude;
            _AscomTools.Transform.SiteLongitude = siteLongitude;
            _AscomTools.Transform.SiteElevation = 192;
            DateTime testTime = new DateTime(2019, 1, 18, 21, 11, 00);
            double lst = AstroConvert.LocalApparentSiderealTime(siteLongitude, testTime);
            _AscomTools.Transform.SetAzimuthElevation(0.0, siteLatitude);
            _AscomTools.Transform.JulianDateTT = _AscomTools.Util.DateLocalToJulian(testTime);
            double ra = _AscomTools.Transform.RATopocentric;
            double dec = _AscomTools.Transform.DECTopocentric;
            double alt = _AscomTools.Transform.ElevationTopocentric;
            double az = _AscomTools.Transform.AzimuthTopocentric;
            double lstExpected = 4.95996448153762;
            double raExpected = 10.9439120745406;
            double decExpected = 89.9999999983757;
            double azExpected = 8.03515690758855E-09;
            double altExpected = 52.6666669999972;
            Assert.AreEqual(lstExpected, lst, 0.001, "LST test failed");
            Assert.AreEqual(raExpected, ra, 0.0001, "RA test failed");
            Assert.AreEqual(decExpected, dec, 0.0001, "Dec test failed");
            Assert.AreEqual(azExpected, az, 0.0001, "Az test failed");
            Assert.AreEqual(altExpected, alt, 0.0001, "Alt test failed");
         }
      }

      [TestMethod]
      public void MountCoordinateTest()
      {
         using (AscomTools _AscomTools = new AscomTools())
         {
            double siteLongitude = -1.333333;
            double siteLatitude = 52.666667;
            _AscomTools.Transform.SiteLatitude = siteLatitude;
            _AscomTools.Transform.SiteLongitude = siteLongitude;
            _AscomTools.Transform.SiteElevation = 192;
            DateTime testTime = new DateTime(2019, 1, 18, 21, 11, 00);
            AltAzCoordinate altAzPosition = new AltAzCoordinate(_AscomTools.Transform.SiteLatitude, 0.0);
            MountCoordinate currentPosition = new MountCoordinate(altAzPosition, _AscomTools, testTime);
            double lst = currentPosition.LocalApparentSiderialTime;
            double ra = currentPosition.Equatorial.RightAscension.Value;
            double dec = currentPosition.Equatorial.Declination.Value;
            double alt = currentPosition.AltAzimuth.Altitude.Value;
            double az = currentPosition.AltAzimuth.Azimuth.Value;
            double lstExpected = 4.95996448153762;
            double raExpected = 10.9439120745406;
            double decExpected = 89.9999999983757;
            double azExpected = 8.03515690758855E-09;
            double altExpected = 52.6666669999972;
            Assert.AreEqual(lstExpected, lst, 0.001, "LST test failed");
            Assert.AreEqual(raExpected, ra, 0.0001, "RA test failed");
            Assert.AreEqual(decExpected, dec, 0.0001, "Dec test failed");
            Assert.AreEqual(azExpected, az, 0.0001, "Az test failed");
            Assert.AreEqual(altExpected, alt, 0.0001, "Alt test failed");
         }
      }
   }
}
