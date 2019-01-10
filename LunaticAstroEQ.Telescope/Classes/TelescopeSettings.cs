using ASCOM.LunaticAstroEQ.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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


      public double? SiteLatitude;
      public double? SiteLongitude;
      public double? SiteElevation;

      public double StartAltitude { get; set; }
      public double StartAzimuth { get; set; }


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
         SiteLatitude = null;
         SiteLongitude = null;
         SiteElevation = null;

      }
   }
}
