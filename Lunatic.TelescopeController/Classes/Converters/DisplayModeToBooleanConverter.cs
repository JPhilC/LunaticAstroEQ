using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Lunatic.TelescopeController
{
   [ValueConversion(typeof(DisplayMode), typeof(Boolean))]
   public class DisplayModeToBooleanConverter : IValueConverter
   {
      /// <summary>
      /// Converts a DisplayMode to a boolean depending whether it matches the parameter
      /// </summary>
      /// <param name="value">The Boolean value to convert. This value can be a standard Boolean value or a nullable Boolean value.</param>
      /// <param name="targetType">This parameter is not used.</param>
      /// <param name="parameter">This parameter is not used.</param>
      /// <param name="culture">This parameter is not used.</param>
      /// <returns> System.Windows.Visibility.Collapsed if value is true; otherwise, System.Windows.Visibility.Visible.</returns>
      public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
      {
         DisplayMode incomingValue = (DisplayMode)value;
         DisplayMode parameterValue = (DisplayMode)parameter;
         return (incomingValue == parameterValue);
      }

      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
      {
         throw new NotImplementedException("Converting from booleans to DisplayMode is not available.");
      }

   }
}
