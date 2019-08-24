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
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Maps.MapControl.WPF;
using SharpDX.DirectInput;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lunatic.TelescopeController.ViewModel
{
   public enum GameControllerCurrentSetting
   {
      None,
      ButtonCommand,
      AxisCommand
   }

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

      private GameControllerCurrentSetting _CurrentSetting;

      private GameControllerButtonMapping _SelectedButtonMapping;

      public GameControllerButtonMapping SelectedButtonMapping
      {
         get
         {
            return _SelectedButtonMapping;
         }
         set
         {
            _CurrentSetting = GameControllerCurrentSetting.ButtonCommand;
            Set(ref _SelectedButtonMapping, value);
         }
      }

      private GameControllerAxisMapping _SelectedAxisMapping;

      public GameControllerAxisMapping SelectedAxisMapping
      {
         get
         {
            return _SelectedAxisMapping;
         }
         set
         {
            _CurrentSetting = GameControllerCurrentSetting.AxisCommand;
            Set(ref _SelectedAxisMapping, value);
         }
      }


      private bool _ControllerConnected = false;

      public bool ControllerConnected
      {
         get
         {
            return _ControllerConnected;
         }
         set
         {
            Set(ref _ControllerConnected, value);
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
         ControllerConnected = GameControllerService.IsInstanceConnected(_OriginalController.Id);
      }

      public async Task StartGameControllerTask()
      {
         var progressHandler = new Progress<GameControllerUpdate>(value =>
         {
            if (value.Notification == GameControllerUpdateNotification.JoystickUpdate)
            {
               ProcessUpdate(value.Update);
            }
            else if (value.Notification == GameControllerUpdateNotification.ConnectedChanged)
            {
               ControllerConnected = GameControllerService.IsInstanceConnected(_OriginalController.Id);
            }
         });

         var progress = progressHandler as IProgress<GameControllerUpdate>;
         _cts = new CancellationTokenSource();
         var token = _cts.Token;
         try
         {
            await Task.Run(() =>
            {
               System.Diagnostics.Debug.WriteLine("Game Controller Configuration task STARTED.");
               bool notResponding = false;
               // Initialize DirectInput
               using (var directInput = new DirectInput())
               {

                  while (true)
                  {
                     token.ThrowIfCancellationRequested();
                     try
                     {
                        // Instantiate the joystick
                        using (Joystick joystick = new Joystick(directInput, _OriginalController.Id))
                        {
                           joystick.Properties.Range = new InputRange(-500, 500);
                           joystick.Properties.DeadZone = 2000;
                           joystick.Properties.Saturation = 8000;
                           // Set BufferSize in order to use buffered data.
                           joystick.Properties.BufferSize = 128;

                           // If configuring populate the joystick objects.
                           if (_Controller.JoystickObjects.Count == 0)
                           {
                              foreach (DeviceObjectInstance doi in joystick.GetObjects())
                              {
                                 JoystickOffset offset;
                                 if (GameControllerService.USAGE_OFFSET.TryGetValue(doi.Usage, out offset))
                                 {
                                    if (((int)doi.ObjectId & (int)DeviceObjectTypeFlags.Axis) != 0)
                                    {
                                       _Controller.JoystickObjects.Add(offset, doi.Name);
                                    }
                                    else if (((int)doi.ObjectId & (int)DeviceObjectTypeFlags.Button) != 0)
                                    {
                                       _Controller.JoystickObjects.Add(offset, doi.Name);
                                    }
                                    else if (((int)doi.ObjectId & (int)DeviceObjectTypeFlags.PointOfViewController) != 0)
                                    {
                                       _Controller.JoystickObjects.Add(offset, doi.Name);
                                    }
                                 }

                              }
                           }


                           // Acquire the joystick
                           joystick.Acquire();


                           while (true)
                           {
                              try
                              {
                                 token.ThrowIfCancellationRequested();
                                 joystick.Poll();
                                 JoystickUpdate[] datas = joystick.GetBufferedData();
                                 // Check for POV as this needs special handling to take the first value
                                 JoystickUpdate povUpdate = datas.Where(s => s.Offset == JoystickOffset.PointOfViewControllers0).OrderBy(s => s.Sequence).FirstOrDefault();
                                 if (povUpdate.Timestamp > 0)
                                 {
                                    if (progress != null)
                                    {
                                       progress.Report(new GameControllerUpdate(povUpdate));
                                    }
                                 }
                                 else
                                 {
                                    foreach (JoystickUpdate state in datas.Where(s => s.Value != -1)) // Just take the down clicks not the up.
                                    {
                                       if (progress != null)
                                       {
                                          progress.Report(new GameControllerUpdate(state));
                                       }
                                    }
                                 }
                                 if (notResponding)
                                 {
                                    notResponding = false;
                                    progress.Report(new GameControllerUpdate());
                                 }
                              }
                              catch (SharpDX.SharpDXException)
                              {
                                 notResponding = true;
                                 progress.Report(new GameControllerUpdate());
                                 Thread.Sleep(2000);
                                 break;
                              }
                           }
                        }
                     }
                     catch (SharpDX.SharpDXException)
                     {
                        notResponding = true;
                        progress.Report(new GameControllerUpdate());
                        Thread.Sleep(2000);
                     }

                  }
               }
            });
         }
         catch (OperationCanceledException)
         {
            System.Diagnostics.Debug.WriteLine("Game Controller Configuration task CANCELLED.");
         }
      }

      public void StopGameControllerTask()
      {
         if (_cts != null)
         {
            _cts.Cancel();
            _cts = null;
         }
      }

      private void ProcessUpdate(JoystickUpdate update)
      {
         GameControllerButtonMapping unsetButtonMapping = null;
         GameControllerAxisMapping unsetAxisMapping = null;
         string name = "<Unknown>";
         _Controller.JoystickObjects.TryGetValue(update.Offset, out name);

         if (_CurrentSetting == GameControllerCurrentSetting.ButtonCommand && update.RawOffset >= 48 && update.RawOffset <= 175)
         {
            if (SelectedButtonMapping != null)
            {
               SelectedButtonMapping.JoystickOffset = update.Offset;
               SelectedButtonMapping.Name = name;
               unsetButtonMapping = Controller.ButtonMappings.Where(m => m.Command != SelectedButtonMapping.Command && m.JoystickOffset == update.Offset).FirstOrDefault();
            }
         }
         else if (_CurrentSetting == GameControllerCurrentSetting.ButtonCommand && update.RawOffset >= 32 && update.RawOffset <= 44) // POV controls
         {
            if (SelectedButtonMapping != null)
            {
               GameControllerPOVDirection direction = GameController.GetPOVDirection(update);
               if (direction != GameControllerPOVDirection.UNMAPPED)
               {
                  SelectedButtonMapping.JoystickOffset = update.Offset;
                  SelectedButtonMapping.Name = $"{name} ({direction})";
                  SelectedButtonMapping.POVDirection = direction;
                  unsetButtonMapping = Controller.ButtonMappings.Where(m => m.Command != SelectedButtonMapping.Command && m.JoystickOffset == update.Offset && m.POVDirection == direction).FirstOrDefault();
               }
            }
         }
         else if (update.RawOffset <= 20) // Axis
         {
            if (_CurrentSetting == GameControllerCurrentSetting.AxisCommand)
            {
               if (SelectedAxisMapping != null)
               {
                  SelectedAxisMapping.JoystickOffset = update.Offset;
                  SelectedAxisMapping.Name = name;
                  unsetAxisMapping = Controller.AxisMappings.Where(m => m.Command != SelectedAxisMapping.Command && m.JoystickOffset == update.Offset).FirstOrDefault();
               }
            }
         }

         if (unsetButtonMapping != null)
         {
            unsetButtonMapping.JoystickOffset = null;
            unsetButtonMapping.Name = null;
            unsetButtonMapping.POVDirection = GameControllerPOVDirection.UNMAPPED;
         }

         if (unsetAxisMapping != null)
         {
            unsetAxisMapping.Name = null;
            unsetAxisMapping.JoystickOffset = null;
            unsetAxisMapping.ReverseDirection = false;
         }
      }



      private RelayCommand<GameControllerCurrentSetting> _SetCurrentSettingCommand;

      public RelayCommand<GameControllerCurrentSetting> SetCurrentSettingCommand
      {
         get
         {
            return _SetCurrentSettingCommand
                ?? (_SetCurrentSettingCommand = new RelayCommand<GameControllerCurrentSetting>(
                                      (currentSetting) =>
                                      {
                                         _CurrentSetting = currentSetting;
                                      }

                                      ));
         }
      }

      private RelayCommand<GameControllerButtonMapping> _ClearCommandCommand;

      /// <summary>
      /// Adds a new chart to the active model
      /// </summary>
      public RelayCommand<GameControllerButtonMapping> ClearCommandCommand
      {
         get
         {
            return _ClearCommandCommand
                ?? (_ClearCommandCommand = new RelayCommand<GameControllerButtonMapping>(
                                      (mapping) =>
                                      {
                                         mapping.JoystickOffset = null;
                                      }

                                      ));
         }
      }

      private RelayCommand<GameControllerAxisMapping> _ClearAxisMappingCommand;

      /// <summary>
      /// Adds a new chart to the active model
      /// </summary>
      public RelayCommand<GameControllerAxisMapping> ClearAxisMappingCommand
      {
         get
         {
            return _ClearAxisMappingCommand
                ?? (_ClearAxisMappingCommand = new RelayCommand<GameControllerAxisMapping>(
                                      (mapping) =>
                                      {
                                         mapping.JoystickOffset = null;
                                      }

                                      ));
         }
      }

      protected override bool OnSaveCommand()
      {
         StopGameControllerTask();
         PushProperties();
         return base.OnSaveCommand();
      }

      protected override bool OnCancelCommand()
      {
         StopGameControllerTask();
         return base.OnCancelCommand();
      }

      public void PopProperties()
      {
         _Controller = new GameController(_OriginalController.Id, _OriginalController.Name);
         foreach (GameControllerButtonMapping originalMapping in _OriginalController.ButtonMappings)
         {
            GameControllerButtonMapping newMapping = _Controller.ButtonMappings.Where(m => m.Command == originalMapping.Command).FirstOrDefault();
            if (newMapping != null)
            {
               newMapping.JoystickOffset = originalMapping.JoystickOffset;
               newMapping.Name = originalMapping.Name;

            }
         }
         foreach (GameControllerAxisMapping originalMapping in _OriginalController.AxisMappings)
         {
            GameControllerAxisMapping newMapping = _Controller.AxisMappings.Where(m => m.Command == originalMapping.Command).FirstOrDefault();
            if (newMapping != null)
            {
               newMapping.JoystickOffset = originalMapping.JoystickOffset;
               newMapping.Name = originalMapping.Name;
               newMapping.ReverseDirection = originalMapping.ReverseDirection;

            }
         }
      }


      public void PushProperties()
      {
         _OriginalController.Name = _Controller.Name;
         foreach (GameControllerButtonMapping newMapping in _Controller.ButtonMappings)
         {
            GameControllerButtonMapping originalMapping = _OriginalController.ButtonMappings.Where(m => m.Command == newMapping.Command).FirstOrDefault();
            if (originalMapping != null)
            {
               originalMapping.JoystickOffset = newMapping.JoystickOffset;
               originalMapping.Name = newMapping.Name;
            }
         }
         foreach (GameControllerAxisMapping newMapping in _Controller.AxisMappings)
         {
            GameControllerAxisMapping originalMapping = _OriginalController.AxisMappings.Where(m => m.Command == newMapping.Command).FirstOrDefault();
            if (originalMapping != null)
            {
               originalMapping.JoystickOffset = newMapping.JoystickOffset;
               originalMapping.Name = newMapping.Name;
               originalMapping.ReverseDirection = newMapping.ReverseDirection;
            }
         }
      }
   }
}