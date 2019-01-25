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
         _Now = new DateTime(2019, 1, 25, 12, 0, 0);
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
      }

      [TestMethod]
      public void GetNCPEquatorial()
      {
         EquatorialCoordinate currentRaDec = _CelestialPolePosition.Equatorial;
         Assert.AreEqual(2.19224, currentRaDec.RightAscension.Value, 0.00001, "RA Value");
         Assert.AreEqual(90.0, currentRaDec.Declination.Value, 0.001, "Declination value");
         Assert.AreEqual(0.0, currentRaDec.DeclinationAxis.Value, 0.001, "Declination axis value");
      }

      #region Cardinals on horizon ...
      [TestMethod]
      public void NorthHorizonAltAz()
      {
         // Equivalent of ASCOM SOP conformance point A
         MountCoordinate mount = new MountCoordinate(
            new AltAzCoordinate(0.0, 0.0),
            _Tools,
            _Now);
         EquatorialCoordinate testRaDec = mount.Equatorial;
         Assert.AreEqual(PierSide.pierWest, mount.PointingSideOfPier, "Pointing side of pier");
         Assert.AreEqual(8.1922, testRaDec.RightAscension.Value, 0.00001, "RA Value");
         Assert.AreEqual(37.333, testRaDec.Declination.Value, 0.001, "Declination value");
         Assert.AreEqual(307.333, testRaDec.DeclinationAxis.Value, 0.001, "Declination axis value");
      }

      [TestMethod]
      public void SouthHorizonAltAz()
      {
         // Equivalent of ASCOM SOP conformance point A
         MountCoordinate mount = new MountCoordinate(
            new AltAzCoordinate(0.0, 180.0),
            _Tools,
            _Now);
         EquatorialCoordinate testRaDec = mount.Equatorial;
         Assert.AreEqual(PierSide.pierWest, mount.PointingSideOfPier, "Pointing side of pier");
         Assert.AreEqual(20.1922, testRaDec.RightAscension.Value, 0.00001, "RA Value");
         Assert.AreEqual(-37.333, testRaDec.Declination.Value, 0.001, "Declination value");
         Assert.AreEqual(127.333, testRaDec.DeclinationAxis.Value, 0.001, "Declination axis value");
      }

      [TestMethod]
      public void EastHorizonAltAz()
      {
         // Equivalent of ASCOM SOP conformance point A
         MountCoordinate mount = new MountCoordinate(
            new AltAzCoordinate(0.0, 90.0),
            _Tools,
            _Now);
         EquatorialCoordinate testRaDec = mount.Equatorial;
         Assert.AreEqual(PierSide.pierWest, mount.PointingSideOfPier, "Pointing side of pier");
         Assert.AreEqual(2.1922, testRaDec.RightAscension.Value, 0.00001, "RA Value");
         Assert.AreEqual(0.0, testRaDec.Declination.Value, 0.001, "Declination value");
         Assert.AreEqual(90.0, testRaDec.DeclinationAxis.Value, 0.001, "Declination axis value");
      }

      [TestMethod]
      public void WestHorizonAltAz()
      {
         // Equivalent of ASCOM SOP conformance point A
         MountCoordinate mount = new MountCoordinate(
            new AltAzCoordinate(0.0, 270.0),
            new AxisPosition(30.0, 270.0),
            _Tools,
            _Now);
         EquatorialCoordinate testRaDec = mount.Equatorial;
         Assert.AreEqual(PierSide.pierEast, mount.PointingSideOfPier, "Pointing side of pier");
         Assert.AreEqual(14.1922, testRaDec.RightAscension.Value, 0.00001, "RA Value");
         Assert.AreEqual(0.0, testRaDec.Declination.Value, 0.001, "Declination value");
         Assert.AreEqual(270.0, testRaDec.DeclinationAxis.Value, 0.001, "Declination axis value");
      }

      [TestMethod]
      public void NorthWestHorizonAltAz()
      {
         // Equivalent of ASCOM SOP conformance point A
         MountCoordinate mount = new MountCoordinate(
            new AltAzCoordinate(0.0, 315.0),
            new AxisPosition(30.0, 300.0),
            _Tools,
            _Now);
         EquatorialCoordinate testRaDec = mount.Equatorial;
         Assert.AreEqual(PierSide.pierEast, mount.PointingSideOfPier, "Pointing side of pier");
         Assert.AreEqual(11.62626, testRaDec.RightAscension.Value, 0.00001, "RA Value");
         Assert.AreEqual(25.39285, testRaDec.Declination.Value, 0.001, "Declination value");
         Assert.AreEqual(295.39285, testRaDec.DeclinationAxis.Value, 0.001, "Declination axis value");
      }

      [TestMethod]
      public void SouthWestHorizonAltAz()
      {
         // Equivalent of ASCOM SOP conformance point A
         MountCoordinate mount = new MountCoordinate(
            new AltAzCoordinate(0.0, 225.0),
            new AxisPosition(330.0, 240.0),
            _Tools,
            _Now);
         EquatorialCoordinate testRaDec = mount.Equatorial;
         Assert.AreEqual(PierSide.pierEast, mount.PointingSideOfPier, "Pointing side of pier");
         Assert.AreEqual(16.75814, testRaDec.RightAscension.Value, 0.00001, "RA Value");
         Assert.AreEqual(-25.39285, testRaDec.Declination.Value, 0.001, "Declination value");
         Assert.AreEqual(244.60715, testRaDec.DeclinationAxis.Value, 0.001, "Declination axis value");
      }

      [TestMethod]
      public void SouthEastHorizonAltAz()
      {
         // Equivalent of ASCOM SOP conformance point A
         MountCoordinate mount = new MountCoordinate(
            new AltAzCoordinate(0.0, 135.0),
            new AxisPosition(30.0, 135.0),
            _Tools,
            _Now);
         EquatorialCoordinate testRaDec = mount.Equatorial;
         Assert.AreEqual(PierSide.pierWest, mount.PointingSideOfPier, "Pointing side of pier");
         Assert.AreEqual(23.62626, testRaDec.RightAscension.Value, 0.00001, "RA Value");
         Assert.AreEqual(-25.39285, testRaDec.Declination.Value, 0.001, "Declination value");
         Assert.AreEqual(115.39285, testRaDec.DeclinationAxis.Value, 0.001, "Declination axis value");
      }

      [TestMethod]
      public void NorthEastHorizonAltAz()
      {
         // Equivalent of ASCOM SOP conformance point A
         MountCoordinate mount = new MountCoordinate(
            new AltAzCoordinate(0.0, 45.0),
            new AxisPosition(315.0, 45.0),
            _Tools,
            _Now);
         EquatorialCoordinate testRaDec = mount.Equatorial;
         Assert.AreEqual(PierSide.pierWest, mount.PointingSideOfPier, "Pointing side of pier");
         Assert.AreEqual(4.75814, testRaDec.RightAscension.Value, 0.00001, "RA Value");
         Assert.AreEqual(25.39285, testRaDec.Declination.Value, 0.001, "Declination value");
         Assert.AreEqual(64.60715, testRaDec.DeclinationAxis.Value, 0.001, "Declination axis value");
      }
      #endregion


      #region Slew tests ...
      [TestMethod]
      public void SlewDec30()
      {
         EquatorialCoordinate currentRaDec = new EquatorialCoordinate(_CelestialPolePosition.Equatorial.RightAscension.Value, _CelestialPolePosition.Equatorial.DeclinationAxis.Value);
         MountCoordinate currentPosition = new MountCoordinate(currentRaDec, _CelestialPolePosition.ObservedAxes, _Tools, _Now);
         MountCoordinate destination = SimulateSlew(currentPosition, new double[] { 0.0, 30.0 });
         EquatorialCoordinate expected = new EquatorialCoordinate(currentPosition.Equatorial.RightAscension.Value, 30.0);
         Assert.AreEqual(expected, destination.Equatorial, "Slewed DEC test");
      }

      [TestMethod]
      public void SlewDecMinus30()
      {
         EquatorialCoordinate currentRaDec = new EquatorialCoordinate(_CelestialPolePosition.Equatorial.RightAscension.Value, _CelestialPolePosition.Equatorial.DeclinationAxis.Value);
         MountCoordinate currentPosition = new MountCoordinate(currentRaDec, _CelestialPolePosition.ObservedAxes, _Tools, _Now);
         MountCoordinate destination = SimulateSlew(currentPosition, new double[] { 0.0, -30.0 });
         EquatorialCoordinate expected = new EquatorialCoordinate(currentPosition.Equatorial.RightAscension.Value, -30.0);
         Assert.AreEqual(expected, destination.Equatorial, "Slewed -DEC test");
      }


      [TestMethod]
      public void SlewRA30()
      {
         EquatorialCoordinate currentRaDec = new EquatorialCoordinate(_CelestialPolePosition.Equatorial.RightAscension.Value, _CelestialPolePosition.Equatorial.DeclinationAxis.Value);
         MountCoordinate currentPosition = new MountCoordinate(currentRaDec, _CelestialPolePosition.ObservedAxes, _Tools, _Now);
         MountCoordinate destination = SimulateSlew(currentPosition, new double[] { 30.0, 0.0 });
         EquatorialCoordinate expected = new EquatorialCoordinate(currentPosition.Equatorial.RightAscension.Value+2.0, 0.0);
         Assert.AreEqual(expected, destination.Equatorial, "Slewed RA test");
      }

      [TestMethod]
      public void SlewRAMinus30()
      {
         EquatorialCoordinate currentRaDec = new EquatorialCoordinate(_CelestialPolePosition.Equatorial.RightAscension.Value, _CelestialPolePosition.Equatorial.DeclinationAxis.Value);
         MountCoordinate currentPosition = new MountCoordinate(currentRaDec, _CelestialPolePosition.ObservedAxes, _Tools, _Now);
         MountCoordinate destination = SimulateSlew(currentPosition, new double[] { -30.0, 0.0 });
         EquatorialCoordinate expected = new EquatorialCoordinate(currentPosition.Equatorial.RightAscension.Value-2.0, 0.0);
         Assert.AreEqual(expected, destination.Equatorial, "Slewed - RA test");
      }

      [TestMethod]
      public void ASCOM_SOP()
      {
         EquatorialCoordinate currentRaDec = new EquatorialCoordinate(_CelestialPolePosition.Equatorial.RightAscension.Value, _CelestialPolePosition.Equatorial.DeclinationAxis.Value);
         MountCoordinate currentPosition = new MountCoordinate(currentRaDec, _CelestialPolePosition.ObservedAxes, _Tools, _Now);

         EquatorialCoordinate targetRaDec = new EquatorialCoordinate(23.62626, 80.0);
         double[] slewDistance = currentPosition.GetSlewAnglesTo(targetRaDec);
         System.Diagnostics.Debug.WriteLine($"{slewDistance[0]} / {slewDistance[1]}");
         currentPosition = SimulateSlew(currentPosition, slewDistance);
         Assert.AreEqual(PierSide.pierWest, currentPosition.PointingSideOfPier, "Point A");

         targetRaDec = new EquatorialCoordinate(11.62626, 330.0);
         slewDistance = currentPosition.GetSlewAnglesTo(targetRaDec);
         System.Diagnostics.Debug.WriteLine($"{slewDistance[0]} / {slewDistance[1]}");
         currentPosition = SimulateSlew(currentPosition, slewDistance);
         Assert.AreEqual(PierSide.pierEast, currentPosition.PointingSideOfPier, "Point B");

         targetRaDec = new EquatorialCoordinate(4.75814, 330.0);
         slewDistance = currentPosition.GetSlewAnglesTo(targetRaDec);
         System.Diagnostics.Debug.WriteLine($"{slewDistance[0]} / {slewDistance[1]}");
         currentPosition = SimulateSlew(currentPosition, slewDistance);
         Assert.AreEqual(PierSide.pierWest, currentPosition.PointingSideOfPier, "Point C");

         targetRaDec = new EquatorialCoordinate(16.75814, 280.0);
         slewDistance = currentPosition.GetSlewAnglesTo(targetRaDec);
         System.Diagnostics.Debug.WriteLine($"{slewDistance[0]} / {slewDistance[1]}");
         currentPosition = SimulateSlew(currentPosition, slewDistance);
         Assert.AreEqual(PierSide.pierEast, currentPosition.PointingSideOfPier, "Point D");

      }


      [TestMethod]
      public void GotoBeyondSouthHorizon()
      {
         EquatorialCoordinate currentRaDec = new EquatorialCoordinate(_CelestialPolePosition.Equatorial.RightAscension.Value, _CelestialPolePosition.Equatorial.DeclinationAxis.Value);
         MountCoordinate currentPosition = new MountCoordinate(currentRaDec, _CelestialPolePosition.ObservedAxes, _Tools, _Now);
         // Calculate the slew distance
         EquatorialCoordinate initialTargetPosition = new EquatorialCoordinate(20.1922, 127.333);     // South Horizon
         double[] slewDistance = currentRaDec.GetAxisOffsetTo(initialTargetPosition);

         // Now slew past/ thus crossing the meridian
         slewDistance[0] += 30.0;

         MountCoordinate newMountPosition = SimulateSlew(currentPosition, slewDistance);



      }



      private MountCoordinate SimulateSlew(MountCoordinate currentPosition, double[] slewBy)
      {
         AxisPosition newAxisPosition = currentPosition.ObservedAxes.RotateBy(slewBy);


         // Calculate change in axis angle
         double[] delta = currentPosition.ObservedAxes.GetDeltaTo(newAxisPosition);

         // Determine RA/Dec by adding the difference to the current celestial pole RA/Dec
         EquatorialCoordinate newPosition = currentPosition.Equatorial.SlewBy(delta);
         return new MountCoordinate(newPosition, newAxisPosition, _Tools, _Now);
      }
      #endregion


   }
}

