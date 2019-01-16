using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace ASCOM.LunaticAstroEQ.Controls
{
   public enum LatLong
   {
      Latitude,
      Longitude
   }

   [ValueConversion(typeof(double), typeof(string))]
   public class GeoCoordinateToStringConverter : IValueConverter
   {
      internal enum Direction
      {
         None = 0,
         North = 0x01,
         South = 0x02,
         East = 0x04,
         West = 0x08
      }

      #region Constants ...
      private const string DegreesPrefixedEw = @"(?<EW>[EW])\s*(?<LongDeg>0?\d?\d(?:\.\d+)?|1[0-7]\d(?:\.\d+)?|180(?:\.0+)?)\u00B0?";
      private const string DegreesSuffixedEw = @"(?<LongDeg>0?\d?\d(?:\.\d+)?|1[0-7]\d(?:\.\d+)?|180(?:\.0+)?)\u00B0?\s*(?<EW>[EW])";
      private const string DegreesSignedEw = @"(?<LongDeg>\-?(?:0?\d?\d(?:\.\d+)?|1[0-7]\d(?:\.\d+)?|180(?:\.0+)?))\u00B0?";
      private const string DegreesPrefixedNs = @"(?<NS>[NS])\s*(?<LatDeg>0?[0-8]?[0-9](?:\.\d+)?|0?90(?:\.0+)?)\u00B0?";
      private const string DegreesSuffixedNs = @"(?<LatDeg>0?[0-8]?[0-9](?:\.\d+)?|0?90(?:\.0+)?)\u00B0?\s*(?<NS>[NS])";
      private const string DegreesSignedNs = @"(?<LatDeg>\-?0?[0-8]?\d(?:\.\d+)?|\-?\s* 0?90(?:\.0+)?)\u00B0?";

      private const string DegMinSecPrefixedEw = @"(?<EW>[EW])\s*(?:(?<LongDeg>0?\d?\d|1[0-7]\d)[\u00B0\s]\s*(?<LongMin>[0-5]?\d)[\'\s]\s*(?<LongSec>[0-5]?\d(?:\.\d+)?)\""?|(?<LongDeg>180)[\u00B0\s]\s*(?<LongMin>0?0)[\'\s]\s*(?<LongSec>0?0(?:\.0+)?)\""?)";
      private const string DegMinSecSuffixedEw = @"(?:(?<LongDeg>0?\d?\d|1[0-7]\d)[\u00B0\s]\s*(?<LongMin>[0-5]?\d)[\'\s]\s*(?<LongSec>[0-5]?\d(?:\.\d+)?)\""?|(?<LongDeg>180)[\u00B0\s]\s*(?<LongMin>0?0)[\'\s]\s*(?<LongSec>0?0(?:\.0+)?)\""?)\s*(?<EW>[EW])";
      private const string DegMinSecSignedEw = @"(?:(?<LongDeg>\-?(?:0?\d?\d|1[0-7]\d))[\u00B0\s]\s*(?<LongMin>\-?\s*[0-5]?\d)[\'\s]\s*(?<LongSec>\-?\s*[0-5]?\d(?:\.\d+)?)\""?|(?<LongDeg>\-?\s*180)[\u00B0\s]\s*(?<LongMin>\-?\s*0?0)[\'\s]\s*(?<LongSec>\-?\s*0?0(?:\.0+)?)\""?)";
      private const string DegMinSecCompactEw = @"(?:(?<LongDeg>0\d\d|1[0-7]\d)(?<LongMin>[0-5]\d)(?<LongSec>[0-5]\d(?:\.\d+)?)|(?<LongDeg>180)(?<LongMin>00)(?<LongSec>00(?:\.0+)?))(?<EW>[EW])";
      private const string DegMinSecPrefixedNs = @"(?<NS>[NS])\s*(?:(?<LatDeg>[0-8]?[0-9])[\u00B0\s]\s*(?<LatMin>[0-5]?\d)[\'\s]\s*(?<LatSec>[0-5]?\d(?:\.\d+)?)\""?|(?<LatDeg>90)[\u00B0\s]\s*(?<LatMin>0?0)[\'\s]\s*(?<LatSec>0?0(?:\.0+)?)\""?)";
      private const string DegMinSecSuffixedNs = @"(?:(?<LatDeg>[0-8]?[0-9])[\u00B0\s]\s*(?<LatMin>[0-5]?\d)[\'\s]\s*(?<LatSec>[0-5]?\d(?:\.\d+)?)\""?|(?<LatDeg>90)[\u00B0\s]\s*(?<LatMin>0?0)[\'\s]\s*(?<LatSec>0?0(?:\.0+)?)\""?)\s*(?<NS>[NS])";
      private const string DegMinSecSignedNs = @"(?:(?<LatDeg>\-?0?[0-8]?\d)[\u00B0\s]\s*(?<LatMin>\-?\s*[0-5]?\d)[\'\s]\s*(?<LatSec>\-?\s*[0-5]?\d(?:\.\d+)?)\""?|(?<LatDeg>\-?\s*0?90)[\u00B0\s]\s*(?<LatMin>\-?\s*0?0)[\'\s]\s*(?<LatSec>\-?\s*0?0(?:\.0+)?)\""?)";
      private const string DegMinSecCompactNs = @"(?:(?<LatDeg>[0-8][0-9])(?<LatMin>[0-5]\d)(?<LatSec>[0-5]\d(?:\.\d+)?)|(?<LatDeg>90)(?<LatMin>00)(?<LatSec>00(?:\.0+)?))(?<NS>[NS])";

      private static Regex[] _DegRegexes;
      private static Regex[] _DmsRegexes;
      private static int _NumberDecimalDigitsForSeconds = 2;


      private const string DEGREES_SYMBOL = "\u00B0";
      private const string LatitudeFormat = "{0:00} {1:00} {2:00}{3}";
      private const string LongitudeFormat = "{0:000} {1:00} {2:00}{3}";
      #endregion

      private int _Degrees = 0;
      private int _Minutes = 0;
      private double _Seconds = 0.0;


      #region Static constructor ...
      static GeoCoordinateToStringConverter()
      {
         string[] degreesFormats = new string[] {DegreesPrefixedEw,
                                                 DegreesSuffixedEw,
                                                 DegreesSignedEw,
                                                 DegreesPrefixedNs,
                                                 DegreesSuffixedNs,
                                                 DegreesSignedNs
                                                };

         string[] degMinSecFormats = new string[] {DegMinSecPrefixedEw,
                                                   DegMinSecSuffixedEw,
                                                   DegMinSecSignedEw,
                                                   DegMinSecCompactEw,
                                                   DegMinSecPrefixedNs,
                                                   DegMinSecSuffixedNs,
                                                   DegMinSecSignedNs,
                                                   DegMinSecCompactNs
                                                  };

         _DegRegexes = BuildRegexArray(degreesFormats);
         _DmsRegexes = BuildRegexArray(degMinSecFormats);

      }

      #endregion

      /// <summary>
      /// Converts a string representation of a coordinate double.
      /// </summary>
      /// <param name="value">The string value to convert. </param>
      /// <param name="targetType">This parameter is not used.</param>
      /// <param name="parameter">This parameter is not used.</param>
      /// <param name="culture">This parameter is not used.</param>
      /// <returns> The coordinate angle as a double</returns>
      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
      {
         string incomingValue = (string)value;
         double angle = ConvertFromString(incomingValue);
         return angle;
      }

      /// <summary>
      /// Converts a double value to a string value.
      /// </summary>
      /// <param name="value">A System.Windows.Visibility enumeration value.</param>
      /// <param name="targetType">This parameter is not used.</param>
      /// <param name="parameter">This parameter is not used.</param>
      /// <param name="culture">true if value is System.Windows.Visibility.Collapsed or Hidden; otherwise, false.</param>
      /// <returns></returns>
      public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
      {
         double incoming = (double)value;
         LatLong latLong = (LatLong)parameter;
         string conversion;
         if (latLong == LatLong.Latitude) {
            conversion = ConvertToString(incoming, false);
         }
         else {
            conversion = ConvertToString(incoming, true);
         }
         return conversion;
      }

      private double ConvertFromString(string angle)
      {
         double value = 0.0;
         bool hasBeenSet = false;

         _Degrees = 0;
         _Minutes = 0;
         _Seconds = 0.0;

         angle = StandardizeDegreesSymbol(angle);
         angle = angle.Replace(CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator, ".");

         if (!hasBeenSet) {
            foreach (Regex regex in _DegRegexes) {
               Match match = regex.Match(angle);
               if (match.Success) {
                  Direction direction = CheckMatchForDirection(match);
                  if (direction == Direction.North
                      || direction == Direction.South) {
                     value = System.Convert.ToDouble(match.Groups["LatDeg"].Value, CultureInfo.InvariantCulture);
                  }
                  else {
                     value = System.Convert.ToDouble(match.Groups["LongDeg"].Value, CultureInfo.InvariantCulture);
                  }

                  if (direction == Direction.South
                      || direction == Direction.West) {
                     value *= -1.0;
                  }

                  SetDmsFromDegrees(value);
                  hasBeenSet = true;
                  break;
               }
            }
         }

         if (!hasBeenSet) {
            foreach (Regex regex in _DmsRegexes) {
               Match match = regex.Match(angle);
               if (match.Success) {
                  Direction direction = CheckMatchForDirection(match);
                  if (direction == Direction.North
                      || direction == Direction.South) {
                     _Degrees = System.Convert.ToInt32(match.Groups["LatDeg"].Value, CultureInfo.InvariantCulture);
                     _Minutes = System.Convert.ToInt32(match.Groups["LatMin"].Value, CultureInfo.InvariantCulture);
                     _Seconds = System.Convert.ToDouble(match.Groups["LatSec"].Value, CultureInfo.InvariantCulture);
                  }
                  else {
                     _Degrees = System.Convert.ToInt32(match.Groups["LongDeg"].Value, CultureInfo.InvariantCulture);
                     _Minutes = System.Convert.ToInt32(match.Groups["LongMin"].Value, CultureInfo.InvariantCulture);
                     _Seconds = System.Convert.ToDouble(match.Groups["LongSec"].Value, CultureInfo.InvariantCulture);
                  }

                  if (direction == Direction.South
                      || direction == Direction.West) {
                     SetDmsToNegative();
                  }

                  value = SetDegreesFromDms();
                  hasBeenSet = true;
                  break;
               }
            }
         }

         if (!hasBeenSet) {
            value = 0.0;
         }
         return value;
      }


      public string ConvertToString(double value, bool asLongitude = true)
      {
         string text = string.Empty;
         int degrees;
         int minutes;
         double seconds;
         int increment;
         SetDmsFromDegrees(value);

         increment = (value >= 0.0 ? 1 : -1);
         seconds = Math.Round(_Seconds, _NumberDecimalDigitsForSeconds);

         if (Math.Abs(seconds) < 60.0) {
            minutes = _Minutes;
         }
         else {
            seconds = 0.0;
            minutes = _Minutes + increment;
         }

         if (Math.Abs(minutes) < 60) {
            degrees = _Degrees;
         }
         else {
            minutes = 0;
            degrees = _Degrees + increment;
         }

         string stringFormat = asLongitude ? LongitudeFormat
                                           : LatitudeFormat;
         text = string.Format(stringFormat, Math.Abs(degrees), Math.Abs(minutes), Math.Abs(seconds),
                              asLongitude ? (value >= 0.0 ? "E" : "W")
                                          : (value >= 0.0 ? "N" : "S"));
         return text;
      }


      private void SetDmsToNegative()
      {
         if (_Degrees > 0) {
            _Degrees *= -1;
         }

         if (_Minutes > 0) {
            _Minutes *= -1;
         }

         if (_Seconds > 0.0) {
            _Seconds *= -1.0;
         }
      }

      private void SetDmsFromDegrees(double angle)
      {
         _Degrees = (int)Math.Truncate(angle);
         angle = (angle - _Degrees) * 60.0;
         _Minutes = (int)Math.Truncate(angle);
         _Seconds = (angle - _Minutes) * 60.0;
      }

      private double SetDegreesFromDms()
      {
         if (_Degrees < 0 || _Minutes < 0 || _Seconds < 0.0) {
            SetDmsToNegative();
         }

         return (double)_Degrees + ((double)_Minutes / 60.0) + (_Seconds / 3600.0);
      }

      private string StandardizeDegreesSymbol(string angle)
      {
         /* Whilst there's only one 'real' degrees symbol, there are a couple of others that
            look similar and might be used instead.  To keep the regexes simple, we'll
            standardise on the real degrees symbol (Google Earth uses this one, in
            particular).
         */
         angle = angle.Replace("\u00BA", DEGREES_SYMBOL);     /* Replaces Masculine Ordinal Indicator character */
         return angle.Replace("\u02DA", DEGREES_SYMBOL);      /* Replaces Ring Above character (unicode) */
      }


      private static Regex[] BuildRegexArray(string[] regexPatterns)
      {
         Regex[] regexes = new Regex[regexPatterns.Length];
         for (int i = 0; i < regexPatterns.Length; i++) {
            regexes[i] = new Regex(@"^\s*" + regexPatterns[i] + @"\s*$",
                                   RegexOptions.Compiled | RegexOptions.IgnoreCase);
         }
         return regexes;
      }

      private static Direction CheckMatchForDirection(Match match)
      {
         Direction direction = Direction.None;
         Group group = match.Groups["EW"];

         if (group.Success) {
            direction = (group.Value.IndexOfAny(new char[] { 'W', 'w' }) >= 0 ? Direction.West : Direction.East);
         }
         else {
            group = match.Groups["NS"];
            if (group.Success) {
               direction = (group.Value.IndexOfAny(new char[] { 'S', 's' }) >= 0 ? Direction.South : Direction.North);
            }
         }

         return direction;
      }

      /// <summary>
      /// Converts the angle to the range 180.0 to -180.0 degrees
      /// </summary>
      private static double NormalizeTo180(double angle)
      {
         angle %= 360.0;   /* Need it in the standard range first */

         if (angle > 180.0) {
            angle -= 360.0;
         }
         else if (angle < -180.0) {
            angle += 360.0;
         }

         return angle;
      }
   }
}
