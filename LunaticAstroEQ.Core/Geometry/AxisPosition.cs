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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.LunaticAstroEQ.Core.Geometry
{

   /// <summary>
   /// A structure to represent telecope mount axis positions
   /// </summary>
   public struct AxisPosition
   {
      private Angle _RAAxis;
      private Angle _DecAxis;

      [JsonProperty]
      public Angle RAAxis
      {
         get
         {
            return _RAAxis;
         }
         private set
         {
            _RAAxis = value;
         }
      }
      
      [JsonProperty]
      public Angle DecAxis
      {
         get
         {
            return _DecAxis;
         }
         private set
         {
            _DecAxis = value;
         }
      }

      public bool DecFlipped { get; set; }

      public int AxisCount
      {
         get
         {
            return 2;
         }
      }

      public PierSide PhysicalSideOfPier
      {
         get
         {
            return RAAxis < 180.0 ? PierSide.pierEast : PierSide.pierWest;
         }
      }

      /// <summary>
      /// Create a new axis position
      /// </summary>
      /// <param name="ra">RA axis angle in degrees</param>
      /// <param name="dec">Dec axis angle in degrees</param>
      public AxisPosition(double ra, double dec, bool decFlipped = false, bool radians = false)
      {
         if (radians)
         {
            _RAAxis = Angle.Range360(Angle.RadiansToDegrees(ra));
            _DecAxis = Angle.Range360(Angle.RadiansToDegrees(dec));
            DecFlipped = decFlipped;
         }
         else
         {
            _RAAxis = Angle.Range360(ra);
            _DecAxis = Angle.Range360(dec);
            DecFlipped = decFlipped;
         }
      }


      public AxisPosition(string axisPositions, bool decFlipped = false)
      {
         string[] positions = axisPositions.Split(',');
         try
         {
            _RAAxis = Angle.Range360(double.Parse(positions[0]));
            _DecAxis = Angle.Range360(double.Parse(positions[1]));
            DecFlipped = decFlipped;
         }
         catch
         {
            throw new ArgumentException("Badly formed axis position string");
         }
      }


      /// <summary>
      /// The axis position in degrees
      /// </summary>
      /// <param name="index"></param>
      /// <returns></returns>
      public double this[int index]
      {
         get
         {
            if (index < 0 || index > 1)
            {
               throw new ArgumentOutOfRangeException();
            }
            return (index == 0 ? _RAAxis.Value : _DecAxis.Value);
         }
         set
         {
            if (index < 0 || index > 1)
            {
               throw new ArgumentOutOfRangeException();
            }
            if (index == 0)
            {
               _RAAxis = value;
            }
            else
            {
               _DecAxis = value;
            }
         }
      }

      /// <summary>
      /// Compares the two specified sets of Axis positions.
      /// </summary>
      public static bool operator ==(AxisPosition pos1, AxisPosition pos2)
      {
         return (pos1.RAAxis == pos2.RAAxis && pos1.DecAxis == pos2.DecAxis);
      }

      public static bool operator !=(AxisPosition pos1, AxisPosition pos2)
      {
         return !(pos1 == pos2);
      }

      public static AxisPosition operator -(AxisPosition pos1, AxisPosition pos2)
      {
         return new AxisPosition(pos1.RAAxis.Value - pos2.RAAxis.Value, pos1.DecAxis.Value - pos2.DecAxis.Value);
      }

      public static AxisPosition operator +(AxisPosition pos1, AxisPosition pos2)
      {
         return new AxisPosition(pos1.RAAxis.Value + pos2.RAAxis.Value, pos1.DecAxis.Value + pos2.DecAxis.Value);
      }

      public override int GetHashCode()
      {
         unchecked // Overflow is fine, just wrap
         {
            int hash = 17;
            // Suitable nullity checks etc, of course :)
            hash = hash * 23 + _RAAxis.GetHashCode();
            hash = hash * 23 + _DecAxis.GetHashCode();
            return hash;
         }
      }

      public override bool Equals(object obj)
      {
         return (obj is AxisPosition
                 && this == (AxisPosition)obj);
      }


      public bool Equals(AxisPosition obj, double toleranceDegrees)
      {
         double deltaRA = Math.Abs(obj.RAAxis.Value - this.RAAxis.Value);
         deltaRA = (deltaRA + 180) % 360 - 180;
         double deltaDec = Math.Abs(obj.DecAxis.Value - this.DecAxis.Value);
         deltaDec = (deltaDec + 180) % 360 - 180;
         System.Diagnostics.Debug.WriteLine($"Delta RA/Dec = {deltaRA:##0.000000000}/{deltaDec:#0.000000000}, Tol: {toleranceDegrees:#0.000000000}");
         return (deltaRA <= toleranceDegrees
            && deltaDec <= toleranceDegrees);
      }

      public override string ToString()
      {
         return string.Format("RAAxis = {0}°, DecAxis = {1}°", Math.Round(_RAAxis.Value, 2), Math.Round(_DecAxis.Value, 2));
      }
      public string ToDegreesString()
      {
         return string.Format("{0},{1}", _RAAxis.Value, _DecAxis.Value);
      }
      public string ToRadiansString()
      {
         return string.Format("{0},{1}", _RAAxis.Radians, _DecAxis.Radians);
      }

      /// <summary>
      /// Flip and axis position as would happen on a telescope doing a meridian flip.
      /// </summary>
      /// <returns></returns>
      public AxisPosition Flip()
      {
         return new AxisPosition(this.RAAxis + 180.0, (this.DecAxis * -1).Range360());
      }

      public Angle[] GetSlewAnglesTo(AxisPosition targetPosition)
      {
         double[] slewAngle = new double[2];
         for (int i = 0; i < 2; i++)
         {
            if (this[i] >= 0.0 && this[i] <= 180.0 && targetPosition[i] > 180.0 && targetPosition[i] <= 360.0)
            {
               slewAngle[i] = -1 * (this[i] + 360.0 - targetPosition[i]);
            }
            else if (this[i] > 180.0 && this[i] <= 360.0 && targetPosition[i] >= 0.0 && targetPosition[i] <= 180.0)
            {
               slewAngle[i] = 360.0 - this[i] + targetPosition[i];
            }
            else
            {
               slewAngle[i] = targetPosition[i] - this[i];
            }
         }
         return new Angle[] { new Angle(slewAngle[0]), new Angle(slewAngle[1])};
      }


      //public double[] GetDeltaTo(AxisPosition targetPosition)
      //{
      //   double[] delta = new double[2];
      //   for (int i = 0; i < 2; i++)
      //   {
      //      delta[i] = targetPosition[i] - this[i];
      //   }
      //   return delta;
      //}

   }
}
