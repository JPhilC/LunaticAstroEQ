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

      private GameController _PoledController = null;

      public void StartGameControllerTask()
      {
         var progressHandler = new Progress<GameControllerProgressArgs>(value =>
         {
            if (value.Notification == GameControllerUpdateNotification.ConnectedChanged)
            {
               ControllerConnected = GameControllerService.IsInstanceConnected(_PoledController.Id);
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
            Task.Factory.StartNew(() =>
            {
               if (_Controller == null)
               {
                  return;
               }
               _PoledController = _Controller;
               bool cancelled = false;
               System.Diagnostics.Debug.WriteLine($"Game controller command task STARTED. ({_PoledController.Name})");
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
                        using (Joystick joystick = new Joystick(directInput, _PoledController.Id))
                        {
                           joystick.Properties.Range = new InputRange(-10000, 10000);
                           joystick.Properties.DeadZone = 100;
                           joystick.Properties.Saturation = 9990;
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
                                 IEnumerable<JoystickUpdate> axisUpdates = datas.Where(s => s.RawOffset <= 20).OrderBy(s => s.Sequence);
                                 IEnumerable<JoystickUpdate> buttonUpdates = datas.Where(s => s.RawOffset >= 48 && s.RawOffset <= 57).OrderBy(s => s.Sequence);
                                 if (progress != null)
                                 {
                                    if (buttonUpdates.Any())
                                    {
                                       ProcessButtons(_PoledController, buttonUpdates, progress);
                                    }
                                    if (povUpdates.Any())
                                    {
                                       ProcessPOVs(_PoledController, povUpdates, progress);
                                    }
                                    if (axisUpdates.Any())
                                    {
                                       ProcessAxes(_PoledController, axisUpdates, progress);
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
                              catch (OperationCanceledException)
                              {
                                 System.Diagnostics.Debug.WriteLine($"Game controller command task CANCELLED. ({_PoledController.Name})");
                                 cancelled = true;
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
                     catch (OperationCanceledException)
                     {
                        System.Diagnostics.Debug.WriteLine($"Game controller command task CANCELLED. ({_PoledController.Name})");
                        cancelled = true;
                        break;
                     }

                     // Outer loop
                     if (cancelled)
                     {
                        break;
                     }
                  }
               }  // Using DirectInput
            }, _controllerTokenSource.Token);
         }
         catch (OperationCanceledException)
         {
            System.Diagnostics.Debug.WriteLine($"Game controller command task CANCELLED. ({_PoledController.Name})");
         }
         finally
         {
            _PoledController = null;
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
            // Need to change 0 (button up to -1) so that we can share the same processing methods as the POV.
            ProcessButtonMapping(mapping, (state.Value == 0 ? -1 : state.Value), progress);
         }
      }

      private void ProcessPOVs(GameController controller, IEnumerable<JoystickUpdate> updates, IProgress<GameControllerProgressArgs> progress)
      {
         GameControllerPOVDirection previousPOVDirection = GameControllerPOVDirection.UNMAPPED;
         GameControllerButtonMapping previousMapping = null;
         foreach (JoystickUpdate state in updates)
         {
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
                  progress.Report(new GameControllerProgressArgs(GameControllerUpdateNotification.CommandDown, mapping.Command));
               }
            }
            else
            {
               if (history != null)
               {
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
         // System.Diagnostics.Debug.WriteLine($"ProcessAxis ({updates.Count()})");
         foreach (JoystickUpdate state in updates) // Just take the down clicks not the up.
         {
            GameControllerAxisMapping mapping = controller.AxisMappings.Where(m => m.JoystickOffset == state.Offset).FirstOrDefault();
            ProcessAxisMapping(mapping, state.Value, progress);
         }
      }

      private void ProcessAxisMapping(GameControllerAxisMapping mapping, int stateValue, IProgress<GameControllerProgressArgs> progress)
      {
         if (mapping != null)
         {
            switch (mapping.Command)
            {
               case GameControllerAxisCommand.SlewNSDualSpeed:
               case GameControllerAxisCommand.SlewEWDualSpeed:
                  ProcessDualSpeedAxis(mapping, stateValue, progress);
                  break;

               case GameControllerAxisCommand.SlewNSHighSpeed:
               case GameControllerAxisCommand.SlewEWHighSpeed:
                  ProcessAxis(mapping, stateValue, true, progress);
                  break;
               default:
                  ProcessAxis(mapping, stateValue, false, progress);
                  break;
            }
         }

      }

      private void ProcessDualSpeedAxis(GameControllerAxisMapping mapping, int stateValue, IProgress<GameControllerProgressArgs> progress)
      {
         if (mapping != null)
         {
            System.Diagnostics.Debug.Write($" {stateValue}");
            bool highspeed = (stateValue <= -9900 || stateValue >= 9900);
            bool reverse = (stateValue <= -10);
            bool stop = (stateValue > -10 && stateValue < 10);
            if (mapping.ReverseDirection)
            {
               System.Diagnostics.Debug.WriteLine("(Reversed direction)");
               reverse = !reverse;
            }
            GameControllerAxisCommandState history = _AxisCommandHistory.Where(h => h.Command == mapping.Command).FirstOrDefault();
            if (history == null)
            {
               if (!stop)
               {
                  // New command
                  _AxisCommandHistory.Add(new GameControllerAxisCommandState(mapping.Command, highspeed, reverse));
                  progress.Report(new GameControllerProgressArgs(GameControllerUpdateNotification.CommandDown, mapping.Command, reverse, highspeed));
               }
            }
            else
            {
               // See if command has changed
               if (stop)
               {
                  // Issue command up and remove history
                  progress.Report(new GameControllerProgressArgs(GameControllerUpdateNotification.CommandUp, mapping.Command, history.Reverse, history.Highspeed));
                  _AxisCommandHistory.Remove(history);
               }
               else if (history.Highspeed != highspeed || history.Reverse != reverse)
               {
                  // There has been a change of something so stop and restart
                  progress.Report(new GameControllerProgressArgs(GameControllerUpdateNotification.CommandUp, mapping.Command, history.Reverse, history.Highspeed));
                  history.Highspeed = highspeed;
                  history.Reverse = reverse;
                  if (reverse)
                  {
                     System.Diagnostics.Debug.Write("(Reverse)");
                  }
                  progress.Report(new GameControllerProgressArgs(GameControllerUpdateNotification.CommandDown, mapping.Command, reverse, highspeed));
               }
            }
         }
      }

      private void ProcessAxis(GameControllerAxisMapping mapping, int stateValue, bool highspeed, IProgress<GameControllerProgressArgs> progress)
      {
         if (mapping != null)
         {
            bool reverse = (stateValue <= -10);
            bool stop = (stateValue > -10 && stateValue < 10);
            if (mapping.ReverseDirection)
            {
               reverse = !reverse;
            }
            GameControllerAxisCommandState history = _AxisCommandHistory.Where(h => h.Command == mapping.Command).FirstOrDefault();
            if (history == null)
            {
               if (!stop)
               {
                  // New command
                  _AxisCommandHistory.Add(new GameControllerAxisCommandState(mapping.Command, highspeed, reverse));
                  progress.Report(new GameControllerProgressArgs(GameControllerUpdateNotification.CommandDown, mapping.Command, reverse, highspeed));
               }
            }
            else
            {
               // See if command has changed
               if (stop)
               {
                  // Issue command up and remove history
                  progress.Report(new GameControllerProgressArgs(GameControllerUpdateNotification.CommandUp, mapping.Command, false, false));
                  _AxisCommandHistory.Remove(history);
               }
               else if (history.Reverse != reverse)
               {
                  // There has been a change of something so stop and restart
                  progress.Report(new GameControllerProgressArgs(GameControllerUpdateNotification.CommandUp, mapping.Command, false, false));
                  history.Highspeed = highspeed;
                  history.Reverse = reverse;
                  progress.Report(new GameControllerProgressArgs(GameControllerUpdateNotification.CommandDown, mapping.Command, highspeed, reverse));
               }
            }
         }
      }



      private void ProcessUpdate(GameControllerProgressArgs update)
      {
         if (update.Notification == GameControllerUpdateNotification.CommandDown || update.Notification == GameControllerUpdateNotification.CommandUp)
         {
            if (update.ButtonCommand.HasValue)
            {
               System.Diagnostics.Debug.WriteLine($"==> Command: {update.Notification}, Commands: Button - [{update.ButtonCommand}] <==");
               HandleButtonCommand(update.ButtonCommand.Value, update.Notification);
            }
            else if (update.AxisCommand.HasValue)
            {
               if (update.Notification == GameControllerUpdateNotification.CommandUp)
               {
                  System.Diagnostics.Debug.WriteLine($"\n==> Command: Stop - [{update.AxisCommand}] <==");
               }
               else
               {
                  System.Diagnostics.Debug.WriteLine($"\n==> Command: {(update.Reverse ? "Reverse" : "Forward")} {(update.Highspeed ? "highspeed" : "lowspeed")} - [{update.AxisCommand}] <==");
               }
            }
         }
      }


      private void HandleButtonCommand(GameControllerButtonCommand command, GameControllerUpdateNotification notification)
      {
         switch (command)
         {
            case GameControllerButtonCommand.UnPark:
               if (notification == GameControllerUpdateNotification.CommandUp)
               {
                  if (ParkCommand.CanExecute(null))
                  {
                     if (IsParked)
                     {
                        ParkCommand.Execute(null);
                     }
                  }
               }
               break;
            case GameControllerButtonCommand.ParkToHome:
               if (notification == GameControllerUpdateNotification.CommandUp)
               {
                  if (ParkCommand.CanExecute(null))
                  {
                     if (!IsParked)
                     {
                        ParkCommand.Execute(null);
                     }
                  }
               }
               break;
            case GameControllerButtonCommand.Sync:
               if (notification == GameControllerUpdateNotification.CommandUp)
               {
                  if (SyncCommand.CanExecute(null))
                  {
                        SyncCommand.Execute(null);
                  }
               }
               break;
         }
      }
   }
}
