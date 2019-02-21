/*
BSD 2-Clause License

Copyright (c) 2019, Philip Crompton
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

using ASCOM.DeviceInterface;
using System;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreConstants = ASCOM.LunaticAstroEQ.Core.Constants;
using System.Diagnostics;
using ASCOM.LunaticAstroEQ.Core;

namespace ASCOM.LunaticAstroEQ
{
   partial class Telescope
   {
      private Timer _PulseGuidingTimer;
      private Stopwatch[] _PulseGuidingStopwatch;
      private int[] _PulseGuideDuration = new int[] { 0, 0 };
      private object[] _PulseGuidingLock = new object[] { new object(), new object() };

      private void InitialisePulseGuidingTimer()
      {
         _PulseGuidingStopwatch = new Stopwatch[] { new Stopwatch(), new Stopwatch() };
         _PulseGuidingTimer = new System.Timers.Timer(Settings.PulseGuidingInterval); // Initialise the pulse guiding timer with a 1 milisecond interval.
         _PulseGuidingTimer.Elapsed += PulseGuidingTimer_Elapsed;

      }

      private void DisposePulseGuidingTimer()
      {
         if (_PulseGuidingTimer != null)
         {
            _PulseGuidingTimer.Enabled = false;
            _PulseGuidingTimer.Elapsed -= PulseGuidingTimer_Elapsed;
            _PulseGuidingTimer.Dispose();
            _PulseGuidingTimer = null;
         }
      }

      private void StartPulseGuiding(GuideDirections direction, int duration)
      {
         _PulseGuidingTimer.Enabled = false;
         double decRate = 0.0;
         double raRate = 0.0;
         switch (direction)
         {
            case GuideDirections.guideNorth:
               lock (_PulseGuidingLock[DEC_AXIS])
               {
                  decRate = Settings.DeclinationRate + (Settings.GuideRateDeclination * 3600.0);
                  _PulseGuideDuration[DEC_AXIS] = duration;
                  Controller.MCStartTrackingRate(AxisId.Axis2_Dec, decRate, Hemisphere, (Hemisphere == HemisphereOption.Northern ? AxisDirection.Forward : AxisDirection.Reverse));
                  _PulseGuidingStopwatch[DEC_AXIS].Restart();
                  _PulseGuidingTimer.Enabled = true;
               }
               break;

            case GuideDirections.guideSouth:
               lock (_PulseGuidingLock[DEC_AXIS])
               {
                  decRate = Settings.DeclinationRate - (Settings.GuideRateDeclination * 3600.0);
                  _PulseGuideDuration[DEC_AXIS] = duration;
                  Controller.MCStartTrackingRate(AxisId.Axis2_Dec, decRate, Hemisphere, (Hemisphere == HemisphereOption.Northern ? AxisDirection.Forward : AxisDirection.Reverse));
                  _PulseGuidingStopwatch[DEC_AXIS].Restart();
                  _PulseGuidingTimer.Enabled = true;
               }
               break;
            case GuideDirections.guideEast:
               lock (_PulseGuidingLock[RA_AXIS])
               {
                  if (Tracking)
                  {
                     raRate = Settings.DriveRateValue[Settings.TrackingRate] + (Settings.RightAscensionRate * CoreConstants.SIDEREAL_RATE * 15.0) - (Settings.GuideRateRightAscension * 3600.0);
                  }
                  else
                  {
                     raRate = -(Settings.GuideRateRightAscension * 3600.0);
                  }
                  _PulseGuideDuration[RA_AXIS] = duration;
                  Controller.MCStartTrackingRate(AxisId.Axis1_RA, raRate, Hemisphere, (Hemisphere == HemisphereOption.Northern ? AxisDirection.Forward : AxisDirection.Reverse));
                  _PulseGuidingStopwatch[RA_AXIS].Restart();
                  _PulseGuidingTimer.Enabled = true;
               }
               break;
            case GuideDirections.guideWest:
               lock (_PulseGuidingLock[RA_AXIS])
               {
                  if (Tracking)
                  {
                     raRate = Settings.DriveRateValue[Settings.TrackingRate] + (Settings.RightAscensionRate * CoreConstants.SIDEREAL_RATE * 15.0) + (Settings.GuideRateRightAscension * 3600.0);
                  }
                  else
                  {
                     raRate = Settings.GuideRateRightAscension * 3600.0;
                  }
                  _PulseGuideDuration[RA_AXIS] = duration;
                  Controller.MCStartTrackingRate(AxisId.Axis1_RA, raRate, Hemisphere, (Hemisphere == HemisphereOption.Northern ? AxisDirection.Forward : AxisDirection.Reverse));
                  _PulseGuidingStopwatch[RA_AXIS].Restart();
                  _PulseGuidingTimer.Enabled = true;
               }
               break;
            default:
               // Shouldn't be able to get here
               throw new ASCOM.InvalidValueException("Unrecognised guide direction.");

         }
      }

      private void StopPulseGuiding(GuideDirections direction)
      {
         double decRate = 0.0;
         double raRate = 0.0;
         switch (direction)
         {
            case GuideDirections.guideNorth:
            case GuideDirections.guideSouth:
               lock (_PulseGuidingLock[DEC_AXIS])
               {
                  _PulseGuideDuration[DEC_AXIS] = 0;
                  _PulseGuidingStopwatch[DEC_AXIS].Stop();
                  decRate = Settings.DeclinationRate;
                  if (decRate != 0.0)
                  {
                     Controller.MCStartTrackingRate(AxisId.Axis2_Dec, decRate, Hemisphere, (Hemisphere == HemisphereOption.Northern ? AxisDirection.Forward : AxisDirection.Reverse));
                  }
                  else
                  {
                     Controller.MCAxisStop(AxisId.Axis2_Dec);
                  }
               }
               break;

            case GuideDirections.guideEast:
            case GuideDirections.guideWest:
               lock (_PulseGuidingLock[DEC_AXIS])
               {
                  _PulseGuideDuration[RA_AXIS] = 0;
                  _PulseGuidingStopwatch[RA_AXIS].Stop();
                  if (Tracking)
                  {
                     raRate = Settings.DriveRateValue[Settings.TrackingRate] + (Settings.RightAscensionRate * CoreConstants.SIDEREAL_RATE * 15.0);
                     Controller.MCStartTrackingRate(AxisId.Axis1_RA, raRate, Hemisphere, (Hemisphere == HemisphereOption.Northern ? AxisDirection.Forward : AxisDirection.Reverse));
                  }
                  else
                  {
                     Controller.MCAxisStop(AxisId.Axis1_RA);
                  }
               }
               break;
            default:
               // Shouldn't be able to get here
               throw new ASCOM.InvalidValueException("Unrecognised guide direction.");

         }
         if (!_PulseGuidingStopwatch[DEC_AXIS].IsRunning && !_PulseGuidingStopwatch[RA_AXIS].IsRunning)
         {
            _PulseGuidingTimer.Enabled = false;
         }

      }

      private void PulseGuidingTimer_Elapsed(object sender, ElapsedEventArgs e)
      {

         lock (_PulseGuidingLock[DEC_AXIS])
         {
            if (_PulseGuidingStopwatch[DEC_AXIS].IsRunning && _PulseGuidingStopwatch[DEC_AXIS].ElapsedMilliseconds > _PulseGuideDuration[DEC_AXIS] + (Settings.PulseGuidingInterval * 0.5))
            {
               // Stop Dec pulse guiding
               StopPulseGuiding(GuideDirections.guideNorth);
            }
         }
         lock (_PulseGuidingLock[DEC_AXIS])
         {
            if (_PulseGuidingStopwatch[RA_AXIS].IsRunning && _PulseGuidingStopwatch[RA_AXIS].ElapsedMilliseconds > _PulseGuideDuration[RA_AXIS] + (Settings.PulseGuidingInterval * 0.5))
            {
               StopPulseGuiding(GuideDirections.guideEast);
            }
         }
         if (!_PulseGuidingStopwatch[DEC_AXIS].IsRunning && !_PulseGuidingStopwatch[RA_AXIS].IsRunning)
         {
            _PulseGuidingTimer.Enabled = false;
         }
      }
   }
}
