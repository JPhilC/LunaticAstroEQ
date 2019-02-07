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
using Constants = ASCOM.LunaticAstroEQ.Core.Constants;

namespace ASCOM.LunaticAstroEQ
{
   public partial class Telescope
   {

      private DateTime _lastMessage = DateTime.Now;
      private DateTime _lastRefresh = DateTime.MinValue;
      private TimeSpan _minRefreshInterval = new TimeSpan(0, 0, 0, 0, 500);
      private double _axisPositionTolerance = 0.00001;

      private PierSide _previousPointingSOP = PierSide.pierUnknown;
      private AxisPosition _previousAxisPosition;

      private string CustomTrackFile = string.Empty;           // 
      private TrackDefinition CustomTrackDefinition = null;



      private void InitialiseCurrentPosition()
      {
         DateTime now = DateTime.Now;
         _ParkedAxisPosition = Settings.AxisParkPosition;
         _CurrentPosition = new MountCoordinate(_ParkedAxisPosition, _AscomToolsCurrentPosition, now);
         _previousAxisPosition = _ParkedAxisPosition;


         lock (Controller)
         {
            Controller.MCSetAxisPosition(_ParkedAxisPosition);
         }

      }


      /// <summary>
      /// Refreshes the NCP and current positions if more than 500ms have elapsed since the last refresh.
      /// </summary>
      private void RefreshCurrentPosition()
      {
         DateTime now = DateTime.Now;
         bool axisHasMoved = false;
         AxisStatus[] axesStatus;
         if (now - _lastRefresh > _minRefreshInterval)
         {
            AxisPosition axisPosition = _previousAxisPosition;
            lock (Controller)
            {
               axesStatus = Controller.MCGetAxesStatus();
               axisPosition = Controller.MCGetAxisPositions();
            }
#if DEBUG
            AxisStatus raStatus = axesStatus[RA_AXIS];
            AxisStatus decStatus = axesStatus[DEC_AXIS];
            System.Diagnostics.Debug.WriteLine($"\nRA : Slewing-{raStatus.Slewing}, SlewingTo-{raStatus.SlewingTo}, Forward-{raStatus.SlewingForward}, FullStop-{raStatus.FullStop}, Tracking-{raStatus.Tracking}");
            System.Diagnostics.Debug.WriteLine($"Dec: Slewing-{decStatus.Slewing}, SlewingTo-{decStatus.SlewingTo}, Forward-{decStatus.SlewingForward}, FullStop-{decStatus.FullStop}, Tracking-{decStatus.Tracking}");
#endif
            if (!axisPosition.Equals(_previousAxisPosition, _axisPositionTolerance))
            {
               // One or the other axis is moving
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
                  if (TrackingState != TrackingStatus.Off)
                  {
                     // Restart tracking
                     RestartTracking(TrackingRate);
                  }
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
               // System.Diagnostics.Debug.WriteLine($"Current Axes: {axisPosition.RAAxis.Value}/{axisPosition.DecAxis.Value}\t{axisPosition.DecFlipped}\tRA/Dec: {_CurrentPosition.Equatorial.RightAscension}/{_CurrentPosition.Equatorial.Declination}\t{_CurrentPosition.PointingSideOfPier}\n");
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
               Controller.MCAxisStop(AxisId.Both_Axes);
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
               Controller.MCAxisSlewTo(_ParkedAxisPosition, Hemisphere);
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

      #region Slewing and goto ...

      private void  SlewToEquatorialCoordinate(double rightAscension, double declination)
      {
         lock (Controller)
         {
            LogMessage("SlewToCoordinates", "RA:{0}/Dec:{1}", _AscomToolsCurrentPosition.Util.HoursToHMS(rightAscension, "h", "m", "s"), _AscomToolsCurrentPosition.Util.DegreesToDMS(declination, ":", ":"));
            DateTime currentTime = DateTime.Now;
            AxisPosition targetAxisPosition = _CurrentPosition.GetAxisPositionForRADec(rightAscension, declination, _AscomToolsCurrentPosition);
            _TargetPosition = new MountCoordinate(targetAxisPosition, _AscomToolsTargetPosition, currentTime);
            System.Diagnostics.Debug.WriteLine($"Physical SOP: { targetAxisPosition.PhysicalSideOfPier}\t\tPointing SOP: {_TargetPosition.GetPointingSideOfPier(false)}");
            _IsSlewing = true;
            Controller.MCAxisSlewTo(targetAxisPosition, Hemisphere);
            TargetRightAscension = rightAscension;
            TargetDeclination = declination;
         }
      }
      #endregion

      #region Tracking stuff ...
      private TrackingStatus TrackingState
      {
         get
         {
            return Settings.TrackingState;
         }
         set
         {
            if (Settings.TrackingState == value)
            {
               return;
            }
            Settings.TrackingState = value;
            TelescopeSettingsProvider.Current.SaveSettings();

         }
      }

      // Called to initialise tracking (After loading settings, if the mount isn't already tracking)
      private void InitialiseTracking(DriveRates trackingRate)
      {
         switch (trackingRate)
         {
            case DriveRates.driveSidereal:
               StartSiderealTracking();
               break;
            case DriveRates.driveLunar:
               StartLunarTracking();
               break;
            case DriveRates.driveSolar:
               StartSolarTracking();
               break;
            default:
               throw new ASCOM.InvalidValueException("TrackingRate");
         }

      }

      private void StopTracking()
      {
         lock (Controller)
         {
            _IsSlewing = false;

            if (Settings.ParkStatus == ParkStatus.Parking)
            {
               // we were slewing to park position
               // well its not happening now!
               Settings.ParkStatus = ParkStatus.Unparked;
            }


            //if (PECEnabled)
            //{
            //   PECStopTracking();
            //}

            Controller.MCAxisStop(AxisId.Both_Axes);


            // LastPECRate = 0;


            Settings.TrackingState = TrackingStatus.Off;
            Settings.DeclinationRate = 0;
            Settings.RightAscensionRate = 0;

            TelescopeSettingsProvider.Current.SaveSettings();
         }
      }

      /// <summary>
      /// Restart tracking after goto etc.
      /// </summary>
      /// <param name="trackingRate"></param>
      private void RestartTracking(DriveRates trackingRate)
      {
         System.Diagnostics.Debug.WriteLine("Restarting tracking");
         LogMessage("Command", "Restarting tracking {0}", TrackingRate);
         Controller.MCStartRATrack(trackingRate, Hemisphere, (Hemisphere == HemisphereOption.Northern ? AxisDirection.Forward : AxisDirection.Reverse));
         Controller.MCAxisStop(AxisId.Axis2_Dec);

      }

      private void StartSiderealTracking()
      {
         lock (Controller)
         {
            // LastPECRate = 0;
            if (Settings.ParkStatus != ParkStatus.Unparked)
            {
               return;
            }
            else
            {
               //  Stop DEC motor
               Controller.MCAxisStop(AxisId.Axis2_Dec);
               Settings.DeclinationRate = 0;


               //  start RA motor at sidereal

               Controller.MCStartRATrack(DriveRates.driveSidereal, Hemisphere, (Hemisphere == HemisphereOption.Northern ? AxisDirection.Forward : AxisDirection.Reverse));
               Settings.TrackingState = TrackingStatus.Sidereal;
               Settings.TrackingRate = DriveRates.driveSidereal;    // ASCOM TrackingRate backing variable
               Settings.RightAscensionRate = Core.Constants.SIDEREAL_RATE_ARCSECS;

               TelescopeSettingsProvider.Current.SaveSettings();
               //if (Settings.TrackUsingPEC)
               //{
               //   //  track using PEC
               //   PECStartTracking();
               //   if (!mute)
               //   {
               //      // EQ_Beep(??)
               //   }
               //}
               //else
               //{
               //   //  Set Caption
               //   // HC.TrackingFrame.Caption = oLangDll.GetLangString(121) & " " & oLangDll.GetLangString(122)
               //   // HC.Add_Message(oLangDll.GetLangString(5014))
               //   if (!mute)
               //   {
               //      // EQ_Beep(10)
               //   }
               //}
            }
         }
      }

      private void StartLunarTracking()
      {
         //LastPECRate = 0;
         if (Settings.ParkStatus != ParkStatus.Unparked)
         {
            // HC.Add_Message(oLangDll.GetLangString(5013))
            return;
         }


         //if (PECEnabled)
         //{
         //   PECStopTracking();
         //}
         Controller.MCAxisStop(AxisId.Axis2_Dec);
         Settings.DeclinationRate = 0;

         Controller.MCStartRATrack(DriveRates.driveLunar, Hemisphere, (Hemisphere == HemisphereOption.Northern ? AxisDirection.Forward : AxisDirection.Reverse));
         Settings.TrackingState = TrackingStatus.Lunar;                 // Lunar rate tracking'
         Settings.TrackingRate = DriveRates.driveLunar;                // Backing variable for ASCOM TrackingRate member.
         Settings.RightAscensionRate = Core.Constants.LUNAR_RATE;

         TelescopeSettingsProvider.Current.SaveSettings();

      }


      private void StartSolarTracking()
      {
         //LastPECRate = 0;
         if (Settings.ParkStatus != ParkStatus.Unparked)
         {
            // HC.Add_Message(oLangDll.GetLangString(5013))
            return;
         }


         //if (PECEnabled)
         //{
         //   PECStopTracking();
         //}
         Controller.MCAxisStop(AxisId.Axis2_Dec);
         Settings.DeclinationRate = 0;


         Controller.MCStartRATrack(DriveRates.driveSolar, Hemisphere, (Hemisphere == HemisphereOption.Northern ? AxisDirection.Forward : AxisDirection.Reverse));
         Settings.TrackingState = TrackingStatus.Solar;                 // Lunar rate tracking'
         Settings.TrackingRate = DriveRates.driveSolar;                // Backing variable for ASCOM TrackingRate member.
         Settings.RightAscensionRate = Core.Constants.SOLAR_RATE;

         TelescopeSettingsProvider.Current.SaveSettings();

      }

      private void StartCustomTracking(bool mute)
      {
         System.Diagnostics.Debug.Assert(false, "Custom tracking has not been tested.");
         //LastPECRate = 0;
         if (Settings.ParkStatus != ParkStatus.Unparked)
         {
            // HC.Add_Message(oLangDll.GetLangString(5013))
            return;
         }


         //if (PECEnabled)
         //{
         //   PECStopTracking();
         //}


         double[] rate = new double[2];


         // On Error GoTo handlerr

         if (CustomTrackDefinition == null)
         {

            rate[RA_AXIS] = Settings.CustomTrackingRate[RA_AXIS];
            rate[DEC_AXIS] = Settings.CustomTrackingRate[DEC_AXIS];
            if (Hemisphere == HemisphereOption.Southern)
            {
               rate[RA_AXIS] = -1 * rate[RA_AXIS];
            }



            if (Math.Abs(rate[RA_AXIS]) > 12000 || Math.Abs(rate[DEC_AXIS]) > 12000)
            {
               StopTracking();
               return;
            }


            CustomMoveAxis(AxisId.Axis1_RA, rate[RA_AXIS], true, "Custom");
            CustomMoveAxis(AxisId.Axis2_Dec, rate[DEC_AXIS], true, "Custom");
         }
         else
         {
            // custom track file is assigned
            // TODO: Sort out custom track stuff
            CustomTrackDefinition.TrackIdx = -1; // = GetTrackFileIdx(1, true);
            if (CustomTrackDefinition.TrackIdx != -1)
            {
               if (CustomTrackDefinition.IsWaypoint)
               {
                  // Call GetTrackTarget(i, j)
                  CustomTrackDefinition.RAAdjustment = _CurrentPosition.Equatorial.RightAscension - rate[RA_AXIS];
                  CustomTrackDefinition.DECAdjustment = _CurrentPosition.Equatorial.Declination - rate[DEC_AXIS];
               }
               else
               {
                  CustomTrackDefinition.RAAdjustment = 0;
                  CustomTrackDefinition.DECAdjustment = 0;
               }
               rate[RA_AXIS] = CustomTrackDefinition.TrackSchedule[CustomTrackDefinition.TrackIdx].RaRate;
               rate[DEC_AXIS] = CustomTrackDefinition.TrackSchedule[CustomTrackDefinition.TrackIdx].DecRate;
               // HC.decCustom.Text = FormatNumber(j, 5)
               if (Hemisphere == HemisphereOption.Southern)
               {
                  // HC.raCustom.Text = FormatNumber(-1 * i, 5)
               }
               else
               {
                  // HC.raCustom.Text = FormatNumber(i, 5)
               }
               CustomMoveAxis(AxisId.Axis1_RA, rate[RA_AXIS], true, Settings.CustomTrackName);
               CustomMoveAxis(AxisId.Axis2_Dec, rate[DEC_AXIS], true, Settings.CustomTrackName);
            }

            CustomTrackDefinition.TrackingChangesEnabled = true;
            // HC.CustomTrackTimer.Enabled = True
         }
         return;



      }

      private void CustomMoveAxis(AxisId axis, double rate, bool initialise, string rateName)
      {
         bool saveSettings = false;
         if (axis == AxisId.Axis1_RA)
         {
            if (initialise)
            {
               StartRATrackingByRate(rate);
            }
            else
            {
               if (rate != Settings.RightAscensionRate)
               {
                  ChangeRATrackingByRate(rate);
               }
            }
            Settings.RightAscensionRate = rate;
            Settings.TrackingState = TrackingStatus.Custom;
            saveSettings = true;
         }
         if (axis == AxisId.Axis2_Dec)
         {

            if (initialise)
            {
               StartDecTrackingByRate(rate);
            }
            else
            {
               if (rate != Settings.DeclinationRate)
               {
                  ChangeDecTrackingByRate(rate);
               }
            }
            Settings.DeclinationRate = rate;
            Settings.TrackingState = TrackingStatus.Custom;
            saveSettings = true;
         }
         if (saveSettings)
         {
            TelescopeSettingsProvider.Current.SaveSettings();
         }
      }

      private void StartRATrackingByRate(double raRate)
      {
         /*
          ' Start RA motor based on an input rate of arcsec per Second

         Public Sub StartRA_by_Rate(ByVal RA_RATE As Double)

         Dim i As Double
         Dim j As Double
         Dim k As Double
         Dim m As Double

             k = 0
             m = 1
             i = Abs(RA_RATE)

             If gMount_Ver > &H301 Then
                 If i > 1000 Then
                     k = 1
                     m = EQGP(0, 10003)
                 End If
             Else
                 If i > 3000 Then
                     k = 1
                     m = EQGP(0, 10003)
                 End If
             End If

             HC.Add_Message (oLangDll.GetLangString(117) & " " & str(m) & " , " & str(RA_RATE) & " arcsec/sec")

             eqres = EQ_MotorStop(0)          ' Stop RA Motor
             If eqres <> EQ_OK Then
                 GoTo RARateEndhome1
             End If

         '    Do
         '       eqres = EQ_GetMotorStatus(0)
         '       If (eqres = EQ_NOTINITIALIZED) Or (eqres = EQ_COMNOTOPEN) Or (eqres = EQ_COMTIMEOUT) Then
         '            GoTo RARateEndhome1
         '       End If
         '    Loop While (eqres And EQ_MOTORBUSY) <> 0


             If RA_RATE = 0 Then
                 gSlewStatus = False
                 gRAStatus_slew = False
                 eqres = EQ_MotorStop(0)
                 gRAMoveAxis_Rate = 0
                 Exit Sub
             End If

             i = RA_RATE
             j = Abs(i)              'Get the absolute value for parameter passing

             If gMount_Ver = &H301 Then
               If (j > 1350) And (j <= 3000) Then
                 If j < 2175 Then
                     j = 1350
                 Else
                     j = 3001
                     k = 1
                     m = EQGP(0, 10003)
                 End If
               End If
             End If

             gRAMoveAxis_Rate = k    'Save Speed Settings

             HC.Add_FileMessage ("StartRARate=" & FormatNumber(RA_RATE, 5))
         '    j = Int((m * 9325.46154 / j) + 0.5) + 30000 'Compute for the rate
             j = Int((m * gTrackFactorRA / j) + 0.5) + 30000 'Compute for the rate

             If i >= 0 Then
                 eqres = EQ_SetCustomTrackRate(0, 1, j, k, gHemisphere, 0)
             Else
                 eqres = EQ_SetCustomTrackRate(0, 1, j, k, gHemisphere, 1)
             End If

         RARateEndhome1:

         End Sub
         */
         throw new NotImplementedException("StartRATrackingByRate");
      }

      private void ChangeRATrackingByRate(double rate)
      {
         /*
      ' Change RA motor rate based on an input rate of arcsec per Second

      Public Sub ChangeRA_by_Rate(ByVal rate As Double)

      Dim j As Double
      Dim k As Double
      Dim m As Double
      Dim dir As Long
      Dim init As Long

          If rate >= 0 Then
              dir = 0
          Else
              dir = 1
          End If

          If rate = 0 Then
              ' rate = 0 so stop motors
              gSlewStatus = False
              eqres = EQ_MotorStop(0)
              gRAStatus_slew = False
              gRAMoveAxis_Rate = 0
              Exit Sub
          End If

          k = 0   ' Assume low speed
          m = 1   ' Speed multiplier = 1

          init = 0
          j = Abs(rate)

          If gMount_Ver > &H301 Then
             ' if above high speed theshold
              If j > 1000 Then
                  k = 1               ' HIGH SPEED
                  m = EQGP(0, 10003)  ' GET HIGH SPEED MULTIPLIER
              End If
          Else
              ' who knows what Mon is up to here - a special for his mount perhaps?
              If gMount_Ver = &H301 Then
                  If (j > 1350) And (j <= 3000) Then
                      If j < 2175 Then
                          j = 1350
                      Else
                          j = 3001
                          k = 1
                          m = EQGP(0, 10003)
                      End If
                  End If
              End If
              ' if above high speed theshold
               If j > 3000 Then
                   k = 1               ' HIGH SPEED
                   m = EQGP(0, 10003)  ' GET HIGH SPEED MULTIPLIER
               End If
          End If

          HC.Add_FileMessage ("ChangeRARate=" & FormatNumber(rate, 5))

          ' if there's a switch between high/low speed or if operating at high speed
          ' we ned to do additional initialisation
          If k <> 0 Or k <> gRAMoveAxis_Rate Then init = 1

          If init = 1 Then
              ' Stop Motor
              HC.Add_FileMessage ("Direction or High/Low speed change")
              eqres = EQ_MotorStop(0)
              If eqres <> EQ_OK Then GoTo RARateEndhome2

      '        ' wait for motor to stop
      '        Do
      '          eqres = EQ_GetMotorStatus(0)
      '          If (eqres = EQ_NOTINITIALIZED) Or (eqres = EQ_COMNOTOPEN) Or (eqres = EQ_COMTIMEOUT) Then
      '               GoTo RARateEndhome2
      '          End If
      '        Loop While (eqres And EQ_MOTORBUSY) <> 0
              'force initialisation
          End If

          gRAMoveAxis_Rate = k

           'Compute for the rate
      '    j = Int((m * 9325.46154 / j) + 0.5) + 30000
          j = Int((m * gTrackFactorRA / j) + 0.5) + 30000

          eqres = EQ_SetCustomTrackRate(0, init, j, k, gHemisphere, dir)
          HC.Add_FileMessage ("EQ_SetCustomTrackRate=0," & CStr(init) & "," & CStr(j) & "," & CStr(k) & "," & CStr(gHemisphere) & "," & CStr(dir))
          HC.Add_Message (oLangDll.GetLangString(117) & "=" & str(rate) & " arcsec/sec" & "," & CStr(eqres))

      RARateEndhome2:

      End Sub
      */
         throw new NotImplementedException("ChangeRATrackingByRate");
      }

      private void StartDecTrackingByRate(double decRate)
      {
         /*
         ' Start DEC motor based on an input rate of arcsec per Second

         Public Sub StartDEC_by_Rate(ByVal DEC_RATE As Double)

         Dim i As Double
         Dim j As Double
         Dim k As Double
         Dim m As Double

             k = 0
             m = 1
             i = Abs(DEC_RATE)

             If gMount_Ver > &H301 Then
                 If i > 1000 Then
                     k = 1
                     m = EQGP(1, 10003)
                 End If
             Else
                 If i > 3000 Then
                     k = 1
                     m = EQGP(1, 10003)
                 End If
             End If


             HC.Add_Message (oLangDll.GetLangString(118) & " " & str(m) & " , " & str(DEC_RATE) & " arcsec/sec")

             eqres = EQ_MotorStop(1)          ' Stop RA Motor
             If eqres <> EQ_OK Then
                 GoTo DECRateEndhome1
             End If

         '    Do
         '       eqres = EQ_GetMotorStatus(1)
         '       If (eqres = EQ_NOTINITIALIZED) Or (eqres = EQ_COMNOTOPEN) Or (eqres = EQ_COMTIMEOUT) Then
         '            GoTo DECRateEndhome1
         '       End If
         '    Loop While (eqres And EQ_MOTORBUSY) <> 0

             If DEC_RATE = 0 Then
                 gSlewStatus = False
                 gRAStatus_slew = False
                 eqres = EQ_MotorStop(1)
                 gDECMoveAxis_Rate = 0
                 Exit Sub
             End If

             i = DEC_RATE
             j = Abs(i)              'Get the absolute value for parameter passing


             If gMount_Ver = &H301 Then
               If (j > 1350) And (j <= 3000) Then
                 If j < 2175 Then
                     j = 1350
                 Else
                     j = 3001
                     k = 1
                     m = EQGP(1, 10003)
                 End If
               End If
             End If


             gDECMoveAxis_Rate = k    'Save Speed Settings

             HC.Add_FileMessage ("StartDecRate=" & FormatNumber(DEC_RATE, 5))
         '    j = Int((m * 9325.46154 / j) + 0.5) + 30000 'Compute for the rate
             j = Int((m * gTrackFactorDEC / j) + 0.5) + 30000 'Compute for the rate

             If i >= 0 Then
                 eqres = EQ_SetCustomTrackRate(1, 1, j, k, gHemisphere, 0)
             Else
                 eqres = EQ_SetCustomTrackRate(1, 1, j, k, gHemisphere, 1)
             End If

         DECRateEndhome1:

         End Sub

         */
         throw new NotImplementedException("StartDecTrackingByRate");
      }

      private void ChangeDecTrackingByRate(double rate)
      {
         /*

               ' Change DEC motor rate based on an input rate of arcsec per Second

               Public Sub ChangeDEC_by_Rate(ByVal rate As Double)

               Dim j As Double
               Dim k As Double
               Dim m As Double
               Dim dir As Long
               Dim init As Long

                   If rate >= 0 Then
                       dir = 0
                   Else
                       dir = 1
                   End If

                   If rate = 0 Then
                       ' rate = 0 so stop motors
                       gSlewStatus = False
                       eqres = EQ_MotorStop(1)
               '        gRAStatus_slew = False
                       gDECMoveAxis_Rate = 0
                       Exit Sub
                   End If

                   k = 0   ' Assume low speed
                   m = 1   ' Speed multiplier = 1
                   init = 0
                   j = Abs(rate)

                   If gMount_Ver > &H301 Then
                      ' if above high speed theshold
                       If j > 1000 Then
                           k = 1               ' HIGH SPEED
                           m = EQGP(1, 10003)  ' GET HIGH SPEED MULTIPLIER
                       End If
                   Else
                       ' who knows what Mon is up to here - a special for his mount perhaps?
                       If gMount_Ver = &H301 Then
                           If (j > 1350) And (j <= 3000) Then
                               If j < 2175 Then
                                   j = 1350
                               Else
                                   j = 3001
                                   k = 1
                                   m = EQGP(1, 10003)
                               End If
                           End If
                       End If
                       ' if above high speed theshold
                        If j > 3000 Then
                            k = 1               ' HIGH SPEED
                            m = EQGP(1, 10003)  ' GET HIGH SPEED MULTIPLIER
                        End If
                   End If

                   HC.Add_FileMessage ("ChangeDECRate=" & FormatNumber(rate, 5))

                   ' if there's a switch between high/low speed or if operating at high speed
                   ' we need to do additional initialisation
                   If k <> 0 Or k <> gDECMoveAxis_Rate Then init = 1

                   If init = 1 Then
                       ' Stop Motor
                       HC.Add_FileMessage ("Direction or High/Low speed change")
                       eqres = EQ_MotorStop(1)
                       If eqres <> EQ_OK Then GoTo DECRateEndhome2

               '        ' wait for motor to stop
               '        Do
               '          eqres = EQ_GetMotorStatus(1)
               '          If (eqres = EQ_NOTINITIALIZED) Or (eqres = EQ_COMNOTOPEN) Or (eqres = EQ_COMTIMEOUT) Then
               '               GoTo DECRateEndhome2
               '          End If
               '        Loop While (eqres And EQ_MOTORBUSY) <> 0
                       'force initialisation
                   End If


                   gDECMoveAxis_Rate = k

                    'Compute for the rate
                   j = Int((m * gTrackFactorDEC / j) + 0.5) + 30000
               '    j = Int((m * 9325.46154 / j) + 0.5) + 30000

                   eqres = EQ_SetCustomTrackRate(1, init, j, k, gHemisphere, dir)
                   HC.Add_FileMessage ("EQ_SetCustomTrackRate=1," & CStr(init) & "," & CStr(j) & "," & CStr(k) & "," & CStr(gHemisphere) & "," & CStr(dir))
                   HC.Add_Message (oLangDll.GetLangString(118) & "=" & str(rate) & " arcsec/sec" & "," & CStr(eqres))

               DECRateEndhome2:

               EndSUB

               */
         throw new NotImplementedException("ChangeDecByRate");
      }

      #endregion



      #region Move Axis test methods ...
      private bool _buttonDown = false;
      private int _currentCompassButton = 0;
      /// <summary>
      /// A test method to allow the active compass button to be toggled
      /// when testing the slew buttons using CdC.
      /// </summary>
      public void ToggleCompassButton()
      {
         _currentCompassButton++;
         if (_currentCompassButton > 3)
         {
            _currentCompassButton = 0;
         }
         string label = "";
         switch (_currentCompassButton)
         {
            case 0:
               label = "N";
               break;
            case 1:
               label = "E";
               break;
            case 2:
               label = "S";
               break;
            case 3:
               label = "W";
               break;
         }
         System.Diagnostics.Debug.WriteLine($"Current compass button = {label}");
      }

      public void TestMoveAxis()
      {
         if (_buttonDown)
         {
            switch (_currentCompassButton)
            {
               case 0:  // N
               case 2:  // S
                  MoveAxis(TelescopeAxes.axisSecondary, 0.0);
                  break;
               case 1:  // E
               case 3:  // W
                  MoveAxis(TelescopeAxes.axisPrimary, 0.0);
                  break;
            }
            _buttonDown = false;
         }
         else
         {
            double rate = 400 * Constants.SIDEREAL_RATE_DEGREES;
            switch (_currentCompassButton)
            {
               case 0:  // N
               case 2:  // S
                  if (_currentCompassButton == 2)
                  {
                     System.Diagnostics.Debug.WriteLine("Slewing South");
                     rate = -rate;
                  }
                  else
                  {
                     System.Diagnostics.Debug.WriteLine("Slewing North");
                  }
                  MoveAxis(TelescopeAxes.axisSecondary, rate);
                  break;
               case 1:  // E
               case 3:  // W
                  if (_currentCompassButton == 3)
                  {
                     System.Diagnostics.Debug.WriteLine("Slewing West");
                     rate = -rate;
                  }
                  else
                  {
                     System.Diagnostics.Debug.WriteLine("Slewing East");
                  }
                  MoveAxis(TelescopeAxes.axisPrimary, rate);
                  break;
            }
            _buttonDown = true;
         }
      }

      #endregion

   }
}
