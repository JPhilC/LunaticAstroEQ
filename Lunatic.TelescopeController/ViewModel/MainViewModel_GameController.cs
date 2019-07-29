using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lunatic.TelescopeController.ViewModel
{
   public partial class MainViewModel
   {
      private CancellationTokenSource _controllerTokenSource;

      private GameController _Controller = null;

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

      public async Task StartGameControllerTask()
      {
         var progressHandler = new Progress<GameControllerUpdate>(value =>
         {
            if (value.Notification == GameControllerUpdateNotification.ConnectedChanged)
            {
               ControllerConnected = GameControllerService.IsInstanceConnected(_Controller.InstanceGuid);
            }
            else if (value.Notification != GameControllerUpdateNotification.JoystickUpdate) {
               ProcessUpdate(value);
            }
         });

         var progress = progressHandler as IProgress<GameControllerUpdate>;
         _controllerTokenSource = new CancellationTokenSource();
         var token = _controllerTokenSource.Token;
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
                     Joystick joystick = new Joystick(directInput, _Controller.InstanceGuid);
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
                           // Reset not responding incase the controller has just been re-connected
                           if (notResponding)
                           {
                              notResponding = false;
                              progress.Report(new GameControllerUpdate());
                           }
                           // Check for POV updates
                           IEnumerable<JoystickUpdate> povUpdates = datas.Where(s => s.Offset == JoystickOffset.PointOfViewControllers0).OrderBy(s => s.Sequence);
                           IEnumerable<JoystickUpdate> axisUpdates = datas.Where(s => s.RawOffset < 20).OrderBy(s => s.Sequence);
                           IEnumerable<JoystickUpdate> buttonUpdates = datas.Where(s => s.RawOffset >= 48 && s.RawOffset <= 57).OrderBy(s => s.Sequence);
                           if (progress != null)
                           {
                              if (buttonUpdates.Any())
                              {
                                 ProcessButtons(buttonUpdates, progress);
                              }
                              if (povUpdates.Any())
                              {
                                 ProcessPOV(povUpdates, progress);
                              }
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
                  catch (SharpDX.SharpDXException)
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

      public void StopGameControllerTask()
      {
         if (_controllerTokenSource != null)
         {
            _controllerTokenSource.Cancel();
            _controllerTokenSource = null;
         }
      }


      private void ProcessButtons(IEnumerable<JoystickUpdate> updates, IProgress<GameControllerUpdate> progress)
      {
         foreach (JoystickUpdate state in updates) // Just take the down clicks not the up.
         {
            GameControllerButton button = (GameControllerButton)(state.RawOffset - 48);
            GameControllerMapping mapping = Settings.GameControllers.ActiveGameController.ButtonMappings.Where(m => m.Button == button).FirstOrDefault();
            if (mapping != null)
            {
               if (state.Value == 0)
               {
                  progress.Report(new GameControllerUpdate(GameControllerUpdateNotification.CommandUp, mapping.Command));
               }
               else
               {
                  progress.Report(new GameControllerUpdate(GameControllerUpdateNotification.CommandDown, mapping.Command));
               }
            }
         }
      }


      private void ProcessPOV(IEnumerable<JoystickUpdate> updates, IProgress<GameControllerUpdate> progress)
      {
         GameControllerMapping previousMapping = null;
         foreach (JoystickUpdate state in updates) // Just take the down clicks not the up.
         {
            GameControllerButton button = GetPOVButton(state);
            if (button == GameControllerButton.UNMAPPED)
            {
               continue;
            }
            GameControllerMapping mapping = Settings.GameControllers.ActiveGameController.ButtonMappings.Where(m => m.Button == button).FirstOrDefault();
            if (mapping != null)
            {
               if (state.Value == -1)
               {
                  progress.Report(new GameControllerUpdate(GameControllerUpdateNotification.CommandUp, mapping.Command));
               }
               else
               {
                  if (mapping != previousMapping)
                  {
                     if (previousMapping != null)
                     {
                        progress.Report(new GameControllerUpdate(GameControllerUpdateNotification.CommandUp, previousMapping.Command));
                     }
                     progress.Report(new GameControllerUpdate(GameControllerUpdateNotification.CommandDown, mapping.Command));
                  }
               }
               previousMapping = mapping;
            }
         }
      }

      private GameControllerButton GetPOVButton(JoystickUpdate update)
      {
         GameControllerButton button = GameControllerButton.UNMAPPED;
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
         return button;
      }



      private void ProcessUpdate(GameControllerUpdate update)
      {
         System.Diagnostics.Debug.WriteLine($"Controller notification:{update.Notification}, command: {update.Command}");
      }
      //else if (offset < 20)
      //{
      //   GameControllerAxisRange currentRange = null;
      //   GameControllerAxisRange currentDeadZone = null;
      //   switch (update.Offset)
      //   {
      //      case JoystickOffset.X:
      //         break;
      //      case JoystickOffset.Y:
      //         break;
      //      case JoystickOffset.Z:
      //         break;
      //      case JoystickOffset.RotationX:
      //         break;
      //      case JoystickOffset.RotationY:
      //         break;
      //   }
      //   if (_CurrentSetting == GameControllerCurrentSetting.AxisRange)
      //   {
      //      currentRange = SelectedAxisRange;
      //   }
      //   else if (_CurrentSetting == GameControllerCurrentSetting.DeadZone)
      //   {
      //      currentRange = SelectedDeadZoneRange;
      //   }

      //   if (currentRange != null)
      //   {
      //      switch (currentRange.Axis)
      //      {
      //         case GameControllerAxis.X:
      //            if (update.Offset == JoystickOffset.X)
      //            {
      //               if (currentRange.MinimumValue.HasValue)
      //               {
      //                  currentRange.MinimumValue = Math.Min(currentRange.MinimumValue.Value, update.Value);
      //               }
      //               else
      //               {
      //                  currentRange.MinimumValue = update.Value;
      //               }
      //               if (currentRange.MaximumValue.HasValue)
      //               {
      //                  currentRange.MaximumValue = Math.Max(currentRange.MaximumValue.Value, update.Value);
      //               }
      //               else
      //               {
      //                  currentRange.MaximumValue = update.Value;
      //               }
      //            }
      //            break;
      //         case GameControllerAxis.Y:
      //            if (update.Offset == JoystickOffset.Y)
      //            {
      //               if (currentRange.MinimumValue.HasValue)
      //               {
      //                  currentRange.MinimumValue = Math.Min(currentRange.MinimumValue.Value, update.Value);
      //               }
      //               else
      //               {
      //                  currentRange.MinimumValue = update.Value;
      //               }
      //               if (currentRange.MaximumValue.HasValue)
      //               {
      //                  currentRange.MaximumValue = Math.Max(currentRange.MaximumValue.Value, update.Value);
      //               }
      //               else
      //               {
      //                  currentRange.MaximumValue = update.Value;
      //               }
      //            }
      //            break;
      //         case GameControllerAxis.Z:
      //            if (update.Offset == JoystickOffset.Z)
      //            {
      //               if (currentRange.MinimumValue.HasValue)
      //               {
      //                  currentRange.MinimumValue = Math.Min(currentRange.MinimumValue.Value, update.Value);
      //               }
      //               else
      //               {
      //                  currentRange.MinimumValue = update.Value;
      //               }
      //               if (currentRange.MaximumValue.HasValue)
      //               {
      //                  currentRange.MaximumValue = Math.Max(currentRange.MaximumValue.Value, update.Value);
      //               }
      //               else
      //               {
      //                  currentRange.MaximumValue = update.Value;
      //               }
      //            }
      //            break;
      //         case GameControllerAxis.RX:
      //            if (update.Offset == JoystickOffset.RotationX)
      //            {
      //               if (currentRange.MinimumValue.HasValue)
      //               {
      //                  currentRange.MinimumValue = Math.Min(currentRange.MinimumValue.Value, update.Value);
      //               }
      //               else
      //               {
      //                  currentRange.MinimumValue = update.Value;
      //               }
      //               if (currentRange.MaximumValue.HasValue)
      //               {
      //                  currentRange.MaximumValue = Math.Max(currentRange.MaximumValue.Value, update.Value);
      //               }
      //               else
      //               {
      //                  currentRange.MaximumValue = update.Value;
      //               }
      //            }
      //            break;
      //         case GameControllerAxis.RY:
      //            if (update.Offset == JoystickOffset.RotationY)
      //            {
      //               if (currentRange.MinimumValue.HasValue)
      //               {
      //                  currentRange.MinimumValue = Math.Min(currentRange.MinimumValue.Value, update.Value);
      //               }
      //               else
      //               {
      //                  currentRange.MinimumValue = update.Value;
      //               }
      //               if (currentRange.MaximumValue.HasValue)
      //               {
      //                  currentRange.MaximumValue = Math.Max(currentRange.MaximumValue.Value, update.Value);
      //               }
      //               else
      //               {
      //                  currentRange.MaximumValue = update.Value;
      //               }
      //            }
      //            break;
      //      }
      //   }


   }
}
