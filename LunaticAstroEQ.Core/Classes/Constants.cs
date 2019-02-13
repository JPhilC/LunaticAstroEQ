/*---------------------------------------------------------------------
   Copyright © 2017 Phil Crompton
   Permission is hereby granted to use this Software for any purpose
   including combining with commercial products, creating derivative
   works, and redistribution of source or binary code, without
   limitation or consideration. Any redistributed copies of this
   Software must include the above Copyright Notice.

   THIS SOFTWARE IS PROVIDED "AS IS". THE AUTHOR OF THIS CODE MAKES NO
   WARRANTIES REGARDING THIS SOFTWARE, EXPRESS OR IMPLIED, AS TO ITS
   SUITABILITY OR FITNESS FOR A PARTICULAR PURPOSE.
---------------------------------------------------------------------

CREDITS:
   Thanks must go to Raymund Sarmiento and Mr John Archbold for the 
   original EQMOD_ASCOM code on which protions of this code are based.

 ---------------------------------------------------------------------*/

using System;

namespace ASCOM.LunaticAstroEQ.Core
{
   public static class Constants
   {
      public const double UTIL_LOCAL2JULIAN_TIME_CORRECTION = 0.2;      // Don't know why but we seem to have to add 0.2 secs to get accurate RA/Dec -> AltAz conversions.
      public const double TENTH_SECOND = 27.778E-6;      // 1/10 second as a decimal degree or hour.

      public const double TWO_PI =        6.28318530718;  // 2 * Math.PI;
      public const double HALF_PI =       1.5707963268;  // Math.PI / 2;
      public const double ONEANDHALF_PI = 4.7123889804;  // Math.PI / 2;
      /// <summary>
      /// Sidereal rate in Radians
      /// </summary>
      public const double SIDEREAL_RATE_RADIANS = 7.29211585531E-5;        // TWO_PI / 86164.090530833, taken from Wikipedia for Mean Sidereal day length in seconds;
      /// <summary>
      /// Sidereal rate in Arc Seconds.
      /// </summary>
      public const double SIDEREAL_RATE_DEGREES = 4.1780746223E-3;           // degrees/sec  (360) / 86164.090530833;
      /// <summary>
      /// Sidereal rate in Arc Seconds.
      /// </summary>
      public const double SIDEREAL_RATE_ARCSECS = 15.0410686403;             // arcsecs/sec  (60*60*360) / 86164.090530833;
      /// <summary>
      /// Solar rate in Arc Seconds.
      /// </summary>
      public const double SOLAR_RATE_ARCSECS = 15.0;
      /// <summary>
      /// Lunar rate in Arc Seconds.
      /// </summary>
      public const double LUNAR_RATE_ARCSECS = 14.511415;                  // Modified from ASCOM settings (as per EQMOD)


      /// <summary>
      /// King rate in Arc Seconds.
      /// </summary>
      public const double KING_RATE_ARCSECS = 15.0369;                     // ASCOM list value acsseconds per second

      /// <summary>
      /// Ratio of from synodic (solar) to sidereal (stellar) rate used with RightAscensionRate property.
      /// </summary>
      public const double SIDEREAL_RATE = 0.9972695677;      // Use to convert from sideral seconds to S seconds.

      /// <summary>
      /// Radians per degree
      /// </summary>
      public const double DEG_RAD = 0.0174532925;           // Radians per degree
      /// <summary>
      /// Degrees per radian
      /// </summary>
      public const double RAD_DEG = 57.2957795;             // Degrees per Radian

      /// <summary>
      /// Radians per hour
      /// </summary>
      public const double HRS_RAD = 0.2617993881;           // Radians per hour
      /// <summary>
      /// Hours per radian
      /// </summary>
      public const double RAD_HRS = 3.81971863;             // Hours per radian

      /// <summary>
      /// Minutes per radian
      /// </summary>
      public const double RAD_MIN = 229.183118052;           // Minutes per radian
      /// <summary>
      /// Seconds per radian
      /// </summary>
      public const double RAD_SEC = 13750.9870831;          // Seconds per radian


      public const double ARCSECSTEP = 0.144;                  // .144 arcesconds / step

      // public const double SID_RATE = 15.041067;             // use SIDEREAL_RATE_ARCSECS instead
      public const double MAX_RATE = (800 * SIDEREAL_RATE_ARCSECS);

      public const double SECONDS_PER_SIDERIAL_DAY = 86164.0905;

      // Iterative GOTO Constants
      //Public Const NUM_SLEW_RETRIES As Long = 5              // Iterative MAX retries
      public const double RA_Allowed_diff = 10;                // Iterative Slew minimum difference


      // Home Position of the mount (pointing at NCP/SCP)

      /// <summary>
      /// RA home position (radians)
      /// </summary>
      public const double RAEncoder_Home_pos = 0;
      /// <summary>
      /// DEC home position (radians) start at 90 deg
      /// </summary>
      public const double DECEncoder_Home_pos = 90 * DEG_RAD;      // Start at 90 Degree position

      /// <summary>
      /// ENCODER 0 Hour initial position (radians)
      /// </summary>
      public const double RAEncoder_Zero_pos = 0;       // ENCODER 0 Hour initial position
      /// <summary>
      /// ENCODER 0 Degree Initial position (radians)
      /// </summary>
      public const double DECEncoder_Zero_pos = 0;      // 

      // public const double Default_step = 9024000;              // Total Encoder count (EQ5/6)



      //Public Const EQ_MAXSYNC = &H111700

      // Public Const EQ_MAXSYNC_Const = &H88B80                 // Allow a 45 degree discrepancy

      #region Driver constants ...
      public const int DRIVER_OK = 0x0;
      public const int DRIVER_COMNOTOPEN = 0x1;
      public const int DRIVER_COMTIMEOUT = 0x3;
      public const int DRIVER_MOTORBUSY = 0x10;
      public const int DRIVER_NOTINITIALIZED = 0xC8;
      public const int DRIVER_INVALIDCOORDINATE = 0x1000000;
      public const int DRIVER_INVALID = 0x3000000;
      #endregion


      #region Mount constants ...
      public const int MOUNT_SUCCESS = 0;           // Success (or connected for the first time);
      public const int MOUNT_NOCOMPORT = 1;         // Comport Not available
      public const int MOUNT_COMCONNECTED = 2;      // Mount already connected
      public const int MOUNT_COMERROR = 3;          // COM Timeout Error
      public const int MOUNT_MOTORBUSY = 4;         // Motor still busy
      public const int MOUNT_NONSTANDARD = 5;       // Mount Initialized on non-standard parameters
      public const int MOUNT_RARUNNING = 6;         // RA Motor still running
      public const int MOUNT_DECRUNNING = 7;        // DEC Motor still running 
      public const int MOUNT_RAERROR = 8;           // Error Initializing RA Motor
      public const int MOUNT_DECERROR = 9;          // Error Initilizing DEC Motor
      public const int MOUNT_MOUNTBUSY = 10;        // Cannot execute command at the current state
      public const int MOUNT_MOTORERROR = 11;       // Motor not initialized
      public const int MOUNT_GENERALERROR = 12;     //
      public const int MOUNT_MOTORINACTIVE = 200;   // Motor not initialized
      public const int MOUNT_EQMOUNT = 301;         // EQG series mount
      public const int MOUNT_NXMOUNT = 302;         // Nexstar series mount
      public const int MOUNT_LXMOUNT = 303;         // LX200 series mount
      public const int MOUNT_BADMOUNT = 998;        // Cant detect mount type
      public const int MOUNT_BADPARAM = 999;        // Invalid parameter

      public const int MOUNT_CONNECTED = 1;         // Connected to EQMOD
      public const int MOUNT_NOTCONNECTED = 0;      // Not connected to EQMOD
      #endregion

      #region Low level error constants ...
      public const int EQ_OK = 0x2000000;         // Success with no return values
      public const int EQ_OKRETURN = 0x0000000;   // 0x0999999 - Success with Mount Return Values
      public const int EQ_BADSTATE = 0x10000ff;   // Unexpected return value from mount
      public const int EQ_ERROR = 0x1000000;      // Bad command to send to mount
      public const int EQ_BADPACKET = 0x1000001;  // Missing or too many parameters
      public const int EQ_MOUNTBUSY = 0x1000002;  // Cannot execute command in current state
      public const int EQ_BADVALUE = 0x1000003;   // Bad Parameter Value
      public const int EQ_NOMOUNT = 0x1000004;    // Mount not enabled
      public const int EQ_COMTIMEOUT = 0x1000005; // Mount communications timeout
      public const int EQ_CRCERROR = 0x1000006;   // Data Packet CRC error
      public const int EQ_PPECERROR = 0x1000008;  // Data Packet CRC error
      public const int EQ_INVALID = 0x3000000;    // Invalid Parameter
      #endregion

   }

}
