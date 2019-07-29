using ASCOM.LunaticAstroEQ.Controls;
using ASCOM.LunaticAstroEQ.Core;
using GalaSoft.MvvmLight;
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
   public enum GameControllerCommand
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
   public enum GameControllerAxis
   {
      [Description("X-Axis")]
      X,
      [Description("Y-Axis")]
      Y,
      [Description("Z-Axis")]
      Z,
      [Description("Rotation X")]
      RX,
      [Description("Rotation Y")]
      RY
   }

   [TypeConverter(typeof(EnumTypeConverter))]
   public enum GameControllerButton
   {
      [Description("")]
      UNMAPPED = -1,
      [Description("Button 0")]
      BUTTON_0,
      [Description("Button 1")]
      BUTTON_1,
      [Description("Button 2")]
      BUTTON_2,
      [Description("Button 3")]
      BUTTON_3,
      [Description("Button 4")]
      BUTTON_4,
      [Description("Button 5")]
      BUTTON_5,
      [Description("Button 6")]
      BUTTON_6,
      [Description("Button 7")]
      BUTTON_7,
      [Description("Button 8")]
      BUTTON_8,
      [Description("Button 9")]
      BUTTON_9,
      [Description("PoV North")]
      POV_N,
      [Description("PoV South")]
      POV_S,
      [Description("PoV West")]
      POV_W,
      [Description("PoV East")]
      POV_E,
      [Description("PoV Northwest")]
      POV_NW,
      [Description("PoV NorthEast")]
      POV_NE,
      [Description("PoV Southwest")]
      POV_SW,
      [Description("PoV Southeast")]
      POV_SE
   }

   public class GameControllerMapping : ObservableObject
   {
      private GameControllerCommand _Command;
      public GameControllerCommand Command
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

      private GameControllerButton _Button;
      public GameControllerButton Button
      {
         get
         {
            return _Button;
         }
         set
         {
            Set(ref _Button, value);
         }
      }


      public GameControllerMapping(GameControllerCommand command)
      {
         this.Command = command;
         this.Button = GameControllerButton.UNMAPPED;
      }

   }

   public class GameControllerMappingCollection : ObservableCollection<GameControllerMapping>
   {
   }

   public class GameControllerAxisRange : ObservableObject
   {
      private GameControllerAxis _Axis;
      [Description("Axis")]
      public GameControllerAxis Axis
      {
         get
         {
            return _Axis;
         }
         private set
         {
            Set(ref _Axis, value);
         }
      }

      private int? _MinimumValue;
      [Description("Minimum value")]
      public int? MinimumValue
      {
         get
         {
            return _MinimumValue;
         }
         set
         {
            Set(ref _MinimumValue, value);
         }
      }

      private int? _MaximumValue;
      [Description("Maximum value")]
      public int? MaximumValue
      {
         get
         {
            return _MaximumValue;
         }
         set
         {
            Set(ref _MaximumValue, value);
         }
      }

      public GameControllerAxisRange(GameControllerAxis axis)
      {
         this.Axis = axis;
      }
   }

   public class GameControllerAxisRangeCollection : ObservableCollection<GameControllerAxisRange>
   { }

   public class GameController : ObservableObject
   {


      [DisplayName("Controller product Id")]
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

      [JsonIgnore]
      public Guid InstanceGuid { get; set; }


      private bool _IsConnected;
      [DisplayName("Controller is connected")]
      [Description("Indicates whether the game controller is currently connected to the computer.")]
      [PropertyOrder(0)]
      public bool IsConnected
      {
         get
         {
            return _IsConnected;
         }
         private set
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
            Set<bool>(ref _IsActiveGameController, value);
         }
      }

      private readonly GameControllerMappingCollection _ButtonMappings = new GameControllerMappingCollection();

      public GameControllerMappingCollection ButtonMappings
      {
         get
         {
            return _ButtonMappings;
         }
      }

      [PropertyOrder(2)]
      [JsonIgnore]
      public IEnumerable<GameControllerMapping> ActiveButtonMappings
      {
         get
         {
            return _ButtonMappings.Where(m => m.Button != GameControllerButton.UNMAPPED);
         }
      }

      private readonly GameControllerAxisRangeCollection _AxisRanges = new GameControllerAxisRangeCollection();

      public GameControllerAxisRangeCollection AxisRanges
      {
         get
         {
            return _AxisRanges;
         }
      }

      [PropertyOrder(3)]
      [JsonIgnore]
      public IEnumerable<GameControllerAxisRange> ActiveAxisRanges
      {
         get
         {
            return _AxisRanges.Where(r => r.MinimumValue.HasValue && r.MaximumValue.HasValue);
         }
      }

      private readonly GameControllerAxisRangeCollection _AxisDeadZones = new GameControllerAxisRangeCollection();

      public GameControllerAxisRangeCollection AxisDeadZones
      {
         get
         {
            return _AxisDeadZones;
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
         foreach (var command in EnumHelper.ToList(typeof(GameControllerCommand)))
         {
            ButtonMappings.Add(new GameControllerMapping((GameControllerCommand)command.Key));
         }
         foreach (var axis in EnumHelper.ToList(typeof(GameControllerAxis)))
         {
            AxisRanges.Add(new GameControllerAxisRange((GameControllerAxis)axis.Key));
            AxisDeadZones.Add(new GameControllerAxisRange((GameControllerAxis)axis.Key));
         }
      }

      [OnDeserialized]
      private void Deserialized(StreamingContext context)
      {
         foreach (var command in EnumHelper.ToList(typeof(GameControllerCommand)))
         {
            if (!ButtonMappings.Any(m => m.Command == (GameControllerCommand)command.Key))
            {
               ButtonMappings.Add(new GameControllerMapping((GameControllerCommand)command.Key));
            }
         }
         foreach (var axis in EnumHelper.ToList(typeof(GameControllerAxis)))
         {
            if (!AxisRanges.Any(r => r.Axis == (GameControllerAxis)axis.Key))
            {
               AxisRanges.Add(new GameControllerAxisRange((GameControllerAxis)axis.Key));
            }
            if (!AxisDeadZones.Any(r => r.Axis == (GameControllerAxis)axis.Key))
            {
               AxisDeadZones.Add(new GameControllerAxisRange((GameControllerAxis)axis.Key));
            }
         }

      }

      public void SetConnected(bool value, bool supressPropertyChangedEvents = false)
      {
         if (supressPropertyChangedEvents)
         {
            _IsConnected = value;
            if (!value)
            {
               _IsActiveGameController = false;
            }
         }
         else
         {
            IsConnected = value;
            if (!value)
            {
               IsActiveGameController = false;
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
            if (ReferenceEquals(_ActiveGameController, value))
            {
               return;
            }
            _ActiveGameController = value;
            RaisePropertyChanged("ActiveGameController");
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
                  foreach (GameController switchoff in Items.Where(c => c.Id != gameController.Id && c.IsActiveGameController))
                  {
                     switchoff.IsActiveGameController = false;
                  }
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

      public void SetActiveGameController(Guid id)
      {
         GameController activeGameController = this.Items.Where(c => c.Id == id && c.IsConnected).FirstOrDefault();
         if (activeGameController != null)
         {
            activeGameController.IsActiveGameController = true;
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
      JoystickUpdate
   }

   public class GameControllerUpdate
   {
      public GameControllerUpdateNotification Notification { get; set; }

      public JoystickUpdate Update { get; set; }

      public GameControllerUpdate(JoystickUpdate update)
      {
         this.Notification = GameControllerUpdateNotification.JoystickUpdate;
         this.Update = update;
      }

      public GameControllerUpdate()
      {
         this.Notification = GameControllerUpdateNotification.ConnectedChanged;
      }

   }
   #endregion
   public class GameControllerService
   {


      private List<Guid> _AvailableInstances = new List<Guid>();

      public GameControllerService()
      {
      }

      public static void UpdateAvailableGameControllers(TelescopeControlSettings settings, bool suppressNotifications = true)
      {
         // Switch off active controller if it is not connected
         Guid activeGameControllerId = Guid.Empty;
         foreach (GameController gameController in settings.GameControllers)
         {
            if (gameController.IsActiveGameController)
            {
               activeGameControllerId = gameController.Id;
            }
            gameController.SetConnected(false, suppressNotifications);
            gameController.InstanceGuid = Guid.Empty;
         }

         DirectInput directInput = new DirectInput();
         foreach (DeviceInstance deviceInstance in directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices))
         {
            GameController existing = settings.GameControllers.Where(c => c.Id == deviceInstance.ProductGuid).FirstOrDefault();
            if (existing == null)
            {
               settings.GameControllers.Add(new GameController(deviceInstance.ProductGuid, deviceInstance.ProductName)
               {
                  InstanceGuid = deviceInstance.InstanceGuid
               });
            }
            else
            {
               existing.SetConnected(true, suppressNotifications);
               existing.InstanceGuid = deviceInstance.InstanceGuid;
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
               existing.SetConnected(true, suppressNotifications);
               existing.InstanceGuid = deviceInstance.InstanceGuid;
            }
         }


         
         settings.GameControllers.SetActiveGameController(activeGameControllerId);
      }
      
      public static bool IsInstanceConnected(Guid instanceGuid)
      {
         DirectInput directInput = new DirectInput();
         return directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices).Any(d => d.InstanceGuid == instanceGuid);
      }
         

      public static void ListAvailableGameControllers()
      {
         DirectInput directInput = new DirectInput();
         System.Diagnostics.Debug.WriteLine("GAME CONTROLLERS");
         foreach (DeviceInstance deviceInstance in directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices))
         {
            System.Diagnostics.Debug.WriteLine($"{deviceInstance.ProductName} - Instance Id: {deviceInstance.InstanceGuid}, instance name: {deviceInstance.InstanceName}");
         }
         System.Diagnostics.Debug.WriteLine("");
      }


      
   }
}
