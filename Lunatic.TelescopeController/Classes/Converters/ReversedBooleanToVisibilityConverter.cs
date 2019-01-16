using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Lunatic.TelescopeController
{
    [ValueConversion(typeof(Boolean), typeof(Visibility))]
   public class ReversedBooleanToVisibilityConverter:IValueConverter
   {
      /// <summary>
      /// Converts a Boolean value to a System.Windows.Visibility enumeration value.
      /// </summary>
      /// <param name="value">The Boolean value to convert. This value can be a standard Boolean value or a nullable Boolean value.</param>
      /// <param name="targetType">This parameter is not used.</param>
      /// <param name="parameter">This parameter is not used.</param>
      /// <param name="culture">This parameter is not used.</param>
      /// <returns> System.Windows.Visibility.Collapsed if value is true; otherwise, System.Windows.Visibility.Visible.</returns>
      public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
      {
         bool incomingValue = (bool)value;
         return (incomingValue ? Visibility.Collapsed : Visibility.Visible);
      }

      /// <summary>
      /// Converts a System.Windows.Visibility enumeration value to a Boolean value.
      /// </summary>
      /// <param name="value">A System.Windows.Visibility enumeration value.</param>
      /// <param name="targetType">This parameter is not used.</param>
      /// <param name="parameter">This parameter is not used.</param>
      /// <param name="culture">true if value is System.Windows.Visibility.Collapsed or Hidden; otherwise, false.</param>
      /// <returns></returns>
      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
      {
         Visibility incoming = (Visibility)value;
         return (incoming == Visibility.Collapsed || incoming == Visibility.Hidden);
      }

   }
}
