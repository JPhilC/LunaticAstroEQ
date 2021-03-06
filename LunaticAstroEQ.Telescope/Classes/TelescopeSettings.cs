﻿/*
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

using ASCOM.DeviceInterface;
using ASCOM.LunaticAstroEQ.Core;
using ASCOM.LunaticAstroEQ.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreConstants = ASCOM.LunaticAstroEQ.Core.Constants;

namespace ASCOM.LunaticAstroEQ
{
   public class TelescopeSettings
   {
      public RetryOption Retry { get; set; }
      //Timeout=1000
      public TimeOutOption Timeout { get; set; }
      //Baud=9600
      public BaudRate BaudRate { get; set; }

      //Port=COM4
      public string COMPort { get; set; }

      // TRACE_STATE
      public bool TracingState { get; set; }


      // The main timer's interval for updating current position 
      // and checking status changes.
      public int RefreshInterval { get; set; }

      /// <summary>
      /// Polar scope bightness value, set to the mount using the V command.
      /// </summary>
      public int PolarSlopeBrightness { get; set; }

      public ParkStatus ParkStatus { get; set; }

      public AxisPosition AxisUnparkPosition { get; set; }
      public AxisPosition AxisParkPosition { get; set; }


      #region Tracking related ...
      public double[] CustomTrackingRate { get; set; } = new double[2];

      public TrackingStatus TrackingState { get; set; }
      public DriveRates TrackingRate { get; set; }

      public string CustomTrackName { get; set; }

      public double RightAscensionRate { get; set; }

      public double DeclinationRate { get; set; }
      #endregion

      #region Guiding related ...
      public double GuideRateDeclination { get; set; }
      public double GuideRateRightAscension { get; set; }
      /// <summary>
      /// Minimum guide rate declination (degrees/sec);
      /// </summary>
      public double GuideRateDeclinationMin { get; set; }
      /// <summary>
      /// Maximum guide rate declination (degrees/sec);
      /// </summary>
      public double GuideRateDeclinationMax { get; set; }
      /// <summary>
      /// Minimum guide rate right ascension (degrees/sec);
      /// </summary>
      public double GuideRateRightAscensionMin { get; set; }
      /// <summary>
      /// Maxiumum guide rate right ascension (degrees/sec);
      /// </summary>
      public double GuideRateRightAscensionMax { get; set; }

      /// <summary>
      /// The pulse guiding time interval (milliseconds)
      /// </summary>
      public int PulseGuidingInterval { get; set; }
      #endregion

      public Dictionary<DriveRates, double> DriveRateValue { get; set; } = new Dictionary<DriveRates, double>();

      public AscomCompliance AscomCompliance { get; set; }

      public TelescopeSettings()
      {
         SetDefaults();
      }


      private void SetDefaults()
      {
         COMPort = "COM3";
         BaudRate = BaudRate.Baud9600;
         Timeout = TimeOutOption.TO2000;
         Retry = RetryOption.Once;
         RefreshInterval = 250;
         PolarSlopeBrightness = 125;
         CustomTrackingRate = new double[] { 0.0D, 0.0D};
         AscomCompliance = new AscomCompliance();
         AxisParkPosition = new AxisPosition(0.0, 0.0);
         AxisUnparkPosition = new AxisPosition(0.0, 0.0);
         CustomTrackingRate = new double[] { 0.0, 0.0 };
         TrackingState = TrackingStatus.Off;
         TrackingRate = DriveRates.driveSidereal;

         DriveRateValue = new Dictionary<DriveRates, double>{
         { DriveRates.driveSidereal, CoreConstants.SIDEREAL_RATE_ARCSECS},
         { DriveRates.driveLunar, CoreConstants.LUNAR_RATE_ARCSECS},
         { DriveRates.driveSolar, CoreConstants.SOLAR_RATE_ARCSECS},
         { DriveRates.driveKing, CoreConstants.KING_RATE_ARCSECS}};

         GuideRateDeclination = 0.0;         // Degrees/Sec
         GuideRateRightAscension = 0.0;      // Degrees/Sec
         GuideRateDeclinationMin = 0.0;      // Degrees/Sec
         GuideRateDeclinationMax = 10.0;     // Degrees/Sec
         GuideRateRightAscensionMin = 0.0;   // Degrees/Sec
         GuideRateRightAscensionMax = 10.0;  // Degrees/Sec
         PulseGuidingInterval = 20;          // Milliseconds
      }
   }
}
