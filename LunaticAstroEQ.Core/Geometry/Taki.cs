/*
BSD 2-Clause License

Copyright (c) 2019, Philip Crompton, Email: phil@lunaticsoftware.org
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

/**
  * Library for coordinates transformations. Calculates the equivalent coordinates between both coordinate systems equatorial and horizontal.
  *
  * It's based on Toshimi Taki's matrix method for coordinates transformation: http://www.geocities.jp/toshimi_taki/matrix/matrix.htm
  * Contains the necessary methods for setting the initial time, the reference objects, the transformation matrix, and to 
  * calculate the equivalent vectors between both coordinate systems.
  * 
  * Found on in the Github project:
  *    https://github.com/juanrmn/Arduino-Telescope-Control
  */


/// NOTE: Barking up the wrong tree on this one with regard to 
/// interpolating axis positions using alignment points.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.LunaticAstroEQ.Core.Geometry
{
   public abstract class TakiBase
   {
      // Auxiliary matrices
      protected Vector _lmn1;
      protected Vector _LMN1;
      protected Vector _lmn2;
      protected Vector _LMN2;
      protected Vector _lmn3;
      protected Vector _LMN3;

      protected Matrix _T;     // Transformation matrix. Transform vectors from equatorial to horizontal system.

      protected Matrix _iT;     // Inverse transformation matrix. Transform vectors from horizontal to equatorial system.

      /// <summary>
      /// Sets the transformation matrix and its inverse (T and iT, respectively).
      /// </summary>
      protected void setT()
      {
         Matrix subT1 = new Matrix(3, 3, 0.0);
         Matrix subT2 = new Matrix(3, 3, 0.0);


         subT1[0, 0] = _lmn1[0]; subT1[0, 1] = _lmn2[0]; subT1[0, 2] = _lmn3[0];
         subT1[1, 0] = _lmn1[1]; subT1[1, 1] = _lmn2[1]; subT1[1, 2] = _lmn3[1];
         subT1[2, 0] = _lmn1[2]; subT1[2, 1] = _lmn2[2]; subT1[2, 2] = _lmn3[2];

         subT2[0, 0] = _LMN1[0]; subT2[0, 1] = _LMN2[0]; subT2[0, 2] = _LMN3[0];
         subT2[1, 0] = _LMN1[1]; subT2[1, 1] = _LMN2[1]; subT2[1, 2] = _LMN3[1];
         subT2[2, 0] = _LMN1[2]; subT2[2, 1] = _LMN2[2]; subT2[2, 2] = _LMN3[2];

         //  _inv(subT2, aux);
         Matrix aux = subT2.Inverse;


         //  _m_prod(subT1, aux, _T);
         _T = subT1 * aux;
         //  _inv(_T, _iT);
         _iT = _T.Inverse;
      }

      /// <summary>
      /// Third reference object calculated from the cross product of the two first ones.
      /// </summary>
      protected void GenRef3()
      {
         double sqrt1, sqrt2;
         _lmn3 = new Vector(3);
         _LMN3 = new Vector(3);
         sqrt1 = (1 / (Math.Sqrt(Math.Pow(((_lmn1[1] * _lmn2[2]) - (_lmn1[2] * _lmn2[1])), 2) +
                     Math.Pow(((_lmn1[2] * _lmn2[0]) - (_lmn1[0] * _lmn2[2])), 2) +
                     Math.Pow(((_lmn1[0] * _lmn2[1]) - (_lmn1[1] * _lmn2[0])), 2))
            ));
         _lmn3[0] = sqrt1 * ((_lmn1[1] * _lmn2[2]) - (_lmn1[2] * _lmn2[1]));
         _lmn3[1] = sqrt1 * ((_lmn1[2] * _lmn2[0]) - (_lmn1[0] * _lmn2[2]));
         _lmn3[2] = sqrt1 * ((_lmn1[0] * _lmn2[1]) - (_lmn1[1] * _lmn2[0]));

         sqrt2 = (1 / (Math.Sqrt(Math.Pow(((_LMN1[1] * _LMN2[2]) - (_LMN1[2] * _LMN2[1])), 2) +
                        Math.Pow(((_LMN1[2] * _LMN2[0]) - (_LMN1[0] * _LMN2[2])), 2) +
                        Math.Pow(((_LMN1[0] * _LMN2[1]) - (_LMN1[1] * _LMN2[0])), 2))
               ));
         _LMN3[0] = sqrt2 * ((_LMN1[1] * _LMN2[2]) - (_LMN1[2] * _LMN2[1]));
         _LMN3[1] = sqrt2 * ((_LMN1[2] * _LMN2[0]) - (_LMN1[0] * _LMN2[2]));
         _LMN3[2] = sqrt2 * ((_LMN1[0] * _LMN2[1]) - (_LMN1[1] * _LMN2[0]));
      }


   }


   /// <summary>
   /// A class containing the logic of Toshimi Taki's matrix method for mapping between
   /// equatorial coordinates and telescope axis positions.
   /// </summary>
   public class TakiEQMountMapper:TakiBase
   {
      /// <summary>
      /// Constant of multiplication for the solar and sidereal time relation.
      /// </summary>
      const double _k = 1.002737908;    // // Constant.. Relationship between the solar time (M) and the sidereal time (S): (S = M * 1.002737908)
      DateTime _timeZero;


      /// <summary>
      /// Get or set the initial sidereal time.
      /// </summary>
      public DateTime Time
      {
         get
         {
            return _timeZero;
         }
      }


      public TakiEQMountMapper(MountCoordinate ref1, MountCoordinate ref2, MountCoordinate ref3, DateTime timeZero)
      {
         _timeZero = timeZero;
         _lmn1 = GetHVC(ref1.ObservedAxes);
         _LMN1 = GetEVC(ref1.Equatorial, ref1.SyncTime);
         _lmn2 = GetHVC(ref2.ObservedAxes);
         _LMN2 = GetEVC(ref2.Equatorial, ref3.SyncTime);
         _lmn3 = GetHVC(ref3.ObservedAxes);
         _LMN3 = GetEVC(ref3.Equatorial, ref3.SyncTime);
         setT();
      }

      public TakiEQMountMapper(MountCoordinate ref1, MountCoordinate ref2, DateTime timeZero)
      {
         _timeZero = timeZero;
         _lmn1 = GetHVC(ref1.ObservedAxes);
         _LMN1 = GetEVC(ref1.Equatorial, ref1.SyncTime);
         _lmn2 = GetHVC(ref2.ObservedAxes);
         _LMN2 = GetEVC(ref2.Equatorial, ref2.SyncTime);
         // 
         GenRef3();
         setT();
      }

      public AxisPosition GetAxisPosition(EquatorialCoordinate eq, DateTime targetTime)
      {
         Vector EVC = GetEVC(eq, targetTime);
         Vector HVC = new Vector(0.0, 0.0, 0.0);
         for (int i = 0; i < 3; i++) {
            for (int j = 0; j < 3; j++) {
               HVC[i] += _T[i, j] * EVC[j];
            }
         }

         return new AxisPosition(AstroConvert.Range2Pi(Math.Atan2(HVC[1], HVC[0])), AstroConvert.Range2Pi(Math.Asin(HVC[2])), true);
      }

      public EquatorialCoordinate GetEquatorialCoords(AxisPosition axes, DateTime localTime)
      {
         Vector EVC = new Vector(0.0, 0.0, 0.0);
         Vector HVC = GetHVC(axes);
         double deltaTime = AstroConvert.HrsToRad(localTime - _timeZero);   
         for (int i = 0; i < 3; i++) {
            for (int j = 0; j < 3; j++) {
               EVC[i] += _iT[i, j] * HVC[j];
            }
         }
         return new EquatorialCoordinate(Math.Atan2(EVC[1], EVC[0]) + (_k * deltaTime), Math.Asin(EVC[2]));
      }


      /// <summary>
      ///  Obtains a vector in polar notation from the equatorial coordinates and the observation time.
      /// </summary>
      /// <param name="coord"></param>
      /// <param name="time"></param>
      /// <returns></returns>
      private Vector GetEVC(EquatorialCoordinate coord, DateTime localTime)
      {
         double deltaTime = AstroConvert.HrsToRad(localTime - _timeZero);  
         Vector evc = new Vector(0.0, 0.0, 0.0);
         evc[0] = Math.Cos(coord.Declination.Radians) * Math.Cos(coord.RightAscension.Radians - (_k * deltaTime));
         evc[1] = Math.Cos(coord.Declination.Radians) * Math.Sin(coord.RightAscension.Radians - (_k * deltaTime));
         evc[2] = Math.Sin(coord.Declination.Radians);
         return evc;
      }

      private Vector GetHVC(AxisPosition axes)
      {
         Vector hvc = new Vector(0.0, 0.0, 0.0);
         hvc[0] = Math.Cos(axes.DecAxis.Radians) * Math.Cos(axes.RAAxis.Radians);
         hvc[1] = Math.Cos(axes.DecAxis.Radians) * Math.Sin(axes.RAAxis.Radians);
         hvc[2] = Math.Sin(axes.DecAxis.Radians);
         return hvc;
      }

   }


   ///// <summary>
   ///// A class based on the logic of Tosimi Taki's matrix method of coordinate mapping
   ///// modified for interpoliting observed axis pospositios from theoretical positisions.
   ///// </summary>
   //public class TakiAlignmentMapper:TakiBase
   //{

   //   public TakiAlignmentMapper(MountCoordinate ref1, MountCoordinate ref2, MountCoordinate ref3)
   //   {
   //      _LMN1 = GetVC(ref1.SuggestedAxes);
   //      _lmn1 = GetVC(ref1.ObservedAxes);
   //      _LMN2 = GetVC(ref2.SuggestedAxes);
   //      _lmn2 = GetVC(ref2.ObservedAxes);
   //      _LMN3 = GetVC(ref3.SuggestedAxes);
   //      _lmn3 = GetVC(ref3.ObservedAxes);
   //      setT();
   //   }

   //   public TakiAlignmentMapper(MountCoordinate ref1, MountCoordinate ref2)
   //   {
   //      _LMN1 = GetVC(ref1.SuggestedAxes);
   //      _lmn1 = GetVC(ref1.ObservedAxes);
   //      _LMN2 = GetVC(ref2.SuggestedAxes);
   //      _lmn2 = GetVC(ref2.ObservedAxes);
   //      // 
   //      GenRef3();
   //      setT();
   //   }

   //   public AxisPosition GetObservedPosition(AxisPosition theoretical)
   //   {

   //      Vector TVC = GetVC(theoretical);
   //      Vector OVC = new Vector(0.0, 0.0, 0.0);

   //      for (int i = 0; i < 3; i++) {
   //         for (int j = 0; j < 3; j++) {
   //            OVC[i] += _T[i, j] * OVC[j];
   //         }
   //      }
   //      return new AxisPosition(Math.Atan2(OVC[1], OVC[0]), Math.Asin(OVC[2]), true);
   //   }

   //   public AxisPosition GetTheoreticalPosition(AxisPosition observed)
   //   {
   //      Vector TVC = new Vector(0.0, 0.0, 0.0);
   //      Vector OVC = GetVC(observed);

   //      for (int i = 0; i < 3; i++) {
   //         for (int j = 0; j < 3; j++) {
   //            TVC[i] += _iT[i, j] * TVC[j];
   //         }
   //      }
   //      return new AxisPosition(Math.Atan2(TVC[1], TVC[0]), Math.Asin(TVC[2]), true);
   //   }


   //   private Vector GetVC(AxisPosition axes)
   //   {
   //      Vector hvc = new Vector(0.0, 0.0, 0.0);
   //      hvc[0] = Math.Cos(axes.DecAxis) * Math.Cos(axes.RAAxis);
   //      hvc[1] = Math.Cos(axes.DecAxis) * Math.Sin(axes.RAAxis);
   //      hvc[2] = Math.Sin(axes.DecAxis);
   //      return hvc;
   //   }

   //}

}
