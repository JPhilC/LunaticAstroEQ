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
using System.ComponentModel;
using System.Speech.Synthesis;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using CoreConstants = ASCOM.LunaticAstroEQ.Core.Constants;
using System;
using SharpDX.DirectInput;
using System.Linq;
using GalaSoft.MvvmLight.Command;
using Lunatic.TelescopeController.Controls;

namespace Lunatic.TelescopeController.ViewModel
{

   [CategoryOrder("Mount Options", 1)]
   [CategoryOrder("Site Information", 2)]
   [CategoryOrder("Gamepad", 3)]
   [CategoryOrder("General", 4)]
   [CategoryOrder("Announcements and voice", 5)]
   public partial class MainViewModel
   {
      #region Settings ...
      ISettingsProvider<TelescopeControlSettings> _SettingsProvider;

      private TelescopeControlSettings _Settings;
      public TelescopeControlSettings Settings
      {
         get
         {
            return _Settings;
         }
      }

      #region Site information ...
      [Category("Site Information")]
      [DisplayName("Current site")]
      [Description("The currently selected telescope site.")]
      [PropertyOrder(0)]
      public Site CurrentSite
      {
         get
         {
            return _Settings.Sites.CurrentSite;
         }
      }

      [Category("Site Information")]
      [DisplayName("Available sites")]
      [Description("Manage the available sites.")]
      [PropertyOrder(1)]
      public SiteCollection Sites
      {
         get
         {
            return _Settings.Sites;
         }
      }
      #endregion

      #region General settings ...
      [Category("General")]
      [DisplayName("Always On Top")]
      [Description("Checking this box will ensure that the Telescope Controller is always the upper most window.")]
      [PropertyOrder(1)]
      public bool AlwaysOnTop
      {
         get
         {
            return _Settings.AlwaysOnTop;
         }
         set
         {
            if (_Settings.AlwaysOnTop == value)
            {
               return;
            }
            _Settings.AlwaysOnTop = value;
            SaveSettings();
            RaisePropertyChanged();
         }
      }
      
      #endregion

      #region Voice and announcement settings
      [Category("Announcements and voice")]
      [DisplayName("Audio announcements")]
      [Description("Checking this box will switch on audio confirmation of commands.")]
      [PropertyOrder(1)]
      public bool AnnouncementsOn
      {
         get
         {
            return _Settings.AnnouncementsOn;
         }
         set
         {
            if (_Settings.AnnouncementsOn == value)
            {
               return;
            }
            _Settings.AnnouncementsOn = value;
            SaveSettings();
            RaisePropertyChanged();
            if (value)
            {
               Announce("Audio announcements are on.", true);
            }
            else
            {
               Announce("Audio announcements are off.", true);
            }
         }
      }

      private List<string> _AvailableVoices = null;
      public List<string> AvailableVoices
      {
         get
         {
            return _AvailableVoices;
         }
      }

      private bool _VoicesAvailable = true;
      public bool VoicesAvailable
      {
         get
         {
            return _VoicesAvailable;
         }
         private set
         {
            Set(ref _VoicesAvailable, value);
         }
      }

      private void GetInstalledVoices()
      {
         _AvailableVoices = new List<string>();
         if (_Synth != null)
         {
            var installedVoices = _Synth.GetInstalledVoices();
            foreach (InstalledVoice voice in installedVoices)
            {
               if (voice.Enabled)
               {
                  _AvailableVoices.Add(voice.VoiceInfo.Name);
               }
            }

         }
         if (_AvailableVoices.Count == 0)
         {
            VoicesAvailable = false;
            _AvailableVoices.Add(string.Empty);
         }
      }

      [Category("Announcements and voice")]
      [DisplayName("Voice name")]
      [Description("Choose a voice for announcments and confirmations")]
      [PropertyOrder(2)]
      public string VoiceName
      {
         get
         {
            return _Settings.VoiceName;
         }
         set
         {
            if (_Settings.VoiceName == value)
            {
               return;
            }
            _Settings.VoiceName = value;
            SaveSettings();
            RaisePropertyChanged();
            if (value != string.Empty)
            {
               _Synth.SelectVoice(value);
               Announce("Voice gender is " + value.ToString(), true);
            }
         }
      }

      [Category("Announcements and voice")]
      [DisplayName("Voice speed")]
      [Description("Choose the speed with which announcements are spoken (range is from -10 (slow) to 10 (fast))")]
      [PropertyOrder(3)]
      [Range(-10, 10, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
      public int VoiceRate
      {
         get
         {
            return _Settings.VoiceRate;
         }
         set
         {
            if (_Settings.VoiceRate == value)
            {
               return;
            }
            _Settings.VoiceRate = value;
            _Synth.Rate = value;
            SaveSettings();
            RaisePropertyChanged();
            Announce("Voice speed is " + value.ToString(), true);
         }
      }
      #endregion


      #region Tracking settings
      private int _MaxRASlewRate = 1;
      public int MaxRASlewRate
      {
         get
         {
            return _MaxRASlewRate;
         }
         set
         {
            Set(ref _MaxRASlewRate, value);
         }
      }
      private int _MaxDecSlewRate = 1;
      public int MaxDecSlewRate
      {
         get
         {
            return _MaxDecSlewRate;
         }
         set
         {
            Set(ref _MaxDecSlewRate, value);
         }
      }


      private void SetMaxSlewRates()
      {
         string result = Driver.CommandString("Lunatic:GetMaxRates", true);
         string[] maxrates = result.Split(',');
         MaxRASlewRate = (int)(Convert.ToDouble(maxrates[0]) / CoreConstants.SIDEREAL_RATE_DEGREES);
         MaxDecSlewRate = (int)(Convert.ToDouble(maxrates[1]) / CoreConstants.SIDEREAL_RATE_DEGREES);
         bool saveSettings = false;
         foreach (SlewRatePreset rate in _Settings.SlewRatePresets)
         {
            if (rate.RARate > MaxRASlewRate)
            {
               rate.RARate = MaxRASlewRate;
               saveSettings = true;
            }
            if (rate.DecRate > MaxDecSlewRate)
            {
               rate.DecRate = MaxDecSlewRate;
               saveSettings = true;
            }
         }
         if (saveSettings)
         {
            SaveSettings();
         }
      }
      #endregion

      #region Gamepad/Joystick control settings...

      [Category("Game Controllers")]
      [DisplayName("Active game controller")]
      [Description("The currently selected telescope site.")]
      [PropertyOrder(0)]
      public GameController ActiveGameController
      {
         get
         {
            return _Settings.GameControllers.ActiveGameController;
         }
      }

      [Category("Game Controllers")]
      [DisplayName("Available game controllers")]
      [Description("The list of available game controllers")]
      [PropertyOrder(1)]
      public GameControllerCollection GameControllers
      {
         get
         {
            return _Settings.GameControllers;
         }
      }

      private bool _GameControllersAvailable = true;
      public bool GameControllersAvailable
      {
         get
         {
            return _GameControllersAvailable;
         }
         private set
         {
            Set(ref _GameControllersAvailable, value);
         }
      }
      public void GetAvailableGameControllers()
      {
         // Switch off active controller if it is not connected
         Guid activeGameControllerId = Guid.Empty;
         foreach (GameController gameController in _Settings.GameControllers)
         {
            if (gameController.IsActiveGameController)
            {
               activeGameControllerId = gameController.Id;
            }
            gameController.SetConnected(false, true);
            gameController.InstanceGuid = Guid.Empty;
         }

         DirectInput directInput = new DirectInput();
         foreach (DeviceInstance deviceInstance in directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices))
         {
            GameController existing = _Settings.GameControllers.Where(c => c.Id == deviceInstance.ProductGuid).FirstOrDefault();
            if (existing == null)
            {
               _Settings.GameControllers.Add(new GameController(deviceInstance.ProductGuid, deviceInstance.ProductName)
               {
                  InstanceGuid = deviceInstance.InstanceGuid
               });
            }
            else
            {
               existing.SetConnected(true, true);
               existing.InstanceGuid = deviceInstance.InstanceGuid;
            }

         }

         // If Gamepad not found, look for a Joystick
         foreach (var deviceInstance in directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices))
         {
            GameController existing = _Settings.GameControllers.Where(c => c.Id == deviceInstance.ProductGuid).FirstOrDefault();
            if (existing == null)
            {
               _Settings.GameControllers.Add(new GameController(deviceInstance.ProductGuid, deviceInstance.InstanceName)
               {
                  InstanceGuid = deviceInstance.InstanceGuid
               });
            }
            else
            {
               existing.SetConnected(true, true);
               existing.InstanceGuid = deviceInstance.InstanceGuid;
            }
         }

         
         GameControllersAvailable = _Settings.GameControllers.Any(c => c.IsConnected);
         _Settings.GameControllers.SetActiveGameController(activeGameControllerId);
      }

      private RelayCommand<GameController> _ConfigureGameControllerCommand;

      /// <summary>
      /// Adds a new chart to the active model
      /// </summary>
      public RelayCommand<GameController> ConfigureGameControllerCommand
      {
         get
         {
            return _ConfigureGameControllerCommand
                ?? (_ConfigureGameControllerCommand = new RelayCommand<GameController>(
                                      (controller) =>
                                      {
                                         GameControllerViewModel vm = new GameControllerViewModel(controller);
                                         GameControllerWindow contollerWindow = new GameControllerWindow(vm);
                                         var result = contollerWindow.ShowDialog();
                                      }

                                      ));
         }
      }
      #endregion
      #endregion
   }
}
