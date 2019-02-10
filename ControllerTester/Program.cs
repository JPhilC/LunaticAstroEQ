using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ASCOM.DeviceInterface;
using ASCOM.LunaticAstroEQ;
using ASCOM.LunaticAstroEQ.Controller;
using ASCOM.LunaticAstroEQ.Core;
using CoreConstants = ASCOM.LunaticAstroEQ.Core.Constants;


namespace ControllerTester
{
   class Program
   {
      static AstroEQController _controller = null;

      static void Main(string[] args)
      {
         _controller = new AstroEQController();
         int result = _controller.Connect("COM3", 9600, 2000, 1);
         if (result == CoreConstants.MOUNT_SUCCESS)
         {

            //Console.WriteLine("Press any key to start tracking");
            //Console.ReadKey(true);
            //_controller.MCStartRATrack(DriveRates.driveSidereal, HemisphereOption.Northern, AxisDirection.Forward);

            // RunGOTO();

            RunSlew();

            // GCodes(0x0);

            Console.WriteLine("Press any key to stop moving");
            Console.ReadKey(true);
            _controller.MCAxisStop(AxisId.Axis1_RA);
            _controller.TalkWithAxis(0, 'f', null);

            Console.WriteLine("Press any key to disconnect");
            Console.ReadKey(true);
            _controller.Disconnect();
         }
      }

      static private void RunGOTO()
      {
         _controller.MCSetAxisPosition(AxisId.Axis1_RA, 0.0);

         double slowPosition = 0.03;    // Radians (will result in lowspeed goto)
         double fastPosition = 0.1;    // Radians (will result in highspeed goto)

         HemisphereOption hemisphere = HemisphereOption.Northern;
         Console.WriteLine("Press any key to start GOTO Slow");
         Console.ReadKey(true);
         System.Diagnostics.Debug.WriteLine("\n=== GOTO Slow ====");
         _controller.MCAxisSlewTo(AxisId.Axis1_RA, slowPosition, hemisphere);
         // Wait for stop
         _controller.TalkWithAxis(0, 'f', null);
         AxisState status = _controller.MCGetAxisStatus(AxisId.Axis1_RA);
         // System.Diagnostics.Debug.WriteLine($"Fwd-{status.SlewingForward}\tSpd-{status.HighSpeed}\tStpd-{status.FullStop}\tSlwg-{status.Slewing}\tSlwgT-{status.SlewingTo}\tInit-{!status.NotInitialized}");
         double ppos = double.MaxValue;
         double pos = _controller.MCGetAxisPosition(AxisId.Axis1_RA);
         while (pos != ppos)
         {
            ppos = pos;
            Thread.Sleep(2000);
            _controller.TalkWithAxis(0, 'f', null);
            status = _controller.MCGetAxisStatus(AxisId.Axis1_RA);
            // System.Diagnostics.Debug.WriteLine($"Fwd-{status.SlewingForward}\tSpd-{status.HighSpeed}\tStpd-{status.FullStop}\tSlwg-{status.Slewing}\tSlwgT-{status.SlewingTo}\tInit-{!status.NotInitialized}");
            pos = _controller.MCGetAxisPosition(AxisId.Axis1_RA);
         }

         Console.WriteLine("Press any key to start reverse GOTO Slow");
         Console.ReadKey(true);
         System.Diagnostics.Debug.WriteLine("\n=== GOTO Slow (reversed) ====");
         _controller.MCAxisSlewTo(AxisId.Axis1_RA, 0.0, hemisphere);
         _controller.TalkWithAxis(0, 'f', null);
         status = _controller.MCGetAxisStatus(AxisId.Axis1_RA);
         // System.Diagnostics.Debug.WriteLine($"Fwd-{status.SlewingForward}\tSpd-{status.HighSpeed}\tStpd-{status.FullStop}\tSlwg-{status.Slewing}\tSlwgT-{status.SlewingTo}\tInit-{!status.NotInitialized}");
         ppos = double.MaxValue;
         pos = _controller.MCGetAxisPosition(AxisId.Axis1_RA);
         while (pos != ppos)
         {
            ppos = pos;
            Thread.Sleep(2000);
            _controller.TalkWithAxis(0, 'f', null);
            status = _controller.MCGetAxisStatus(AxisId.Axis1_RA);
            // System.Diagnostics.Debug.WriteLine($"Fwd-{status.SlewingForward}\tSpd-{status.HighSpeed}\tStpd-{status.FullStop}\tSlwg-{status.Slewing}\tSlwgT-{status.SlewingTo}\tInit-{!status.NotInitialized}");
            pos = _controller.MCGetAxisPosition(AxisId.Axis1_RA);
         }


         Console.WriteLine("Press any key to start GOTO Fast");
         Console.ReadKey(true);
         System.Diagnostics.Debug.WriteLine("\n=== GOTO Fast ====");
         _controller.MCAxisSlewTo(AxisId.Axis1_RA, fastPosition, hemisphere);
         // Wait for stop
         _controller.TalkWithAxis(0, 'f', null);
         status = _controller.MCGetAxisStatus(AxisId.Axis1_RA);
         // System.Diagnostics.Debug.WriteLine($"Fwd-{status.SlewingForward}\tSpd-{status.HighSpeed}\tStpd-{status.FullStop}\tSlwg-{status.Slewing}\tSlwgT-{status.SlewingTo}\tInit-{!status.NotInitialized}");
         ppos = double.MaxValue;
         pos = _controller.MCGetAxisPosition(AxisId.Axis1_RA);
         while (pos != ppos)
         {
            ppos = pos;
            Thread.Sleep(2000);
            _controller.TalkWithAxis(0, 'f', null);
            status = _controller.MCGetAxisStatus(AxisId.Axis1_RA);
            // System.Diagnostics.Debug.WriteLine($"Fwd-{status.SlewingForward}\tSpd-{status.HighSpeed}\tStpd-{status.FullStop}\tSlwg-{status.Slewing}\tSlwgT-{status.SlewingTo}\tInit-{!status.NotInitialized}");
            pos = _controller.MCGetAxisPosition(AxisId.Axis1_RA);
         }

         Console.WriteLine("Press any key to start reverse GOTO Fast");
         Console.ReadKey(true);
         System.Diagnostics.Debug.WriteLine("\n=== GOTO Fast (reversed) ====");
         _controller.MCAxisSlewTo(AxisId.Axis1_RA, 0.0, hemisphere);
         _controller.TalkWithAxis(0, 'f', null);
         status = _controller.MCGetAxisStatus(AxisId.Axis1_RA);
         // System.Diagnostics.Debug.WriteLine($"Fwd-{status.SlewingForward}\tSpd-{status.HighSpeed}\tStpd-{status.FullStop}\tSlwg-{status.Slewing}\tSlwgT-{status.SlewingTo}\tInit-{!status.NotInitialized}");
         ppos = double.MaxValue;
         pos = _controller.MCGetAxisPosition(AxisId.Axis1_RA);
         while (pos != ppos)
         {
            ppos = pos;
            Thread.Sleep(2000);
            _controller.TalkWithAxis(0, 'f', null);
            status = _controller.MCGetAxisStatus(AxisId.Axis1_RA);
            // System.Diagnostics.Debug.WriteLine($"Fwd-{status.SlewingForward}\tSpd-{status.HighSpeed}\tStpd-{status.FullStop}\tSlwg-{status.Slewing}\tSlwgT-{status.SlewingTo}\tInit-{!status.NotInitialized}");
            pos = _controller.MCGetAxisPosition(AxisId.Axis1_RA);
         }
      }

      static private void RunSlew()
      {
         _controller.MCSetAxisPosition(AxisId.Axis1_RA, 0.0);

         double slowSpeed = 0.007;    // Radians (will result in lowspeed goto)
         double fastSpeed = 0.02;    // Radians (will result in highspeed goto)

         HemisphereOption hemisphere = HemisphereOption.Northern;
         Console.WriteLine("Press any key to start SLEW Slow");
         Console.ReadKey(true);
         System.Diagnostics.Debug.WriteLine("\n=== SLEW Slow ====");
         _controller.MCAxisSlew(AxisId.Axis1_RA, slowSpeed, hemisphere);
         int ct = 0;
         while (ct < 3)
         {
            Thread.Sleep(2000);
            _controller.TalkWithAxis(0, 'f', null);
            ct++;
         }
         System.Diagnostics.Debug.WriteLine("\n=== SLEW Stop ====");
         _controller.MCAxisSlew(AxisId.Axis1_RA, 0.0, hemisphere);
         ct = 0;
         while (ct < 3)
         {
            Thread.Sleep(2000);
            _controller.TalkWithAxis(0, 'f', null);
            ct++;
         }

         Console.WriteLine("Press any key to start reverse SLEW Slow");
         Console.ReadKey(true);
         System.Diagnostics.Debug.WriteLine("\n=== SLEW Slow (reversed) ====");
         _controller.MCAxisSlew(AxisId.Axis1_RA, -slowSpeed, hemisphere);
         ct = 0;
         while (ct < 3)
         {
            Thread.Sleep(2000);
            _controller.TalkWithAxis(0, 'f',  null);
            ct++;
         }
         System.Diagnostics.Debug.WriteLine("\n=== SLEW Stop ====");
         _controller.MCAxisSlew(AxisId.Axis1_RA, 0.0, hemisphere);
         ct = 0;
         while (ct < 3)
         {
            Thread.Sleep(2000);
            _controller.TalkWithAxis(0, 'f', null);
            ct++;
         }


         Console.WriteLine("Press any key to start SLEW Fast");
         Console.ReadKey(true);
         System.Diagnostics.Debug.WriteLine("\n=== SLEW Fast ====");
         _controller.MCAxisSlew(AxisId.Axis1_RA, fastSpeed, hemisphere);
         ct = 0;
         while (ct < 3)
         {
            Thread.Sleep(2000);
            _controller.TalkWithAxis(0, 'f', null);
            ct++;
         }
         _controller.MCAxisSlew(AxisId.Axis1_RA, 0.0, hemisphere);
         System.Diagnostics.Debug.WriteLine("\n=== SLEW Stop ====");
         _controller.MCAxisSlew(AxisId.Axis1_RA, 0.0, hemisphere);
         ct = 0;
         while (ct < 3)
         {
            Thread.Sleep(2000);
            _controller.TalkWithAxis(0, 'f', null);
            ct++;
         }

         Console.WriteLine("Press any key to start reverse SLEW Fast");
         Console.ReadKey(true);
         System.Diagnostics.Debug.WriteLine("\n=== SLEW Fast (reversed) ====");
         _controller.MCAxisSlew(AxisId.Axis1_RA, -fastSpeed, hemisphere);
         ct = 0;
         while (ct < 3)
         {
            Thread.Sleep(2000);
            _controller.TalkWithAxis(0, 'f', null);
            ct++;
         }
         System.Diagnostics.Debug.WriteLine("\n=== SLEW Stop ====");
         _controller.MCAxisSlew(AxisId.Axis1_RA, 0.0, hemisphere);
         ct = 0;
         while (ct < 3)
         {
            Thread.Sleep(2000);
            _controller.TalkWithAxis(0, 'f', null);
            ct++;
         }
      }

      // System.Diagnostics.Debug.WriteLine($"Raw state 2: {MCGetRawAxisStatus(AxisId.Axis1_RA)}\n");

   }
}
