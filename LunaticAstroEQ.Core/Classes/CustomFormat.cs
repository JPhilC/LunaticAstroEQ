using System;
using System.Diagnostics;
using System.Globalization;

namespace ASCOM.LunaticAstroEQ.Core
{
   public static class CustomFormat
   {
      /// <summary>
      /// Formats the value according to Culture,Number of Digits (Resolution) and Units 
      /// </summary>
      /// <param name="value">value to format</param>
      /// <param name="numberDecimalDigits"> number of decimal places to use in numeric values.</param>
      /// <param name="masterFormat">master format string</param>  
      /// <returns></returns>
      public static string ToString(string masterFormat, int numberDecimalDigits, params object[] args)
      {
         return CustomFormat.ToString(CultureInfo.CurrentCulture, masterFormat, numberDecimalDigits, args);
      }

      public static string ToString(IFormatProvider provider, string masterFormat, int numberDecimalDigits, params object[] args)
      {
         /*
          example for masterFormat:
          "{{0:f{0}}} km"
          "{{0:f{0}}} {{1}}" 
          */

         Debug.Assert((masterFormat != string.Empty), "Master Format cannot be empty");
         string result = string.Format(CultureInfo.InvariantCulture, masterFormat, numberDecimalDigits); //setting the number of decimal places
         result = string.Format(provider, result, args);//formatting the value

         return result;
      }
   }
}
