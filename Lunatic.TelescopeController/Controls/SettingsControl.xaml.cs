using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Xceed.Wpf.Toolkit.PropertyGrid;

namespace Lunatic.TelescopeController.Controls
{
   /// <summary>
   /// Interaction logic for SettingsControl.xaml
   /// </summary>
   public partial class SettingsControl : UserControl
   {
      public SettingsControl()
      {
         InitializeComponent();
      }

      private void propertyGrid_PreparePropertyItem(object sender, Xceed.Wpf.Toolkit.PropertyGrid.PropertyItemEventArgs e)
      {
         PropertyDescriptor theDescriptor = ((PropertyItem)e.PropertyItem).PropertyDescriptor;
         if (theDescriptor.IsBrowsable) {
            e.PropertyItem.Visibility = Visibility.Visible;
         }
         else {
            e.PropertyItem.Visibility = Visibility.Collapsed;
         }
      }

   }
}
