using ASCOM.LunaticAstroEQ.Core;
using GalaSoft.MvvmLight.Messaging;
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
         GameController controllerInstance = null;
         if (_Controller == null)
         {
            return;
         }
         controllerInstance = _Controller;
         var progressHandler = new Progress<GameControllerProgressArgs>(value =>
         {
            if (value.Notification == GameControllerUpdateNotification.ConnectedChanged)
            {
               ControllerConnected = GameControllerService.IsInstanceConnected(controllerInstance.Id);
            }
            else if (value.Notification != GameControllerUpdateNotification.JoystickUpdate)
            {
               ProcessUpdate(value);
            }
         });

         var progress = progressHandler as IProgress<GameControllerProgressArgs>;
         _controllerTokenSource = new CancellationTokenSource();
         var token = _controllerTokenSource.Token;
         try
         {
            await Task.Run(() =>
            {
               System.Diagnostics.Debug.WriteLine($"Game controller command task STARTED. ({controllerInstance.Name})");
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
                        using (Joystick joystick = new Joystick(directInput, controllerInstance.Id))
                        {
                           joystick.Properties.Range = new InputRange(-5000, 5000);
                           joystick.Properties.DeadZone = 5;
                           joystick.Properties.Saturation = 4800;
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
                                    progress.Report(new GameControllerProgressArgs());
                                 }
                                 // Check for POV updates
                                 IEnumerable<JoystickUpdate> povUpdates = datas.Where(s => s.Offset == JoystickOffset.PointOfViewControllers0).OrderBy(s => s.Sequence);
                                 IEnumerable<JoystickUpdate> axisUpdates = datas.Where(s => s.RawOffset < 20).OrderBy(s => s.Sequence);
                                 IEnumerable<JoystickUpdate> buttonUpdates = datas.Where(s => s.RawOffset >= 48 && s.RawOffset <= 57).OrderBy(s => s.Sequence);
                                 if (progress != null)
                                 {
                                    if (buttonUpdates.Any())
                                    {
                                       ProcessButtons(controllerInstance, buttonUpdates, progress);
                                    }
                                    if (povUpdates.Any())
                                    {
                                       ProcessPOVs(controllerInstance, povUpdates, progress);
                                    }
                                    if (axisUpdates.Any())
                                    {
                                       ProcessAxes(controllerInstance, axisUpdates, progress);
                                    }
                                 }
                              }
                              catch (SharpDX.SharpDXException)
                              {
                                 notResponding = true;
                                 progress.Report(new GameControllerProgressArgs());
                                 Thread.Sleep(2000);
                                 break;
                              }
                           }
                        }
                     }
                     catch (SharpDX.SharpDXException)
                     {
                        notResponding = true;
                        progress.Report(new GameControllerProgressArgs());
                        Thread.Sleep(2000);
                     }

                  }
               }
            });
         }
         catch (OperationCanceledException)
         {
            System.Diagnostics.Debug.WriteLine($"Game controller command task CANCELLED. ({controllerInstance.Name})");
         }
         finally
         {
            controllerInstance = null;
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


      private void ProcessButtons(GameController controller, IEnumerable<JoystickUpdate> updates, IProgress<GameControllerProgressArgs> progress)
      {
         foreach (JoystickUpdate state in updates) // Just take the down clicks not the up.
         {
            GameControllerButtonMapping mapping = controller.ButtonMappings.Where(m => m.JoystickOffset == state.Offset).FirstOrDefault();
            if (mapping != null)
            {
               if (state.Value == 0)
               {
                  progress.Report(new GameControllerProgressArgs(GameControllerUpdateNotification.CommandUp, mapping.Command));
               }
               else
               {
                  progress.Report(new GameControllerProgressArgs(GameControllerUpdateNotification.CommandDown, mapping.Command));
               }
            }
         }
      }

      private void ProcessPOVs(GameController controller, IEnumerable<JoystickUpdate> updates, IProgress<GameControllerProgressArgs> progress)
      {

         GameControllerButtonCommand? currentCommand = null;
         foreach (JoystickUpdate state in updates)
         {
            if (state.Value > -1)
            {
               GameControllerPOVDirection povDirection = GameController.GetPOVDirection(state);
               GameControllerButtonMapping mapping = controller.ButtonMappings.Where(m => m.JoystickOffset == state.Offset && m.POVDirection == povDirection).FirstOrDefault();
               if (mapping != null)
               {
                  currentCommand = mapping.Command;
                  progress.Report(new GameControllerProgressArgs(GameControllerUpdateNotification.CommandDown, currentCommand.Value));
               }
            }
            else
            {
               if (currentCommand.HasValue)
               {
                  progress.Report(new GameControllerProgressArgs(GameControllerUpdateNotification.CommandUp, currentCommand.Value));
                  currentCommand = null;
               }
            }
         }
      }

      private void ProcessAxes(GameController controller, IEnumerable<JoystickUpdate> updates, IProgress<GameControllerProgressArgs> progress)
      {
         foreach (JoystickUpdate state in updates) // Just take the down clicks not the up.
         {
            System.Diagnostics.Debug.WriteLine(state.Value);
            GameControllerAxisMapping mapping = controller.AxisMappings.Where(m => m.JoystickOffset == state.Offset).FirstOrDefault();
            if (mapping != null)
            {
               bool highSpeed = (state.Value <= -4000 || state.Value >= 4000);
               GameControllerUpdateNotification notification = (state.Value != 0 ? GameControllerUpdateNotification.CommandDown : GameControllerUpdateNotification.CommandUp);
               bool reverse = (state.Value < 0);
               if (mapping.ReverseDirection)
               {
                  reverse = !reverse;
               }
               progress.Report(new GameControllerProgressArgs(notification, mapping.Command, reverse, highSpeed));
            }
         }
      }




      private void ProcessUpdate(GameControllerProgressArgs update)
      {
         if (update.Notification == GameControllerUpdateNotification.CommandDown || update.Notification == GameControllerUpdateNotification.CommandUp)
         {
            if (update.ButtonCommand.HasValue)
            {
               System.Diagnostics.Debug.WriteLine($"Controller notification:{update.Notification}, Commands: Button - [{update.ButtonCommand}], Axis - [{update.AxisCommand}]");
            }
            else if (update.AxisCommand.HasValue)
            {
               System.Diagnostics.Debug.WriteLine($"Controller notification:{update.Notification}, Commands: Axis - [{update.AxisCommand}], highspeed - [{update.Highspeed}], reverse - [{update.Reverse}]");
            }
         }
      }


   }
}
