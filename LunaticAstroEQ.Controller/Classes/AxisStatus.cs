using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASCOM.LunaticAstroEQ.Controller
{
   public struct AxisStatus
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
      public bool SlewingForward;
      public bool HighSpeed;
      public bool NotInitialized;

      public void SetFullStop()
      {
         FullStop = true;
         SlewingTo = Slewing = false;
      }
      public void SetSlewing(bool forward, bool highspeed)
      {
         FullStop = SlewingTo = false;
         Slewing = true;

         SlewingForward = forward;
         HighSpeed = highspeed;
      }
      public void SetSlewingTo(bool forward, bool highspeed)
      {
         FullStop = Slewing = false;
         SlewingTo = true;

         SlewingForward = forward;
         HighSpeed = highspeed;
      }

      //// Mask for axis status
      //public const long AXIS_FULL_STOPPED = 0x0001;		// 該軸處於完全停止狀態
      //public const long AXIS_SLEWING = 0x0002;			// 該軸處於恒速運行狀態
      //public const long AXIS_SLEWING_TO = 0x0004;		    // 該軸處於運行到指定目標位置的過程中
      //public const long AXIS_SLEWING_FORWARD = 0x0008;	// 該軸正向運轉
      //public const long AXIS_SLEWING_HIGHSPEED = 0x0010;	// 該軸處於高速運行狀態
      //public const long AXIS_NOT_INITIALIZED = 0x0020;    // MC控制器尚未初始化, axis is not initialized.
   }
}
