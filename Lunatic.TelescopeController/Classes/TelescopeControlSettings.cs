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

using ASCOM.LunaticAstroEQ.Controls;
using ASCOM.LunaticAstroEQ.Core;
using ASCOM.LunaticAstroEQ.Core.Model;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Lunatic.TelescopeController
{
   public class ParkPosition : DataObjectBase
   {
      public Guid Id { get; set; }
      public string Name { get; set; }
      public int DecCount { get; set; }
      public int RACount { get; set; }

   }

   public class SlewRatePreset : ObservableObject
   {
      public int Rate { get; private set; }

      private int _RARate;
      public int RARate
      {
         get
         {
            return _RARate;
         }
         set
         {
            Set<int>(ref _RARate, value);
         }
      }

      private int _DecRate;
      public int DecRate
      {
         get
         {
            return _DecRate;
         }
         set
         {
            Set<int>(ref _DecRate, value);
         }
      }

      public SlewRatePreset(int rate, int raRate, int decRate)
      {
         Rate = rate;
         RARate = raRate;
         DecRate = decRate;
      }
   }


   [TypeConverter(typeof(EnumTypeConverter))]
   public enum PierSide
   {
      [Description("Unknown")]
      Unknown = -1,
      [Description("East pointing west")]
      East = 0,
      [Description("West pointing east")]
      West = 1
   }

   public enum ThreePointAlgorithm
   {
      [Description("Best Center")]
      BestCenter,
      [Description("Closest Points")]
      ClosestPoints
   }


   public class Site : DataObjectBase
   {
      public Site()
      {
         this.Id = Guid.NewGuid();
      }
      public Site(Guid id)
      {
         this.Id = id;
      }

      public Guid Id { get; private set; }
      //SiteName=Lime Grove

      private string _SiteName;
      [DisplayName("Site name")]
      [Description("Enter a name for this site.")]
      [PropertyOrder(0)]
      public string SiteName
      {
         get
         {
            return _SiteName;
         }
         set
         {
            Set<string>(ref _SiteName, value);
         }
      }

      //Elevation=173

      private double _Elevation;
      [DisplayName("Elevation (m)")]
      [Description("Enter the elevation of the site in metres.")]
      [PropertyOrder(3)]

      public double Elevation
      {
         get
         {
            return _Elevation;
         }
         set
         {
            Set<double>(ref _Elevation, value);
         }
      }
      //HemisphereNS=0
      private HemisphereOption _Hemisphere;
      [DisplayName("Hemisphere")]
      [Description("The hemisphere in which the site is located.")]
      [PropertyOrder(4)]
      public HemisphereOption Hemisphere
      {
         get
         {
            return _Hemisphere;
         }
         private set
         {
            Set<HemisphereOption>(ref _Hemisphere, value);
         }
      }

      //LatitudeNS=0
      //LatitudeDeg=52
      //LatitudeMin=40
      //LatitudeSec=6.0
      private const string LatitudePropertyName = "Latitude";
      private double _Latitude;
      [DisplayName("Latitude")]
      [Description("Enter the site latitude in the format DD MM SS(W/E) (e.g. 52 40 7N)")]
      [PropertyOrder(1)]
      public double Latitude
      {
         get
         {
            return _Latitude;
         }
         set
         {
            if (value != 0.0 && _Latitude == value)
            {
               return;
            }
            _Latitude = value;
            if (_Latitude == 0.0)
            {
               AddError(LatitudePropertyName, "Please enter a valid latitude.");
            }
            else
            {
               RemoveError(LatitudePropertyName);
            }
            RaisePropertyChanged();
            if (_Latitude < 0)
            {
               Hemisphere = HemisphereOption.Southern;
            }
            else
            {
               Hemisphere = HemisphereOption.Northern;
            }
         }
      }

      //LongitudeEW=1
      //LongitudeSec=21.0
      //LongitudeMin=20
      //LongitudeDeg=1

      private const string LongitudePropertyName = "Longitude";
      private double _Longitude;


      [DisplayName("Longitude")]
      [Description("Enter the site longitude in the format DD MM SS(N/S) (e.g. 1 20 21W)")]
      [PropertyOrder(2)]
      public double Longitude
      {
         get
         {
            return _Longitude;
         }
         set
         {
            if (value != 0.0 && _Longitude == value)
            {
               return;
            }
            _Longitude = value;
            if (_Longitude == 0.0)
            {
               AddError(LongitudePropertyName, "Please enter a valid longitude.");
            }
            else
            {
               RemoveError(LongitudePropertyName);
            }
            RaisePropertyChanged();
         }
      }

      private double _Temperature = 10.0;


      [DisplayName("Temperature")]
      [Description("Enter the temperature (°C)")]
      [PropertyOrder(5)]
      public double Temperature
      {
         get
         {
            return _Temperature;
         }
         set
         {
            Set<double>("Temperature", ref _Temperature, value);
         }
      }


      private bool _IsCurrentSite;
      [DisplayName("Current site")]
      [Description("Tick this box if this is the current site")]
      [PropertyOrder(0)]
      public bool IsCurrentSite
      {
         get
         {
            return _IsCurrentSite;
         }
         set
         {
            Set<bool>(ref _IsCurrentSite, value);
         }
      }

   }

   public class SiteCollection : ObservableCollection<Site>
   {

      public event EventHandler<EventArgs> CurrentSiteChanged;
      public new event EventHandler<PropertyChangedEventArgs> PropertyChanged;

      private Site _CurrentSite;
      [DisplayName("Current site")]
      [Description("The currently selected site")]
      public Site CurrentSite
      {
         get
         {
            return _CurrentSite;
         }
         private set
         {
            if (ReferenceEquals(_CurrentSite, value))
            {
               return;
            }
            _CurrentSite = value;
            RaisePropertyChanged("CurrentSite");
         }
      }

      private bool resetingCurrentSite = false;
      public SiteCollection() : base() { }


      [OnDeserialized]
      private void Deserialized(StreamingContext context)
      {
         this.CurrentSite = this.Items.Where(s => s.IsCurrentSite).FirstOrDefault();
         // WeakEventManager<SiteCollection, EventArgs>.AddHandler(this.Sites, "CurrentSiteChanged", Sites_CurrentSiteChanged);
      }


      protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
      {
         if (e.Action == NotifyCollectionChangedAction.Add)
         {
            foreach (Site site in e.NewItems)
            {
               // First remove the event because OnCollection changed is called twice when deserialising.
               WeakEventManager<Site, PropertyChangedEventArgs>.RemoveHandler(site, "PropertyChanged", Site_PropertyChanged);
               WeakEventManager<Site, PropertyChangedEventArgs>.AddHandler(site, "PropertyChanged", Site_PropertyChanged);
            }
         }
         else if (e.Action == NotifyCollectionChangedAction.Remove)
         {
            foreach (Site site in e.OldItems)
            {
               WeakEventManager<Site, PropertyChangedEventArgs>.RemoveHandler(site, "PropertyChanged", Site_PropertyChanged);
            }
         }
         base.OnCollectionChanged(e);

      }

      private void Site_PropertyChanged(object sender, PropertyChangedEventArgs e)
      {
         Site site = sender as Site;
         if (e.PropertyName == "IsCurrentSite")
         {
            if (!resetingCurrentSite)
            {
               if (site.IsCurrentSite)
               {
                  resetingCurrentSite = true;
                  foreach (Site switchoff in Items.Where(s => s.Id != site.Id && s.IsCurrentSite))
                  {
                     switchoff.IsCurrentSite = false;
                  }
                  CurrentSite = site;
                  OnCurrentSiteChanged();
                  resetingCurrentSite = false;
               }
            }
         }
         if (site.IsCurrentSite)
         {
            RaisePropertyChanged("CurrentSite." + e.PropertyName);
         }
         else
         {
            RaisePropertyChanged("SomeSite." + e.PropertyName);
         }
      }


      private void OnCurrentSiteChanged()
      {
         CurrentSiteChanged?.Invoke(this, EventArgs.Empty);
      }

      private void RaisePropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }
   }

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
      [Description("POV-N")]
      POV_N,
      [Description("POV-S")]
      POV_S,
      [Description("POV-W")]
      POV_W,
      [Description("POV-E")]
      POV_E,
      [Description("POV-NW")]
      POV_NW,
      [Description("POV-NE")]
      POV_NE,
      [Description("POV-SW")]
      POV_SW,
      [Description("POV-SE")]
      POV_SE
   }

   public class GameControllerMapping:ObservableObject
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

      private double? _MinimumValue;
      [Description("Minimum value")]
      public double? MinimumValue
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

      private double? _MaximumValue;
      [Description("Maximum value")]
      public double? MaximumValue
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

   public class GameController: ObservableObject
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

   public class TelescopeControlSettings : DataObjectBase
   {
      #region Properties ...

      public string DriverId { get; set; }
      public string DriverName { get; set; }

      public DisplayMode DisplayMode { get; set; }

      #region Voice/audio ...
      public bool AnnouncementsOn { get; set; }

      public string VoiceName { get; set; }

      [Range(-10, 10, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
      public int VoiceRate { get; set; }

      #endregion

      #region Sites ...
      private SiteCollection _Sites;
      public SiteCollection Sites
      {
         get
         {
            return _Sites;
         }
         private set
         {
            _Sites = value;
         }
      }

      public Site CurrentSite { get; set; }
      //Retry=1

      #endregion

      #region Game controller settings ...

      private GameControllerCollection _GameControllers;
      public GameControllerCollection GameControllers
      {
         get
         {
            return _GameControllers;
         }
         private set
         {
            _GameControllers = value;
         }
      }

      #endregion


      public bool AlwaysOnTop { get; set; }
      // ON_TOP1=0
      // LIMIT_FILE=

      #region Other / legacy (EQMOD) ...
      public string LimitFile { get; set; }
      // FILE_HIDDEN_DIR=0
      public bool FileHiddenDir { get; set; }

      //DEC_REVERSE=0
      private bool _ReverseDec;
      public bool ReverseDec
      {
         get
         {
            return _ReverseDec;
         }
         set
         {
            Set<bool>(ref _ReverseDec, value);
         }
      }

      //RA_REVERSE=1
      private bool _ReverseRA = true;
      public bool ReverseRA
      {
         get
         {
            return _ReverseRA;
         }
         set
         {
            Set<bool>(ref _ReverseRA, value);
         }
      }

      ////DSYNC01=0
      //public int DecSync { get; set; }
      ////RSYNC01=0
      //public int RASync { get; set; }

      ////DALIGN01=0 gDEC1Star
      //public double Dec1Star { get; set; }

      ////RALIGN01=0 gRA1Star
      //public double RA1Star { get; set; }

      ////BAR03_2=809  // From Mouse SlewPad
      ////BAR03_1=809  // From Mouse SlewPad
      ////SlewPadHeight=7875
      ////SlewPadWidth=7470

      ////UNPARK_DEC=9003010 gDECEncoderUNPark
      //public int DecEncoderUNParkPos { get; set; }
      ////UNPARK_RA=8388570 gRAEncoderUNPark
      //public int RAEncoderUNParkPos { get; set; }
      ////LASTPOS_DEC=8375645 gDECEncoderlastpos
      //public int DECEncoderLastPos { get; set; }
      ////LASTPOS_RA=9150089 gRAEncoderlastpos
      //public int RAEncoderLastPos { get; set; }
      ////TimeDelta= 0 gEQTimeDelta
      //public double TimeDelta { get; set; }


      //DEFAULT_UNPARK_MODE = 0
      public ParkPosition DefaultUnpark { get; set; }
      public ObservableCollection<ParkPosition> UNParkPositions { get; private set; }
      //DEFAULT_PARK_MODE=2
      public ParkPosition DefaultPark { get; set; }
      public ObservableCollection<ParkPosition> ParkPositions { get; private set; }


      //PULSEGUIDE_TIMER_INTERVAL=20
      public int PulseGuidingTimeInterval { get; set; }
      //AUTOSYNCRA=1 RAAutoSync

      public bool RAAutoSync { get; set; }

      //BAR01_6=1 DecOverrideRate
      public int DecOverrideRate { get; set; }
      //BAR01_5=1  RAOverrideRate
      public int RAOverrideRate { get; set; }
      //BAR01_4=1  DecRate
      public int DecRate { get; set; }
      //BAR01_3=1  RARate
      public int RARate { get; set; }
      //BAR01_2=17 DecSlewRate

      public ObservableCollection<SlewRatePreset> SlewRatePresets { get; private set; }

      private SlewRatePreset _SlewRatePreset;

      /// <summary>
      /// The currently selected slew preset. Not stored in the config settings as
      /// always starts off on the lowest setting.
      /// </summary>
      [JsonIgnore]
      public SlewRatePreset SlewRatePreset
      {
         get
         {
            return _SlewRatePreset;
         }
         set
         {
            if (Set<SlewRatePreset>(ref _SlewRatePreset, value))
            {
               string announcement = string.Empty;
               if (_SlewRatePreset.RARate == _SlewRatePreset.DecRate)
               {
                  announcement = $"Slew rate now {_SlewRatePreset.RARate} times sidereal.";
               }
               else
               {
                  announcement = $"Right ascension slew rate now {_SlewRatePreset.RARate} times sidereal. Declination slew rate now {_SlewRatePreset.DecRate} times sidereal.";
               }
               Messenger.Default.Send<AnnounceNotificationMessage>(new AnnounceNotificationMessage(announcement));
            }
         }
      }
      #endregion

      #region Alignment settings ...

      public bool ThreePointAlignment { get; set; }
      #endregion

      #endregion

      public TelescopeControlSettings()
      {
         this.DriverId = string.Empty;
         this.DriverName = string.Empty;
         this.DisplayMode = DisplayMode.MountPosition;
         this.ParkPositions = new ObservableCollection<ParkPosition>();
         this.UNParkPositions = new ObservableCollection<ParkPosition>();
         this.Sites = new SiteCollection();
         this.SlewRatePresets = new ObservableCollection<SlewRatePreset>();

         this.VoiceName = string.Empty;
         this.VoiceRate = 0;     // Range -10 to 10

         this.GameControllers = new GameControllerCollection();

      }

      [OnDeserialized]
      private void Deserialized(StreamingContext context)
      {
         this.CurrentSite = this.Sites.Where(s => s.IsCurrentSite).FirstOrDefault();
         if (this.SlewRatePresets.Count == 0)
         {
            this.SlewRatePresets.Add(new SlewRatePreset(1, 1, 1));
            this.SlewRatePresets.Add(new SlewRatePreset(2, 8, 8));
            this.SlewRatePresets.Add(new SlewRatePreset(3, 64, 64));
            this.SlewRatePresets.Add(new SlewRatePreset(4, 400, 400));
            this.SlewRatePresets.Add(new SlewRatePreset(5, 800, 800));
         }
         // Always start with the lowest rate selected.
         this.SlewRatePreset = this.SlewRatePresets.OrderBy(p => p.Rate).FirstOrDefault();
      }
   }
}
