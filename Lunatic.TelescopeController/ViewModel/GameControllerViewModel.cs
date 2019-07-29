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
      Command,
      AxisRange,
      DeadZone
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

      private GameControllerMapping _SelectedButtonMapping;

      public GameControllerMapping SelectedButtonMapping
      {
         get
         {
            return _SelectedButtonMapping;
         }
         set
         {
            _CurrentSetting = GameControllerCurrentSetting.Command;
            Set(ref _SelectedButtonMapping, value);
         }
      }

      private GameControllerAxisRange _SelectedAxisRange;

      public GameControllerAxisRange SelectedAxisRange
      {
         get
         {
            return _SelectedAxisRange;
         }
         set
         {
            _CurrentSetting = GameControllerCurrentSetting.AxisRange;
            Set(ref _SelectedAxisRange, value);
         }
      }

      private GameControllerAxisRange _SelectedDeadZoneRange;

      public GameControllerAxisRange SelectedDeadZoneRange
      {
         get
         {
            return _SelectedDeadZoneRange;
         }
         set
         {
            _CurrentSetting = GameControllerCurrentSetting.DeadZone;
            Set(ref _SelectedDeadZoneRange, value);
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
         ControllerConnected = GameControllerService.IsInstanceConnected(_OriginalController.InstanceGuid);
      }

      public async Task StartGameControllerTask()
      {
         var progressHandler = new Progress<GameControllerUpdate>(value =>
         {
            if (value.Notification == GameControllerUpdateNotification.JoystickUpdate)
            {
               ProcessUpdate(value.Update);
            }
            else
            {
               ControllerConnected = GameControllerService.IsInstanceConnected(_OriginalController.InstanceGuid);
            }
         });

         var progress = progressHandler as IProgress<GameControllerUpdate>;
         _cts = new CancellationTokenSource();
         var token = _cts.Token;
         try
         {
            await Task.Run(() =>
            {
               bool notResponding = false;
               // Initialize DirectInput
               var directInput = new DirectInput();

               while (true)
               {
                  token.ThrowIfCancellationRequested();
                  try
                  {
                     // Instantiate the joystick
                     Joystick joystick = new Joystick(directInput, _OriginalController.InstanceGuid);
                     // Set BufferSize in order to use buffered data.
                     joystick.Properties.BufferSize = 128;

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
                        catch (SharpDX.SharpDXException ex)
                        {
                           notResponding = true;
                           progress.Report(new GameControllerUpdate());
                           Thread.Sleep(2000);
                           break;
                        }
                     }
                  }
                  catch (SharpDX.SharpDXException ex)
                  {
                     notResponding = true;
                     progress.Report(new GameControllerUpdate());
                     Thread.Sleep(2000);
                  }

               }
            });
         }
         catch (OperationCanceledException)
         {
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

      private void ProcessUpdate(JoystickUpdate update)
      {
         GameControllerMapping unsetMapping = null;
         GameControllerButton button = GameControllerButton.UNMAPPED;
         int offset = (int)update.Offset;
         if (_CurrentSetting == GameControllerCurrentSetting.Command && offset >= 48 && offset <= 57)
         {
            button = (GameControllerButton)(offset - 48);
            // Buttons 0 - 9
            if (SelectedButtonMapping != null)
            {
               SelectedButtonMapping.Button = button;
               unsetMapping = Controller.ButtonMappings.Where(m => m.Command != SelectedButtonMapping.Command && m.Button == button).FirstOrDefault();
            }
         }
         else if (_CurrentSetting == GameControllerCurrentSetting.Command && update.Offset == JoystickOffset.PointOfViewControllers0)
         {
            if (SelectedButtonMapping != null)
            {
               // POV 0
               switch (update.Value)
               {
                  case 0:
                     button = GameControllerButton.POV_N;
                     break;
                  case 4500:
                     button = GameControllerButton.POV_NE;
                     break;
                  case 9000:
                     button = GameControllerButton.POV_E;
                     break;
                  case 13500:
                     button = GameControllerButton.POV_SE;
                     break;
                  case 18000:
                     button = GameControllerButton.POV_S;
                     break;
                  case 22500:
                     button = GameControllerButton.POV_SW;
                     break;
                  case 27000:
                     button = GameControllerButton.POV_W;
                     break;
                  case 31500:
                     button = GameControllerButton.POV_NW;
                     break;
               }
               if (button != GameControllerButton.UNMAPPED)
               {
                  SelectedButtonMapping.Button = button;
                  unsetMapping = Controller.ButtonMappings.Where(m => m.Command != SelectedButtonMapping.Command && m.Button == button).FirstOrDefault();
               }
            }
         }
         else if (offset < 20)
         {
            GameControllerAxisRange currentRange = null;
            if (_CurrentSetting == GameControllerCurrentSetting.AxisRange)
            {
               currentRange = SelectedAxisRange;
            }
            else if (_CurrentSetting == GameControllerCurrentSetting.DeadZone)
            {
               currentRange = SelectedDeadZoneRange;
            }

            if (currentRange != null)
            {
               switch (currentRange.Axis)
               {
                  case GameControllerAxis.X:
                     if (update.Offset == JoystickOffset.X)
                     {
                        if (currentRange.MinimumValue.HasValue)
                        {
                           currentRange.MinimumValue = Math.Min(currentRange.MinimumValue.Value, update.Value);
                        }
                        else
                        {
                           currentRange.MinimumValue = update.Value;
                        }
                        if (currentRange.MaximumValue.HasValue)
                        {
                           currentRange.MaximumValue = Math.Max(currentRange.MaximumValue.Value, update.Value);
                        }
                        else
                        {
                           currentRange.MaximumValue = update.Value;
                        }
                     }
                     break;
                  case GameControllerAxis.Y:
                     if (update.Offset == JoystickOffset.Y)
                     {
                        if (currentRange.MinimumValue.HasValue)
                        {
                           currentRange.MinimumValue = Math.Min(currentRange.MinimumValue.Value, update.Value);
                        }
                        else
                        {
                           currentRange.MinimumValue = update.Value;
                        }
                        if (currentRange.MaximumValue.HasValue)
                        {
                           currentRange.MaximumValue = Math.Max(currentRange.MaximumValue.Value, update.Value);
                        }
                        else
                        {
                           currentRange.MaximumValue = update.Value;
                        }
                     }
                     break;
                  case GameControllerAxis.Z:
                     if (update.Offset == JoystickOffset.Z)
                     {
                        if (currentRange.MinimumValue.HasValue)
                        {
                           currentRange.MinimumValue = Math.Min(currentRange.MinimumValue.Value, update.Value);
                        }
                        else
                        {
                           currentRange.MinimumValue = update.Value;
                        }
                        if (currentRange.MaximumValue.HasValue)
                        {
                           currentRange.MaximumValue = Math.Max(currentRange.MaximumValue.Value, update.Value);
                        }
                        else
                        {
                           currentRange.MaximumValue = update.Value;
                        }
                     }
                     break;
                  case GameControllerAxis.RX:
                     if (update.Offset == JoystickOffset.RotationX)
                     {
                        if (currentRange.MinimumValue.HasValue)
                        {
                           currentRange.MinimumValue = Math.Min(currentRange.MinimumValue.Value, update.Value);
                        }
                        else
                        {
                           currentRange.MinimumValue = update.Value;
                        }
                        if (currentRange.MaximumValue.HasValue)
                        {
                           currentRange.MaximumValue = Math.Max(currentRange.MaximumValue.Value, update.Value);
                        }
                        else
                        {
                           currentRange.MaximumValue = update.Value;
                        }
                     }
                     break;
                  case GameControllerAxis.RY:
                     if (update.Offset == JoystickOffset.RotationY)
                     {
                        if (currentRange.MinimumValue.HasValue)
                        {
                           currentRange.MinimumValue = Math.Min(currentRange.MinimumValue.Value, update.Value);
                        }
                        else
                        {
                           currentRange.MinimumValue = update.Value;
                        }
                        if (currentRange.MaximumValue.HasValue)
                        {
                           currentRange.MaximumValue = Math.Max(currentRange.MaximumValue.Value, update.Value);
                        }
                        else
                        {
                           currentRange.MaximumValue = update.Value;
                        }
                     }
                     break;
               }
            }

         }
         if (unsetMapping != null)
         {
            unsetMapping.Button = GameControllerButton.UNMAPPED;
         }
      }



      private RelayCommand<GameControllerCurrentSetting> _SetCurrentSettingCommand;

      /// <summary>
      /// Adds a new chart to the active model
      /// </summary>
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

      private RelayCommand<GameControllerMapping> _ClearCommandCommand;

      /// <summary>
      /// Adds a new chart to the active model
      /// </summary>
      public RelayCommand<GameControllerMapping> ClearCommandCommand
      {
         get
         {
            return _ClearCommandCommand
                ?? (_ClearCommandCommand = new RelayCommand<GameControllerMapping>(
                                      (mapping) =>
                                      {
                                         mapping.Button = GameControllerButton.UNMAPPED;
                                      }

                                      ));
         }
      }

      private RelayCommand<GameControllerAxisRange> _ClearAxisRangeCommand;

      /// <summary>
      /// Adds a new chart to the active model
      /// </summary>
      public RelayCommand<GameControllerAxisRange> ClearAxisRangeCommand
      {
         get
         {
            return _ClearAxisRangeCommand
                ?? (_ClearAxisRangeCommand = new RelayCommand<GameControllerAxisRange>(
                                      (range) =>
                                      {
                                         range.MinimumValue = null;
                                         range.MaximumValue = null;
                                      }

                                      ));
         }
      }

      private RelayCommand<GameControllerAxisRange> _ClearDeadZoneCommand;

      /// <summary>
      /// Adds a new chart to the active model
      /// </summary>
      public RelayCommand<GameControllerAxisRange> ClearDeadZoneCommand
      {
         get
         {
            return _ClearDeadZoneCommand
                ?? (_ClearDeadZoneCommand = new RelayCommand<GameControllerAxisRange>(
                                      (range) =>
                                      {
                                         range.MinimumValue = null;
                                         range.MaximumValue = null;
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
         foreach (GameControllerMapping originalMapping in _OriginalController.ButtonMappings)
         {
            GameControllerMapping newMapping = _Controller.ButtonMappings.Where(m => m.Command == originalMapping.Command).FirstOrDefault();
            if (newMapping != null)
            {
               newMapping.Button = originalMapping.Button;
            }
         }
         foreach (GameControllerAxisRange originalRange in _OriginalController.AxisRanges)
         {
            GameControllerAxisRange newRange = _Controller.AxisRanges.Where(r => r.Axis == originalRange.Axis).FirstOrDefault();
            if (newRange != null)
            {
               newRange.MinimumValue = originalRange.MinimumValue;
               newRange.MaximumValue = originalRange.MaximumValue;
            }
         }
         foreach (GameControllerAxisRange originalRange in _OriginalController.AxisDeadZones)
         {
            GameControllerAxisRange newRange = _Controller.AxisDeadZones.Where(r => r.Axis == originalRange.Axis).FirstOrDefault();
            if (newRange != null)
            {
               newRange.MinimumValue = originalRange.MinimumValue;
               newRange.MaximumValue = originalRange.MaximumValue;
            }
         }
      }


      public void PushProperties()
      {
         foreach (GameControllerMapping newMapping in _Controller.ButtonMappings)
         {
            GameControllerMapping originalMapping = _OriginalController.ButtonMappings.Where(m => m.Command == newMapping.Command).FirstOrDefault();
            if (originalMapping != null)
            {
               originalMapping.Button = newMapping.Button;
            }
         }
         foreach (GameControllerAxisRange newRange in _Controller.AxisRanges)
         {
            GameControllerAxisRange originalRange = _OriginalController.AxisRanges.Where(r => r.Axis == newRange.Axis).FirstOrDefault();
            if (originalRange != null)
            {
               originalRange.MinimumValue = newRange.MinimumValue;
               originalRange.MaximumValue = newRange.MaximumValue;
            }
         }
         foreach (GameControllerAxisRange newRange in _Controller.AxisDeadZones)
         {
            GameControllerAxisRange originalRange = _OriginalController.AxisDeadZones.Where(r => r.Axis == newRange.Axis).FirstOrDefault();
            if (originalRange != null)
            {
               originalRange.MinimumValue = newRange.MinimumValue;
               originalRange.MaximumValue = newRange.MaximumValue;
            }
         }
      }
   }
}