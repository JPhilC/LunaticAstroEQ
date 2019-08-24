﻿/*
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
using System.ComponentModel;
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
   public partial class GameControllerWindow : Window
   {
      private GameControllerViewModel _ViewModel;
      public GameControllerWindow(GameControllerViewModel viewModel)
      {
         InitializeComponent();
         _ViewModel = viewModel;
         this.DataContext = _ViewModel;
         // InitializeController();
         this.Title = "Configure " + _ViewModel.Controller.Name;

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

      protected override void OnClosing(CancelEventArgs e)
      {
         base.OnClosing(e);
         _ViewModel.StopGameControllerTask();   // In case the window is closed with the X button.
      }

      protected override void OnClosed(EventArgs e)
      {
         base.OnClosed(e);
         _ViewModel.SaveAndCloseAction = null;
         _ViewModel.CancelAndCloseAction = null;
      }

      private async void Window_Loaded(object sender, RoutedEventArgs e)
      {
         await _ViewModel.StartGameControllerTask();
      }

      //private void InitializeController()
      //{
      //   if (_ViewModel.Site.Latitude != 0.0 && _ViewModel.Site.Longitude != 0.0) {
      //      SiteMap.Center = new Location(_ViewModel.Site.Latitude, _ViewModel.Site.Longitude);
      //      Pushpin pin = new Pushpin() { Location = SiteMap.Center };
      //      SiteMap.Children.Add(pin);
      //   }
      //}

   }
}
