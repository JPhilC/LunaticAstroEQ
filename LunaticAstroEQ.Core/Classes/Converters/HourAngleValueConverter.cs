using ASCOM.LunaticAstroEQ.Core.Geometry;
using System;
using System.Globalization;
using System.Windows.Data;

namespace ASCOM.LunaticAstroEQ.Controls
{
   [ValueConversion(typeof(double), typeof(string))]
   public class HourAngleValueConverter : IValueConverter
   {


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
         HourAngle ha = new HourAngle(incomingValue);
         return ha.Value;
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
         HourAngle ha = new HourAngle(incoming);
         return ha.ToString(HourAngleFormat.HoursMinutesSeconds);
      }

   }
}
