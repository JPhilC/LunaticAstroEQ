using System;
using ASCOM.DeviceInterface;
using ASCOM.LunaticAstroEQ.Core;
using ASCOM.LunaticAstroEQ.Core.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LunaticAstroEQ.Tests
{

   [TestClass]
   public class SlewTests
   {
      private AscomTools _NCPTools;
      private AscomTools _Tools;
      private DateTime _Now;
      private MountCoordinate _CelestialPolePosition;

      [TestInitialize]
      public void Initialize()
      {
         _Now = new DateTime(2019, 1, 25, 11, 47, 40);      // Gives an hourangle of approximately 20
         _Tools = new AscomTools();
         _Tools.Transform.SiteElevation = 192;
         _Tools.Transform.SiteLatitude = 52.667;
         _Tools.Transform.SiteLongitude = -1.333;
         _Tools.Transform.SiteTemperature = 15.0;
         _NCPTools = new AscomTools();
         _NCPTools.Transform.SiteElevation = 192;
         _NCPTools.Transform.SiteLatitude = 52.667;
         _NCPTools.Transform.SiteLongitude = -1.333;
         _NCPTools.Transform.SiteTemperature = 15.0;
         _CelestialPolePosition = new MountCoordinate(
            new AltAzCoordinate(_NCPTools.Transform.SiteLatitude, 0.0),
            new AxisPosition(0.0, 0.0),
            _NCPTools,
            _Now);
      }

      [TestCleanup]
      public void Cleanup()
      {
         _Tools.Dispose();
         _NCPTools.Dispose();
      }

      [TestMethod]
      public void GetNCPEquatorial()
      {
         EquatorialCoordinate currentRaDec = _CelestialPolePosition.Equatorial;
         Assert.AreEqual(2.19224, currentRaDec.RightAscension.Value, 0.00001, "RA Value");
         Assert.AreEqual(90.0, currentRaDec.Declination.Value, 0.001, "Declination value");
      }

      #region Cardinals on horizon ...
      [TestMethod]
      public void NorthHorizonAltAz()
      {
         EquatorialCoordinate target = _Tools.GetEquatorial(0.0, 0.0, _Now);
         MountCoordinate mount = new MountCoordinate(
               new AltAzCoordinate(_Tools.Transform.SiteLatitude, 0.0),
               new AxisPosition(0.0, 0.0),
               _Tools,
               _Now);
         Angle[] delta = mount.GetRADecSlewAnglesTo(target.RightAscension, target.Declination, _Tools);
         mount.MoveRADec(delta, _Tools, _Now);
         EquatorialCoordinate testRaDec = mount.Equatorial;
         Assert.AreEqual(PierSide.pierWest, mount.GetPointingSideOfPier(false), "Pointing side of pier");
         Assert.AreEqual(8.1922, testRaDec.RightAscension.Value, 0.00001, "RA Value");
         Assert.AreEqual(37.333, testRaDec.Declination.Value, 0.001, "Declination value");
         Assert.AreEqual(307.333, mount.ObservedAxes[1], 0.001, "Declination axis value");
      }

      [TestMethod]
      public void SouthHorizonAltAz()
      {
         EquatorialCoordinate target = _Tools.GetEquatorial(0.0, 180.0, _Now);
         MountCoordinate mount = new MountCoordinate(
               new AltAzCoordinate(_Tools.Transform.SiteLatitude, 0.0),
               new AxisPosition(0.0, 0.0),
               _Tools,
               _Now);
         Angle[] delta = mount.GetRADecSlewAnglesTo(target.RightAscension, target.Declination, _Tools);
         mount.MoveRADec(delta, _Tools, _Now);
         EquatorialCoordinate testRaDec = mount.Equatorial;
         Assert.AreEqual(PierSide.pierWest, mount.GetPointingSideOfPier(false), "Pointing side of pier");
         Assert.AreEqual(20.1922, testRaDec.RightAscension.Value, 0.00001, "RA Value");
         Assert.AreEqual(-37.333, testRaDec.Declination.Value, 0.001, "Declination value");
         Assert.AreEqual(127.333, mount.ObservedAxes[1], 0.001, "Declination axis value");
      }

      [TestMethod]
      public void EastHorizonAltAz()
      {
         EquatorialCoordinate target = _Tools.GetEquatorial(0.0, 90.0, _Now);
         MountCoordinate mount = new MountCoordinate(
               new AltAzCoordinate(_Tools.Transform.SiteLatitude, 0.0),
               new AxisPosition(0.0, 0.0),
               _Tools,
               _Now);
         Angle[] delta = mount.GetRADecSlewAnglesTo(target.RightAscension, target.Declination, _Tools);
         mount.MoveRADec(delta, _Tools, _Now);
         EquatorialCoordinate testRaDec = mount.Equatorial;
         Assert.AreEqual(PierSide.pierWest, mount.GetPointingSideOfPier(false), "Pointing side of pier");
         Assert.AreEqual(2.1922, testRaDec.RightAscension.Value, 0.00001, "RA Value");
         Assert.AreEqual(0.0, testRaDec.Declination.Value, 0.001, "Declination value");
         Assert.AreEqual(0.0, mount.ObservedAxes[1], 0.001, "Declination axis value");
      }

      [TestMethod]
      public void WestHorizonAltAz()
      {
         EquatorialCoordinate target = _Tools.GetEquatorial(0.0, 270.0, _Now);
         MountCoordinate mount = new MountCoordinate(
               new AltAzCoordinate(_Tools.Transform.SiteLatitude, 0.0),
               new AxisPosition(0.0, 0.0),
               _Tools,
               _Now);
         Angle[] delta = mount.GetRADecSlewAnglesTo(target.RightAscension, target.Declination, _Tools);
         mount.MoveRADec(delta, _Tools, _Now);
         EquatorialCoordinate testRaDec = mount.Equatorial;
         Assert.AreEqual(PierSide.pierEast, mount.GetPointingSideOfPier(false), "Pointing side of pier");
         Assert.AreEqual(14.1922, testRaDec.RightAscension.Value, 0.00001, "RA Value");
         Assert.AreEqual(0.0, testRaDec.Declination.Value, 0.001, "Declination value");
         Assert.AreEqual(270.0, mount.ObservedAxes[1], 0.001, "Declination axis value");
      }

      [TestMethod]
      public void NorthWestHorizonAltAz()
      {
         EquatorialCoordinate target = _Tools.GetEquatorial(0.0, 315.0, _Now);
         MountCoordinate mount = new MountCoordinate(
               new AltAzCoordinate(_Tools.Transform.SiteLatitude, 0.0),
               new AxisPosition(0.0, 0.0),
               _Tools,
               _Now);
         Angle[] delta = mount.GetRADecSlewAnglesTo(target.RightAscension, target.Declination, _Tools);
         mount.MoveRADec(delta, _Tools, _Now);
         EquatorialCoordinate testRaDec = mount.Equatorial;
         Assert.AreEqual(PierSide.pierEast, mount.GetPointingSideOfPier(false), "Pointing side of pier");
         Assert.AreEqual(11.62626, testRaDec.RightAscension.Value, 0.00001, "RA Value");
         Assert.AreEqual(25.39285, testRaDec.Declination.Value, 0.001, "Declination value");
         Assert.AreEqual(295.39285, mount.ObservedAxes[1], 0.001, "Declination axis value");
      }

      [TestMethod]
      public void SouthWestHorizonAltAz()
      {
         EquatorialCoordinate target = _Tools.GetEquatorial(0.0, 225.0, _Now);
         MountCoordinate mount = new MountCoordinate(
               new AltAzCoordinate(_Tools.Transform.SiteLatitude, 0.0),
               new AxisPosition(0.0, 0.0),
               _Tools,
               _Now);
         Angle[] delta = mount.GetRADecSlewAnglesTo(target.RightAscension, target.Declination, _Tools);
         mount.MoveRADec(delta, _Tools, _Now);
         EquatorialCoordinate testRaDec = mount.Equatorial;
         Assert.AreEqual(PierSide.pierEast, mount.GetPointingSideOfPier(false), "Pointing side of pier");
         Assert.AreEqual(16.75814, testRaDec.RightAscension.Value, 0.00001, "RA Value");
         Assert.AreEqual(-25.39285, testRaDec.Declination.Value, 0.001, "Declination value");
         Assert.AreEqual(244.60715, mount.ObservedAxes[1], 0.001, "Declination axis value");
      }

      [TestMethod]
      public void SouthEastHorizonAltAz()
      {
         EquatorialCoordinate target = _Tools.GetEquatorial(0.0, 135.0, _Now);
         MountCoordinate mount = new MountCoordinate(
               new AltAzCoordinate(_Tools.Transform.SiteLatitude, 0.0),
               new AxisPosition(0.0, 0.0),
               _Tools,
               _Now);
         Angle[] delta = mount.GetRADecSlewAnglesTo(target.RightAscension, target.Declination, _Tools);
         mount.MoveRADec(delta, _Tools, _Now);
         EquatorialCoordinate testRaDec = mount.Equatorial;
         Assert.AreEqual(PierSide.pierWest, mount.GetPointingSideOfPier(false), "Pointing side of pier");
         Assert.AreEqual(23.62626, testRaDec.RightAscension.Value, 0.00001, "RA Value");
         Assert.AreEqual(-25.39285, testRaDec.Declination.Value, 0.001, "Declination value");
         Assert.AreEqual(115.39285, mount.ObservedAxes[1], 0.001, "Declination axis value");
      }

      [TestMethod]
      public void NorthEastHorizonAltAz()
      {
         EquatorialCoordinate target = _Tools.GetEquatorial(0.0, 45.0, _Now);
         MountCoordinate mount = new MountCoordinate(
               new AltAzCoordinate(_Tools.Transform.SiteLatitude, 0.0),
               new AxisPosition(0.0, 0.0),
               _Tools,
               _Now);
         Angle[] delta = mount.GetRADecSlewAnglesTo(target.RightAscension, target.Declination, _Tools);
         mount.MoveRADec(delta, _Tools, _Now);
         EquatorialCoordinate testRaDec = mount.Equatorial;
         Assert.AreEqual(PierSide.pierWest, mount.GetPointingSideOfPier(false), "Pointing side of pier");
         Assert.AreEqual(4.75814, testRaDec.RightAscension.Value, 0.00001, "RA Value");
         Assert.AreEqual(25.39285, testRaDec.Declination.Value, 0.001, "Declination value");
         Assert.AreEqual(64.60715, mount.ObservedAxes[1], 0.001, "Declination axis value");
      }
      #endregion


      #region Slew tests ...
      [DataRow(30.0, 60.0)]
      [DataRow(120.0, -30.0)]
      [DataRow(180.0, -90.0)]
      [DataRow(240.0, -30.0)]
      [DataRow(270.0, 0.0)]
      [DataRow(-30.0, 60.0)]
      [DataRow(-120.0, -30.0)]
      [DataRow(-180.0, -90.0)]
      [DataRow(-240.0, -30.0)]
      [DataRow(-270.0, 0.0)]
      [DataTestMethod]
      public void SlewDec(double slewAngle, double expectedDec)
      {
         EquatorialCoordinate currentRaDec = new EquatorialCoordinate(_CelestialPolePosition.Equatorial.RightAscension.Value, _CelestialPolePosition.Equatorial.Declination.Value);
         MountCoordinate currentPosition = new MountCoordinate(currentRaDec, _CelestialPolePosition.ObservedAxes, _Tools, _Now);
         currentPosition.MoveRADec(new AxisPosition(0.0, slewAngle), _Tools, _Now);
         EquatorialCoordinate expected = new EquatorialCoordinate(currentPosition.Equatorial.RightAscension.Value, expectedDec);
         Assert.AreEqual(expected, currentPosition.Equatorial, "Slewed DEC test");
      }

      [DataRow(30.0, 2.0)]
      [DataRow(60.0, 4.0)]
      [DataRow(90.0, 6.0)]
      [DataRow(120.0, 8.0)]
      [DataRow(150.0, 10.0)]
      [DataRow(180.0, 12.0)]
      [DataRow(210.0, 14.0)]
      [DataRow(240.0, 16.0)]
      [DataRow(270.0, 18.0)]
      [DataRow(300.0, 220.0)]
      [DataRow(330.0, 22.0)]
      [DataRow(360.0, 0.0)]
      [DataTestMethod]
      public void SlewRA(double slewAngle, double expectedRA)
      {
         EquatorialCoordinate currentRaDec = new EquatorialCoordinate(_CelestialPolePosition.Equatorial.RightAscension.Value, _CelestialPolePosition.Equatorial.Declination.Value);
         MountCoordinate currentPosition = new MountCoordinate(currentRaDec, _CelestialPolePosition.ObservedAxes, _Tools, _Now);
         currentPosition.MoveRADec(new AxisPosition(slewAngle, 0.0), _Tools, _Now);
         EquatorialCoordinate expected = new EquatorialCoordinate(currentPosition.Equatorial.RightAscension.Value + expectedRA, 0.0);
         Assert.AreEqual(expected, currentPosition.Equatorial, "Slewed RA test");
      }


      [TestMethod]
      public void ASCOM_SOP()
      {
         EquatorialCoordinate currentRaDec = new EquatorialCoordinate(_CelestialPolePosition.Equatorial.RightAscension.Value, _CelestialPolePosition.Equatorial.Declination.Value);
         MountCoordinate currentPosition = new MountCoordinate(currentRaDec, _CelestialPolePosition.ObservedAxes, _Tools, _Now);
         double ha = currentPosition.LocalApparentSiderialTime;

         Angle[] slewDistance = currentPosition.GetRADecSlewAnglesTo(AstroConvert.RangeRA(ha - 3.0), 10.0, _Tools);
         System.Diagnostics.Debug.WriteLine($"{slewDistance[0]} / {slewDistance[1]}");
         currentPosition.MoveRADec(slewDistance, _Tools, _Now);
         Assert.AreEqual(PierSide.pierWest, currentPosition.GetPointingSideOfPier(false), "Point A");

         slewDistance = currentPosition.GetRADecSlewAnglesTo(AstroConvert.RangeRA(ha + 9.0), 60.0, _Tools);
         System.Diagnostics.Debug.WriteLine($"{slewDistance[0]} / {slewDistance[1]}");
         currentPosition.MoveRADec(slewDistance, _Tools, _Now);
         Assert.AreEqual(PierSide.pierEast, currentPosition.GetPointingSideOfPier(false), "Point B");

         slewDistance = currentPosition.GetRADecSlewAnglesTo(AstroConvert.RangeRA(ha - 9.0), 60.0, _Tools);
         System.Diagnostics.Debug.WriteLine($"{slewDistance[0]} / {slewDistance[1]}");
         currentPosition.MoveRADec(slewDistance, _Tools, _Now);
         Assert.AreEqual(PierSide.pierWest, currentPosition.GetPointingSideOfPier(false), "Point C");

         slewDistance = currentPosition.GetRADecSlewAnglesTo(AstroConvert.RangeRA(ha + 3.0), 10.0, _Tools);
         System.Diagnostics.Debug.WriteLine($"{slewDistance[0]} / {slewDistance[1]}");
         currentPosition.MoveRADec(slewDistance, _Tools, _Now);
         Assert.AreEqual(PierSide.pierEast, currentPosition.GetPointingSideOfPier(false), "Point D");

      }


      [TestMethod]
      public void CalculateSlewAngles()
      {
         EquatorialCoordinate currentRaDec = new EquatorialCoordinate(_CelestialPolePosition.Equatorial.RightAscension.Value, _CelestialPolePosition.Equatorial.Declination.Value);
         MountCoordinate currentPosition = new MountCoordinate(currentRaDec, _CelestialPolePosition.ObservedAxes, _Tools, _Now);
         double ha = currentPosition.LocalApparentSiderialTime;

         System.Diagnostics.Debug.WriteLine("\nPoint A (SE)");
         System.Diagnostics.Debug.WriteLine("============");
         AxisPosition targetAngles = currentPosition.GetAxisPositionForRADec(AstroConvert.RangeRA(ha-3.0), 10.0, _Tools);
         System.Diagnostics.Debug.WriteLine($"{targetAngles[0]} / {targetAngles[1]}");
         currentPosition.MoveRADec(targetAngles, _Tools, _Now);
         currentPosition.DumpDebugInfo();
         // Assert.AreEqual(PierSide.pierWest, currentPosition.PointingSideOfPier, "Point A");

         System.Diagnostics.Debug.WriteLine("\nPoint B (NW)");
         System.Diagnostics.Debug.WriteLine("============");
         targetAngles = currentPosition.GetAxisPositionForRADec(AstroConvert.RangeRA(ha+9.0), 60.0, _Tools);
         System.Diagnostics.Debug.WriteLine($"{targetAngles[0]} / {targetAngles[1]}");
         currentPosition.MoveRADec(targetAngles, _Tools, _Now);
         currentPosition.DumpDebugInfo();
         // Assert.AreEqual(PierSide.pierEast, currentPosition.PointingSideOfPier, "Point B");

         System.Diagnostics.Debug.WriteLine("\nPoint C (NE)");
         System.Diagnostics.Debug.WriteLine("============");
         targetAngles = currentPosition.GetAxisPositionForRADec(AstroConvert.RangeRA(ha-9.0), 60.0, _Tools);
         System.Diagnostics.Debug.WriteLine($"{targetAngles[0]} / {targetAngles[1]}");
         currentPosition.MoveRADec(targetAngles, _Tools, _Now);
         currentPosition.DumpDebugInfo();
         // Assert.AreEqual(PierSide.pierWest, currentPosition.PointingSideOfPier, "Point C");

         System.Diagnostics.Debug.WriteLine("\nPoint D (SW)");
         System.Diagnostics.Debug.WriteLine("============");
         targetAngles = currentPosition.GetAxisPositionForRADec(AstroConvert.RangeRA(ha+3.0), 10.0, _Tools);
         System.Diagnostics.Debug.WriteLine($"{targetAngles[0]} / {targetAngles[1]}");
         currentPosition.MoveRADec(targetAngles, _Tools, _Now);
         currentPosition.DumpDebugInfo();
         Assert.AreEqual(PierSide.pierEast, currentPosition.GetPointingSideOfPier(false), "Point D");

      }


      #endregion


   }
}

