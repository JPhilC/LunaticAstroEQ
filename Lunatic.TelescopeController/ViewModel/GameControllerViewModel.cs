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

using ASCOM.LunaticAstroEQ.Core;
using GalaSoft.MvvmLight.Command;
using Microsoft.Maps.MapControl.WPF;
using SharpDX.DirectInput;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lunatic.TelescopeController.ViewModel
{
   /// <summary>
   /// This class contains properties that the main View can data bind to.
   /// <para>
   /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
   /// </para>
   /// <para>
   /// You can also use Blend to data bind with the tool's support.
   /// </para>
   /// <para>
   /// See http://www.galasoft.ch/mvvm
   /// </para>
   /// </summary>
   public class GameControllerViewModel : LunaticViewModelBase
   {


      #region Properties ....

      #region Game Controller ...

      private GameController _OriginalController;
      private GameController _Controller;

      public GameController Controller
      {
         get
         {
            return _Controller;
         }
      }
      #endregion

      #region Test text property ...
      private string _Test;
      public string Test
      {
         get
         {
            return _Test;
         }
         set
         {
            Set(ref _Test, value);
         }
      }
      #endregion
      #endregion

      private CancellationTokenSource _cts;

      /// <summary>
      /// Initializes a new instance of the MainViewModel class.
      /// </summary>
      /// 
      public GameControllerViewModel(GameController gameController)
      {
         _OriginalController = gameController;
         PopProperties();
      }

      private async Task StartGameControllerTask()
      {
         var progressHandler = new Progress<string>(value =>
         {
            Test = value;
         });
         var progress = progressHandler as IProgress<string>;
         _cts = new CancellationTokenSource();
         var token = _cts.Token;
         try
         {
            await Task.Run(() =>
            {
               // Initialize DirectInput
               var directInput = new DirectInput();

               // Instantiate the joystick
               Joystick joystick = new Joystick(directInput, _OriginalController.InstanceGuid);
               if (progress != null)
               {
                  progress.Report($"Found Joystick/Gamepad with {joystick.Properties.ProductName}" );
               }
               // Set BufferSize in order to use buffered data.
               joystick.Properties.BufferSize = 128;

               // Acquire the joystick
               joystick.Acquire();

               while (true)
               {
                  token.ThrowIfCancellationRequested();
                  joystick.Poll();
                  JoystickUpdate[] datas = joystick.GetBufferedData();
                  foreach (JoystickUpdate state in datas)
                  {
                     if (progress != null)
                     {
                        progress.Report(state.ToString());
                     }
                  }
               }
            });
            if (progress != null)
            {
               progress.Report("Completed");
            }
         }
         catch (OperationCanceledException)
         {
            if (progress != null)
            {
               progress.Report("Cancelled");
            }
         }
      }

      private void StopGameControllerTask()
      {
         if (_cts != null)
         {
            _cts.Cancel();
            _cts = null;
         }
      }


      private RelayCommand _ToggleTaskCommand;

      /// <summary>
      /// Adds a new chart to the active model
      /// </summary>
      public RelayCommand ToggleTaskCommand
      {
         get
         {
            return _ToggleTaskCommand
                ?? (_ToggleTaskCommand = new RelayCommand(
                                      async () =>
                                      {
                                         if (_cts == null)
                                         {
                                            await StartGameControllerTask();
                                         }
                                         else
                                         {
                                            StopGameControllerTask();
                                         }
                                      }

                                      ));
         }
      }


      protected override bool OnSaveCommand()
      {
         PushProperties();
         return base.OnSaveCommand();
      }

      public void PopProperties()
      {
         _Controller = new GameController(_OriginalController.Id, _OriginalController.Name)
         {

         };

      }

      public void PushProperties()
      {
         //_OriginalController.Latitude = Site.Latitude;
         //_OriginalSite.Longitude = Site.Longitude;

      }
   }
}