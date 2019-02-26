/*
BSD 2-Clause License

Copyright (c) 2019, Philip Crompton
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
#define Telescope

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;
using System.Timers;
using ASCOM;
using ASCOM.Astrometry;
using ASCOM.Astrometry.AstroUtils;
using ASCOM.Utilities;
using ASCOM.DeviceInterface;
using System.Globalization;
using System.Collections;
using System.Linq;
using System.Reflection;
using Core = ASCOM.LunaticAstroEQ.Core;
using ASCOM.LunaticAstroEQ.Controller;
using ASCOM.LunaticAstroEQ.Core;
using ASCOM.LunaticAstroEQ.Core.Geometry;
using CoreConstants = ASCOM.LunaticAstroEQ.Core.Constants;
using System.Threading;

namespace ASCOM.LunaticAstroEQ
{
   //
   // Your driver's DeviceID is ASCOM.LunaticAstroEQ.Telescope
   //
   // The Guid attribute sets the CLSID for ASCOM.LunaticAstroEQ.Telescope
   // The ClassInterface/None addribute prevents an empty interface called
   // _LunaticAstroEQ from being created and used as the [default] interface
   //
   // TODO Replace the not implemented exceptions with code to implement the function or
   // throw the appropriate ASCOM exception.
   //

   /// <summary>
   /// ASCOM Telescope Driver for LunaticAstroEQ.
   /// </summary>
   [Guid("3b88ba0e-c3ed-4154-add1-cab66da84bae")]
   [ProgId("ASCOM.LunaticAstroEQ.Telescope")]
   [ServedClassName("Driver for AstroEQ telescope controllers")]
   [ClassInterface(ClassInterfaceType.None)]
   public partial class Telescope : ReferenceCountedObjectBase, ITelescopeV3
   {
      /// <summary>
      /// ASCOM DeviceID (COM ProgID) for this driver.
      /// The DeviceID is used by ASCOM applications to load the driver at runtime.
      /// </summary>
      internal string driverID;
      // TODO Change the descriptive string for your driver then remove this line
      /// <summary>
      /// Driver description that displays in the ASCOM Chooser.
      /// </summary>
      internal string driverDescription = "ASCOM Telescope Driver for LunaticAstroEQ.";
      internal string driverName = "AstroEQ ASCOM Driver";

      private const int RA_AXIS = 0;
      private const int DEC_AXIS = 1;

      private AstroEQController Controller
      {
         get
         {
            return SharedResources.Controller;
         }
      }

      private static TraceLogger _tl = null;
      /// <summary>
      /// Variable to hold the trace logger object (creates a diagnostic log file with information that you specify)
      /// </summary>
      internal static TraceLogger tl
      {
         get
         {
            if (_tl == null)
            {
               _tl = new TraceLogger("", "LunaticAstroEQ");
            }
            return _tl;
         }
      }

      internal bool TraceState
      {
         get
         {
            return tl.Enabled;
         }
         set
         {
            tl.Enabled = value;
            Settings.TracingState = value;
         }
      }

      private HemisphereOption Hemisphere
      {
         get
         {
            return (SiteLatitude >= 0.0 ? HemisphereOption.Northern : HemisphereOption.Southern);
         }

      }

      internal TelescopeSettings Settings
      {
         get
         {
            return TelescopeSettingsProvider.Current.Settings;
         }
      }

      /// <summary>
      /// Private variable to hold an ASCOM Utilities object
      /// </summary>
      private AscomTools _AscomToolsCurrentPosition;
      private AscomTools _AscomToolsTargetPosition;

      private AxisPosition _ParkedAxisPosition;
      private MountCoordinate _CurrentPosition;
      private MountCoordinate _TargetPosition;


      private Dictionary<DriveRates, double> _TrackingRate = null;


      private System.Timers.Timer _Timer = null;

      /// <summary>
      /// Initializes a new instance of the <see cref="LunaticAstroEQ"/> class.
      /// Must be public for COM registration.
      /// </summary>
      public Telescope()
      {
         driverID = Marshal.GenerateProgIdForType(this.GetType());
         driverDescription = GetDriverDescription();



         tl.Enabled = Settings.TracingState; // This will also load the settings as it is the first time it is accessed.


         LogMessage("Telescope", "Starting initialisation");

         IsConnected = false;
         InitialiseAscomTools();

         InitialiseTimer();

         InitialisePulseGuidingTimer();


         LogMessage("Telescope", "Completed initialisation");
      }

      #region Timer ...
      private void InitialiseTimer()
      {
         _Timer = new System.Timers.Timer(Settings.RefreshInterval); // Initialise the pulse guiding timer with a 1 milisecond interval.
         _Timer.Enabled = false;
         _Timer.Elapsed += Timer_Elapsed;

      }

      private void DisposeTimer()
      {
         if (_Timer != null)
         {
            _Timer.Enabled = false;
            _Timer.Elapsed -= PulseGuidingTimer_Elapsed;
            _Timer.Dispose();
            _Timer = null;
         }
      }

      private bool processingElapsed = false;
      private void Timer_Elapsed(object sender, ElapsedEventArgs e)
      {
         if (processingElapsed)
         {
            return;
         }
         try
         {
            processingElapsed = true;
            RefreshCurrentPosition();
            processingElapsed = false;
         }
         catch (Exception ex)
         {
            System.Diagnostics.Debug.WriteLine(ex.Message);
            throw;
         }
         finally
         {
            processingElapsed = false;
         }
      }
      #endregion


      private void InitialiseAscomTools()
      {
         double latitude, longitude, elevation, temperature;
         lock (Controller)
         {
            latitude = Controller.ObservatoryLocation.Latitude.Value;
            longitude = Controller.ObservatoryLocation.Longitude.Value;
            elevation = Controller.ObservatoryElevation;
            temperature = 15.0;
         }
         _AscomToolsCurrentPosition = new AscomTools();

         // Initialise the transform from the site details stored with the controller
         _AscomToolsCurrentPosition.Transform.SiteLatitude = latitude;
         _AscomToolsCurrentPosition.Transform.SiteLongitude = longitude;
         _AscomToolsCurrentPosition.Transform.SiteElevation = elevation;
         _AscomToolsCurrentPosition.Transform.SiteTemperature = temperature;

         _AscomToolsTargetPosition = new AscomTools();

         // Initialise the transform from the site details stored with the controller
         _AscomToolsTargetPosition.Transform.SiteLatitude = latitude;
         _AscomToolsTargetPosition.Transform.SiteLongitude = longitude;
         _AscomToolsTargetPosition.Transform.SiteElevation = elevation;
         _AscomToolsTargetPosition.Transform.SiteTemperature = temperature;
      }
      private string GetDriverDescription()
      {
         string descr;
         if (this.GetType().GetCustomAttributes(typeof(ServedClassNameAttribute), true).FirstOrDefault() is ServedClassNameAttribute attr)
         {
            descr = attr.DisplayName;
         }
         else
         {
            descr = this.GetType().Assembly.FullName;
         }
         return descr;
      }

      //
      // PUBLIC COM INTERFACE ITelescopeV3 IMPLEMENTATION
      //

      #region Common properties and methods.

      /// <summary>
      /// Displays the Setup Dialog form.
      /// If the user clicks the OK button to dismiss the form, then
      /// the new settings are saved, otherwise the old values are reloaded.
      /// THIS IS THE ONLY PLACE WHERE SHOWING USER INTERFACE IS ALLOWED!
      /// </summary>
      public void SetupDialog()
      {
         // consider only showing the setup dialog if not connected
         // or call a different dialog if connected
         if (IsConnected)
         {
            System.Windows.Forms.MessageBox.Show("Already connected, just press OK");
         }


         using (SetupDialogForm F = new SetupDialogForm(this))
         {
            var result = F.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
               SaveSettings();
            }
         }
      }

      public ArrayList SupportedActions
      {
         get
         {
            tl.LogMessage("SupportedActions Get", "Returning empty arraylist");
            return new ArrayList(_SupportedActions);
         }
      }

      /// <summary>
      /// Invokes the specified device-specific action.
      /// </summary>
      /// <param name="ActionName">
      /// A well known name agreed by interested parties that represents the action to be carried out. 
      /// </param>
      /// <param name="ActionParameters">List of required parameters or an <see cref="String.Empty">Empty String</see> if none are required.
      /// </param>
      /// <returns>A string response. The meaning of returned strings is set by the driver author.</returns>
      /// <exception cref="ASCOM.MethodNotImplementedException">Throws this exception if no actions are suported.</exception>
      /// <exception cref="ASCOM.ActionNotImplementedException">It is intended that the SupportedActions method will inform clients 
      /// of driver capabilities, but the driver must still throw an ASCOM.ActionNotImplemented exception if it is asked to 
      /// perform an action that it does not support.</exception>
      /// <exception cref="NotConnectedException">If the driver is not connected.</exception>
      /// <exception cref="DriverException">Must throw an exception if the call was not successful</exception>
      /// <example>Suppose filter wheels start to appear with automatic wheel changers; new actions could 
      /// be “FilterWheel:QueryWheels” and “FilterWheel:SelectWheel”. The former returning a 
      /// formatted list of wheel names and the second taking a wheel name and making the change, returning appropriate 
      /// values to indicate success or failure.
      /// </example>
      /// <remarks><p style="color:red"><b>Can throw a not implemented exception</b></p> 
      /// This method is intended for use in all current and future device types and to avoid name clashes, management of action names 
      /// is important from day 1. A two-part naming convention will be adopted - <b>DeviceType:UniqueActionName</b> where:
      /// <list type="bullet">
      /// <item><description>DeviceType is the same value as would be used by <see cref="ASCOM.Utilities.Chooser.DeviceType"/> e.g. Telescope, Camera, Switch etc.</description></item>
      /// <item><description>UniqueActionName is a single word, or multiple words joined by underscore characters, that sensibly describes the action to be performed.</description></item>
      /// </list>
      /// <para>
      /// It is recommended that UniqueActionNames should be a maximum of 16 characters for legibility.
      /// Should the same function and UniqueActionName be supported by more than one type of device, the reserved DeviceType of 
      /// “General” will be used. Action names will be case insensitive, so FilterWheel:SelectWheel, filterwheel:selectwheel 
      /// and FILTERWHEEL:SELECTWHEEL will all refer to the same action.</para>
      /// <para>The names of all supported actions must be returned in the <see cref="SupportedActions"/> property.</para>
      /// </remarks>
      public string Action(string actionName, string actionParameters)
      {
         CheckConnected("Action");
         LogMessage("Action", string.Format("({0}, {1})", actionName, actionParameters));
         return ProcessCustomAction(actionName, actionParameters);
      }

      public void CommandBlind(string command, bool raw)
      {
         CheckConnected("CommandBlind");
         // Call CommandString and return as soon as it finishes
         this.CommandString(command, raw);
         // or
         throw new ASCOM.MethodNotImplementedException("CommandBlind");
         // DO NOT have both these sections!  One or the other
      }

      /// <summary>
      /// Transmits an arbitrary string to the device and waits for a boolean response.
      /// Optionally, protocol framing characters may be added to the string before transmission.
      /// </summary>
      /// <param name="Command">The literal command string to be transmitted.</param>
      /// <param name="Raw">
      /// if set to <c>true</c> the string is transmitted 'as-is'.
      /// If set to <c>false</c> then protocol framing characters may be added prior to transmission.
      /// </param>
      /// <returns>
      /// Returns the interpreted boolean response received from the device.
      /// </returns>
      /// <exception cref="MethodNotImplementedException">If the method is not implemented</exception>
      /// <exception cref="NotConnectedException">If the driver is not connected.</exception>
      /// <exception cref="DriverException">Must throw an exception if the call was not successful</exception>
      /// <remarks><p style="color:red"><b>Can throw a not implemented exception</b></p> </remarks>
      public bool CommandBool(string command, bool raw)
      {
         if (command != "Lunatic:IsInitialised")
         {
            CheckConnected("CommandBool");
         }
         LogMessage("CommandBool", string.Format("({0}, {1})", command, raw));
         return ProcessCommandBool(command, raw);
      }

      /// <summary>
      /// Transmits an arbitrary string to the device and waits for a string response.
      /// Optionally, protocol framing characters may be added to the string before transmission.
      /// </summary>
      /// <param name="Command">The literal command string to be transmitted.</param>
      /// <param name="Raw">
      /// if set to <c>true</c> the string is transmitted 'as-is'.
      /// If set to <c>false</c> then protocol framing characters may be added prior to transmission.
      /// </param>
      /// <returns>
      /// Returns the string response received from the device.
      /// </returns>
      /// <exception cref="MethodNotImplementedException">If the method is not implemented</exception>
      /// <exception cref="NotConnectedException">If the driver is not connected.</exception>
      /// <exception cref="DriverException">Must throw an exception if the call was not successful</exception>
      /// <remarks><p style="color:red"><b>Can throw a not implemented exception</b></p> </remarks>
      public string CommandString(string command, bool raw)
      {
         CheckConnected("CommandBlind");
         LogMessage("CommandString", string.Format("({0}, {1})", command, raw));
         // it's a good idea to put all the low level communication with the device here,
         // then all communication calls this function
         // you need something to ensure that only one command is in progress at a time
         return ProcessCommandString(command, raw);
      }

      public void Dispose()
      {
         // Clean up the tracelogger and util objects
         //tl.Enabled = false;
         //tl.Dispose();
         //tl = null;
         DisposeTimer();
         DisposePulseGuidingTimer();
         _AscomToolsCurrentPosition.Dispose();
         _AscomToolsTargetPosition.Dispose();
         TelescopeSettingsProvider.Current.Dispose();
      }

      public bool Connected
      {
         get
         {
            LogMessage("Connected", "Get {0}", IsConnected);
            return IsConnected;
         }
         set
         {
            lock (Controller)
            {
               LogMessage("Connected", "Set {0}", value);
               if (value == IsConnected)
                  return;

               if (value)
               {
                  if (string.IsNullOrWhiteSpace(Settings.COMPort))
                  {
                     throw new ASCOM.ValueNotSetException("comPort");
                  }
                  LogMessage("Connected Set", "Connecting to port {0}", Settings.COMPort);
                  int connectionResult = Controller.Connect(Settings.COMPort, (int)Settings.BaudRate, (int)Settings.Timeout, (int)Settings.Retry);
                  if (connectionResult == Core.Constants.MOUNT_SUCCESS)
                  {
                     IsConnected = true;
                     InitialiseCurrentPosition(true);
                     _Timer.Enabled = true;
                     // Set the polar scope brightness
                     Controller.MCSetPolarScopeBrightness(Settings.PolarSlopeBrightness);

                  }
                  else if (connectionResult == Core.Constants.MOUNT_COMCONNECTED)
                  {
                     IsConnected = true;
                     InitialiseCurrentPosition(false);
                     _Timer.Enabled = true;

                  }
                  else
                  {
                     // Something went wrong so not connected.
                     IsConnected = false;
                  }
               }
               else
               {
                  _Timer.Enabled = false;
                  Controller.Disconnect();
                  LogMessage("Connected Set", "Disconnecting from port {0}", Settings.COMPort);
                  IsConnected = false; ;
               }
            }
         }
      }

      public string Description
      {
         // TODO customise this device description
         get
         {
            tl.LogMessage("Description Get", driverDescription);
            return driverDescription;
         }
      }

      public string DriverInfo
      {
         get
         {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0}, Version {1}.{2}\n", driverDescription,
               TelescopeSettingsProvider.MajorVersion,
               TelescopeSettingsProvider.MinorVersion);
            sb.AppendLine(TelescopeSettingsProvider.CompanyName);
            sb.AppendLine(TelescopeSettingsProvider.Copyright);
            sb.AppendLine(TelescopeSettingsProvider.Comments);
            string driverInfo = sb.ToString();
            tl.LogMessage("DriverInfo Get", driverInfo);
            return driverInfo;
         }
      }

      public string DriverVersion
      {
         get
         {
            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            string driverVersion = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
            tl.LogMessage("DriverVersion Get", driverVersion);
            return driverVersion;
         }
      }

      public short InterfaceVersion
      {
         // set by the driver wizard
         get
         {
            LogMessage("InterfaceVersion Get", "3");
            return Convert.ToInt16("3");
         }
      }

      public string Name
      {
         get
         {
            string name = driverName;
            tl.LogMessage("Name Get", name);
            return name;
         }
      }

      #endregion

      #region ITelescope Implementation
      public void AbortSlew()
      {
         tl.LogMessage("AbortSlew", "");
         if (Settings.ParkStatus == ParkStatus.Parked)
         {
            throw new ASCOM.InvalidOperationException("Abort slew is invvalid when the scope is parked.");
         }
         AbortSlewInternal();
      }


      private AlignmentModes _AlignmentMode = AlignmentModes.algGermanPolar;
      public AlignmentModes AlignmentMode
      {
         get
         {
            LogMessage("AlignmentMode", "Get - {0}", _AlignmentMode);
            return _AlignmentMode;
         }
      }

      public double Altitude
      {
         get
         {
            lock (Controller)
            {
               double altitude = _CurrentPosition.AltAzimuth.Altitude.Value;
               tl.LogMessage("Altitude", "Get - " + _AscomToolsCurrentPosition.Util.DegreesToDMS(altitude, ":", ":"));
               return altitude;
            }
         }
      }

      public double ApertureArea
      {
         get
         {
            tl.LogMessage("ApertureArea Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("ApertureArea", false);
         }
      }

      public double ApertureDiameter
      {
         get
         {
            tl.LogMessage("ApertureDiameter Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("ApertureDiameter", false);
         }
      }

      public bool AtHome
      {
         get
         {
            tl.LogMessage("AtHome", "Get - " + false.ToString());
            return false;
         }
      }

      public bool AtPark
      {
         get
         {
            bool atPark = (Settings.ParkStatus == ParkStatus.Parked);
            LogMessage("AtPark", "Get - {0}", atPark);
            return atPark;
         }
      }

      public IAxisRates AxisRates(TelescopeAxes axis)
      {
         LogMessage("Command", "AxisRates");
         return _AxisRates[(int)axis];
      }

      public double Azimuth
      {
         get
         {
            lock (Controller)
            {
               double azimuth = _CurrentPosition.AltAzimuth.Azimuth.Value;
               tl.LogMessage("Azimuth", "Get - " + _AscomToolsCurrentPosition.Util.DegreesToDMS(azimuth, ":", ":"));
               return azimuth;
            }
         }
      }

      public bool CanFindHome
      {
         get
         {
            tl.LogMessage("CanFindHome", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanMoveAxis(TelescopeAxes Axis)
      {
         bool canMove = false;
         switch (Axis)
         {
            case TelescopeAxes.axisPrimary:
            case TelescopeAxes.axisSecondary:
               canMove = true;
               break;
            case TelescopeAxes.axisTertiary:
               break;
            default: throw new InvalidValueException("CanMoveAxis", Axis.ToString(), "0 to 2");
         }
         LogMessage("CanMoveAxis", "Get - {0}:{1}", Axis, canMove);
         return canMove;
      }

      private bool _CanPark = true;
      public bool CanPark
      {
         get
         {
            if (!Connected)
            {
               throw new NotConnectedException("Astro EQ is not connected.");
            }
            LogMessage("CanPark", "Get - {0}", _CanPark);
            return _CanPark;
         }
      }


      private bool _CanPulseGuide = true;
      public bool CanPulseGuide
      {
         get
         {
            if (!Connected)
            {
               throw new NotConnectedException("Astro EQ is not connected.");
            }
            LogMessage("CanPulseGuide", "Get - {0}", _CanPulseGuide);
            return _CanPulseGuide;
         }
      }

      private bool _CanSetDeclinationRate = true;
      public bool CanSetDeclinationRate
      {
         get
         {
            if (!Connected)
            {
               throw new NotConnectedException("Astro EQ is not connected.");
            }
            LogMessage("CanSetDeclinationRate", "Get - {0}", _CanSetDeclinationRate);
            return _CanSetDeclinationRate;
         }
      }

      private bool _CanSetGuideRates = true;
      public bool CanSetGuideRates
      {
         get
         {
            if (!Connected)
            {
               throw new NotConnectedException("Astro EQ is not connected.");
            }
            LogMessage("CanSetGuideRates", "Get - {0}", _CanSetGuideRates);
            return _CanSetGuideRates;
         }
      }

      private bool _CanSetPark = true;
      public bool CanSetPark
      {
         get
         {
            if (!Connected)
            {
               throw new NotConnectedException("Astro EQ is not connected.");
            }
            LogMessage("CanSetPark", "Get - {0}", _CanSetPark);
            return _CanSetPark;
         }
      }

      public bool CanSetPierSide
      {
         get
         {
            //TODO: CanSetPierSide - Needs functionality to force a meridian flip.
            LogMessage("CanSetPierSide", "Get - " + false.ToString());
            return false;
         }
      }

      private bool _CanSetRightAscensionRate = true;
      public bool CanSetRightAscensionRate
      {
         get
         {
            if (!Connected)
            {
               throw new NotConnectedException("Astro EQ is not connected.");
            }
            LogMessage("CanSetRightAscensionRate", "Get - {0}", _CanSetRightAscensionRate);
            return _CanSetRightAscensionRate;
         }
      }

      private bool _CanSetTracking = true;
      public bool CanSetTracking
      {
         get
         {
            if (!Connected)
            {
               throw new NotConnectedException("Astro EQ is not connected.");
            }
            LogMessage("CanSetTracking", "Get - {0}", _CanSetTracking);
            return _CanSetTracking;
         }
      }

      private bool _CanSlew = true;
      public bool CanSlew
      {
         get
         {
            if (!Connected)
            {
               throw new NotConnectedException("Astro EQ is not connected.");
            }
            LogMessage("CanSlew", "Get - {0}", _CanSlew);
            return _CanSlew;
         }
      }

      private bool _CanSlewAltAz = false;
      public bool CanSlewAltAz
      {
         get
         {
            LogMessage("CanSlewAltAz", "Get - {0}", _CanSlewAltAz);
            return _CanSlewAltAz;
         }
      }

      private bool _CanSlewAltAzAsync = false;
      public bool CanSlewAltAzAsync
      {
         get
         {
            LogMessage("CanSlewAltAzAsync", "Get - {0}", _CanSlewAltAzAsync);
            return _CanSlewAltAzAsync;
         }
      }


      private bool _CanSlewAsync = true;
      public bool CanSlewAsync
      {
         get
         {
            LogMessage("CanSlewAsync", "Get - {0}", _CanSlewAsync);
            return _CanSlewAsync;
         }
      }


      // TODO: Enable Synching
      private bool _CanSync = false;
      public bool CanSync
      {
         get
         {
            if (!Connected)
            {
               throw new NotConnectedException("Astro EQ is not connected.");
            }
            LogMessage("CanSync", "Get - {0}", _CanSync);
            return _CanSync;
         }
      }


      private bool _CanSyncAltAz = false;
      public bool CanSyncAltAz
      {
         get
         {
            LogMessage("CanSyncAltAz", "Get - {0}", _CanSyncAltAz);
            return _CanSyncAltAz;
         }
      }

      private bool _CanUnpark = true;
      public bool CanUnpark
      {
         get
         {
            LogMessage("CanUnpark", "Get - {0}", _CanUnpark);
            return _CanUnpark;
         }
      }

      public double Declination
      {
         get
         {
            lock (Controller)
            {
               double declination = _CurrentPosition.Equatorial.Declination;
               LogMessage("Declination", "Get - {0}", _AscomToolsCurrentPosition.Util.DegreesToDMS(declination, ":", ":"));
               // System.Diagnostics.Debug.WriteLine($"Declination Get - {_AscomToolsCurrentPosition.Util.DegreesToDMS(declination, ":", ":")}");

               return declination;
            }
         }
      }

      public double DeclinationRate
      {
         get
         {
            LogMessage("DeclinationRate", "Get - {0}", _AscomToolsCurrentPosition.Util.DegreesToDMS(Settings.DeclinationRate, ":", ":"));
            return Settings.DeclinationRate;
         }
         set
         {
            LogMessage("DeclinationRate", "Set - {0}" + _AscomToolsCurrentPosition.Util.DegreesToDMS(value, ":", ":"));
            if (value == Settings.DeclinationRate)
            {
               return;
            }
            Settings.DeclinationRate = value;
            SaveSettings();
            if (Tracking)
            {
               StartTracking();  // Force tracking to refresh with the new rate.
            }
         }
      }

      public PierSide DestinationSideOfPier(double rightAscension, double declination)
      {
         PierSide destinationSideOfPier = GetDestinationSideOfPier(rightAscension, declination);
         LogMessage("DestinationSideOfPier", "Get - {0}", destinationSideOfPier);
         return destinationSideOfPier;
      }

      private bool _DoesRefraction = false;
      public bool DoesRefraction
      {
         get
         {
            LogMessage("DoesRefraction", "Get - {0}", _DoesRefraction);
            return _DoesRefraction;
         }
         set
         {
            tl.LogMessage("DoesRefraction Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("DoesRefraction", true);
         }
      }

      public EquatorialCoordinateType EquatorialSystem
      {
         get
         {
            EquatorialCoordinateType equatorialSystem = EquatorialCoordinateType.equTopocentric;
            tl.LogMessage("DeclinationRate", "Get - " + equatorialSystem.ToString());
            return equatorialSystem;
         }
      }

      public void FindHome()
      {
         tl.LogMessage("FindHome", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("FindHome");
      }

      public double FocalLength
      {
         get
         {
            tl.LogMessage("FocalLength Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("FocalLength", false);
         }
      }

      public double GuideRateDeclination
      {
         get
         {
            LogMessage("GuideRateDeclination", "Get - {0}", Settings.GuideRateDeclination);
            return Settings.GuideRateDeclination;
         }
         set
         {
            if (value == Settings.GuideRateDeclination)
            {
               return;
            }
            if (value < Settings.GuideRateDeclinationMin || value > Settings.GuideRateDeclinationMax)
            {
               throw new ASCOM.InvalidValueException($"GuideRateDeclination must be in the range {Settings.GuideRateDeclinationMin} to {Settings.GuideRateDeclinationMax} degrees/sec.");
            }
            LogMessage("GuideRateDeclination", "Set -{0}", value);
            Settings.GuideRateDeclination = value;
            SaveSettings();
         }
      }

      public double GuideRateRightAscension
      {
         get
         {
            LogMessage("GuideRateRightAscension", "Get - {0}", Settings.GuideRateRightAscension);
            return Settings.GuideRateRightAscension;
         }
         set
         {
            if (value == Settings.GuideRateRightAscension)
            {
               return;
            }
            if (value < Settings.GuideRateRightAscensionMin || value > Settings.GuideRateRightAscensionMax)
            {
               throw new ASCOM.InvalidValueException($"GuideRateRightAscension must be in the range {Settings.GuideRateRightAscensionMin} to {Settings.GuideRateRightAscensionMax} degrees/sec.");
            }
            LogMessage("GuideRateRightAscension", "Set -{0}", value);
            Settings.GuideRateRightAscension = value;
            SaveSettings();
         }
      }

      public bool IsPulseGuiding
      {
         get
         {
            bool isPulseGuiding = (_PulseGuidingStopwatch[RA_AXIS].IsRunning || _PulseGuidingStopwatch[DEC_AXIS].IsRunning);
            LogMessage("IsPulseGuiding", "Get - {0}", isPulseGuiding);
            return isPulseGuiding;
         }
      }


      /// <summary>
      /// 
      /// </summary>
      /// <param name="axis"></param>
      /// <param name="rate">The rate in degrees per second</param>
      public void MoveAxis(TelescopeAxes axis, double rate)
      {
         bool isRASlewing = false;
         bool isDecSlewing = false;
         if (axis == TelescopeAxes.axisTertiary)
         {
            throw new ASCOM.InvalidValueException("Driver does not support tertiary axis.");
         }

         if (Settings.ParkStatus != ParkStatus.Unparked)
         {
            throw new ASCOM.ParkedException("The mount is currently parked or parking.");
         }

         IRate limits = _AxisRates[(int)axis][1];  // IRate is 1 based.
         double absRate = Math.Abs(rate);
         if (absRate < limits.Minimum || absRate > limits.Maximum)
         {
            throw new ASCOM.InvalidValueException($"Method MoveAxis() rate must be in the range ±{limits.Minimum} to ±{limits.Maximum}.");
         }

         lock (Controller)
         {
            // System.Diagnostics.Debug.WriteLine(String.Format("MoveAxis({0}, {1})", axis, rate));
            LogMessage("MoveAxis", "({0}, {1})", axis, rate);

            switch (axis)
            {
               case TelescopeAxes.axisPrimary:
                  isRASlewing = (rate != 0);
                  Controller.MCAxisSlew(AxisId.Axis1_RA, rate, Hemisphere);
                  break;
               case TelescopeAxes.axisSecondary:
                  isDecSlewing = (rate != 0);
                  Controller.MCAxisSlew(AxisId.Axis2_Dec, rate, Hemisphere);
                  break;
               default:
                  throw new ASCOM.InvalidValueException("Tertiary axis is not supported by MoveAxis command");
            }
            _IsMoveAxisSlewing = (isRASlewing || isDecSlewing);
         }
      }

      public void Park()
      {
         LogMessage("Command", "Park");
         ParkInternal();
      }

      public void PulseGuide(GuideDirections direction, int duration)
      {
         if (Settings.ParkStatus != ParkStatus.Unparked)
         {
            throw new ASCOM.ParkedException("The mount is currently parked or parking.");
         }
         if (Slewing)
         {
            // Just return if slewing
            return;
         }
         lock (Controller)
         {
            LogMessage("Command", "PulseGuide {0} {1}", direction, duration);
            if (duration > 0)
            {
               StartPulseGuiding(direction, duration);
            }
            else
            {
               StopPulseGuiding(direction);
            }
         }
      }

      public double RightAscension
      {
         get
         {
            lock (Controller)
            {
               double rightAscension = _CurrentPosition.Equatorial.RightAscension.Value;
               LogMessage("RightAscension", "Get - {0}", _AscomToolsCurrentPosition.Util.HoursToHMS(rightAscension));
               // System.Diagnostics.Debug.WriteLine($"RightAscension Get - {_AscomToolsCurrentPosition.Util.HoursToHMS(rightAscension)}");
               return rightAscension;
            }
         }
      }



      public double RightAscensionRate
      {
         get
         {
            LogMessage("RightAscensionRate", "Get - {0}", Settings.RightAscensionRate);
            return Settings.RightAscensionRate;
         }
         set
         {
            LogMessage("RightAscensionRate", "Set - {0}", _AscomToolsCurrentPosition.Util.DegreesToDMS(value, ":", ":"));
            if (value == Settings.RightAscensionRate)
            {
               return;
            }
            Settings.RightAscensionRate = value;
            SaveSettings();
            if (Tracking)
            {
               StartTracking();  // Restart tracking with the new rate.
            }
         }
      }

      public void SetPark()
      {
         LogMessage("Command", "SetPark");
         _ParkedAxisPosition = _CurrentPosition.ObservedAxes;
         Settings.AxisParkPosition = _ParkedAxisPosition;
         SaveSettings();
      }

      public PierSide SideOfPier
      {
         get
         {
            PierSide value = _CurrentPosition.GetPointingSideOfPier(false);
            LogMessage("SideOfPier", "Get - {0}", value);
            return value;
         }
         set
         {
            LogMessage("SideOfPier Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("SideOfPier", true);
         }
      }

      public double SiderealTime
      {
         get
         {
            lock (Controller)
            {
               double lst = _CurrentPosition.LocalApparentSiderialTime;
               tl.LogMessage("SiderealTime", "Get - " + _AscomToolsCurrentPosition.Util.HoursToHMS(lst));
               return lst;
            }
         }
      }

      public double SiteElevation
      {
         get
         {
            lock (Controller)
            {
               LogMessage("SiteElevation", "Get {0}", Controller.ObservatoryElevation);
               return Controller.ObservatoryElevation;
            }
         }
         set
         {
            lock (Controller)
            {
               LogMessage("SiteElevation", "Set {0}", value);
               if (value < -300.0 || value > 10000.0)
               {
                  throw new ASCOM.InvalidValueException("SiteElevation must be between -300m and 10,000m");
               }
               if (Controller.ObservatoryElevation == value)
               {
                  return;
               }
               Controller.ObservatoryElevation = value;
               RelocateMounts(SiteLatitude, SiteLongitude, value);
            }
         }
      }

      public double SiteLatitude
      {
         get
         {
            lock (Controller)
            {
               LogMessage("SiteLatitude", "Get {0}", Controller.ObservatoryLocation.Latitude.Value);
               return Controller.ObservatoryLocation.Latitude.Value;
            }
         }
         set
         {
            lock (Controller)
            {
               LogMessage("SiteLatitude", "Set {0}", value);
               if (value < -90.0 || value > 90.0)
               {
                  throw new ASCOM.InvalidValueException("Site Latitude must be in the range -90 to 90.");
               }
               if (Controller.ObservatoryLocation.Latitude.Value == value)
               {
                  return;
               }
               Controller.ObservatoryLocation.Latitude = value;
               RelocateMounts(value, SiteLongitude, SiteElevation);
            }
         }
      }


      public double SiteLongitude
      {
         get
         {
            lock (Controller)
            {
               LogMessage("SiteLongitude", "Get {0}", Controller.ObservatoryLocation.Longitude.Value);
               return Controller.ObservatoryLocation.Longitude.Value;
            }
         }
         set
         {
            lock (Controller)
            {
               LogMessage("SiteLongitude", "Set {0}", value);
               if (value < -180.0 || value > 180.0)
               {
                  throw new ASCOM.InvalidValueException("Site Longitude must be in the range -180.0 to 180.0.");
               }
               if (Controller.ObservatoryLocation.Longitude.Value == value)
               {
                  return;
               }
               Controller.ObservatoryLocation.Longitude = value;
               RelocateMounts(SiteLatitude, value, SiteElevation);
            }
         }
      }





      public short SlewSettleTime
      {
         get
         {
            tl.LogMessage("SlewSettleTime Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("SlewSettleTime", false);
         }
         set
         {
            tl.LogMessage("SlewSettleTime Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("SlewSettleTime", true);
         }
      }

      public void SlewToAltAz(double Azimuth, double Altitude)
      {
         tl.LogMessage("SlewToAltAz", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SlewToAltAz");
      }

      public void SlewToAltAzAsync(double Azimuth, double Altitude)
      {
         tl.LogMessage("SlewToAltAzAsync", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SlewToAltAzAsync");
      }

      public void SlewToCoordinates(double rightAscension, double declination)
      {
         if (Settings.ParkStatus != ParkStatus.Unparked)
         {
            throw new ASCOM.ParkedException("The mount is currently parked or parking.");
         }
         if (!Tracking)
         {
            throw new ASCOM.InvalidValueException("Mount is not currently tracking.");
         }
         LogMessage("Command", "SlewToCoordinates RA:{0}, Dec: {0}", rightAscension, declination);
         SlewToEquatorialCoordinate(rightAscension, declination);
         // Block until the slew completes
         while (_IsSlewing)
         {
            Thread.Sleep(1000);// Allow time for main timer loop to update the axis state
         }
         // Refine the slew
         SlewToEquatorialCoordinate(rightAscension, declination);
         // Block until the slew completes
         while (_IsSlewing)
         {
            Thread.Sleep(1000);  // Allow time for main timer loop to update the axis state
         }

      }

      public void SlewToCoordinatesAsync(double rightAscension, double declination)
      {
         if (Settings.ParkStatus != ParkStatus.Unparked)
         {
            throw new ASCOM.ParkedException("The mount is currently parked or parking.");
         }
         if (!Tracking)
         {
            throw new ASCOM.InvalidValueException("Mount is not currently tracking.");
         }
         _RefineGoto = true;
         LogMessage("Command", "SlewToCoordinatesAsync RA:{0}, Dec: {0}", rightAscension, declination);
         SlewToEquatorialCoordinate(rightAscension, declination);
      }

      public void SlewToTarget()
      {
         if (Settings.ParkStatus != ParkStatus.Unparked)
         {
            throw new ASCOM.ParkedException("The mount is currently parked or parking.");
         }
         if (!Tracking)
         {
            throw new ASCOM.InvalidValueException("Mount is not currently tracking.");
         }
         if (!_TargetRightAscension.HasValue)
         {
            throw new ASCOM.InvalidValueException("Target Right Ascension is not set.");
         }
         if (!_TargetDeclination.HasValue)
         {
            throw new ASCOM.InvalidValueException("Target Declination is not set.");
         }
         LogMessage("Command", "SlewToTarget", TargetRightAscension, TargetDeclination);
         SlewToEquatorialCoordinate(TargetRightAscension, TargetDeclination);
         // Block until the slew completes
         while (_IsSlewing)
         {
            Thread.Sleep(1000);// Allow time for main timer loop to update the axis state
         }
         // Refine the GOTO
         SlewToEquatorialCoordinate(TargetRightAscension, TargetDeclination);
         // Block until the slew completes
         while (_IsSlewing)
         {
            Thread.Sleep(1000);// Allow time for main timer loop to update the axis state
         }

      }

      public void SlewToTargetAsync()
      {
         if (Settings.ParkStatus != ParkStatus.Unparked)
         {
            throw new ASCOM.ParkedException("The mount is currently parked or parking.");
         }
         if (!Tracking)
         {
            throw new ASCOM.InvalidValueException("Mount is not currently tracking.");
         }
         if (!_TargetRightAscension.HasValue)
         {
            throw new ASCOM.InvalidValueException("Target Right Ascension is not set.");
         }
         if (!_TargetDeclination.HasValue)
         {
            throw new ASCOM.InvalidValueException("Target Declination is not set.");
         }
         LogMessage("Command", "SlewToTargetAsync", TargetRightAscension, TargetDeclination);
         _RefineGoto = true;
         SlewToEquatorialCoordinate(TargetRightAscension, TargetDeclination);
      }

      bool _IsSlewing;
      bool _IsMoveAxisSlewing;

      public bool Slewing
      {
         get
         {
            bool isSlewing = false;
            switch (Settings.ParkStatus)
            {
               case ParkStatus.Unparked:
                  isSlewing = _IsSlewing;
                  if (!isSlewing)
                  {
                     isSlewing = _IsMoveAxisSlewing;
                  }
                  break;
               case ParkStatus.Parked:
               case ParkStatus.Unparking:
                  isSlewing = false;
                  break;
               case ParkStatus.Parking:
                  isSlewing = true;
                  break;
            }
            LogMessage("Slewing", "Get - {0}", isSlewing);
            return isSlewing;
         }
      }

      public void SyncToAltAz(double Azimuth, double Altitude)
      {
         tl.LogMessage("SyncToAltAz", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SyncToAltAz");
      }

      public void SyncToCoordinates(double rightAscension, double declination)
      {
         LogMessage("COMMAND - ", "SyncToCoordinate({0},{1}", rightAscension, declination);
         if (Settings.ParkStatus == ParkStatus.Unparked)
         {
            if (TrackingState == TrackingStatus.Off)
            {
               throw new ASCOM.InvalidOperationException("RaDec sync is not permitted if moumt is not Tracking.");
            }
            else
            {
               throw new ASCOM.MethodNotImplementedException("SyncToCoordinates");
               //if (ValidateRADEC(rightAscension, declination))
               //{
               //   // TODO: HC.Add_Message("SynCoor: " & oLangDll.GetLangString(105) & "[ " & FmtSexa(RightAscension, False) & "] " & oLangDll.GetLangString(106) & "[ " & FmtSexa(Declination, True) & " ]")
               //   if (SyncToRADEC(rightAscension, declination, SiteLongitude, Hemisphere))
               //   {
               //      // EQ_Beep(4)
               //   }
               //}
               //else
               //{
               //   throw new ASCOM.InvalidValueException("Invalid value passed to SyncToCoordinates()");
               //}
            }
         }
         else
         {
            throw new ASCOM.InvalidOperationException("SyncToCoordinates() is not valid whilst the scope is parked.");
         }

      }

      public void SyncToTarget()
      {
         tl.LogMessage("SyncToTarget", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SyncToTarget");
      }

      private double? _TargetDeclination;
      public double TargetDeclination
      {
         get
         {
            if (!_TargetDeclination.HasValue)
            {
               throw new ASCOM.InvalidOperationException("Target declination has not been set.");
            }
            LogMessage("TargetDeclination", " - Get {0}", _TargetDeclination.Value);
            return _TargetDeclination.Value;
         }
         set
         {
            LogMessage("TargetDeclination", " - Set {0}", value);
            if (value < -90.0 || value > 90.0)
            {
               throw new ASCOM.InvalidValueException("Target declination must be in the range -90.0 to 90.0.");
            }
            _TargetDeclination = value;
         }
      }

      private double? _TargetRightAscension;
      public double TargetRightAscension
      {
         get
         {
            if (!_TargetRightAscension.HasValue)
            {
               throw new ASCOM.InvalidOperationException("Target right ascention has not been set.");
            }
            LogMessage("TargetRightAscension", " - Get {0}", _TargetRightAscension.Value);
            return _TargetRightAscension.Value;
         }
         set
         {
            LogMessage("TargetRightAscension", " - Set {0}", value);
            if (value < 0.0 || value > 24.0)
            {
               throw new ASCOM.InvalidValueException("Target right ascention must be in the range 0.0 to 24.0 hours.");
            }
            _TargetRightAscension = value;
         }
      }

      public bool Tracking
      {
         get
         {
            bool tracking = (TrackingState != TrackingStatus.Off);
            LogMessage("Tracking", "Get - {0}", tracking);
            return tracking;
         }
         set
         {
            LogMessage("Tracking", "Set - {0}", value);
            if (Settings.ParkStatus == ParkStatus.Unparked || (Settings.ParkStatus == ParkStatus.Parked && value))
            {
               lock (Controller)
               {
                  if (value)
                  {
                     //if (Settings.DeclinationRate == 0)
                     //{
                     // track at sidereal
                     StartTracking();   // This method takes into account RightAscensionRate as well.
                     //}
                     //else
                     //{
                     //   // DeclinationRate != 0.0 so tracking with both axes (i.e. custom tracking)
                     //   StartCustomTracking();
                     //   // track at custom rate
                     //   //if (PECEnabled)
                     //   //{
                     //   //   PECStopTracking();
                     //   //}
                     //   // Call CustomMoveAxis(0, gRightAscensionRate, True, oLangDll.GetLangString(189))
                     //   // Call CustomMoveAxis(1, gDeclinationRate, True, oLangDll.GetLangString(189))
                     //}
                  }
                  else
                  {
                     Controller.MCAxisStop(AxisId.Both_Axes);
                     // Announce tracking stopped
                     Settings.TrackingState = TrackingStatus.Off;
                     // not sure that we should be clearing the rate offests ASCOM Spec is no help
                     SaveSettings();
                  }
               }
            }
            else
            {
               throw new ASCOM.ParkedException("Tracking change not allowed when mount is parked.");
            }
         }
      }

      public DriveRates TrackingRate
      {
         get
         {
            LogMessage("TrackingRate", "Get - {0}", Settings.TrackingRate);
            return Settings.TrackingRate;
         }
         set
         {
            LogMessage("TrackingRate", "Set - {0}", value);
            if (Tracking && value == Settings.TrackingRate)
            {
               return;
            }
            switch (value)
            {
               case DriveRates.driveSidereal:
               case DriveRates.driveLunar:
               case DriveRates.driveSolar:
               case DriveRates.driveKing:
                  Settings.TrackingRate = value;
                  SaveSettings();
                  break;
               default:
                  throw new ASCOM.InvalidValueException("TrackingRate");
            }
         }
      }


      public ITrackingRates TrackingRates
      {
         get
         {
            ITrackingRates trackingRates = new TrackingRates();
            LogMessage("TrackingRates", "Get - ");
            foreach (DriveRates driveRate in trackingRates)
            {
               LogMessage("TrackingRates", "Get - {0}", driveRate);
            }
            return trackingRates;
         }
      }

      public DateTime UTCDate
      {
         get
         {
            DateTime utcDate = DateTime.UtcNow;
            LogMessage("UTCDate", "Get - {0:}", utcDate.ToString("MM/dd/yy HH:mm:ss"));
            return utcDate;
         }
         set
         {
            LogMessage("UTCDate Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("UTCDate", true);
         }
      }

      public void Unpark()
      {
         LogMessage("COMMAND", "Unpark");
         if (Settings.ParkStatus == ParkStatus.Parked)
         {
            lock (Controller)
            {
               //TODO: Sort out whether tracking should be restarted and restart if necessary.
               Settings.ParkStatus = ParkStatus.Unparked;
               SaveSettings();
            }
         }
      }

      #endregion

      #region Private properties and methods

      // here are some useful properties and methods that can be used as required
      // to help with driver development

      /// <summary>
      /// Returns true if there is a valid connection to the driver hardware
      /// </summary>
      private bool IsConnected { get; set; }

      /// <summary>
      /// Use this function to throw an exception if we aren't connected to the hardware
      /// </summary>
      /// <param name="message"></param>
      private void CheckConnected(string message)
      {
         if (!IsConnected)
         {
            throw new ASCOM.NotConnectedException(message);
         }
      }


      private void SaveSettings()
      {
         TelescopeSettingsProvider.Current.SaveSettings();
      }

      /// <summary>
      /// Log helper function that takes formatted strings and arguments
      /// </summary>
      /// <param name="identifier"></param>
      /// <param name="message"></param>
      /// <param name="args"></param>
      internal static void LogMessage(string identifier, string message)
      {
         tl.LogMessage(identifier, message);
         // System.Diagnostics.Debug.WriteLine($"{identifier}: {msg}");
      }

      /// <summary>
      /// Log helper function that takes formatted strings and arguments
      /// </summary>
      /// <param name="identifier"></param>
      /// <param name="message"></param>
      /// <param name="args"></param>
      internal static void LogMessage(string identifier, string message, params object[] args)
      {
         string msg;
         if (args != null && args.Length > 0)
         {
            msg = string.Format(message, args);
         }
         else
         {
            msg = message;
         }
         tl.LogMessage(identifier, msg);
         // System.Diagnostics.Debug.WriteLine($"{identifier}: {msg}");
      }
      #endregion
   }
}
