using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASCOM.LunaticAstroEQ.Controller
{
   public static class Constants
   {
      public const double SIDEREALRATE = 2 * Math.PI / 86164.09065;

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
