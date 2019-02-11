using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASCOM.LunaticAstroEQ.Controller
{
   public struct AxisState
   {
      /// <summary>
      /// 4 different state
      /// 1. FullStop
      /// 2. Slewing
      /// 3. SlewingTo
      /// 4. Notinitialized
      /// </summary>

      public bool FullStop;
      public bool Slewing;
      public bool SlewingTo;
      public bool MeshedForReverse;
      public bool NotInitialized;
      public bool HighSpeed;
      public bool Tracking;
      public double TrackingRate;

      public void SetSlewingTo(bool forward, bool highspeed)
      {
         SlewingTo = true;
         Slewing = false;
         Tracking = false;
         TrackingRate = 0.0;
         HighSpeed = highspeed;
         MeshedForReverse = !forward;
      }

      public void SetSlewing(bool forward, bool highspeed)
      {
         Slewing = true;
         SlewingTo = false;
         Tracking = false;
         TrackingRate = 0.0;
         HighSpeed = highspeed;
         MeshedForReverse = !forward;
      }

      public void SetStopped()
      {
         FullStop = true;
         HighSpeed = false;
         Slewing = false;
         SlewingTo = false;
         Tracking = false;
         TrackingRate = 0.0;
      }


      public void SetTracking(bool tracking, double trackingRate)
      {
         Tracking = tracking;
         if (tracking)
         {
            TrackingRate = trackingRate;
         }
         else
         {
            TrackingRate = 0.0;
         }
      }

      //// Mask for axis status
      //public const long AXIS_FULL_STOPPED = 0x0001;		   // [00 0001] The axis is completely stopped
      //public const long AXIS_SLEWING = 0x0002;			   // [00 0010] The axis is at constant speed
      //public const long AXIS_SLEWING_TO = 0x0004;		   // [00 0100] The axis is in the process of running to the specified target position
      //public const long AXIS_SLEWING_FORWARD = 0x0008;	   // [00 1000] The axis is running in the forward direction
      //public const long AXIS_SLEWING_HIGHSPEED = 0x0010;	// [01 0000] The axis is running at high speed
      //public const long AXIS_NOT_INITIALIZED = 0x0020;    // [10 0000]The axis has not been initialized.
   }
}
