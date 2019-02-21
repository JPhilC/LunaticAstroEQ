using System;
using ASCOM.LunaticAstroEQ;
using ASCOM.LunaticAstroEQ.Controller;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ASCOM.LunaticAstroEQ.Core;
using CoreConstants = ASCOM.LunaticAstroEQ.Core.Constants;
using System.Threading;

namespace LunaticAstroEQ.Tests
{
   [TestClass]
   public class ControllerTests
   {
      private const string COMPort = "COM6";
      private const int BaudRate = 9600;
      private const int Timeout = 2000;
      private const int Retry = 2;

      private AstroEQController _Controller;

      [TestInitialize]
      public void Initialize()
      {
         _Controller = SharedResources.Controller;
         lock (_Controller)
         {
            _Controller.Connect(COMPort, BaudRate, Timeout, Retry);
            _Controller.MCSetAxisPosition(AxisId.Axis1_RA, 0.0);
            _Controller.MCSetAxisPosition(AxisId.Axis1_RA, 0.0);
         }
      }


      [TestCleanup]
      public void Cleanup()
      {
         _Controller.MCAxisStopAndRelease(AxisId.Both_Axes);
         _Controller.Disconnect();
      }

      [TestMethod]
      public void TestMCGetAxisState()
      {
         lock (_Controller)
         {
            AxisState state = _Controller.MCGetAxisState(AxisId.Axis1_RA);
            Assert.AreEqual(false, state.MeshedForReverse);
            Assert.AreEqual(false, state.NotInitialized);
            Assert.AreEqual(false, state.HighSpeed);
            Assert.AreEqual(false, state.Slewing);
            Assert.AreEqual(false, state.SlewingTo);
         }
      }


      [TestMethod]
      public void TestTracking()
      {
         AxisId axis = AxisId.Axis1_RA;
         double startPosition = _Controller.MCGetAxisPosition(axis);
         _Controller.MCStartTrackingRate(axis, CoreConstants.SIDEREAL_RATE_ARCSECS, HemisphereOption.Northern, AxisDirection.Forward);
         Thread.Sleep(5000);
         double firstPosition = _Controller.MCGetAxisPosition(axis);

         _Controller.MCStartTrackingRate(axis, CoreConstants.SIDEREAL_RATE_ARCSECS * 10.0, HemisphereOption.Northern, AxisDirection.Forward);
         Thread.Sleep(5000);
         double secondPosiion = _Controller.MCGetAxisPosition(axis);
         //_Controller.MCStartTrackingRate(axis, CoreConstants.SIDEREAL_RATE_ARCSECS*100.0, HemisphereOption.Northern, AxisDirection.Forward);
         //Thread.Sleep(5000);
         _Controller.MCAxisStop(axis);
         double delta1 = firstPosition - startPosition;
         double delta2 = secondPosiion - firstPosition;
         Assert.AreEqual(delta1 * 10.0, delta2, 0.0005);
      }

      [TestMethod]
      public void TestSlewStates()
      {
         AxisId axis = AxisId.Axis1_RA;
         string response;
         int state;
         System.Diagnostics.Debug.WriteLine("Slew Forward Low Speed");
         _Controller.MCAxisSlew(axis, (CoreConstants.SIDEREAL_RATE_ARCSECS / 3600.0), HemisphereOption.Northern);
         System.Diagnostics.Debug.WriteLine(_Controller.TalkWithAxis(axis, 'f', null));
         response = _Controller.TalkWithAxis(axis, 'f', null);
         state = Convert.ToInt32(response.Substring(1, response.Length - 2), 16);
         Assert.AreEqual(0x111, state);
         Thread.Sleep(2000);

         _Controller.MCAxisStop(axis);
         System.Diagnostics.Debug.WriteLine("Slew Forward High Speed");
         _Controller.MCAxisSlew(axis, (CoreConstants.SIDEREAL_RATE_ARCSECS / 3600.0) * 200, HemisphereOption.Northern);
         System.Diagnostics.Debug.WriteLine(_Controller.TalkWithAxis(axis, 'f', null));
         response = _Controller.TalkWithAxis(axis, 'f', null);
         state = Convert.ToInt32(response.Substring(1, response.Length - 2), 16);
         Assert.AreEqual(0x511, state);
         Thread.Sleep(2000);

         _Controller.MCAxisStop(axis);
         System.Diagnostics.Debug.WriteLine("Slew Reverse High Speed");
         _Controller.MCAxisSlew(axis, (CoreConstants.SIDEREAL_RATE_ARCSECS / 3600.0) * -200, HemisphereOption.Northern);
         System.Diagnostics.Debug.WriteLine(_Controller.TalkWithAxis(axis, 'f', null));
         response = _Controller.TalkWithAxis(axis, 'f', null);
         state = Convert.ToInt32(response.Substring(1, response.Length - 2), 16);
         Assert.AreEqual(0x711, state);
         Thread.Sleep(2000);

         _Controller.MCAxisStop(axis);
         System.Diagnostics.Debug.WriteLine("Slew Reverse Low Speed");
         _Controller.MCAxisSlew(axis, (CoreConstants.SIDEREAL_RATE_ARCSECS / 3600.0) * -1, HemisphereOption.Northern);
         System.Diagnostics.Debug.WriteLine(_Controller.TalkWithAxis(axis, 'f', null));
         response = _Controller.TalkWithAxis(axis, 'f', null);
         state = Convert.ToInt32(response.Substring(1, response.Length - 2), 16);
         Assert.AreEqual(0x311, state);
         Thread.Sleep(2000);

         _Controller.MCAxisStop(axis);
      }

      [TestMethod]
      public void TestGotoStates()
      {
         AxisId axis = AxisId.Axis1_RA;
         string response;
         int state;
         _Controller.MCAxisSlewTo(axis, 0.25 * CoreConstants.DEG_RAD, HemisphereOption.Northern);
         //System.Diagnostics.Debug.WriteLine("Goto Forward Low Speed");
         //System.Diagnostics.Debug.WriteLine(_Controller.TalkWithAxis(axis, 'f', null));
         response = _Controller.TalkWithAxis(axis, 'f', null);
         state = Convert.ToInt32(response.Substring(1, response.Length - 2), 16);

         AxisState axisState = _Controller.MCGetAxisState(axis);
         while (!axisState.FullStop)
         {
            Thread.Sleep(100);
            axisState = _Controller.MCGetAxisState(axis);
         }
         Assert.AreEqual(0x011, state);

         _Controller.MCAxisSlewTo(axis, 30.0 * CoreConstants.DEG_RAD, HemisphereOption.Northern);
         System.Diagnostics.Debug.WriteLine("Goto Forward High Speed");
         //System.Diagnostics.Debug.WriteLine(_Controller.TalkWithAxis(axis, 'f', null));
         response = _Controller.TalkWithAxis(axis, 'f', null);
         state = Convert.ToInt32(response.Substring(1, response.Length - 2), 16);
         axisState = _Controller.MCGetAxisState(axis);
         while (!axisState.FullStop)
         {
            Thread.Sleep(100);
            axisState = _Controller.MCGetAxisState(axis);
         }
         Assert.AreEqual(0x411, state);

         _Controller.MCAxisSlewTo(axis, 0.25 * CoreConstants.DEG_RAD, HemisphereOption.Northern);
         System.Diagnostics.Debug.WriteLine("Goto Reverse High Speed");
         //System.Diagnostics.Debug.WriteLine(_Controller.TalkWithAxis(axis, 'f', null));
         response = _Controller.TalkWithAxis(axis, 'f', null);
         state = Convert.ToInt32(response.Substring(1, response.Length - 2), 16);
         axisState = _Controller.MCGetAxisState(axis);
         while (!axisState.FullStop)
         {
            Thread.Sleep(100);
            axisState = _Controller.MCGetAxisState(axis);
         }
         Assert.AreEqual(0x611, state);

         _Controller.MCAxisSlewTo(axis, 0.0 * CoreConstants.DEG_RAD, HemisphereOption.Northern);
         System.Diagnostics.Debug.WriteLine("Goto Reverse Low Speed");
         //System.Diagnostics.Debug.WriteLine(_Controller.TalkWithAxis(axis, 'f', null));
         response = _Controller.TalkWithAxis(axis, 'f', null);
         state = Convert.ToInt32(response.Substring(1, response.Length - 2), 16);
         axisState = _Controller.MCGetAxisState(axis);
         while (!axisState.FullStop)
         {
            Thread.Sleep(100);
            axisState = _Controller.MCGetAxisState(axis);
         }
         Assert.AreEqual(0x211, state);

      }


      [TestMethod]
      public void TestGetMaxRates()
      {
         double expected = 812 * CoreConstants.SIDEREAL_RATE_DEGREES;
         double[] maxRates = _Controller.MCGetMaxRates();
         Assert.AreEqual(2, maxRates.Length, "Number of rates returned");
         Assert.AreEqual(expected, maxRates[0], "Max RA rate");
         Assert.AreEqual(expected, maxRates[1], "Max Dec rate");

      }
   }
}
