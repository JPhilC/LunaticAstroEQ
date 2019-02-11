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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.LunaticAstroEQ.Core.Geometry
{
   /// <summary>
   /// A structure to represent an EquatorialCoordinate
   /// </summary>
   public class EquatorialCoordinate
   {
      private HourAngle _RA;
      private Angle _Dec;
      //private DateTime _ObservedWhen;
      //private Angle _Longitude;

      public HourAngle RightAscension
      {
         get
         {
            return _RA;
         }
      }
      public Angle Declination
      {
         get
         {
            return _Dec;
         }
      }

      public EquatorialCoordinate()   // , double longitude, DateTime observedTime)
      {
         _RA = new HourAngle(0.0);
         _Dec = new Angle(0.0);
      }

      public EquatorialCoordinate(double rightAscension, double declination):this()   // , double longitude, DateTime observedTime)
      {
         if (declination > 90.0 || declination < -90.0)
         {
            throw new ArgumentOutOfRangeException("Declination");
         }
         _RA.Value = AstroConvert.RangeRA(rightAscension);
         _Dec.Value = declination;
      }


      public EquatorialCoordinate(HourAngle rightAscension, Angle declination):this()   // , Angle longitude, DateTime observedTime)
      {
         if (declination > 90.0 || declination < -90.0)
         {
            throw new ArgumentOutOfRangeException("Declination");
         }
         _RA.Value = AstroConvert.RangeRA(rightAscension.Value);
         _Dec = declination.Value;
      }

      #region Operator overloads ...
      /// <summary>
      /// Compares the two specified sets of Axis positions.
      /// </summary>
      public static bool operator ==(EquatorialCoordinate pos1, EquatorialCoordinate pos2)
      {
         return (pos1.RightAscension.Value == pos2.RightAscension.Value && pos1.Declination.Value == pos2.Declination.Value);
      }

      public static bool operator !=(EquatorialCoordinate pos1, EquatorialCoordinate pos2)
      {
         return !(pos1 == pos2);
      }

      public override int GetHashCode()
      {
         unchecked // Overflow is fine, just wrap
         {
            int hash = 17;
            // Suitable nullity checks etc, of course :)
            hash = hash * 23 + _RA.GetHashCode();
            hash = hash * 23 + _Dec.GetHashCode();
            return hash;
         }
      }

      public override bool Equals(object obj)
      {
         return (obj is EquatorialCoordinate
                 && this == (EquatorialCoordinate)obj);
      }

      public static EquatorialCoordinate operator -(EquatorialCoordinate pos1, EquatorialCoordinate pos2)
      {
         return new EquatorialCoordinate(pos1.RightAscension - pos2.RightAscension, pos1.Declination - pos2.Declination);
      }

      public static EquatorialCoordinate operator +(EquatorialCoordinate pos1, EquatorialCoordinate pos2)
      {
         return new EquatorialCoordinate(pos1.RightAscension + pos2.RightAscension, pos1.Declination + pos2.Declination);
      }


      public override string ToString()
      {
         return string.Format("{0}/{1}", _RA, _Dec);
      }
      #endregion

      public CarteseanCoordinate ToCartesean(Angle latitude, bool affineTaki = true)
      {
         CarteseanCoordinate cartCoord;
         if (affineTaki) {
            // Get Polar (or should than be get AltAzimuth) from Equatorial coordinate (formerly call to EQ_SphericalPolar)
            AltAzCoordinate polar = AstroConvert.GetAltAz(this, latitude);
            // Get  Cartesean from Polar (formerly call to EQ_Polar2Cartes)
            cartCoord = polar.ToCartesean();
         }
         else {
            cartCoord = new CarteseanCoordinate(this.RightAscension.Radians, this.Declination.Radians, 1.0);
         }
         return cartCoord;
      }


   }

}
