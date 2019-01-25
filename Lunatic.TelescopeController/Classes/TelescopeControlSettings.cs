using ASCOM.LunaticAstroEQ.Controls;
using ASCOM.LunaticAstroEQ.Core;
using ASCOM.LunaticAstroEQ.Core.Model;
using GalaSoft.MvvmLight;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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

   public class TelescopeControlSettings : DataObjectBase
   {
      #region Properties ...

      public string DriverId { get; set; }
      public string DriverName { get; set; }

      public DisplayMode DisplayMode { get; set; }

      public VoiceGender VoiceGender { get; set; }

      public VoiceAge VoiceAge { get; set; }

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

      public bool OnTop { get; set; }
      // ON_TOP1=0
      // LIMIT_FILE=
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

      //DSYNC01=0
      public int DecSync { get; set; }
      //RSYNC01=0
      public int RASync { get; set; }

      //DALIGN01=0 gDEC1Star
      public double Dec1Star { get; set; }

      //RALIGN01=0 gRA1Star
      public double RA1Star { get; set; }

      //BAR03_2=809  // From Mouse SlewPad
      //BAR03_1=809  // From Mouse SlewPad
      //SlewPadHeight=7875
      //SlewPadWidth=7470

      //UNPARK_DEC=9003010 gDECEncoderUNPark
      public int DecEncoderUNParkPos { get; set; }
      //UNPARK_RA=8388570 gRAEncoderUNPark
      public int RAEncoderUNParkPos { get; set; }
      //LASTPOS_DEC=8375645 gDECEncoderlastpos
      public int DECEncoderLastPos { get; set; }
      //LASTPOS_RA=9150089 gRAEncoderlastpos
      public int RAEncoderLastPos { get; set; }
      //TimeDelta= 0 gEQTimeDelta
      public double TimeDelta { get; set; }


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
            Set<SlewRatePreset>(ref _SlewRatePreset, value);
         }
      }


      public bool ThreePointAlignment { get; set; }

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

         this.VoiceGender = VoiceGender.Female;
         this.VoiceAge = VoiceAge.Teen;
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
