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

using ASCOM.DeviceInterface;
using ASCOM.LunaticAstroEQ.Core;
using ASCOM.LunaticAstroEQ.Core.Geometry;
using ASCOM.LunaticAstroEQ.Core.Services;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Threading;
using Lunatic.TelescopeController.Controls;
using CoreConstants = ASCOM.LunaticAstroEQ.Core.Constants;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Globalization;
using GalaSoft.MvvmLight.Messaging;

namespace Lunatic.TelescopeController.ViewModel
{
   public partial class MainViewModel : LunaticViewModelBase, IDisposable
   {

      #region Properties ....
      private bool LunaticDriver = false;

      SpeechSynthesizer _Synth;

      #region Telescope driver selection etc ...
      private ASCOM.DriverAccess.Telescope _Driver;

      private ASCOM.DriverAccess.Telescope Driver
      {
         get
         {
            return _Driver;
         }
         set
         {
            if (ReferenceEquals(_Driver, value))
            {
               return;
            }
            if (_Driver != null)
            {
               _Driver.Dispose();
            }
            _Driver = value;
            if (_Driver != null)
            {
               SetSupportedActions(_Driver.SupportedActions);
            }

         }
      }

      private List<string> DriverSupportedActions = null;

      private void SetSupportedActions(ArrayList driverActions)
      {
         if (DriverSupportedActions == null)
         {
            DriverSupportedActions = new List<string>();
         }
         DriverSupportedActions.Clear();
         DriverSupportedActions.AddRange(driverActions.Cast<string>().ToList());
      }

      private bool DriverActionAvailable(string action)
      {
         if (DriverSupportedActions != null)
         {
            return DriverSupportedActions.Exists(a => String.Equals(a, action, StringComparison.OrdinalIgnoreCase));
         }
         else
         {
            return false;
         }
      }

      public bool IsConnected
      {
         get
         {
            return ((_Driver != null) && (_Driver.Connected == true));
         }
      }

      private bool _IsParked;
      public bool IsParked
      {
         get
         {
            if (IsInDesignMode)
            {
               // Code runs in Blend --> create design time data.
               return true;
            }
            else
            {
               return _IsParked;
            }
         }
         set
         {
            if (Set(ref _IsParked, value))
            {
               ParkCommand.RaiseCanExecuteChanged();
               RefreshParkStatus();
            }
         }
      }

      private ParkStatus _ParkStatus;

      public ParkStatus ParkStatus
      {
         get
         {
            return _ParkStatus;
         }
         set
         {
            Set<ParkStatus>("ParkStatus", ref _ParkStatus, value);
         }
      }

      private string _ParkCaption;

      public string ParkCaption
      {
         get
         {
            return _ParkCaption;
         }
         set
         {
            Set<string>("ParkCaption", ref _ParkCaption, value);
         }
      }

      private string _ParkStatusPosition;

      public string ParkStatusPosition
      {
         get
         {
            return _ParkStatusPosition;
         }
         set
         {
            Set<string>("ParkStatusPosition", ref _ParkStatusPosition, value);
         }
      }

      private TrackingMode _CurrentTrackingMode;
      public TrackingMode CurrentTrackingMode
      {
         get
         {
            return _CurrentTrackingMode;
         }
         set
         {
            Set<TrackingMode>("CurrentTrackingMode", ref _CurrentTrackingMode, value);
         }
      }

      public bool CanSetRightAscensionRate
      {
         get
         {
            return (IsConnected ? (Driver.CanSetRightAscensionRate && !Driver.Tracking) : false);
         }
      }

      private double _TrackingRateRA;
      public double TrackingRateRA
      {
         get
         {
            return _TrackingRateRA;
         }
         set
         {
            if (Set(ref _TrackingRateRA, value))
            {
               Driver.RightAscensionRate = (_TrackingRateRA - CoreConstants.SIDEREAL_RATE_ARCSECS) * CoreConstants.SIDEREAL_RATE;
            }
         }
      }

      public bool CanSetDeclinationRate
      {
         get
         {
            return (IsConnected ? (Driver.CanSetDeclinationRate && !Driver.Tracking) : false);

         }
      }

      private double _TrackingRateDec;
      public double TrackingRateDec
      {
         get
         {
            return _TrackingRateDec;
         }
         set
         {
            if (Set(ref _TrackingRateDec, value))
            {
               Driver.DeclinationRate = _TrackingRateDec;
            }
         }
      }


      public bool IsSlewing
      {
         get
         {
            return ((_Driver != null) && _Driver.Slewing);
         }
      }

      public bool DriverSelected
      {
         get
         {
            return (_Driver != null);
         }
      }

      private string _DriverId;
      public string DriverId
      {
         get
         {
            return _DriverId;
         }
         set
         {
            Set<string>(ref _DriverId, value);
            OnDriverChanged();
         }
      }

      private string _DriverName;
      public string DriverName
      {
         get
         {
            return _DriverName;
         }
         set
         {
            Set(ref _DriverName, value);
         }
      }


      public string SetupMenuHeader
      {
         get
         {
            return (String.IsNullOrWhiteSpace(DriverName) ? "Setup" : "Setup " + DriverName + "...");
         }
      }

      public string DisconnectMenuHeader
      {
         get
         {
            return (String.IsNullOrWhiteSpace(DriverName) ? "Disconnect" : "Disconnect from " + DriverName);
         }
      }
      public string ConnectMenuHeader
      {
         get
         {
            return (String.IsNullOrWhiteSpace(DriverName) ? "Connect ..." : "Connect to " + DriverName);
         }
      }

      private AxisPosition _AxisPosition;

      public AxisPosition AxisPosition
      {
         get
         {
            return _AxisPosition;
         }
         set
         {
            Set<AxisPosition>("AxisPosition", ref _AxisPosition, value);
         }
      }

      private PierSide _PierSide = PierSide.Unknown;

      public PierSide PierSide
      {
         get
         {
            return _PierSide;
         }
         set
         {
            Set<PierSide>("PierSide", ref _PierSide, value);
         }
      }

      private void OnDriverChanged(bool saveSettings = true)
      {
         if (saveSettings)
         {
            _Settings.DriverId = DriverId;
            _Settings.DriverName = DriverName;
            _SettingsProvider.SaveSettings();
         }
         RaisePropertyChanged("DriverName");
         RaisePropertyChanged("DriverSelected");
         RaisePropertyChanged("SetupMenuHeader");
         RaisePropertyChanged("DisconnectMenuHeader");
         RaisePropertyChanged("ConnectMenuHeader");
         ConnectCommand.RaiseCanExecuteChanged();
         StatusMessage = (DriverSelected ? DriverName + " selected." : (String.IsNullOrEmpty(StatusMessage) ? "Telescope driver not selected" : StatusMessage));  // Keeps error displayed if failed to connect.
      }

      #endregion

      private string _StatusMessage = "Not connected.";
      public string StatusMessage
      {
         get
         {
            return _StatusMessage;
         }
         private set
         {
            if (Set<string>(ref _StatusMessage, value))
            {
               _StatusMoreInfo = String.Empty;
            }
         }
      }

      private string _StatusMoreInfo = "";
      public string StatusMoreInfo
      {
         get
         {
            return _StatusMoreInfo;
         }
         private set
         {
            Set<string>(ref _StatusMoreInfo, value);
         }
      }

      EquatorialCoordinate _GotoTargetCoordinate = new EquatorialCoordinate();
      public EquatorialCoordinate GotoTargetCoordinate
      {
         get
         {
            return _GotoTargetCoordinate;
         }
         set
         {
            Set<EquatorialCoordinate>("GotoTargetCoordinate", ref _GotoTargetCoordinate, value);
         }
      }

      #endregion

      #region Visibility display properties ...

      /// <summary>
      /// Used to control the main form component visiblity
      /// </summary>

      private DisplayMode _DisplayMode = DisplayMode.MountPosition;
      public DisplayMode DisplayMode
      {
         get
         {
            return _DisplayMode;
         }
         set
         {
            if (Set<DisplayMode>(ref _DisplayMode, value))
            {
               _Settings.DisplayMode = DisplayMode;
               _SettingsProvider.SaveSettings();
               RaiseVisiblitiesChanged();
            }
         }
      }

      public Visibility ReducedSlewVisibility
      {
         get
         {
            return (((long)DisplayMode & (long)Modules.ReducedSlew) == (long)Modules.ReducedSlew ? Visibility.Visible : Visibility.Collapsed);
         }
      }

      public Visibility SlewVisibility
      {
         get
         {
            return (((long)DisplayMode & (long)Modules.Slew) == (long)Modules.Slew ? Visibility.Visible : Visibility.Collapsed);
         }
      }

      public Visibility MountPositionVisibility
      {
         get
         {
            return (((long)DisplayMode & (long)Modules.MountPosition) == (long)Modules.MountPosition ? Visibility.Visible : Visibility.Collapsed);
         }
      }

      public Visibility TrackingVisibility
      {
         get
         {
            return (((long)DisplayMode & (long)Modules.Tracking) == (long)Modules.Tracking ? Visibility.Visible : Visibility.Collapsed);
         }
      }

      public Visibility ParkStatusVisibility
      {
         get
         {
            return (((long)DisplayMode & (long)Modules.ParkStatus) == (long)Modules.ParkStatus ? Visibility.Visible : Visibility.Collapsed);
         }
      }

      public Visibility ExpanderVisibility
      {
         get
         {
            return (((long)DisplayMode & (long)Modules.Expander) == (long)Modules.Expander ? Visibility.Visible : Visibility.Collapsed);
         }
      }

      public Visibility AxisPositionVisibility
      {
         get
         {
            return (((long)DisplayMode & (long)Modules.AxisPosition) == (long)Modules.AxisPosition ? Visibility.Visible : Visibility.Collapsed);
         }
      }

      public Visibility MessageCentreVisibility
      {
         get
         {
            return (((long)DisplayMode & (long)Modules.MessageCentre) == (long)Modules.MessageCentre ? Visibility.Visible : Visibility.Collapsed);
         }
      }

      public Visibility PECVisibility
      {
         get
         {
            return (((long)DisplayMode & (long)Modules.PEC) == (long)Modules.PEC ? Visibility.Visible : Visibility.Collapsed);
         }
      }

      public Visibility PulseGuidingVisibility
      {
         get
         {
            return (((long)DisplayMode & (long)Modules.PulseGuide) == (long)Modules.PulseGuide ? Visibility.Visible : Visibility.Collapsed);
         }
      }

      private void RaiseVisiblitiesChanged()
      {
         RaisePropertyChanged("ReducedSlewVisibility");
         RaisePropertyChanged("SlewVisibility");
         RaisePropertyChanged("MountPositionVisibility");
         RaisePropertyChanged("TrackingVisibility");
         RaisePropertyChanged("ParkStatusVisibility");
         RaisePropertyChanged("ExpanderVisibility");
         RaisePropertyChanged("AxisPositionVisibility");
         RaisePropertyChanged("MessageCentreVisibility");
         RaisePropertyChanged("PECVisibility");
         RaisePropertyChanged("PulseGuidingVisibility");
      }

      #endregion

      #region Telescope driver properties
      private double _LocalSiderealTime;

      public double LocalSiderealTime
      {
         get
         {
            return _LocalSiderealTime;
         }
         set
         {
            Set<double>(ref _LocalSiderealTime, value);
         }
      }

      private double _RightAscension;

      public double RightAscension
      {
         get
         {
            return _RightAscension;
         }
         set
         {
            Set<double>("RightAscension", ref _RightAscension, value);
         }
      }

      private double _Declination;

      public double Declination
      {
         get
         {
            return _Declination;
         }
         set
         {
            Set<double>("Declination", ref _Declination, value);
         }
      }

      private double _Altitude;

      public double Altitude
      {
         get
         {
            return _Altitude;
         }
         set
         {
            Set<double>("Altitude", ref _Altitude, value);
         }
      }

      private double _Azimuth;

      public double Azimuth
      {
         get
         {
            return _Azimuth;
         }
         set
         {
            Set<double>("Azimuth", ref _Azimuth, value);
         }
      }

      #region GuideRateDeclination ...
      // TODO: Migrate GuideRateDeclination and just pass the value to the driver
      private double _GuideRateDeclination;
      public double GuideRateDeclination
      {
         get
         {
            return _GuideRateDeclination;
         }
         set
         {
            _GuideRateDeclination = value;
         }
      }
      /*
      private double _GuideRateDeclination;
      public double GuideRateDeclination
      {
         get
         {
            if (Settings.AscomCompliance.AllowPulseGuide) {
               // movement rate offset in degress/sec
               // TODO: _GuideRateDeclination = (HC.HScrollDecRate.Value * 0.1 * SID_RATE) / 3600
            _Logger.LogMessage("GuideRateDeclination", "Get - " + _GuideRateDeclination.ToString());
                  }
            else {
               // RaiseError SCODE_NOT_IMPLEMENTED, ERR_SOURCE, "Property Get GuideRateDeclination" & MSG_NOT_IMPLEMENTED
               Select Case HC.DECGuideRateList.ListIndex
                   Case 1
                        GuideRateDeclination = (0.5 * SID_RATE) / 3600
                    Case 2
                        GuideRateDeclination = (0.75 * SID_RATE) / 3600
                    Case 3
                        GuideRateDeclination = (SID_RATE) / 3600
                    Case 4
                        RaiseError SCODE_NOT_IMPLEMENTED, ERR_SOURCE, "Property Get GuideRateDeclination" & MSG_NOT_IMPLEMENTED
                    Case Else
                        GuideRateDeclination = (0.25 * SID_RATE) / 3600
                End Select
                If AscomTrace.AscomTraceEnabled Then AscomTrace.Add_log 4, "GET GuideRateDEC :" & CStr(GuideRateDeclination)
            }

            throw new ASCOM.PropertyNotImplementedException("GuideRateDeclination", false);
         }
         set
         {
            _Logger.LogMessage("GuideRateDeclination Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("GuideRateDeclination", true);
         }
      }
       */
      #endregion


      #region GuideRateRightAscension ...
      // TODO: Migrate GuideRateRightAscension and just pass the value to the driver
      private double _GuideRateRightAscension;
      public double GuideRateRightAscension
      {
         get
         {
            return _GuideRateRightAscension;
         }
         set
         {
            _GuideRateRightAscension = value;
         }
      }
      /*
Public Property Get GuideRateRightAscension() As Double
 If gAscomCompatibility.AllowPulseGuide Then
     ' movement rate offset in degrees/sec
     GuideRateRightAscension = (HC.HScrollRARate.Value * 0.1 * SID_RATE) / 3600
     If AscomTrace.AscomTraceEnabled Then AscomTrace.Add_log 4, "GET GuideRateRA :" & CStr(GuideRateRightAscension)
 Else
     ' RaiseError SCODE_NOT_IMPLEMENTED, ERR_SOURCE, "Property Get GuideRateRightAscension" & MSG_NOT_IMPLEMENTED
     Select Case HC.RAGuideRateList.ListIndex
         Case 1
             GuideRateRightAscension = (0.5 * SID_RATE) / 3600
         Case 2
             GuideRateRightAscension = (0.75 * SID_RATE) / 3600
         Case 3
             GuideRateRightAscension = (SID_RATE) / 3600
         Case 4
             RaiseError SCODE_NOT_IMPLEMENTED, ERR_SOURCE, "Property Get GuideRateRightAscension" & MSG_NOT_IMPLEMENTED
         Case Else
             GuideRateRightAscension = (0.25 * SID_RATE) / 3600
     End Select
     If AscomTrace.AscomTraceEnabled Then AscomTrace.Add_log 4, "GET GuideRateRA :" & CStr(GuideRateRightAscension)
 End If

End Property

Public Property Let GuideRateRightAscension(ByVal newval As Double)
 ' We can't support properly beacuse the ASCOM spec does not distinquish between ST4 and Pulseguiding
 ' and states that this property relates to both - crazy!
 If gAscomCompatibility.AllowPulseGuide Then
     If AscomTrace.AscomTraceEnabled Then AscomTrace.Add_log 4, "LET GuideRateRA(" & newval & ")"
     newval = newval * 3600 / (0.1 * SID_RATE)
     If newval < HC.HScrollRARate.min Then
         newval = HC.HScrollRARate.min
     Else
         If newval > HC.HScrollRARate.max Then
             newval = HC.HScrollRARate.max
         End If
     End If
     HC.HScrollRARate.Value = CInt(newval)
 Else
     If HC.RAGuideRateList.ListIndex = 4 Then
         If AscomTrace.AscomTraceEnabled Then AscomTrace.Add_log 4, "LET GuideRateRA(" & newval & ") :NOT_SUPPORTED"
         RaiseError SCODE_NOT_IMPLEMENTED, ERR_SOURCE, "Property Let GuideRateRightAscension" & MSG_NOT_IMPLEMENTED
     Else
         newval = newval * 3600 / SID_RATE
         If newval > 0.75 Then
             HC.RAGuideRateList.ListIndex = 3
         Else
             If newval > 0.5 Then
                 HC.RAGuideRateList.ListIndex = 2
             Else
                 If newval > 0.25 Then
                     HC.RAGuideRateList.ListIndex = 1
                 Else
                     HC.RAGuideRateList.ListIndex = 0
                 End If
             End If
         End If
         If AscomTrace.AscomTraceEnabled Then AscomTrace.Add_log 4, "LET GuideRateRA(" & newval & ")"
     End If
 End If
End Property
       */
      #endregion


      #endregion

      /// <summary>
      /// Initializes a new instance of the MainViewModel class.
      /// </summary>
      public MainViewModel(ISettingsProvider<TelescopeControlSettings> settingsProvider)
      {

         if (IsInDesignMode)
         {
            // Code runs in Blend --> create design time data.
         }
         else
         {
            // Code runs "for real"


            _SettingsProvider = settingsProvider;
            _Settings = settingsProvider.Settings;
            PopSettings();

            #region Setup the voice synth
            _Synth = new SpeechSynthesizer();
            _Synth.SetOutputToDefaultAudioDevice();
            GetInstalledVoices();
            if (VoicesAvailable)
            {
               if (_Settings.VoiceName == string.Empty)
               {
                  _Settings.VoiceName = _AvailableVoices.First();
                  SaveSettings();
               }
               _Synth.Rate = Settings.VoiceRate;
               _Synth.SelectVoice(Settings.VoiceName);
            }
            else
            {
               VoiceName = string.Empty;
               AnnouncementsOn = false;
               _Synth.Dispose();
               _Synth = null;
            }
            #endregion

            #region Check for game controllers
            GameControllerService.UpdateAvailableGameControllers(_Settings);
            GameControllersAvailable = _Settings.GameControllers.Any(c => c.IsConnected);
            _Controller = _Settings.GameControllers.ActiveGameController;
            if (_Controller != null)
            {
               ControllerConnected = GameControllerService.IsInstanceConnected(_Controller.InstanceGuid);
            }
            #endregion

            // Get current temperature via call to OpenWeatherAPI
            RefreshTemperature();

            _DisplayTimer = new DispatcherTimer();
            _DisplayTimer.Interval = TimeSpan.FromMilliseconds(500);
            _DisplayTimer.Tick += new EventHandler(this.DisplayTimer_Tick);

            MessengerInstance.Register<AnnounceNotificationMessage>(this, message => { Announce(message.Notification); });
            MessengerInstance.Register<ErrorNotificationMessage>(this, message => { MessageBox.Show(message.Notification, "Error", MessageBoxButton.OK, MessageBoxImage.Error); });
            MessengerInstance.Register<NotificationMessage>(this, message => 
            {
               if (message.Notification == DeviceNotificationService.USBDeviceAddedNotification ||
                     message.Notification == DeviceNotificationService.USBDeviceRemovedNotification)
               {
                  GameControllerService.UpdateAvailableGameControllers(_Settings, false);
                  ConfigureGameControllerCommand.RaiseCanExecuteChanged(); ;
               }
            });
         }

      }

      public override void Cleanup()
      {
         // Release the reference to the driver.
         Driver = null;
         base.Cleanup();
      }

      private void InitialiseGUI()
      {
         if (Driver != null && !_ProcessingDisplayTimerTick)
         {
            _ProcessingDisplayTimerTick = true;
            LocalSiderealTime = Driver.SiderealTime;
            RightAscension = Driver.RightAscension;
            Declination = Driver.Declination;
            Altitude = Driver.Altitude;
            Azimuth = Driver.Azimuth;
            PierSide = (PierSide)Driver.SideOfPier;

            #region Current tracking related stuff ...
            // The next two would trigger an update to a driver if we updated the property viat the setter
            _TrackingRateRA = CoreConstants.SIDEREAL_RATE_ARCSECS + (Driver.RightAscensionRate / CoreConstants.SIDEREAL_RATE);
            _TrackingRateDec = Driver.DeclinationRate;
            RaisePropertyChanged("TrackingRateRA");
            RaisePropertyChanged("TrackingRateDec");
            RaisePropertyChanged("CanSetRightAscensionRate");
            RaisePropertyChanged("CanSetDeclinationRate");
            CurrentTrackingMode = GetCurrentTrackingMode();
            #endregion

            if (LunaticDriver)
            {
               try
               {
                  AxisPosition = new AxisPosition(Driver.CommandString("Lunatic:GetAxisPositions", true));
               }
               catch
               {
                  // TODO: Log message
               }

               // Sort out max SlewRates
               try
               {
                  SetMaxSlewRates();
               }
               catch
               {
                  // TODO: Log message
               }
            }
            IsParked = Driver.AtPark;

            RaisePropertyChanged("IsSlewing");
            RaiseCanExecuteChanged();
            _ProcessingDisplayTimerTick = false;
         }
      }

      #region Weather API calls ...
      private bool IsWeatherAPIAvailable = true;
      private void RefreshTemperature()
      {
         if (_Settings.CurrentSite != null && IsWeatherAPIAvailable)
         {
            ThreadPool.QueueUserWorkItem(async callback =>
            {
               double temp = await WeatherService.GetCurrentTemperature(
                  _Settings.CurrentSite.Latitude,
                  _Settings.CurrentSite.Longitude);
               DispatcherHelper.CheckBeginInvokeOnUI(() =>
               {
               if (!double.IsNaN(temp))
               {
                  _Settings.CurrentSite.Temperature = temp;
               }
               else
               {
                      // Temperatures are not available so don't bother trying to update in future
                      IsWeatherAPIAvailable = false;
               }
            });
            });
         }
      }
      #endregion
      #region Timers ...
      // This code creates a new DispatcherTimer with an interval of 15 seconds.
      private DispatcherTimer _DisplayTimer;
      private bool _ProcessingDisplayTimerTick = false;

      private void DisplayTimer_Tick(object state, EventArgs e)
      {
         if (Driver != null && !_ProcessingDisplayTimerTick)
         {
            _ProcessingDisplayTimerTick = true;
            LocalSiderealTime = Driver.SiderealTime;
            RightAscension = Driver.RightAscension;
            Declination = Driver.Declination;
            Altitude = Driver.Altitude;
            Azimuth = Driver.Azimuth;
            PierSide = (PierSide)Driver.SideOfPier;

            CurrentTrackingMode = GetCurrentTrackingMode();

            if (LunaticDriver)
            {
               try
               {
                  AxisPosition = new AxisPosition(Driver.CommandString("Lunatic:GetAxisPositions", true));
               }
               catch
               {
                  // TODO: Log message
               }
            }
            IsParked = Driver.AtPark;

            RaisePropertyChanged("IsSlewing");
            RaiseCanExecuteChanged();
            _ProcessingDisplayTimerTick = false;
         }
      }

      private void RefreshParkStatus()
      {
         try
         {
            string result = Driver.CommandString("Lunatic:GetParkStatus", false);
            int parkStatus;
            if (int.TryParse(result, out parkStatus))
            {
               ParkStatus = (ParkStatus)parkStatus;
               if (ParkStatus == ParkStatus.Parked)
               {
                  ParkCaption = "Unpark";
                  ParkStatusPosition = "HOME";
               }
               else
               {
                  ParkCaption = "Park: HOME";
                  ParkStatusPosition = "";
               }
            }
            else
            {
               StatusMessage = "Invalid park status returned.";
            }
         }
         catch (Exception ex)
         {
            // TODO: Sort out better error message display
            StatusMessage = ex.Message;
         }
      }

      #endregion

      #region Settings ...
      private void PopSettings()
      {
         _DisplayMode = _Settings.DisplayMode;

         _Settings.Sites.PropertyChanged += Sites_PropertyChanged;
         _Settings.GameControllers.PropertyChanged += GameControllers_PropertyChanged;
         // Better try to instantiate the driver as well if we have a driver ID
         if (!string.IsNullOrWhiteSpace(Settings.DriverId))
         {
            try
            {
               DriverName = Settings.DriverName;
               DriverId = Settings.DriverId;
            }
            catch (Exception)
            {
               DriverName = string.Empty;
               DriverId = string.Empty;
               _StatusMessage = "Failed select previous telescope driver";
            }
         }
         //#endif
         // TODO: Replace the following to determine it from the driver
         // CurrentTrackingMode = TrackingMode.Stop;
      }

      private void Sites_PropertyChanged(object sender, PropertyChangedEventArgs e)
      {
         switch (e.PropertyName)
         {
            case "CurrentSite":
               RaisePropertyChanged("CurrentSite");
               SaveSettings();
               UpdateDriverSiteDetails();
               break;
            case "CurrentSite.Latitude":
            case "CurrentSite.Longitude":
            case "CurrentSite.Elevation":
               SaveSettings();
               UpdateDriverSiteDetails();
               break;
            case "CurrentSite.Temperature":
               SaveSettings();
               UpdateDriverSiteDetails(true);
               break;
            default:
               SaveSettings();
               break;
         }
      }

      private void GameControllers_PropertyChanged(object sender, PropertyChangedEventArgs e)
      {
         switch (e.PropertyName)
         {
            case "ActiveGameController":
               RaisePropertyChanged("ActiveGameController");
               SaveSettings();
               // UpdateDriverGameControllerDetails();

               break;
            default:
               SaveSettings();
               break;
         }
      }

      private void PushSettings()
      {
         _Settings.DriverId = this.DriverId;
         _Settings.DisplayMode = this.DisplayMode;
      }

      public void SaveSettings()
      {
         PushSettings();
         _SettingsProvider.SaveSettings();
      }

      #endregion

      #region Relay commands ...
      private RelayCommand<DisplayMode> _DisplayModeCommand;

      /// <summary>
      /// Command to cycle through the display modes.
      /// </summary>
      public RelayCommand<DisplayMode> DisplayModeCommand
      {
         get
         {
            return _DisplayModeCommand
               ?? (_DisplayModeCommand = new RelayCommand<DisplayMode>((mode) =>
               {
                  DisplayMode = mode;
               }));
         }
      }

      #region Site Relay commands ...
      private RelayCommand<SiteCollection> _AddSiteCommand;

      /// <summary>
      /// Adds a new chart to the active model
      /// </summary>
      public RelayCommand<SiteCollection> AddSiteCommand
      {
         get
         {
            return _AddSiteCommand
                ?? (_AddSiteCommand = new RelayCommand<SiteCollection>(
                                      (collection) =>
                                      {
                                         collection.Add(new Site(Guid.NewGuid()) { SiteName = "<Site name>" });
                                      }

                                      ));
         }
      }


      private RelayCommand<Site> _RemoveSiteCommand;

      /// <summary>
      /// Adds a new chart to the active model
      /// </summary>
      public RelayCommand<Site> RemoveSiteCommand
      {
         get
         {
            return _RemoveSiteCommand
                ?? (_RemoveSiteCommand = new RelayCommand<Site>(
                                      (site) =>
                                      {
                                         Sites.Remove(site);
                                      }

                                      ));
         }
      }

      private RelayCommand<Site> _GetSiteCoordinateCommand;

      /// <summary>
      /// Adds a new chart to the active model
      /// </summary>
      public RelayCommand<Site> GetSiteCoordinateCommand
      {
         get
         {
            return _GetSiteCoordinateCommand
                ?? (_GetSiteCoordinateCommand = new RelayCommand<Site>(
                                      (site) =>
                                      {
                                         MapViewModel vm = new MapViewModel(site);
                                         MapWindow map = new MapWindow(vm);
                                         var result = map.ShowDialog();
                                      }

                                      ));
         }
      }

      private void UpdateDriverSiteDetails(bool skipTempCheck = false)
      {
         if (!skipTempCheck)
         {
            RefreshTemperature();
         }
         if (Driver != null)
         {
            // Transfer location any other initialisation needed.
            if (Settings.CurrentSite != null)
            {
               Driver.SiteElevation = Settings.CurrentSite.Elevation;
               Driver.SiteLatitude = Settings.CurrentSite.Latitude;
               Driver.SiteLongitude = Settings.CurrentSite.Longitude;
               if (DriverActionAvailable("Lunatic:SetSiteTemperature"))
               {
                  Driver.Action("Lunatic:SetSiteTemperature", Settings.CurrentSite.Temperature.ToString());
               }
            }
         }
      }
      #endregion


      #region Choose, Connect, Disconnect etc ...
      private RelayCommand _ChooseCommand;

      public RelayCommand ChooseCommand
      {
         get
         {
            return _ChooseCommand
               ?? (_ChooseCommand = new RelayCommand(() =>
               {
#if INSTANTIATE_DIRECT
                  Driver = new Telescope();

#else
                      string driverId = ASCOM.DriverAccess.Telescope.Choose(DriverId);
                  if (!string.IsNullOrEmpty(driverId))
                  {
                     Driver = new ASCOM.DriverAccess.Telescope(driverId);
                     DriverName = Driver.Description;
                     DriverId = driverId; // Triggers a refresh of menu options etc so must happen AFTER updating the driver name.
                         Connect();
                  }
                  else
                  {
                     Driver = null;
                     DriverName = string.Empty;
                     DriverId = string.Empty;
                  }
#endif
                      RaiseCanExecuteChanged();
               }, () => { return !IsConnected; }));
         }
      }

      private RelayCommand _ConnectCommand;

      public RelayCommand ConnectCommand
      {
         get
         {
            return _ConnectCommand
               ?? (_ConnectCommand = new RelayCommand(() =>
               {
                  if (IsConnected)
                  {
                     Disconnect();
                  }
                  else
                  {
                     Connect();
                  }
                  RaisePropertyChanged("IsConnected");
                  RaisePropertyChanged("IsParked");
                  RaiseCanExecuteChanged();
               }, () => { return !String.IsNullOrEmpty(DriverId); }));
         }
      }

      // Perform the logic when connecting.
      private void Connect()
      {
         try
         {
            // Check to see if the driver is already connected
            if (Driver == null)
            {
               Driver = new ASCOM.DriverAccess.Telescope(DriverId);
            }
            Driver.Connected = true;
            try
            {
               LunaticDriver = Driver.CommandBool("Lunatic:IsLunaticDriver", true);
            }
            catch
            {
               LunaticDriver = false;
            }
            InitialiseGUI();

            _ProcessingDisplayTimerTick = false;
            _DisplayTimer.Start();
            StatusMessage = "Connected to " + DriverName + ".";
            AlignSiteDetails();
         }
         catch (Exception ex)
         {
            StatusMessage = "Connection to telescope mount failed (for more details see tooltip).";
            StatusMoreInfo = ex.Message;
         }
      }

      private void Disconnect()
      {
         _DisplayTimer.Stop();
         _ProcessingDisplayTimerTick = false;
         if (Driver != null)
         {
            Driver.Connected = false;
            Driver = null;
         }
         LunaticDriver = false;
         StatusMessage = "Not connected.";
      }

      private void AlignSiteDetails()
      {
         if (Driver.SiteLatitude == 0.0 && Driver.SiteLongitude == 0.0 && Driver.SiteElevation == 0.0)
         {
            UpdateDriverSiteDetails(true);
         }
         else
         {
            Site controllerSite = Sites.Where(s => s.Latitude == Driver.SiteLatitude
                                                && s.Longitude == Driver.SiteLongitude
                                                && s.Elevation == Driver.SiteElevation)
               .FirstOrDefault();
            if (controllerSite == null)
            {
               controllerSite = new Site()
               {
                  SiteName = $"Telescope_{DateTime.Now.ToString("yyyyMMddHHmmss")}",
                  Latitude = Driver.SiteLatitude,
                  Longitude = Driver.SiteLongitude,
                  Elevation = Driver.SiteElevation
               };
               Sites.Add(controllerSite);
            }
            controllerSite.IsCurrentSite = true;
            SaveSettings();
         }
      }

      private RelayCommand _SetupCommand;

      public RelayCommand SetupCommand
      {
         get
         {
            return _SetupCommand
               ?? (_SetupCommand = new RelayCommand(() =>
               {
                  Driver.SetupDialog();
               }, () => { return Driver != null; }));
         }
      }

      #endregion

      #region Slewing commands ...
      private RelayCommand<SlewButton> _StartSlewCommand;

      public RelayCommand<SlewButton> StartSlewCommand
      {
         get
         {
            return _StartSlewCommand
               ?? (_StartSlewCommand = new RelayCommand<SlewButton>((button) =>
               {
                  double rate;      // 10 x Sidereal;
                      switch (button)
                  {
                     case SlewButton.North:
                     case SlewButton.South:
                        rate = Settings.SlewRatePreset.DecRate * Constants.SIDEREAL_RATE_DEGREES;
                        if (button == SlewButton.South)
                        {
                           Announce("slewing south");
                           rate = -rate;
                        }
                        else
                        {
                           Announce("slewing north");
                        }
                        if (Settings.ReverseDec)
                        {
                           rate = -rate;
                        }
                        Driver.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisSecondary, rate);
                        break;
                     case SlewButton.East:
                     case SlewButton.West:
                        rate = Settings.SlewRatePreset.RARate * Constants.SIDEREAL_RATE_DEGREES;
                        if (button == SlewButton.West)
                        {
                           Announce("slewing west");
                           rate = -rate;
                        }
                        else
                        {
                           Announce("slewing east");
                        }
                        if (Settings.ReverseRA)
                        {
                           rate = -rate;
                        }
                        Driver.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, rate);
                        break;
                  }


               }, (button) => { return (IsConnected && !IsParked); }));   // Check that we are connected and not parked
         }
      }

      private RelayCommand<SlewButton> _StopSlewCommand;

      public RelayCommand<SlewButton> StopSlewCommand
      {
         get
         {
            return _StopSlewCommand
               ?? (_StopSlewCommand = new RelayCommand<SlewButton>((button) =>
               {
                  switch (button)
                  {
                     case SlewButton.Stop:
                        Driver.AbortSlew();
                        break;
                     case SlewButton.North:
                        Driver.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisSecondary, 0.0);
                        break;
                     case SlewButton.South:
                        Driver.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisSecondary, 0.0);
                        break;
                     case SlewButton.East:
                        Driver.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, 0.0);
                        break;
                     case SlewButton.West:
                        Driver.MoveAxis(ASCOM.DeviceInterface.TelescopeAxes.axisPrimary, 0.0);
                        break;
                  }
               }, (button) => { return (IsConnected && !IsParked); }));   // Check that we are connected
         }
      }
      #endregion

      #region Goto relay commands ...
      GotoWindow gotoWindow = null;
      private RelayCommand _ShowGotoWindowCommand;

      public RelayCommand ShowGotoWindowCommand
      {
         get
         {
            return _ShowGotoWindowCommand
               ?? (_ShowGotoWindowCommand = new RelayCommand(() =>
               {
                  if (gotoWindow == null)
                  {
                     gotoWindow = new GotoWindow(this);
                     gotoWindow.Show();
                  }
                  else
                  {
                     gotoWindow.Activate();
                  }

               }));
         }
      }

      public void OnGotoWindowClosed()
      {
         gotoWindow = null;
      }

      private RelayCommand _GotoCommand;

      public RelayCommand GotoCommand
      {
         get
         {
            return _GotoCommand
               ?? (_GotoCommand = new RelayCommand(() =>
               {
                  StatusMessage = GotoTargetCoordinate.ToString();
               }));
            //, () => { return (IsConnected && !IsParked); }
         }
      }


      #endregion

      #region Tracking Command and associated methods ...
      private RelayCommand<TrackingMode> _StartTrackingCommand;

      public RelayCommand<TrackingMode> StartTrackingCommand
      {
         get
         {
            return _StartTrackingCommand
               ?? (_StartTrackingCommand = new RelayCommand<TrackingMode>((trackingMode) =>
               {
                  string announcement = "";
                  switch (trackingMode)
                  {
                     case TrackingMode.Stop:
                        Driver.Tracking = false;
                        announcement = "Tracking is OFF.";
                        break;
                     case TrackingMode.Sidereal:
                        Driver.RightAscensionRate = 0.0;
                        Driver.DeclinationRate = 0.0;
                        Driver.TrackingRate = DriveRates.driveSidereal;
                        _TrackingRateRA = CoreConstants.SIDEREAL_RATE_ARCSECS;
                        Driver.Tracking = true;
                        announcement = "Sidereal tracking is ON.";
                        break;
                     case TrackingMode.Lunar:
                        Driver.RightAscensionRate = 0.0;
                        Driver.DeclinationRate = 0.0;
                        Driver.TrackingRate = DriveRates.driveLunar;
                        _TrackingRateRA = CoreConstants.LUNAR_RATE_ARCSECS;
                        Driver.Tracking = true;
                        announcement = "Lunar tracking is ON.";
                        break;
                     case TrackingMode.Solar:
                        Driver.RightAscensionRate = 0.0;
                        Driver.DeclinationRate = 0.0;
                        Driver.TrackingRate = DriveRates.driveSolar;
                        _TrackingRateRA = CoreConstants.SOLAR_RATE_ARCSECS;
                        Driver.Tracking = true;
                        announcement = "Solar tracking is ON.";
                        break;
                     case TrackingMode.Custom:
                        if (Driver.CanSetDeclinationRate && Driver.CanSetRightAscensionRate)
                        {
                           Driver.TrackingRate = DriveRates.driveSidereal;
                           _TrackingRateRA = CoreConstants.SIDEREAL_RATE_ARCSECS + (Driver.RightAscensionRate / CoreConstants.SIDEREAL_RATE);
                           Driver.Tracking = true;
                           announcement = "Custom tracking is ON.";
                        }
                        else
                        {
                           StatusMessage = "Custom tracking is not supported by the currently selected driver.";
                           announcement = "Custom is not supported.";
                           Driver.Tracking = false;
                        }
                        break;
                  }
                  _TrackingRateDec = Driver.DeclinationRate;
                  RaisePropertyChanged("TrackingRateRA");
                  RaisePropertyChanged("TrackingRateDec");
                  RaisePropertyChanged("CanSetRightAscensionRate");
                  RaisePropertyChanged("CanSetDeclinationRate");
                  CurrentTrackingMode = trackingMode;
                  Announce(announcement);

               }, (trackingMode) => { return (IsConnected && !IsParked && Driver.CanSetTracking); }));   // Check that we are connected and not parked
         }
      }


      private TrackingMode GetCurrentTrackingMode()
      {
         TrackingMode mode = TrackingMode.Stop;
         if (Driver.Tracking)
         {
            switch (Driver.TrackingRate)
            {
               case DriveRates.driveSidereal:
                  if (Driver.RightAscensionRate != 0.0 || Driver.DeclinationRate != 0.0)
                  {
                     mode = TrackingMode.Custom;
                  }
                  else
                  {
                     mode = TrackingMode.Sidereal;
                  }
                  break;
               case DriveRates.driveLunar:
                  mode = TrackingMode.Lunar;
                  break;
               case DriveRates.driveSolar:
                  mode = TrackingMode.Solar;
                  break;
               case DriveRates.driveKing:
                  mode = TrackingMode.King;
                  break;
               default:
                  StatusMessage = "Unrecognised driver rate set on current driver.";
                  break;
            }
         }
         return mode;
      }

      #endregion

      #region Parking and unparking commands ...
      private RelayCommand _ParkCommand;

      public RelayCommand ParkCommand
      {
         get
         {
            return _ParkCommand
               ?? (_ParkCommand = new RelayCommand(() =>
               {
                  if (IsParked)
                  {
                     Announce("Unparking");
                     Driver.Unpark();
                  }
                  else
                  {
                     Announce("Parking");
                     Driver.Park();
                  }
                  RaisePropertyChanged("IsParked");
                  RaiseCanExecuteChanged();
               }, () => { return (IsConnected && !IsSlewing); }));   // Check that we are connected
         }
      }

      #endregion

      #endregion


      private void RaiseCanExecuteChanged()
      {
         ChooseCommand.RaiseCanExecuteChanged();
         ConnectCommand.RaiseCanExecuteChanged();
         StartSlewCommand.RaiseCanExecuteChanged();
         StopSlewCommand.RaiseCanExecuteChanged();
         ParkCommand.RaiseCanExecuteChanged();
         StartTrackingCommand.RaiseCanExecuteChanged();
         GotoCommand.RaiseCanExecuteChanged();
      }


      private void Announce(string message, bool always = false)
      {
         if (_Synth != null && (always || _Settings.AnnouncementsOn))
         {
            _Synth.SpeakAsync(message);
         }
      }

      #region IDisposable ...
      public void Dispose()
      {
         Dispose(true);
         GC.SuppressFinalize(this);
      }

      protected virtual void Dispose(bool disposing)
      {
         if (disposing)
         {
            if (_Driver != null)
            {
               _Driver.Dispose();
            }
            if (_Synth != null)
            {
               _Synth.Dispose();
            }
         }
      }
      #endregion
   }
}
