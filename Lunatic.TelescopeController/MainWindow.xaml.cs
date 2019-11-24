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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Lunatic.TelescopeController
{
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window
   {

      MainViewModel _ViewModel;
      public MainWindow()
      {
         InitializeComponent();
         _ViewModel = this.DataContext as MainViewModel;
         WeakEventManager<MainViewModel, System.ComponentModel.PropertyChangedEventArgs>.AddHandler(_ViewModel, "PropertyChanged", _ViewModel_PropertyChanged);
         if (_ViewModel.AlwaysOnTop)
         {
            this.Topmost = true;
         }

         // Hook up to the viewmodels close actions
         if (_ViewModel.SaveAndCloseAction == null)
         {
            _ViewModel.SaveAndCloseAction = new Action(() =>
            {
               if (_ViewModel.IsConnected)
               {
                  MessageBoxResult result = MessageBox.Show("Program is currently connected to your mount. Press OK to confirm that you want to disconnect and close the program.", "Confirm Disconnection", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);
                  if (result != MessageBoxResult.OK)
                  {
                     return;
                  }
                  // Disconnect.
                  _ViewModel.ConnectCommand.Execute(null);
               }
               this.Close();
            });
         }

      }

      private void _ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
      {
         if (e.PropertyName == "AlwaysOnTop")
         {
            this.Topmost = _ViewModel.AlwaysOnTop;
         }
      }

      private void Window_Deactivated(object sender, EventArgs e)
      {
         if (_ViewModel.AlwaysOnTop)
         {
            Window window = (Window)sender;
            window.Topmost = true;
         }
      }


      protected override void OnClosing(CancelEventArgs e)
      {
         base.OnClosing(e);
         _ViewModel.StopGameControllerTask();   // In case the window is closed with the X button.
      }


      private void Window_Closed(object sender, EventArgs e)
      {
         _ViewModel.StopGameControllerTask();
         WeakEventManager<MainViewModel, System.ComponentModel.PropertyChangedEventArgs>.RemoveHandler(_ViewModel, "PropertyChanged", _ViewModel_PropertyChanged);
         _ViewModel.SaveAndCloseAction = null;
      }

      private async void Windows_Loaded(object sender, RoutedEventArgs e)
      {
         // await _ViewModel.StartGameControllerTask();
      }
   }
}
