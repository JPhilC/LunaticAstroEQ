/*
BSD 2-Clause License

Copyright (c) 2019, LunaticSoftware.org, Email: phil@lunaticsoftware.org
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
