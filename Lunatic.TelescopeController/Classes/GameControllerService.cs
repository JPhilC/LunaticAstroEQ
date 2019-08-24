using ASCOM.LunaticAstroEQ.Controls;
using ASCOM.LunaticAstroEQ.Core;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Lunatic.TelescopeController
{

   #region Game Controller classes and Enums ...
   [TypeConverter(typeof(EnumTypeConverter))]
   public enum GameControllerButtonCommand
   {
      [Description("Emergency Stop")]
      EmergencyStop,
      [Description("Park To Home")]
      ParkToHome,
      [Description("Park To User Defined")]
      ParkToUserDefined,
      [Description("Park To Current Position")]
      ParkToCurrentPosition,
      [Description("UnPark")]
      UnPark,
      [Description("Slew North")]
      North,
      [Description("Slew South")]
      South,
      [Description("Slew East")]
      East,
      [Description("Slew West")]
      West,
      [Description("Sidereal Rate")]
      SiderealRate,
      [Description("Lunar Rate")]
      LunarRate,
      [Description("Solar Rate")]
      SolarRate,
      [Description("Custom Rate")]
      CustomRate,
      [Description("Reverse RA")]
      ReverseRA,
      [Description("Reverse Dec")]
      ReverseDec,
      [Description("Increate RA Rate")]
      IncreaseRARate,
      [Description("Decrease RA Rate")]
      DecreaseRARate,
      [Description("Increase DEC Rate")]
      IncreaseDECRate,
      [Description("Decrease DEC Rate")]
      DecreaseDECRate,
      [Description("Increment Preset")]
      IncrementPreset,
      [Description("Decrement Preset")]
      DecrementPreset,
      [Description("Alignment Accept")]
      AlignmentAccept,
      [Description("Alignment Cancel")]
      AlignmentCancel,
      [Description("Alignment End")]
      AlignmentEnd,
      [Description("Sync")]
      Sync
   }

   [TypeConverter(typeof(EnumTypeConverter))]
   public enum GameControllerAxisCommand
   {
      [Description("Slew North/South (Low speed)")]
      SlewNSLowSpeed,
      [Description("Slew East/West (Low speed)")]
      SlewEWLowSpeed,
      [Description("Slew North/South (High speed)")]
      SlewNSHighSpeed,
      [Description("Slew East/West (High speed)")]
      SlewEWHighSpeed,
      [Description("Slew North/South (Dual speed)")]
      SlewNSDualSpeed,
      [Description("Slew East/West (Dual speed)")]
      SlewEWDualSpeed
   }

   [TypeConverter(typeof(EnumTypeConverter))]
   public enum GameControllerPOVDirection
   {
      [Description("")]
      UNMAPPED = 0,
      [Description("North")]
      N,
      [Description("South")]
      S,
      [Description("West")]
      W,
      [Description("East")]
      E,
      [Description("Northwest")]
      NW,
      [Description("NorthEast")]
      NE,
      [Description("Southwest")]
      SW,
      [Description("Southeast")]
      SE
   }


   public abstract class GameControllerMapping : ObservableObject
   {

      private JoystickOffset? _JoystickOffset;
      public JoystickOffset? JoystickOffset
      {
         get
         {
            return _JoystickOffset;
         }
         set
         {
            if (Set(ref _JoystickOffset, value))
            {
               if (!_JoystickOffset.HasValue)
               {
                  Name = null;
               }
            }
         }
      }

      private string _Name;
      [Description("Name")]
      public string Name
      {
         get
         {
            return _Name;
         }
         set
         {
            Set(ref _Name, value);
         }
      }

   }

   public class GameControllerButtonMapping : GameControllerMapping
   {
      private GameControllerButtonCommand _Command;
      public GameControllerButtonCommand Command
      {
         get
         {
            return _Command;
         }
         private set
         {
            Set(ref _Command, value);
         }
      }

      private GameControllerPOVDirection _POVDirection;
      [Description("POV Direction")]
      public GameControllerPOVDirection POVDirection
      {
         get
         {
            return _POVDirection;
         }
         set
         {
            if (Set(ref _POVDirection, value))
            {
               RaisePropertyChanged("DisplayName");
            }
         }
      }

      public GameControllerButtonMapping(GameControllerButtonCommand command) : base()
      {
         this.Command = command;
      }

   }

   public class GameControllerButtonMappingCollection : ObservableCollection<GameControllerButtonMapping>
   {
   }


   public class GameControllerAxisMapping : GameControllerMapping
   {
      private GameControllerAxisCommand _Command;
      public GameControllerAxisCommand Command
      {
         get
         {
            return _Command;
         }
         private set
         {
            Set(ref _Command, value);
         }
      }

      private bool _ReverseDirection;
      [Description("Reverse Direction")]
      public bool ReverseDirection
      {
         get
         {
            return _ReverseDirection;
         }
         set
         {
            Set(ref _ReverseDirection, value);
         }
      }

      public GameControllerAxisMapping(GameControllerAxisCommand command, bool reverseDirection) : base()
      {
         this.Command = command;
         this.ReverseDirection = reverseDirection;
      }
   }

   public class GameControllerAxisMappingCollection : ObservableCollection<GameControllerAxisMapping>
   {
   }


   public class GameController : ObservableObject
   {

      public static GameControllerPOVDirection GetPOVDirection(JoystickUpdate update)
      {
         GameControllerPOVDirection direction = GameControllerPOVDirection.UNMAPPED;
         switch (update.Value)
         {
            case 0:
               direction = GameControllerPOVDirection.N;
               break;
            case 4500:
               direction = GameControllerPOVDirection.NE;
               break;
            case 9000:
               direction = GameControllerPOVDirection.E;
               break;
            case 13500:
               direction = GameControllerPOVDirection.SE;
               break;
            case 18000:
               direction = GameControllerPOVDirection.S;
               break;
            case 22500:
               direction = GameControllerPOVDirection.SW;
               break;
            case 27000:
               direction = GameControllerPOVDirection.W;
               break;
            case 31500:
               direction = GameControllerPOVDirection.NW;
               break;
         }
         return direction;
      }

      [DisplayName("Controller instance Id")]
      public Guid Id { get; set; }

      private string _Name;

      [PropertyOrder(1)]
      public string Name
      {
         get
         {
            return _Name;
         }
         set
         {
            Set(ref _Name, value);
         }
      }


      private bool _IsConnected;
      [DisplayName("Controller is connected")]
      [Description("Indicates whether the game controller is currently connected to the computer.")]
      [PropertyOrder(0)]
      [JsonIgnore]
      public bool IsConnected
      {
         get
         {
            return _IsConnected;
         }
         set
         {
            Set<bool>(ref _IsConnected, value);
         }
      }

      private bool _IsActiveGameController;
      [DisplayName("Current game controller")]
      [Description("Tick this box if this is the current game controller")]
      [PropertyOrder(0)]
      public bool IsActiveGameController
      {
         get
         {
            return _IsActiveGameController;
         }
         set
         {
            if (Set<bool>(ref _IsActiveGameController, value))
            {
               WasActiveGameController = false;
            }
         }
      }

      /// <summary>
      /// Used internally when watching for controllers being connected.
      /// </summary>
      public bool WasActiveGameController { get; set; }

      private readonly GameControllerButtonMappingCollection _ButtonMappings = new GameControllerButtonMappingCollection();

      public GameControllerButtonMappingCollection ButtonMappings
      {
         get
         {
            return _ButtonMappings;
         }
      }

      [PropertyOrder(2)]
      [Description("Configured buttons")]
      [JsonIgnore]
      public IEnumerable<GameControllerButtonMapping> ActiveButtonMappings
      {
         get
         {
            return _ButtonMappings.Where(m => m.JoystickOffset.HasValue);
         }
      }

      private readonly GameControllerAxisMappingCollection _AxisMappings = new GameControllerAxisMappingCollection();

      public GameControllerAxisMappingCollection AxisMappings
      {
         get
         {
            return _AxisMappings;
         }
      }

      [PropertyOrder(3)]
      [Description("Configured axes")]
      [JsonIgnore]
      public IEnumerable<GameControllerAxisMapping> ActiveAxisMappings
      {
         get
         {
            return _AxisMappings.Where(m => m.JoystickOffset.HasValue);
         }
      }


      private Dictionary<JoystickOffset, string> _JoystickObjects = new Dictionary<JoystickOffset, string>();
      /// <summary>
      /// Internal list of objects found on game controller by Joystick offset
      /// populated whenever the device is activated on the PC.
      /// </summary>
      [JsonIgnore]
      public Dictionary<JoystickOffset, string> JoystickObjects
      {
         get
         {
            return _JoystickObjects;
         }
      }

      public GameController()
      {

      }



      public GameController(Guid id, string name)
      {
         this.Id = id;
         this.Name = name;
         this.IsConnected = true;
         foreach (var command in EnumHelper.ToList(typeof(GameControllerButtonCommand)))
         {
            ButtonMappings.Add(new GameControllerButtonMapping((GameControllerButtonCommand)command.Key));
         }
         foreach (var command in EnumHelper.ToList(typeof(GameControllerAxisCommand)))
         {
            AxisMappings.Add(new GameControllerAxisMapping((GameControllerAxisCommand)command.Key, false));
         }
      }

      [OnDeserialized]
      private void Deserialized(StreamingContext context)
      {
         foreach (var command in EnumHelper.ToList(typeof(GameControllerButtonCommand)))
         {
            if (!ButtonMappings.Any(m => m.Command == (GameControllerButtonCommand)command.Key))
            {
               ButtonMappings.Add(new GameControllerButtonMapping((GameControllerButtonCommand)command.Key));
            }
         }

         foreach (var command in EnumHelper.ToList(typeof(GameControllerAxisCommand)))
         {
            if (!AxisMappings.Any(m => m.Command == (GameControllerAxisCommand)command.Key))
            {
               AxisMappings.Add(new GameControllerAxisMapping((GameControllerAxisCommand)command.Key, false));
            }
         }

      }

   }

   public class GameControllerCollection : ObservableCollection<GameController>
   {

      public event EventHandler<EventArgs> ActiveGameControllerChanged;
      public new event EventHandler<PropertyChangedEventArgs> PropertyChanged;

      private GameController _ActiveGameController;
      [DisplayName("Active game controller")]
      [Description("The currently selected game controller")]
      public GameController ActiveGameController
      {
         get
         {
            return _ActiveGameController;
         }
         private set
         {
            GameController oldValue = _ActiveGameController;
            if (ReferenceEquals(_ActiveGameController, value))
            {
               return;
            }
            _ActiveGameController = value;
            RaisePropertyChanged("ActiveGameController");
            Messenger.Default.Send<ActiveGameControllerChangedMessage>(new ActiveGameControllerChangedMessage(this, oldValue, _ActiveGameController));
         }
      }

      private bool resetingCurrentGameController = false;
      public GameControllerCollection() : base() { }


      [OnDeserialized]
      private void Deserialized(StreamingContext context)
      {
         this.ActiveGameController = this.Items.Where(c => c.IsActiveGameController).FirstOrDefault();
         // WeakEventManager<GameControllerCollection, EventArgs>.AddHandler(this.GameControllers, "CurrentGameControllerChanged", GameControllers_CurrentGameControllerChanged);
      }


      protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
      {
         if (e.Action == NotifyCollectionChangedAction.Add)
         {
            foreach (GameController gameController in e.NewItems)
            {
               // First remove the event because OnCollection changed is called twice when deserialising.
               WeakEventManager<GameController, PropertyChangedEventArgs>.RemoveHandler(gameController, "PropertyChanged", GameController_PropertyChanged);
               WeakEventManager<GameController, PropertyChangedEventArgs>.AddHandler(gameController, "PropertyChanged", GameController_PropertyChanged);
            }
         }
         else if (e.Action == NotifyCollectionChangedAction.Remove)
         {
            foreach (GameController gameController in e.OldItems)
            {
               WeakEventManager<GameController, PropertyChangedEventArgs>.RemoveHandler(gameController, "PropertyChanged", GameController_PropertyChanged);
            }
         }
         base.OnCollectionChanged(e);

      }

      private void GameController_PropertyChanged(object sender, PropertyChangedEventArgs e)
      {
         GameController gameController = sender as GameController;
         if (e.PropertyName == "IsActiveGameController")
         {
            if (!resetingCurrentGameController)
            {
               resetingCurrentGameController = true;
               if (gameController.IsActiveGameController)
               {
                  foreach (GameController switchoff in Items.Where(c => c.Id != gameController.Id))
                  {
                     switchoff.IsActiveGameController = false;
                     switchoff.WasActiveGameController = false;
                  }
                  gameController.WasActiveGameController = true;
                  ActiveGameController = gameController;
               }
               else
               {
                  ActiveGameController = null;
               }
               OnActiveGameControllerChanged();
               resetingCurrentGameController = false;
            }
         }
         if (gameController.IsActiveGameController)
         {
            RaisePropertyChanged("ActiveGameController." + e.PropertyName);
         }
         else
         {
            RaisePropertyChanged("SomeGameController." + e.PropertyName);
         }
      }


      private void OnActiveGameControllerChanged()
      {
         ActiveGameControllerChanged?.Invoke(this, EventArgs.Empty);
      }

      private void RaisePropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
   }

   public enum GameControllerUpdateNotification
   {
      ConnectedChanged,
      JoystickUpdate,
      CommandDown,
      CommandUp,
   }

   public class GameControllerUpdate
   {
      public GameControllerUpdateNotification Notification { get; set; }

      public JoystickUpdate Update { get; set; }

      public GameControllerButtonCommand ButtonCommand { get; set; }
      public GameControllerAxisCommand AxisCommand { get; set; }

      public GameControllerUpdate(JoystickUpdate update)
      {
         this.Notification = GameControllerUpdateNotification.JoystickUpdate;
         this.Update = update;
      }

      public GameControllerUpdate(GameControllerUpdateNotification notification, GameControllerButtonCommand command)
      {
         this.Notification = notification;
         this.ButtonCommand = command;
      }

      public GameControllerUpdate(GameControllerUpdateNotification notification, GameControllerAxisCommand command)
      {
         this.Notification = notification;
         this.AxisCommand = command;
      }

      public GameControllerUpdate()
      {
         this.Notification = GameControllerUpdateNotification.ConnectedChanged;
      }

   }

   public class ActiveGameControllerChangedMessage
   {
      public object Sender { get; private set; }
      public GameController OldController { get; private set; }
      public GameController NewController { get; private set; }

      public ActiveGameControllerChangedMessage(object sender, GameController oldController, GameController newController)
      {
         Sender = sender;
         OldController = oldController;
         NewController = newController;
      }
   }
   #endregion

   public class GameControllerService
   {
      public static Dictionary<int, JoystickOffset> USAGE_OFFSET = new Dictionary<int, JoystickOffset>
      {
         {1,JoystickOffset.Buttons0 },
         {2,JoystickOffset.Buttons1 },
         {3,JoystickOffset.Buttons2 },
         {4,JoystickOffset.Buttons3 },
         {5,JoystickOffset.Buttons4 },
         {6,JoystickOffset.Buttons5 },
         {7,JoystickOffset.Buttons6 },
         {8,JoystickOffset.Buttons7 },
         {9,JoystickOffset.Buttons8 },
         {10,JoystickOffset.Buttons9 },
         {11,JoystickOffset.Buttons10 },
         {12,JoystickOffset.Buttons11 },
         {13,JoystickOffset.Buttons12 },
         {14,JoystickOffset.Buttons13 },
         {15,JoystickOffset.Buttons14 },
         {16,JoystickOffset.Buttons15 },
         {17,JoystickOffset.Buttons16 },
         {18,JoystickOffset.Buttons17 },
         {19,JoystickOffset.Buttons18 },
         {20,JoystickOffset.Buttons19 },
         {48,JoystickOffset.X },
         {49,JoystickOffset.Y },
         {50,JoystickOffset.Z },
         {51,JoystickOffset.RotationX },
         {52,JoystickOffset.RotationY },
         {53,JoystickOffset.RotationZ },
         {57,JoystickOffset.PointOfViewControllers0 },
         {58,JoystickOffset.PointOfViewControllers1 },
         {59,JoystickOffset.PointOfViewControllers2 },
         {60,JoystickOffset.PointOfViewControllers3 }
      };

      private List<Guid> _AvailableInstances = new List<Guid>();

      public GameControllerService()
      {
      }

      public static void UpdateAvailableGameControllers(TelescopeControlSettings settings, bool announce = false)
      {
         bool setActiveController = false;
         string announcement = "";

         List<Guid> existingConnectedIds = settings.GameControllers.Where(c => c.IsConnected).Select(c => c.Id).ToList();
         List<Guid> connectedIds = new List<Guid>();
         List<GameController> newControllers = new List<GameController>();
         GameController controller;

         using (DirectInput directInput = new DirectInput())
         {
            ProcessDevices(directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices), settings, announce, ref existingConnectedIds, ref connectedIds, ref newControllers);
            ProcessDevices(directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices), settings, announce, ref existingConnectedIds, ref connectedIds, ref newControllers);


            // Disconnect any remaining existing connections as they are not now connected
            foreach (Guid existingId in existingConnectedIds)
            {
               controller = settings.GameControllers.Where(c => c.Id == existingId).FirstOrDefault();
               if (controller != null)
               {
                  if (controller.IsActiveGameController)
                  {
                     // We'll need to set the active controller to the first new Controller
                     setActiveController = true;
                     // controller.WasActiveGameController = true MUST follow controller.IsActiveGameController otherwise the flag will be cleared in the IsActive... setter.
                     controller.IsActiveGameController = false;
                     controller.WasActiveGameController = true;
                     announcement = $"The active {controller.Name} has disconnected.";
                  }
                  else
                  {
                     announcement = $"{controller.Name} has disconnected.";
                  }
                  controller.IsConnected = false;
               }
               if (announce)
               {
                  Messenger.Default.Send<AnnounceNotificationMessage>(new AnnounceNotificationMessage(announcement));
               }
            }
            // Add any new controllers
            foreach (GameController newController in newControllers)
            {
               settings.GameControllers.Add(newController);
               if (setActiveController)
               {
                  newController.IsActiveGameController = true;
                  setActiveController = false;
                  announcement = $"{newController.Name} is now connected and active.";
               }
               else
               {
                  announcement = $"{newController.Name} is now connected.";
               }
               if (announce)
               {
                  Messenger.Default.Send<AnnounceNotificationMessage>(new AnnounceNotificationMessage(announcement));
               }
            }
         }
      }


      private static void ProcessDevices(IList<DeviceInstance> devices, TelescopeControlSettings settings, bool announce, ref List<Guid> existingConnectedIds, ref List<Guid> connectedIDs, ref List<GameController> newControllers)
      {
         GameController controller;
         string announcement = "";
         foreach (DeviceInstance deviceInstance in devices)
         {
            controller = settings.GameControllers.Where(c => c.Id == deviceInstance.InstanceGuid).FirstOrDefault();
            if (controller == null)
            {
               controller = new GameController(deviceInstance.InstanceGuid, deviceInstance.ProductName);
               newControllers.Add(controller);
            }
            else
            {
               // Remove from list that should be disconnected
               existingConnectedIds.Remove(controller.Id);
               if (!controller.IsConnected)
               {
                  if (controller.WasActiveGameController)
                  {
                     controller.IsActiveGameController = true;
                     announcement = $"{controller.Name} is now connected and active.";
                  }
                  else
                  {
                     announcement = $"{controller.Name} is now connected.";
                  }
                  if (announce)
                  {
                     Messenger.Default.Send<AnnounceNotificationMessage>(new AnnounceNotificationMessage(announcement));
                  }
               }
            }
            controller.IsConnected = true;
            connectedIDs.Add(controller.Id);
         }

      }

      /*
      public static void UpdateAvailableGameControllers_Old(TelescopeControlSettings settings)
      {
         // Switch off active controller if it is not connected
         foreach (GameController gameController in settings.GameControllers)
         {
            if (gameController.IsActiveGameController)
            {
               gameController.WasActiveGameController = true;
               gameController.IsActiveGameController = false;
            }
            gameController.IsConnected = false;
            gameController.InstanceGuid = Guid.Empty;
         }

         using (DirectInput directInput = new DirectInput())
         {
            foreach (DeviceInstance deviceInstance in directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices))
            {
               GameController controller = settings.GameControllers.Where(c => c.Id == deviceInstance.ProductGuid).FirstOrDefault();
               if (controller == null)
               {
                  controller = new GameController(deviceInstance.ProductGuid, deviceInstance.ProductName)
                  {
                     InstanceGuid = deviceInstance.InstanceGuid
                  };
                  settings.GameControllers.Add(controller);
               }
               else
               {
                  controller.IsConnected = true;
                  controller.InstanceGuid = deviceInstance.InstanceGuid;
               }
            }

            // If Gamepad not found, look for a Joystick
            foreach (var deviceInstance in directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices))
            {
               GameController existing = settings.GameControllers.Where(c => c.Id == deviceInstance.ProductGuid).FirstOrDefault();
               if (existing == null)
               {
                  settings.GameControllers.Add(new GameController(deviceInstance.ProductGuid, deviceInstance.InstanceName)
                  {
                     InstanceGuid = deviceInstance.InstanceGuid
                  });
               }
               else
               {
                  existing.IsConnected = true;
                  existing.InstanceGuid = deviceInstance.InstanceGuid;
               }
            }
         }
         settings.GameControllers.SetActiveGameController();   // or Not
      }
      */

      public static bool IsInstanceConnected(Guid instanceGuid)
      {
         using (DirectInput directInput = new DirectInput())
         {
            return directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices).Any(d => d.InstanceGuid == instanceGuid);
         }
      }


      public static void ListAvailableGameControllers()
      {
         using (DirectInput directInput = new DirectInput())
         {
            System.Diagnostics.Debug.WriteLine("GAME CONTROLLERS");
            foreach (DeviceInstance deviceInstance in directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices))
            {
               System.Diagnostics.Debug.WriteLine($"{deviceInstance.ProductName} - Instance Id: {deviceInstance.InstanceGuid}, instance name: {deviceInstance.InstanceName}");
            }
            System.Diagnostics.Debug.WriteLine("");
         }
      }



   }
}
