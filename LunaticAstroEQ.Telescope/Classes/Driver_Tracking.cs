/*
BSD 2-Clause License

Copyright (c) 2019, LunaticSoftware.org, Email: phil@lunaticsoftware.org
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this
  list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE. 
*/

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
        private double _axisPositionTolerance = 0.00001;

        private PierSide _previousPointingSOP = PierSide.pierUnknown;
        private AxisPosition _previousAxisPosition;
        private bool _wasMoving = false;
        private AxisRates[] _AxisRates = new AxisRates[3];
        private AxisState[] _AxisState = new AxisState[2];

        private string CustomTrackFile = string.Empty;           // 
        private TrackDefinition CustomTrackDefinition = null;

        // Flag set a the beginning of a Goto to trigger a refinement after the first goto ends
        private bool _RefineGoto = false;

        private void InitialiseCurrentPosition(bool firstConnection)
        {
            lock (Controller)
            {
                DateTime now = DateTime.Now;
                // This method is only called if this is the first connection to the mount
                // so override saved ParkStatus and always start off parked.
                _ParkedAxisPosition = Settings.AxisParkPosition;
                if (firstConnection)
                {
                    _CurrentPosition = new MountCoordinate(_ParkedAxisPosition, _AscomToolsCurrentPosition, now);
                    _previousAxisPosition = _ParkedAxisPosition;
                    Controller.MCSetAxisPosition(_ParkedAxisPosition);
                }
                else
                {
                    _previousAxisPosition = Controller.MCGetAxisPositions();
                    _CurrentPosition = new MountCoordinate(_previousAxisPosition, _AscomToolsCurrentPosition, now);
                }
                //if (Settings.ParkStatus != ParkStatus.Parked)
                //{
                //   Settings.ParkStatus = ParkStatus.Parked;
                //   SaveSettings();
                //}

                // Get the MoveAxis value limits.
                InitialiseAxisRates();

                // Get the controllers current axis states and see if we need to 
                // start tracking.
                _AxisState = Controller.MCGetAxesStates();
                if (TrackingState != TrackingStatus.Off)
                {
                    if ((!_AxisState[RA_AXIS].Slewing && !_AxisState[DEC_AXIS].Slewing))
                    {
                        StartTracking();
                    }
                }
            }

        }

        /// <summary>
        /// Initialised the axis rates used to validate MoveAxis values.
        /// </summary>
        private void InitialiseAxisRates()
        {
            // Set the axis rates (used with the MoveAxis command).
            double[] maxRates = Controller.MCGetMaxRates();
            AxisRates raAxisRates = new AxisRates(TelescopeAxes.axisPrimary, 0.0, maxRates[RA_AXIS]);
            AxisRates decAxisRates = new AxisRates(TelescopeAxes.axisSecondary, 0.0, maxRates[DEC_AXIS]);
            AxisRates terRate = new AxisRates(TelescopeAxes.axisTertiary, 0.0, 0.0);


            // Form the time being the maximum rate is not coming back from the controller
            _AxisRates = new AxisRates[] { raAxisRates, decAxisRates, terRate };
        }


        double? _previousDeltaRa;
        bool _decFlippedConfirmed = false;
        /// <summary>
        /// Refreshes the NCP and current positions if more than 500ms have elapsed since the last refresh.
        /// </summary>
        private void RefreshCurrentPosition()
        {
            DateTime now = DateTime.Now;
            bool axisHasMoved = false;
            AxisPosition axisPosition = _previousAxisPosition;
            lock (Controller)
            {
                _AxisState = Controller.MCGetAxesStates();
                axisPosition = Controller.MCGetAxisPositions();
            }
#if DEBUG
            //string raStatus = Convert.ToString(axesStatus[RA_AXIS], 2);
            //string decStatus = Convert.ToString(axesStatus[DEC_AXIS], 2); ;
            //System.Diagnostics.Debug.WriteLine($"\nAxis states - RA: {raStatus}, Dec : {decStatus}\n");
            //AxisState raStatus = _AxisState[RA_AXIS];
            //AxisState decStatus = _AxisState[DEC_AXIS];
            //System.Diagnostics.Debug.WriteLine($"\nRA : Slewing-{raStatus.Slewing}, SlewingTo-{raStatus.SlewingTo}, Forward-{!raStatus.MeshedForReverse}, FullStop-{raStatus.FullStop}, Tracking-{raStatus.Tracking}");
            //System.Diagnostics.Debug.WriteLine($"Dec: Slewing-{decStatus.Slewing}, SlewingTo-{decStatus.SlewingTo}, Forward-{!decStatus.MeshedForReverse}, FullStop-{decStatus.FullStop}, Tracking-{decStatus.Tracking}");
#endif
            /// if (!axisPosition.Equals(_previousAxisPosition, _axisPositionTolerance))
            if (!_AxisState[RA_AXIS].FullStop || !_AxisState[DEC_AXIS].FullStop)
            {
                // One or the other axis is moving
                axisHasMoved = true;
                if (_previousPointingSOP == PierSide.pierUnknown)
                {
                    // First move away from home (NCP/SCP) position.
                    if (_TargetPosition != null)
                    {
                        System.Diagnostics.Debug.WriteLine("Initialising SOP from goto target.");
                        System.Diagnostics.Debug.WriteLine($"Target Axes: {_TargetPosition.ObservedAxes.RAAxis.Value}/{_TargetPosition.ObservedAxes.DecAxis.Value}\t{_TargetPosition.ObservedAxes.DecFlipped}\tRA/Dec: {_TargetPosition.Equatorial.RightAscension}/{_TargetPosition.Equatorial.Declination}\t{_TargetPosition.PointingSideOfPier}");
                        axisPosition.DecFlipped = _TargetPosition.ObservedAxes.DecFlipped;
                        _previousPointingSOP = _TargetPosition.PointingSideOfPier;
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
                // Check if parking.
                if (Settings.ParkStatus == ParkStatus.Parking)
                {
                    LogMessage("Command", "Park - complete");
                    // Update the parked axis position decflipped setting if it isn't set.
                    _ParkedAxisPosition.DecFlipped = _previousAxisPosition.DecFlipped;
                    if (axisPosition == _ParkedAxisPosition)
                    {
                        Settings.ParkStatus = ParkStatus.Parked;
                        SaveSettings();
                    }
                    else
                    {
                        // Need to keep going
                        Controller.MCAxisSlewTo(_ParkedAxisPosition, Hemisphere);
                    }
                }

                axisPosition.DecFlipped = _previousAxisPosition.DecFlipped;
            }

            // Update current position
            _CurrentPosition.MoveRADec(axisPosition, _AscomToolsCurrentPosition, now);

            if (!_decFlippedConfirmed && _CurrentPosition.PointingSideOfPier != PierSide.pierUnknown)
            {
                double ha = AstroConvert.RangeHA(SiderealTime - _CurrentPosition.Equatorial.RightAscension.Value);
                if (_CurrentPosition.PointingSideOfPier == PierSide.pierWest)
                {
                    if (ha >= 0)
                    {
                        // need to flip the DEC axis as it is reporting the wrong RA
                        axisPosition.DecFlipped = !axisPosition.DecFlipped;
                        _CurrentPosition.MoveRADec(axisPosition, _AscomToolsCurrentPosition, now);
                    }
                }
                else
                {
                    if (ha < 0)
                    {
                        // need to flip the DEC axis as it is reporting the wrong RA
                        axisPosition.DecFlipped = !axisPosition.DecFlipped;
                        _CurrentPosition.MoveRADec(axisPosition, _AscomToolsCurrentPosition, now);
                    }
                }
                _decFlippedConfirmed = true;
            }

            //// Just check that we have the DecFlipped setting correct
            //if (!_decFlippedConfirmed && _IsSlewing)
            //{
            //   double deltaRa = _TargetPosition.Equatorial.RightAscension - _CurrentPosition.Equatorial.RightAscension;
            //   if (_previousDeltaRa.HasValue)
            //   {
            //      if (deltaRa > _previousDeltaRa.Value)
            //      {
            //         // Going in the wrong direction so flip the Dec axis
            //         axisPosition.DecFlipped = !axisPosition.DecFlipped;
            //         _CurrentPosition.MoveRADec(axisPosition, _AscomToolsCurrentPosition, now);
            //      }
            //      _decFlippedConfirmed = true;
            //   }
            //   else {
            //      // Get it on the next cycle
            //      _previousDeltaRa = deltaRa;
            //   }
            //}

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


            if (Settings.ParkStatus == ParkStatus.Parking)
            {
                System.Diagnostics.Debug.WriteLine($"Parking to: {_ParkedAxisPosition.RAAxis.Value}/{_ParkedAxisPosition.RAAxis.Value}\t{_ParkedAxisPosition.DecFlipped}");
            }

            if (_TargetPosition != null)
            {
                System.Diagnostics.Debug.WriteLine($"Target Axes: {_TargetPosition.ObservedAxes.RAAxis.Value}/{_TargetPosition.ObservedAxes.DecAxis.Value}\t{_TargetPosition.ObservedAxes.DecFlipped}\tRA/Dec: {_TargetPosition.Equatorial.RightAscension}/{_TargetPosition.Equatorial.Declination}\t{_TargetPosition.PointingSideOfPier}");
            }
            if (Settings.ParkStatus == ParkStatus.Parked)
            {
                System.Diagnostics.Debug.WriteLine("Parked");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Current Axes: {axisPosition.RAAxis.Value}/{axisPosition.DecAxis.Value}\t{axisPosition.DecFlipped}\tRA/Dec: {_CurrentPosition.Equatorial.RightAscension}/{_CurrentPosition.Equatorial.Declination}\t{_CurrentPosition.PointingSideOfPier}\n");
            }

            _previousAxisPosition = axisPosition;
            // See if we can sort out the initial SideOfPier
            if (_previousPointingSOP == PierSide.pierUnknown)
            {
                // Only initialise the previous pointing SOP if we know for certain what it should be
                if (axisHasMoved && (_IsSlewing || _IsMoveAxisSlewing))
                {
                    // Remember the current pointing side of the pier.
                    // Until the SOP has been determined from a SLEW or GOTO it should not be updated.
                    _previousPointingSOP = _CurrentPosition.PointingSideOfPier;
                }
            }
            else
            {
                _previousPointingSOP = _CurrentPosition.PointingSideOfPier;
            }

            // If the axis has stopped moving there are some bit to clear up
            if (_wasMoving && !axisHasMoved)
            {
                // See if we need to refine a goto.
                if (_RefineGoto)
                {
                    _RefineGoto = false;
                    System.Diagnostics.Debug.WriteLine("Refining GOTO");
                    SlewToEquatorialCoordinate(TargetRightAscension, TargetDeclination);
                }
                else
                {
                    if (Settings.ParkStatus == ParkStatus.Unparked)
                    {
                        // Check if slew finished
                        if (_IsSlewing)
                        {
                            _IsSlewing = false;
                            // Announce("Slew complete.");
                        }
                        if (_IsMoveAxisSlewing)
                        {
                            _IsMoveAxisSlewing = false;
                        }
                        if (!_IsMoveAxisSlewing && !_IsSlewing)
                        {
                            if (TrackingState != TrackingStatus.Off)
                            {
                                System.Diagnostics.Debug.WriteLine("Restarting tracking");
                                LogMessage("Command", "Restarting tracking {0}", TrackingState);
                                StartTracking();
                            }
                        }
                    }
                }
            }

            _wasMoving = axisHasMoved;
        }

        private void RelocateMounts(double latitude, double longitude, double elevation)
        {
            _AscomToolsCurrentPosition.Transform.SiteLongitude = longitude;
            _AscomToolsTargetPosition.Transform.SiteLongitude = longitude;
            _AscomToolsCurrentPosition.Transform.SiteLatitude = latitude;
            _AscomToolsTargetPosition.Transform.SiteLatitude = latitude;
            _AscomToolsCurrentPosition.Transform.SiteElevation = elevation;
            _AscomToolsTargetPosition.Transform.SiteElevation = elevation;

            // Refresh the current positiona using the new longitude
            _CurrentPosition = new MountCoordinate(_CurrentPosition.ObservedAxes, _AscomToolsCurrentPosition, _CurrentPosition.SyncTime);
            if (_TargetPosition != null)
            {
                _TargetPosition = new MountCoordinate(_TargetPosition.ObservedAxes, _AscomToolsTargetPosition, _TargetPosition.SyncTime);
            }
            RefreshCurrentPosition();
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
                _IsMoveAxisSlewing = false;
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

                    // Clear tracking settings.
                    Settings.TrackingState = TrackingStatus.Off;

                    // Set status to Parking
                    Settings.ParkStatus = ParkStatus.Parking;
                    SaveSettings();
                    Controller.MCAxisSlewTo(_ParkedAxisPosition, Hemisphere);
                    if (Settings.AscomCompliance.UseSynchronousParking)
                    {
                        // Block until the park completes
                        while (Settings.ParkStatus != ParkStatus.Parked)
                        {
                            System.Diagnostics.Debug.WriteLine("Waiting for 5 seconds");
                            // wait 5 seconds
                            Thread.Sleep(2000);
                            _AxisState = Controller.MCGetAxesStates();
                        }
                    }
                    // If not using synchronous parking the usual refresh current possition handles everything.
                }
            }
        }

        #region Slewing and goto ...
        private PierSide GetDestinationSideOfPier(double rightAscension, double declination)
        {
            DateTime currentTime = DateTime.Now;
            AxisPosition targetAxisPosition = _CurrentPosition.GetAxisPositionForRADec(rightAscension, declination, _AscomToolsCurrentPosition);
            MountCoordinate targetPosition = new MountCoordinate(targetAxisPosition, _AscomToolsTargetPosition, currentTime);
            return targetPosition.PointingSideOfPier;
        }

        private void SlewToEquatorialCoordinate(double rightAscension, double declination)
        {
            lock (Controller)
            {
                TargetRightAscension = rightAscension;
                TargetDeclination = declination;
                LogMessage("SlewToCoordinates", "RA:{0}/Dec:{1}", _AscomToolsCurrentPosition.Util.HoursToHMS(rightAscension, "h", "m", "s"), _AscomToolsCurrentPosition.Util.DegreesToDMS(declination, ":", ":"));
                DateTime currentTime = DateTime.Now;
                Controller.MCAxisStop(AxisId.Both_Axes); // Stop the axes moving to get distances
                AxisPosition currentAxisPosition = _CurrentPosition.ObservedAxes;
                AxisPosition targetAxisPosition = _CurrentPosition.GetAxisPositionForRADec(rightAscension, declination, _AscomToolsCurrentPosition);

                double slewSeconds = Controller.MCGetSlewTimeEstimate(targetAxisPosition, Hemisphere);

                // Get a refined target position allowing for slew time.
                targetAxisPosition = _CurrentPosition.GetAxisPositionForRADec(rightAscension, declination, _AscomToolsCurrentPosition, slewSeconds);

                _TargetPosition = new MountCoordinate(targetAxisPosition, _AscomToolsTargetPosition, currentTime);
                System.Diagnostics.Debug.WriteLine($"Current Physical SOP: { currentAxisPosition.PhysicalSideOfPier}\t\tPointing SOP: {_CurrentPosition.GetPointingSideOfPier(false)}");
                System.Diagnostics.Debug.WriteLine($" Target Physical SOP: { targetAxisPosition.PhysicalSideOfPier}\t\tPointing SOP: {_TargetPosition.GetPointingSideOfPier(false)}");
                System.Diagnostics.Debug.WriteLine($" PreviousAxisPosition: { _previousAxisPosition}\t\tPrevious DecFlipped: {_previousAxisPosition.DecFlipped}");
                System.Diagnostics.Debug.WriteLine($" Previous Pointing SOP: { _previousPointingSOP}\t\tPrevious DecFlipped: {_previousAxisPosition.DecFlipped}");

                _IsSlewing = true;
                Controller.MCAxisSlewTo(targetAxisPosition, Hemisphere);
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
                SaveSettings();

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

                SaveSettings();
            }
        }


        private void StartTracking()
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
                    double raRate = 0.0;
                    double decRate = 0.0;
                    double adjustedRaRate = 0.0;
                    if (Settings.DeclinationRate == 0.0)
                    {
                        //  Stop DEC motor
                        Controller.MCAxisStop(AxisId.Axis2_Dec);
                        switch (Settings.TrackingRate)
                        {
                            case DriveRates.driveKing:
                                Settings.TrackingState = TrackingStatus.King;
                                break;
                            case DriveRates.driveLunar:
                                Settings.TrackingState = TrackingStatus.Lunar;
                                break;
                            case DriveRates.driveSidereal:
                                Settings.TrackingState = TrackingStatus.Sidereal;
                                break;
                            case DriveRates.driveSolar:
                                Settings.TrackingState = TrackingStatus.Solar;
                                break;
                            default:
                                throw new ASCOM.InvalidValueException("Unexpected tracking rate set.");
                        }
                    }
                    else
                    {
                        decRate = Settings.DeclinationRate;
                        Controller.MCStartTrackingRate(AxisId.Axis2_Dec, decRate, Hemisphere, (Hemisphere == HemisphereOption.Northern ? AxisDirection.Forward : AxisDirection.Reverse));
                        Settings.TrackingState = TrackingStatus.Custom;
                    }

                    raRate = Settings.DriveRateValue[Settings.TrackingRate];
                    if (Settings.RightAscensionRate == 0)
                    {
                        adjustedRaRate = raRate;
                    }
                    else
                    {
                        // RightAscensionRate is in seconds or RA per sidereal second. The divisor below
                        // converts this to seconds of RA per SI second as expected by the controller
                        // the 15 converts from seconds of time to arcseconds.
                        adjustedRaRate = raRate + (Settings.RightAscensionRate * CoreConstants.SIDEREAL_RATE * 15.0);
                    }
                    Controller.MCStartTrackingRate(AxisId.Axis1_RA, adjustedRaRate, Hemisphere, (Hemisphere == HemisphereOption.Northern ? AxisDirection.Forward : AxisDirection.Reverse));


                    SaveSettings();
                }
            }
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
                SaveSettings();
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
