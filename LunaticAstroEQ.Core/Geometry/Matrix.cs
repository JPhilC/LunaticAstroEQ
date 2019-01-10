using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ASCOM.LunaticAstroEQ.Core.Geometry
{
   /// <summary>
   /// 3 x 3 matrix structure (because I couldn't find an algorithm to calculate
   /// the determinant of an n x n square matrix.
   /// </summary>
   public struct Matrix
   {
      double[,] _data;

      public int RowCount
      {
         get
         {
            return _data.GetLength(0);
         }
      }

      public int ColumnCount
      {
         get
         {
            return _data.GetLength(1);
         }
      }


      /// <summary>
      /// Returns the determinant of the current matrix
      /// </summary>
      public double Determinant
      {
         get
         {
            Debug.Assert((RowCount == ColumnCount && RowCount == 3), "Determinant not valid unless a 3x3 matrix.");
            //TODO: Replace this with a general calculation for n x n matrix.
            return (this[0, 0] * this[1, 1] * this[2, 2]) + (this[0, 1] * this[1, 2] * this[2, 0]) + (this[0, 2] * this[1, 0] * this[2, 1])
                    - (this[0, 2] * this[1, 1] * this[2, 0]) - (this[0, 1] * this[1, 0] * this[2, 2]) - (this[0, 0] * this[1, 2] * this[2, 1]);
         }
      }

      /// <summary>
      /// Returns the inverse of the current matrix.
      /// </summary>
      public Matrix Inverse
      {
         get
         {
            return GetInverse();
         }
      }

      /// <summary>
      /// Create a new matrix with rowCount rows and colCount columns
      /// </summary>
      /// <param name="rowCount"></param>
      /// <param name="colCount"></param>
      public Matrix(int rows, int cols, double initialValue)
      {
         _data = new double[rows, cols];
         for (int i = 0; i < rows; i++) {
            for (int j = 0; j < cols; j++) {
               _data[i, j] = initialValue;
            }
         }
      }


      public double this[int row, int col]
      {
         get
         {
            return _data[row, col];
         }
         set
         {
            _data[row, col] = value;
         }
      }

      public double[] this[int row]
      {
         get
         {
            double[] result = new double[ColumnCount];
            for (int i = 0; i < ColumnCount; i++) {
               result[i] = _data[row, i];
            }
            return result;
         }
         set
         {
            for (int i = 0; i < ColumnCount; i++) {
               _data[row, i] = value[i];
            }
         }
      }

      #region Operator overloads
      public static Matrix operator *(Matrix m1, Matrix m2)
      {
         Matrix result = new Matrix(m1.RowCount, m1.ColumnCount, 0.0);
         for (int i = 0; i < m1.RowCount; i++)
            for (int j = 0; j < m1.ColumnCount; j++) {
               result[i, j] = 0.0;
               for (int k = 0; k < m1.ColumnCount; k++) //multiplying row by column
                  result[i, j] += m1[i, k] * m2[k, j];
            }
         return result;
      }

      #endregion


      /// <summary>
      /// Copies a range of rows in reverse order onto a matching range of rows.
      /// </summary>
      /// <param name="srcStart">Start row</param>
      /// <param name="srcEnd">End row</param>
      /// <param name="targetStart">First target row</param>
      /// <param name="targetEnd">Last target row</param>
      public void TransposeRows(int srcStart, int srcEnd, int targetStart, int targetEnd)
      {
         if (srcStart == targetStart && srcEnd == targetEnd) {
            return;     // There is nothing to do.
         }
         Debug.Assert(srcStart >=0 && srcEnd >=0 && targetStart >=0 && targetEnd>=0, "Invalid range values.");
         Debug.Assert(Math.Abs(srcEnd - srcStart) == Math.Abs(targetEnd - targetStart), "Ranges are different lengths.");
         int rangeLength = Math.Abs(srcEnd - srcStart) + 1;
         bool increasingSrc = (srcEnd >= srcStart);
         bool increasingTarget = (targetEnd > targetStart);
         // Get an array of the source values
         double[,] srcValues = new double[rangeLength, ColumnCount];
         int sr = 0;
         if (increasingSrc) {
            for (int r = srcStart; r <= srcEnd; r++) {
               for (int c = 0; c < ColumnCount; c++) {
                  srcValues[sr, c] = _data[r, c];
               }
               sr++;
            }
         }
         else {
            for (int r = srcStart; r >= srcEnd; r--) {
               for (int c = 0; c < ColumnCount; c++) {
                  srcValues[sr, c] = _data[r, c];
               }
               sr++;
            }
         }
         // Now overwrite the original data
         int tr = targetStart;
         for (sr = 0; sr < rangeLength; sr++) {
            for (int c = 0; c < ColumnCount; c++) {
               _data[tr, c] = srcValues[sr, c];
            }
            if (increasingTarget) {
               tr++;
            }
            else {
               tr--;
            }
         }
      }

      private Matrix GetInverse()
      {
         //TODO fix up for m x b matrix.
         Debug.Assert((RowCount == ColumnCount && RowCount == 3), "GetInverse needs fixing for general M x N matrix");
         Matrix inverse = new Matrix(RowCount, ColumnCount, 0.0);
         double idet = 1.0 / this.Determinant;
         inverse[0, 0] = ((this[1, 1] * this[2, 2]) - (this[2, 1] * this[1, 2])) * idet;
         inverse[0, 1] = ((this[2, 1] * this[0, 2]) - (this[0, 1] * this[2, 2])) * idet;
         inverse[0, 2] = ((this[0, 1] * this[1, 2]) - (this[1, 1] * this[0, 2])) * idet;

         inverse[1, 0] = ((this[1, 2] * this[2, 0]) - (this[2, 2] * this[1, 0])) * idet;
         inverse[1, 1] = ((this[2, 2] * this[0, 0]) - (this[0, 2] * this[2, 0])) * idet;
         inverse[1, 2] = ((this[0, 2] * this[1, 0]) - (this[1, 2] * this[0, 0])) * idet;

         inverse[2, 0] = ((this[1, 0] * this[2, 1]) - (this[2, 0] * this[1, 1])) * idet;
         inverse[2, 1] = ((this[2, 0] * this[0, 1]) - (this[0, 0] * this[2, 1])) * idet;
         inverse[2, 2] = ((this[0, 0] * this[1, 1]) - (this[1, 0] * this[0, 1])) * idet;
         return inverse;
      }
   }

}
