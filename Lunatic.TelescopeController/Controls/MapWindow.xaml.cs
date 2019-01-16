using Lunatic.TelescopeController.ViewModel;
using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Lunatic.TelescopeController.Controls
{
   /// <summary>
   /// Interaction logic for Map.xaml
   /// </summary>
   public partial class MapWindow : Window
   {
      private MapViewModel _ViewModel;
      public MapWindow(MapViewModel viewModel)
      {
         InitializeComponent();
         _ViewModel = viewModel;
         this.DataContext = _ViewModel;
         InitializeMap();
         this.Title = "Pick location of " + _ViewModel.Site.SiteName;

         // Hook up to the viewmodels close actions
         if (_ViewModel.SaveAndCloseAction == null) {
            _ViewModel.SaveAndCloseAction = new Action(() => {
               this.DialogResult = true;
               this.Close();
            });
         }
         if (_ViewModel.CancelAndCloseAction == null) {
            _ViewModel.CancelAndCloseAction = new Action(() => {
               this.DialogResult = false;
               this.Close();
            });
         }

      }

      protected override void OnClosed(EventArgs e)
      {
         base.OnClosed(e);
         _ViewModel.SaveAndCloseAction = null;
         _ViewModel.CancelAndCloseAction = null;
      }

      private void InitializeMap()
      {
         if (_ViewModel.Site.Latitude != 0.0 && _ViewModel.Site.Longitude != 0.0) {
            SiteMap.Center = new Location(_ViewModel.Site.Latitude, _ViewModel.Site.Longitude);
            Pushpin pin = new Pushpin() { Location = SiteMap.Center };
            SiteMap.Children.Add(pin);
         }
      }

      private void SiteMap_MouseDoubleClick(object sender, MouseButtonEventArgs e)
      {
         // Disables the default mouse double-click action.
         e.Handled = true;

         // Determin the location to place the pushpin at on the map.

         //Get the mouse click coordinates
         Point mousePosition = e.GetPosition(this);
         //Convert the mouse coordinates to a locatoin on the map
         Location pinLocation = SiteMap.ViewportPointToLocation(mousePosition);
         Pushpin pin;
         // The pushpin to add to the map.
         if (SiteMap.Children.Count == 0) {
            pin = new Pushpin();
            pin.Location = pinLocation;
            // Adds the pushpin to the map.
            SiteMap.Children.Add(pin);
         }
         else {
            pin = SiteMap.Children[0] as Pushpin;
            if (pin != null) {
               pin.Location = pinLocation;
            }
         }
         _ViewModel.SetLocation(pinLocation);
      }

   }
}
