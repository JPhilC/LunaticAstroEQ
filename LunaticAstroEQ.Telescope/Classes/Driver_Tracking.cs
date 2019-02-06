using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

using ASCOM;
using ASCOM.Astrometry;
using ASCOM.Astrometry.AstroUtils;
using ASCOM.Utilities;
using ASCOM.DeviceInterface;
using System.Globalization;
using System.Collections;
using System.Linq;
using System.Reflection;
using Core = ASCOM.LunaticAstroEQ.Core;
using ASCOM.LunaticAstroEQ.Controller;
using ASCOM.LunaticAstroEQ.Core;
using ASCOM.LunaticAstroEQ.Core.Geometry;
using CoreConstants = ASCOM.LunaticAstroEQ.Core.Constants;
using System.Threading;

namespace ASCOM.LunaticAstroEQ
{
   public partial class Telescope
   {

      private DateTime _lastMessage = DateTime.Now;
      private DateTime _lastRefresh = DateTime.MinValue;
      private TimeSpan _minRefreshInterval = new TimeSpan(0, 0, 0, 0, 500);
      private double _axisPositionTolerance = 0.00001;


      /// <summary>
      /// Refreshes the NCP and current positions if more than 500ms have elapsed since the last refresh.
      /// </summary>
      private void RefreshCurrentPosition()
      {
         DateTime now = DateTime.Now;
         bool axisHasMoved = false;
         if (now - _lastRefresh > _minRefreshInterval)
         {
            AxisPosition axisPosition = _previousAxisPosition;
            lock (Controller)
            {
               axisPosition = Controller.MCGetAxisPositions();
            }
            if (!axisPosition.Equals(_previousAxisPosition, _axisPositionTolerance))
            {
               // Axes have moved.
               axisHasMoved = true;
               if (_previousPointingSOP == PierSide.pierUnknown)
               {
                  // First move away from home (NCP/SCP) position.
                  if (_IsSlewing && _TargetPosition != null)
                  {
                     System.Diagnostics.Debug.WriteLine("Initialising SOP from goto target.");
                     axisPosition.DecFlipped = _TargetPosition.ObservedAxes.DecFlipped;
                  }
               }
               else
               {
                  // Keep the axis position the same
                  axisPosition.DecFlipped = _previousAxisPosition.DecFlipped;
               }
            }
            else
            {
               // Axes have not moved.
               if (_TargetPosition != null)
               {
                  // System.Diagnostics.Debug.WriteLine($"RA:{Math.Abs(_TargetPosition.ObservedAxes.RAAxis.Value - axisPosition.RAAxis.Value)} Dec: {Math.Abs(_TargetPosition.ObservedAxes.DecAxis.Value - axisPosition.DecAxis.Value)}");
                  _TargetPosition = null;
               }
               // Check if slew finished
               if (_IsSlewing)
               {
                  _IsSlewing = false;
                  LogMessage("Command", "Slew - complete");
               }
               if (Settings.ParkStatus == ParkStatus.Parking)
               {
                  LogMessage("Command", "Park - complete");
                  // Update the parked axis position decflipped setting if it isn't set.
                  _ParkedAxisPosition.DecFlipped = _previousAxisPosition.DecFlipped;
                  Settings.ParkStatus = ParkStatus.Parked;
                  Settings.AxisParkPosition = _ParkedAxisPosition;
                  TelescopeSettingsProvider.Current.SaveSettings();

               }

               axisPosition.DecFlipped = _previousAxisPosition.DecFlipped;
            }

            _CurrentPosition.MoveRADec(axisPosition, _AscomToolsCurrentPosition, now);
            if (_previousPointingSOP != PierSide.pierUnknown)
            {
               // See if pointing has moved across the meridian
               if (_CurrentPosition.PointingSideOfPier != _previousPointingSOP)
               {
                  System.Diagnostics.Debug.WriteLine("** Crossing the meridian **");
                  // Pointing has moved across the meridian so toggle the DecFlipped value;
                  axisPosition.DecFlipped = !_previousAxisPosition.DecFlipped;
                  _CurrentPosition.MoveRADec(axisPosition, _AscomToolsCurrentPosition, now);

               }
            }
            if (axisHasMoved)
            {
               _previousPointingSOP = _CurrentPosition.PointingSideOfPier;
            }

            if (Settings.ParkStatus == ParkStatus.Parking)
            {
               System.Diagnostics.Debug.WriteLine($"Parking to: {_ParkedAxisPosition.RAAxis.Value}/{_ParkedAxisPosition.RAAxis.Value}\t{_ParkedAxisPosition.DecFlipped}");
            }

            if (_TargetPosition != null)
            {
               System.Diagnostics.Debug.WriteLine($"Target Axes: {_TargetPosition.ObservedAxes.RAAxis.Value}/{_TargetPosition.ObservedAxes.DecAxis.Value}\t{_TargetPosition.ObservedAxes.DecFlipped}\tRA/Dec: {_TargetPosition.Equatorial.RightAscension}/{_TargetPosition.Equatorial.Declination}\t{_TargetPosition.PointingSideOfPier}");
            }
            if (AtPark)
            {
               System.Diagnostics.Debug.WriteLine("Parked");
            }
            else
            {
               System.Diagnostics.Debug.WriteLine($"Current Axes: {axisPosition.RAAxis.Value}/{axisPosition.DecAxis.Value}\t{axisPosition.DecFlipped}\tRA/Dec: {_CurrentPosition.Equatorial.RightAscension}/{_CurrentPosition.Equatorial.Declination}\t{_CurrentPosition.PointingSideOfPier}\n");
            }
               _previousAxisPosition = axisPosition;
            _lastRefresh = now;
         }

      }



      private void AbortSlewInternal()
      {
         if (Slewing)
         {
            lock (Controller)
            {
               Controller.MCAxisStop(AXISID.AXIS1);
               Controller.MCAxisStop(AXISID.AXIS2);

               // TODO: Restart tracking
            }
            _IsSlewing = false;
            _TargetPosition = null;
         }
      }

      private void ParkInternal()
      {
         if (Settings.ParkStatus == ParkStatus.Unparked)
         {
            lock (Controller)
            {
               // Abort slew to stop
               AbortSlewInternal();

               // Set status to Parking
               Settings.ParkStatus = ParkStatus.Parking;
               TelescopeSettingsProvider.Current.SaveSettings();
               Controller.MCAxisSlewTo(_ParkedAxisPosition);
               if (Settings.AscomCompliance.UseSynchronousParking)
               {
                  // Block until the park completes
                  AxisPosition currentPosition = Controller.MCGetAxisPositions();
                  while (!currentPosition.Equals(_ParkedAxisPosition, _axisPositionTolerance))
                  {
                     System.Diagnostics.Debug.Write("Waiting for 5 seconds");
                     // wait 5 seconds
                     Thread.Sleep(5000);
                     currentPosition = Controller.MCGetAxisPositions();
                  }
               }
               // If not using synchronous parking the usual refresh current possition handles everything.
            }
         }
      }

   }
}
