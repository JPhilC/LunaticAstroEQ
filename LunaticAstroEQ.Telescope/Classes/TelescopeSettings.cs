using ASCOM.LunaticAstroEQ.Core;
using ASCOM.LunaticAstroEQ.Core.Geometry;
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



      public ParkStatus ParkStatus { get; set; }

      public AxisPosition AxisUnparkPosition { get; set; }
      public AxisPosition AxisParkPosition { get; set; }
      public double[] CustomTrackingRate { get; set; } = new double[2];

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
         CustomTrackingRate = new double[] { 0.0D, 0.0D};
         AscomCompliance = new AscomCompliance();

      }
   }
}
