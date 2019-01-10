using ASCOM.Astrometry.Transform;
using ASCOM.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LunaticAstroEQ.Core
{
   /// <summary>
   /// Fit an affine transformation to given point sets.
   /// More precisely: solve(least squares fit) matrix 'A'and 't' from
   /// 'p ~= A*q+t', given vectors 'p' and 'q'.
   /// Works with arbitrary dimensional vectors(2d, 3d, 4d...).
   ///
   /// Written by Jarno Elonen<elonen@iki.fi> in 2007.
   /// Placed in Public Domain.
   ///
   /// Based on paper "Fitting affine and orthogonal transformations
   /// between two sets of points, by Helmuth Späth (2003).
   /// </summary>
   public class Affine
   {
      double[][] _from;
      double[][] _to;
      int _axisCount = 0;
      int _coordinateCount = 0;
      Matrix _c;
      Matrix _Q;
      Matrix _M;

      public Affine(List<MountCoordinate> coordinates, Transform transform)
      {
         _coordinateCount = coordinates.Count;
         _axisCount = 2;   // We are working with X & Y determined from AltAzimuth coordinates
         if (_coordinateCount < _axisCount) {
            throw new ArgumentOutOfRangeException("Too few axis positions provided.");
         }
         _from = new double[_coordinateCount][];
         _to = new double[_coordinateCount][];
         int i = 0;
         foreach (MountCoordinate mc in coordinates) {
            AltAzCoordinate suggestedAltAzimuth = mc.GetAltAzimuth(transform);
            _from[i] = new double[2] { suggestedAltAzimuth.X, suggestedAltAzimuth.Y };
            _to[i] = new double[2] { mc.AltAzimuth.X, mc.AltAzimuth.Y };
            i++;
         }
         if (!GenerateTransformationMatrix()) {
            throw new ArgumentException("Unable to generate affine transformation matrix");
         }
      }

      public Affine(double[][] from, double[][] to)
      {
         _coordinateCount = from.Length;
         _axisCount = from[0].Length;
         if (_coordinateCount < _axisCount) {
            throw new ArgumentOutOfRangeException("Too few axis positions provided.");
         }
         //         _Coordinates = coordinates.ToArray();
         _from = from;
         _to = to;
         if (!GenerateTransformationMatrix()) {
            throw new ArgumentException("Unable to generate affine transformation matrix");
         }
      }


      public AltAzCoordinate Transform(AltAzCoordinate from)
      {
         double[] to = new double[2];
         for (int j = 0; j < _axisCount; j++) {
            for (int i = 0; i < _axisCount; i++) {
               to[j] += from[i] * _M[i, j + _axisCount + 1];
            }
            to[j] += _M[_axisCount, j + _axisCount + 1];
         }
         return AltAzCoordinate.FromCartesean(to[0], to[1]);
      }

      public double[] Transform(double[] from)
      {
         double[] to = new double[2];
         for (int j = 0; j < _axisCount; j++) {
            for (int i = 0; i < _axisCount; i++) {
               to[j] += from[i] * _M[i, j + _axisCount + 1];
            }
            to[j] += _M[_axisCount, j + _axisCount + 1];
         }
         return to;
      }

      private bool GenerateTransformationMatrix()
      {
         Matrix affine = new Matrix(_axisCount, _axisCount + 1, 0.0);
         // c - Make an empty (dim) x (dim+1) matrix and fill it
         _c = new Matrix(_axisCount + 1, _axisCount, 0.0);
         for (int j = 0; j < _axisCount; j++) {
            for (int k = 0; k < _axisCount + 1; k++) {
               for (int i = 0; i < _coordinateCount; i++) {
                  double[] qt = new double[] {
                     //_Coordinates[i].SuggestedAltAzimuth[0],
                     //_Coordinates[i].SuggestedAltAzimuth[1],
                     _from[i][0],
                     _from[i][1],
                     1.0 };
                  _c[k, j] += qt[k] * _to[i][j];
               }
            }
         }
         // Q - Make an empty (dim+1) x (dim+1) matrix and fill it
         _Q = new Matrix(_axisCount + 1, _axisCount + 1, 0.0);
         foreach (double[] qi in _from) {
            double[] qt = new double[] { qi[0], qi[1], 1.0 };
            for (int i = 0; i < _axisCount + 1; i++) {
               for (int j = 0; j < _axisCount + 1; j++) {
                  _Q[i, j] += qt[i] * qt[j];
               }
            }
         }
         // Augement Q with c and solve Q * a' = c by Gauss-Jordan
         _M = new Matrix(_axisCount + 1, _Q.ColumnCount + _c.ColumnCount, 0.0);
         for (int i = 0; i < _axisCount + 1; i++) {
            var z = new double[_Q.ColumnCount + _c.ColumnCount];
            _Q[i].CopyTo(z, 0);
            _c[i].CopyTo(z, _Q[i].Length);
            _M[i] = z;
         }
         return GaussJordanSolve();
      }
      /// <summary>
      /// Puts given matrix (2D array) into the Reduced Row Echelon Form.
      /// Returns True if successful, False if 'm' is singular.
      /// NOTE: make sure all the matrix items support fractions! Int matrix will NOT work!
      /// Written by Jarno Elonen in April 2005, released into Public Domain
      /// </summary>
      /// <param name="m"></param>
      /// <param name="tolerance"></param>
      private bool GaussJordanSolve(double eps = 1e-10)
      {
         int h = _M.RowCount;
         int w = _M.ColumnCount;
         int maxRow;
         for (int y = 0; y < h; y++) {
            maxRow = y;
            for (int y2 = y + 1; y2 < h; y2++) {
               if (Math.Abs(_M[y2, y]) > Math.Abs(_M[maxRow, y])) {
                  maxRow = y2;
               }
            }
            // Py: (_M[y], _M[maxrow]) = (_M[maxrow], _M[y]) (Need to replicate this in C#)
            _M.TransposeRows(maxRow, y, y, maxRow);

            if (Math.Abs(_M[y, y]) <= eps) {     //  Singular?
               return false;
            }
            for (int y2 = y + 1; y2 < h; y2++) {    // Eliminate column y
               double c = _M[y2, y] / _M[y, y];
               for (int x = y; x < w; x++) {
                  _M[y2, x] -= _M[y, x] * c;
               }
            }
         }
         for (int y = h - 1; y >= 0; y--) { // Backsubstiture 
            double c = _M[y, y];
            for (int y2 = 0; y2 < y; y2++) {
               for (int x = w - 1; x > y - 1; x--) {
                  _M[y2, x] -= _M[y, x] * _M[y2, y] / c;
               }
            }
            _M[y, y] /= c;
            for (int x = h; x < w; x++) {   // Normalize row y
               _M[y, x] /= c;
            }
         }
         return true;
      }

      /*
 def gauss_jordan(m, eps = 1.0/(10**10)):
      """Puts given matrix (2D array) into the Reduced Row Echelon Form.
         Returns True if successful, False if 'm' is singular.
         NOTE: make sure all the matrix items support fractions! Int matrix will NOT work!
         Written by Jarno Elonen in April 2005, released into Public Domain"""
      (h, w) = (len(m), len(m[0]))
      for y in range(0,h):
        maxrow = y
        for y2 in range(y+1, h):    # Find max pivot
          if abs(m[y2][y]) > abs(m[maxrow][y]):
            maxrow = y2
        (m[y], m[maxrow]) = (m[maxrow], m[y])
        if abs(m[y][y]) <= eps:     # Singular?
          return False
        for y2 in range(y+1, h):    # Eliminate column y
          c = m[y2][y] / m[y][y]
          for x in range(y, w):
            m[y2][x] -= m[y][x] * c
      for y in range(h-1, 0-1, -1): # Backsubstitute
        c  = m[y][y]
        for y2 in range(0,y):
          for x in range(w-1, y-1, -1):
            m[y2][x] -=  m[y][x] * m[y2][y] / c
        m[y][y] /= c
        for x in range(h, w):       # Normalize row y
          m[y][x] /= c
      return True       */
   }

}
