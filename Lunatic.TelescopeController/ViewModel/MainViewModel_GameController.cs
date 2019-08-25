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

      protected class GameControllerButtonCommandState
      {
         public GameControllerButtonCommand Command { get; set; }

         public int Value { get; set; }


         public int Count { get; set; }

         public long Timestamp { get; set; }


         public GameControllerButtonCommandState(GameControllerButtonCommand command, int value)
         {
            Command = command;
            Value = value;
            Timestamp = DateTime.Now.Ticks;
         }

         public void Increment()
         {
            Count++;
         }

      }

      protected class GameControllerAxisCommandState
      {
         public GameControllerAxisCommand Command { get; set; }
         public bool Highspeed { get; set; }

         public bool Reverse { get; set; }

         public long Timestamp { get; set; }

         public GameControllerAxisCommandState(GameControllerAxisCommand command, bool highspeed, bool reverse)
         {
            Command = command;
            Highspeed = highspeed;
            Reverse = reverse;
            Timestamp = DateTime.Now.Ticks;
         }

      }

      // Used to record button command states
      private List<GameControllerButtonCommandState> _ButtomCommandHistory = new List<GameControllerButtonCommandState>();

      // User to record axis command states;
      private List<GameControllerAxisCommandState> _AxisCommandHistory = new List<GameControllerAxisCommandState>();

      private CancellationTokenSource _controllerTokenSource;

      private GameController _Controller = null;

      private bool _ControllerConnected = false;

      private JoystickUpdate _PreviousPOVState = new JoystickUpdate();

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
         System.Diagnostics.Debug.WriteLine($"ProcessButtons, count={updates.Count()})");
         foreach (JoystickUpdate state in updates) // Just take the down clicks not the up.
         {
            GameControllerButtonMapping mapping = controller.ButtonMappings.Where(m => m.JoystickOffset == state.Offset).FirstOrDefault();
            // Need to change 0 (button up to -1) so that we can share the same processing methods as the POV.
            ProcessButtonMapping(mapping, (state.Value == 0 ? -1 : state.Value), progress);
         }
      }

      private void ProcessPOVs(GameController controller, IEnumerable<JoystickUpdate> updates, IProgress<GameControllerProgressArgs> progress)
      {
         System.Diagnostics.Debug.WriteLine($"ProcessPOVs, count={updates.Count()})");
         GameControllerPOVDirection previousPOVDirection = GameControllerPOVDirection.UNMAPPED;
         GameControllerButtonMapping previousMapping = null;
         foreach (JoystickUpdate state in updates)
         {
            System.Diagnostics.Debug.WriteLine($"Process state ({state.Value}), previous state timestamp = {_PreviousPOVState.Timestamp}");

            if (_PreviousPOVState.Timestamp != 0)
            {
               // Get previous stuff
               previousPOVDirection = GameController.GetPOVDirection(_PreviousPOVState);
               previousMapping = controller.ButtonMappings.Where(m => m.JoystickOffset == _PreviousPOVState.Offset && m.POVDirection == previousPOVDirection).FirstOrDefault();
            }
            GameControllerPOVDirection povDirection = GameController.GetPOVDirection(state);
            if (povDirection == GameControllerPOVDirection.UNMAPPED)
            {
               if (previousMapping != null)
               {
                  ProcessButtonMapping(previousMapping, -1, progress);
               }
               _PreviousPOVState = new JoystickUpdate();
            }
            else
            {
               if (povDirection != previousPOVDirection && previousPOVDirection != GameControllerPOVDirection.UNMAPPED)
               {
                  // switch from one direction to another without coming up so need to UP the previous mapping.
                  ProcessButtonMapping(previousMapping, -1, progress);
               }
               GameControllerButtonMapping mapping = controller.ButtonMappings.Where(m => m.JoystickOffset == state.Offset && m.POVDirection == povDirection).FirstOrDefault();
               ProcessButtonMapping(mapping, state.Value, progress);
               _PreviousPOVState = state;
            }
         }
      }

      private void ProcessButtonMapping(GameControllerButtonMapping mapping, int stateValue, IProgress<GameControllerProgressArgs> progress)
      {
         if (mapping != null)
         {
            switch (mapping.Command)
            {
               case GameControllerButtonCommand.Sync:
                  ProcessDoubleClickButton(mapping, stateValue, progress);
                  break;
               case GameControllerButtonCommand.North:
               case GameControllerButtonCommand.South:
               case GameControllerButtonCommand.West:
               case GameControllerButtonCommand.East:
                  ProcessDownAndUpButtonClick(mapping, stateValue, progress);
                  break;
               default:
                  ProcessCancellableButtonClick(mapping, stateValue, progress);
                  break;
            }
         }
      }

      /// <summary>
      /// Processes a button click where the command can be cancelled by holding the button down for more than 2 seconds.
      /// </summary>
      private void ProcessCancellableButtonClick(GameControllerButtonMapping mapping, int stateValue, IProgress<GameControllerProgressArgs> progress)
      {
         System.Diagnostics.Debug.WriteLine("ProcessCancellableButtonClick");
         long now = DateTime.Now.Ticks;
         GameControllerButtonCommandState history = _ButtomCommandHistory.Where(h => h.Command == mapping.Command).FirstOrDefault();
         if (history == null || history.Value != stateValue)
         {
            // Not seen the command or seen it and the state has changed
            if (stateValue >= 0)
            {
               if (history == null) // Initial command down
               {
                  _ButtomCommandHistory.Add(new GameControllerButtonCommandState(mapping.Command, stateValue));
                  // progress.Report(new GameControllerProgressArgs(GameControllerUpdateNotification.CommandDown, mapping.Command));
               }
            }
            else
            {
               if (history != null)
               {
                  if (now - history.Timestamp < 15E6) // Test to make sure that press is less than 1.5 seconds.Allows command to be cancelled by long hold.
                  {
                     progress.Report(new GameControllerProgressArgs(GameControllerUpdateNotification.CommandUp, mapping.Command));
                  }
                  // Remove history record
                  _ButtomCommandHistory.Remove(history);
               }
            }
         }
      }

      /// <summary>
      /// Processes a button click where the command can be cancelled by holding the button down for more than 2 seconds.
      /// </summary>
      private void ProcessDownAndUpButtonClick(GameControllerButtonMapping mapping, int stateValue, IProgress<GameControllerProgressArgs> progress)
      {
         System.Diagnostics.Debug.WriteLine($"ProcessDownAndUpButtonClick ({stateValue})");
         long now = DateTime.Now.Ticks;
         GameControllerButtonCommandState history = _ButtomCommandHistory.Where(h => h.Command == mapping.Command).FirstOrDefault();
         if (history == null || history.Value != stateValue)
         {
            // Not seen the command or seen it and the state has changed
            if (stateValue >= 0)
            {
               if (history == null) // Initial command down
               {
                  System.Diagnostics.Debug.WriteLine("Add history, report");
                  _ButtomCommandHistory.Add(new GameControllerButtonCommandState(mapping.Command, stateValue));
                  progress.Report(new GameControllerProgressArgs(GameControllerUpdateNotification.CommandDown, mapping.Command));
               }
            }
            else
            {
               if (history != null)
               {
                  System.Diagnostics.Debug.WriteLine("Remove history, report");
                  // Remove history record
                  _ButtomCommandHistory.Remove(history);
                  progress.Report(new GameControllerProgressArgs(GameControllerUpdateNotification.CommandUp, mapping.Command));
               }
            }
         }
      }


      /// <summary>
      /// Processes a button click where it must be double clicked within second.
      /// </summary>
      private void ProcessDoubleClickButton(GameControllerButtonMapping mapping, int stateValue, IProgress<GameControllerProgressArgs> progress)
      {
         System.Diagnostics.Debug.WriteLine($"ProcessDoubleClickButton value = {stateValue}");
         long now = DateTime.Now.Ticks;
         GameControllerButtonCommandState history = _ButtomCommandHistory.Where(h => h.Command == mapping.Command).FirstOrDefault();
         if (history == null)
         {
            // Not seen the command or seen it and the state has changed
            if (stateValue >= 0)
            {
               _ButtomCommandHistory.Add(new GameControllerButtonCommandState(mapping.Command, stateValue));
            }
         }
         else
         {
            System.Diagnostics.Debug.WriteLine($"History counter = {history.Count}");
            if (stateValue >= 0)
            {
               if ((now - history.Timestamp) < 10E06)  // Second click must be within 1 second
               {
                  // Increment the history count
                  history.Increment();
               }
               history.Timestamp = now;
            }
            else
            {
               // Button up
               if (now - history.Timestamp >= 15E6) // Test to make sure that press is less than 1.5 seconds.Allows command to be cancelled by long hold.
               {
                  // Remove history record
                  _ButtomCommandHistory.Remove(history);
               }
               else
               {
                  if (history.Count > 0)
                  {
                     _ButtomCommandHistory.Remove(history);
                     progress.Report(new GameControllerProgressArgs(GameControllerUpdateNotification.CommandUp, mapping.Command));
                  }
                  else
                  {
                     // Update timestamp and value
                     history.Timestamp = now;
                     history.Value = stateValue;
                  }
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
