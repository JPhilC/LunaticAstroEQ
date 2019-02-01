using System;
using ASCOM.LunaticAstroEQ.Core.Geometry;
using ASCOM.Astrometry.Transform;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ASCOM.LunaticAstroEQ.Core;

namespace LunaticAstroEQ.Tests
{
   [TestClass]
   public class TakiTests
   {
      readonly DateTime _localTime = new DateTime(2017, 3, 17, 9, 9, 38);
      [TestMethod]
      public void TakiExample5_4_4()
      {
         using (AscomTools tools = new AscomTools())
         {
            Angle longitude = new Angle("-1°20'20.54\"");
            tools.Transform.SiteLatitude = new Angle("52°40'6.38\"");
            tools.Transform.SiteLongitude = longitude;
            tools.Transform.SiteElevation = 175.5;
            DateTime initialTime = new DateTime(2017, 03, 28, 21, 0, 0);
            DateTime observationTime = new DateTime(2017, 03, 28, 21, 27, 56);
            MountCoordinate star1 = new MountCoordinate(new EquatorialCoordinate("0h7m54.0s", "29.038°"), new AxisPosition(1.732239, 1.463808, true), tools, observationTime);
            observationTime = new DateTime(2017, 03, 28, 21, 37, 02);
            MountCoordinate star2 = new MountCoordinate(new EquatorialCoordinate("2h21m45.0s", "89.222°"), new AxisPosition(5.427625, 0.611563, true), tools, observationTime);
            TakiEQMountMapper taki = new TakiEQMountMapper(star1, star2, initialTime);

            EquatorialCoordinate bCet = new EquatorialCoordinate("0h43m07s", "-18.038°");
            DateTime targetTime = new DateTime(2017, 03, 28, 21, 52, 12);
            AxisPosition bCetExpected = new AxisPosition(2.27695654215, 0.657465529226, true);  // 130.46°, 37.67°
            AxisPosition bCetCalculated = taki.GetAxisPosition(bCet, targetTime);

            System.Diagnostics.Debug.WriteLine("Expected: {0}, calculated: {1}", bCetExpected, bCetCalculated);

            double tolerance = 0.5; // degrees.
            bool testResult = ((Math.Abs(bCetExpected.DecAxis - bCetCalculated.DecAxis) < tolerance)
                  && (Math.Abs(bCetExpected.RAAxis - bCetCalculated.RAAxis) < tolerance));
            Assert.IsTrue(testResult);
         }
      }

      [TestMethod]
      public void GetTheoreticalFromEquatorial()
      {
         using (AscomTools tools = new AscomTools())
         {
            Angle longitude = new Angle("-1°20'20.54\"");
            tools.Transform.SiteLatitude = new Angle("52°40'6.38\"");
            tools.Transform.SiteLongitude = longitude;
            tools.Transform.SiteElevation = 175.5;
            MountCoordinate mirphac = new MountCoordinate(new EquatorialCoordinate("3h25m34.77s", "49°55'12.0\""), new AxisPosition(1.04551212078025, 0.882804566344625, true), tools, _localTime);
            MountCoordinate almaak = new MountCoordinate(new EquatorialCoordinate("2h04m58.83s", "42°24'41.1\""), new AxisPosition(0.597795712351665, 0.817146830684098, true), tools, _localTime);
            MountCoordinate ruchbah = new MountCoordinate(new EquatorialCoordinate("1h26m58.39s", "60°19'33.3\""), new AxisPosition(0.506260233480349, 1.09753088667021, true), tools, _localTime);
            TakiEQMountMapper taki = new TakiEQMountMapper(mirphac, almaak, ruchbah, _localTime);
            EquatorialCoordinate gPer = new EquatorialCoordinate("2h03m28.89s", "54°34'10.9\"");
            AxisPosition gPerExpected = new AxisPosition(0.649384407012042, 0.998796900509728, true);
            AxisPosition gPerCalculated = taki.GetAxisPosition(gPer, _localTime);
            System.Diagnostics.Debug.WriteLine("Calculated: {0}, expected: {1}", gPerExpected, gPerCalculated);
            double tolerance = 0.25; // degrees.
            bool testResult = ((Math.Abs(gPerExpected.DecAxis - gPerCalculated.DecAxis) < tolerance)
                  && (Math.Abs(gPerExpected.RAAxis - gPerCalculated.RAAxis) < tolerance));

            Assert.IsTrue(testResult);
         }
      }

      //[TestMethod]
      //public void AlignmentTest()
      //{
      //   MountCoordinate mirphac = new MountCoordinate(
      //      new EquatorialCoordinate("3h25m34.77s", "49°55'12.0\""),
      //      new AxisPosition(0.897009787, 0.871268363));
      //   mirphac.SetObservedAxis(new AxisPosition(0.8884478, 0.9392852), _localTime);

      //   MountCoordinate almaak = new MountCoordinate(
      //      new EquatorialCoordinate("2h04m58.83s", "42°24'41.1\""),
      //      new AxisPosition(0.545291764, 0.740218861));
      //   almaak.SetObservedAxis(new AxisPosition(0.5515027, 0.7739144), _localTime);

      //   MountCoordinate ruchbah = new MountCoordinate(
      //      new EquatorialCoordinate("1h26m58.39s", "60°19'33.3\""),
      //      new AxisPosition(0.37949203, 1.05288587));
      //   ruchbah.SetObservedAxis(new AxisPosition(0.37949203, 1.0685469), _localTime);

      //   TakiAlignmentMapper taki = new TakiAlignmentMapper(mirphac, almaak, ruchbah);
      //   AxisPosition gPerTheoretical = new AxisPosition(0.538789685, 0.95242084);

      //   AxisPosition gPerExpected = new AxisPosition(0.523934, 0.9844184);
      //   AxisPosition gPerCalculated = taki.GetObservedPosition(gPerTheoretical);
      //   Assert.AreEqual(gPerExpected, gPerCalculated);
      //}

   }

}
