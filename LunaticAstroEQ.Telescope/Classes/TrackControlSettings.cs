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
