using ASCOM.LunaticAstroEQ.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.LunaticAstroEQ
{
   public enum TrackFileFormat
   {
      Unknown,
      MPC,
      MPC2,
      JPL,
      JPL2

   }
   public class TrackRecord
   {
      public double TimeMJD { get; set; }
      public double DeltaRa { get; set; }
      public double DeltaDec { get; set; }
      public double RaRate { get; set; }
      public double DecRate { get; set; }
      public AxisDirection DecDirection { get; set; }
      public double RAJ2000 { get; set; }
      public double DECJ2000 { get; set; }
      public double RaRateRaw { get; set; }
      public double DECRateRaw { get; set; }
      public bool UseRate { get; set; }

   }

   public class TrackDefinition
   {
      public TrackFileFormat FileFormat { get; set; }
      public bool Precess { get; set; }
      public bool IsWaypoint { get; set; }
      public double RAAdjustment { get; set; }
      public double DECAdjustment { get; set; }
      public int TrackIdx { get; set; }
      public bool TrackingChangesEnabled { get; set; }

      private List<TrackRecord> _TrackSchedule = null;
      public List<TrackRecord> TrackSchedule
      {
         get
         {
            return _TrackSchedule;
         }
      }

      public TrackDefinition()
      {
         _TrackSchedule = new List<TrackRecord>();
      }
   }

}
