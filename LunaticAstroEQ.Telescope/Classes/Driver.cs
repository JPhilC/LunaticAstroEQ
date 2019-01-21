//tabs=4
// --------------------------------------------------------------------------------
// TODO fill in this information for your driver, then remove this line!
//
// ASCOM Telescope driver for LunaticAstroEQ
//
// Description:	Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam 
//				nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam 
//				erat, sed diam voluptua. At vero eos et accusam et justo duo 
//				dolores et ea rebum. Stet clita kasd gubergren, no sea takimata 
//				sanctus est Lorem ipsum dolor sit amet.
//
// Implements:	ASCOM Telescope interface version: <To be completed by driver developer>
// Author:		(XXX) Your N. Here <your@email.here>
//
// Edit Log:
//
// Date			Who	Vers	Description
// -----------	---	-----	-------------------------------------------------------
// dd-mmm-yyyy	XXX	6.0.0	Initial edit, created from ASCOM driver template
// --------------------------------------------------------------------------------
//


// This is used to define code in the template that is specific to one class implementation
// unused code canbe deleted and this definition removed.
#define Telescope

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

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

      private AstroEQController _Controller;

      /// <summary>
      /// Variable to hold the trace logger object (creates a diagnostic log file with information that you specify)
      /// </summary>
      internal static TraceLogger tl;

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
      private AscomTools _AscomToolsCelestialPole;
      private AscomTools _AscomToolsTargetPosition;

      private MountCoordinate _CelestialPolePosition;
      private MountCoordinate _CurrentPosition;
      private MountCoordinate _TargetPosition;

      /// <summary>
      /// Initializes a new instance of the <see cref="LunaticAstroEQ"/> class.
      /// Must be public for COM registration.
      /// </summary>
      public Telescope()
      {
         driverID = Marshal.GenerateProgIdForType(this.GetType());
         driverDescription = GetDriverDescription();


         _Controller = AstroEQController.Instance;

         tl = new TraceLogger("", "LunaticAstroEQ")
         {
            Enabled = Settings.TracingState // This will also load the settings as it is the first time it is accessed.
         };

         tl.LogMessage("Telescope", "Starting initialisation");

         IsConnected = false;
         InitialiseAscomTools();


         tl.LogMessage("Telescope", "Completed initialisation");
      }

      private void InitialiseAscomTools()
      {
         _AscomToolsCurrentPosition = new AscomTools();

         // Initialise the transform from the site details stored with the controller
         _AscomToolsCurrentPosition.Transform.SiteLatitude = _Controller.ObservatoryLocation.Latitude.Value;
         _AscomToolsCurrentPosition.Transform.SiteLongitude = _Controller.ObservatoryLocation.Longitude.Value;
         _AscomToolsCurrentPosition.Transform.SiteElevation = _Controller.ObservatoryElevation;
         _AscomToolsCurrentPosition.Transform.SiteTemperature = 15.0;

         _AscomToolsCelestialPole = new AscomTools();

         // Initialise the transform from the site details stored with the controller
         _AscomToolsCelestialPole.Transform.SiteLatitude = _Controller.ObservatoryLocation.Latitude.Value;
         _AscomToolsCelestialPole.Transform.SiteLongitude = _Controller.ObservatoryLocation.Longitude.Value;
         _AscomToolsCelestialPole.Transform.SiteElevation = _Controller.ObservatoryElevation;
         _AscomToolsCelestialPole.Transform.SiteTemperature = 15.0;

         _AscomToolsTargetPosition = new AscomTools();

         // Initialise the transform from the site details stored with the controller
         _AscomToolsTargetPosition.Transform.SiteLatitude = _Controller.ObservatoryLocation.Latitude.Value;
         _AscomToolsTargetPosition.Transform.SiteLongitude = _Controller.ObservatoryLocation.Longitude.Value;
         _AscomToolsTargetPosition.Transform.SiteElevation = _Controller.ObservatoryElevation;
         _AscomToolsTargetPosition.Transform.SiteTemperature = 15.0;
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
               TelescopeSettingsProvider.Current.SaveSettings();
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
         tl.Enabled = false;
         tl.Dispose();
         tl = null;
         _AscomToolsCurrentPosition.Dispose();
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
               int connectionResult = _Controller.Connect(Settings.COMPort, (int)Settings.BaudRate, (int)Settings.Timeout, (int)Settings.Retry);
               if (connectionResult == Core.Constants.MOUNT_SUCCESS)
               {
                  IsConnected = true;
                  InitialiseCurrentPosition();

               }
               else if (connectionResult == Core.Constants.MOUNT_COMCONNECTED)
               {
                  IsConnected = true;
               }
               else
               {
                  // Something went wrong so not connected.
                  IsConnected = false;
               }
            }
            else
            {
               _Controller.Disconnect();
               LogMessage("Connected Set", "Disconnecting from port {0}", Settings.COMPort);
               IsConnected = false; ;
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
            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            // TODO customise this driver description
            string driverInfo = "Information about the driver itself. Version: " + String.Format(CultureInfo.InvariantCulture, "{0}.{1}", version.Major, version.Minor);
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
            string name = "AstroEQ ASCOM Driver";
            tl.LogMessage("Name Get", name);
            return name;
         }
      }

      #endregion

      #region ITelescope Implementation
      public void AbortSlew()
      {
         tl.LogMessage("AbortSlew", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("AbortSlew");
      }

      public AlignmentModes AlignmentMode
      {
         get
         {
            tl.LogMessage("AlignmentMode Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("AlignmentMode", false);
         }
      }

      public double Altitude
      {
         get
         {
            lock (_CurrentPosition)
            {
               RefreshCurrentPosition();
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
            return false;
         }
      }

      public IAxisRates AxisRates(TelescopeAxes Axis)
      {
         tl.LogMessage("AxisRates", "Get - " + Axis.ToString());
         return new AxisRates(Axis);
      }

      public double Azimuth
      {
         get
         {
            lock (_CurrentPosition)
            {
               RefreshCurrentPosition();
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
         tl.LogMessage("CanMoveAxis", "Get - " + Axis.ToString());
         switch (Axis)
         {
            case TelescopeAxes.axisPrimary: return false;
            case TelescopeAxes.axisSecondary: return false;
            case TelescopeAxes.axisTertiary: return false;
            default: throw new InvalidValueException("CanMoveAxis", Axis.ToString(), "0 to 2");
         }
      }

      public bool CanPark
      {
         get
         {
            tl.LogMessage("CanPark", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanPulseGuide
      {
         get
         {
            tl.LogMessage("CanPulseGuide", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanSetDeclinationRate
      {
         get
         {
            tl.LogMessage("CanSetDeclinationRate", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanSetGuideRates
      {
         get
         {
            tl.LogMessage("CanSetGuideRates", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanSetPark
      {
         get
         {
            tl.LogMessage("CanSetPark", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanSetPierSide
      {
         get
         {
            tl.LogMessage("CanSetPierSide", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanSetRightAscensionRate
      {
         get
         {
            tl.LogMessage("CanSetRightAscensionRate", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanSetTracking
      {
         get
         {
            tl.LogMessage("CanSetTracking", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanSlew
      {
         get
         {
            tl.LogMessage("CanSlew", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanSlewAltAz
      {
         get
         {
            tl.LogMessage("CanSlewAltAz", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanSlewAltAzAsync
      {
         get
         {
            tl.LogMessage("CanSlewAltAzAsync", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanSlewAsync
      {
         get
         {
            tl.LogMessage("CanSlewAsync", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanSync
      {
         get
         {
            tl.LogMessage("CanSync", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanSyncAltAz
      {
         get
         {
            tl.LogMessage("CanSyncAltAz", "Get - " + false.ToString());
            return false;
         }
      }

      public bool CanUnpark
      {
         get
         {
            tl.LogMessage("CanUnpark", "Get - " + false.ToString());
            return false;
         }
      }

      public double Declination
      {
         get
         {
            lock (_CurrentPosition)
            {
               RefreshCurrentPosition();
               double declination = _CurrentPosition.Equatorial.Declination.Value;
               tl.LogMessage("Declination", "Get - " + _AscomToolsCurrentPosition.Util.DegreesToDMS(declination, ":", ":"));
               return declination;
            }
         }
      }

      public double DeclinationRate
      {
         get
         {
            double declination = 0.0;
            tl.LogMessage("DeclinationRate", "Get - " + declination.ToString());
            return declination;
         }
         set
         {
            tl.LogMessage("DeclinationRate Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("DeclinationRate", true);
         }
      }

      public PierSide DestinationSideOfPier(double RightAscension, double Declination)
      {
         tl.LogMessage("DestinationSideOfPier Get", "Not implemented");
         throw new ASCOM.PropertyNotImplementedException("DestinationSideOfPier", false);
      }

      public bool DoesRefraction
      {
         get
         {
            tl.LogMessage("DoesRefraction Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("DoesRefraction", false);
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
            tl.LogMessage("GuideRateDeclination Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("GuideRateDeclination", false);
         }
         set
         {
            tl.LogMessage("GuideRateDeclination Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("GuideRateDeclination", true);
         }
      }

      public double GuideRateRightAscension
      {
         get
         {
            tl.LogMessage("GuideRateRightAscension Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("GuideRateRightAscension", false);
         }
         set
         {
            tl.LogMessage("GuideRateRightAscension Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("GuideRateRightAscension", true);
         }
      }

      public bool IsPulseGuiding
      {
         get
         {
            tl.LogMessage("IsPulseGuiding Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("IsPulseGuiding", false);
         }
      }

      public void MoveAxis(TelescopeAxes axis, double rate)
      {
         bool isRASlewing = false;
         bool isDecSlewing = false;
         double deltaMax = AstroEQController.MAX_SLEW_SPEED_DEGREES - rate;
         lock (_Controller)
         {
            System.Diagnostics.Debug.WriteLine(String.Format("MoveAxis({0}, {1})", axis, rate));
            if (AtPark)
            {
               throw new ASCOM.ParkedException("Method MoveAxis");
            }
            if (deltaMax < -0.000001)
            {
               throw new ASCOM.InvalidValueException("Method MoveAxis() rate exceed maximum allowed.");
            }
            LogMessage("MoveAxis", "({0}, {1})", axis, rate);

            switch (axis)
            {
               case TelescopeAxes.axisPrimary:
                  isRASlewing = (rate > 0);
                  _Controller.MCAxisSlew((AXISID)AxisId.Axis1_RA, rate);
                  break;
               case TelescopeAxes.axisSecondary:
                  isDecSlewing = (rate > 0);
                  _Controller.MCAxisSlew((AXISID)AxisId.Axis2_DEC, rate);
                  break;
               default:
                  throw new ASCOM.InvalidValueException("Tertiary axis is not supported by MoveAxis command");
            }
            _IsMoveAxisSlewing = (isRASlewing || isDecSlewing);
         }
      }

      public void Park()
      {
         LogMessage("Park", "Parking");
         _Controller.MCAxisSlewTo(AXISID.AXIS1, _CelestialPolePosition.ObservedAxes[0]);    // Target position in radians
         _Controller.MCAxisSlewTo(AXISID.AXIS2, _CelestialPolePosition.ObservedAxes[1]);    // Target position in radians
      }

      public void PulseGuide(GuideDirections Direction, int Duration)
      {
         tl.LogMessage("PulseGuide", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("PulseGuide");
      }

      public double RightAscension
      {
         get
         {
            lock (_CurrentPosition)
            {
               RefreshCurrentPosition();
               double rightAscension = _CurrentPosition.Equatorial.RightAscension.Value;
               tl.LogMessage("RightAscension", "Get - " + _AscomToolsCurrentPosition.Util.HoursToHMS(rightAscension));
               return rightAscension;
            }
         }
      }

      public double RightAscensionRate
      {
         get
         {
            double rightAscensionRate = 0.0;
            tl.LogMessage("RightAscensionRate", "Get - " + rightAscensionRate.ToString());
            return rightAscensionRate;
         }
         set
         {
            tl.LogMessage("RightAscensionRate Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("RightAscensionRate", true);
         }
      }

      public void SetPark()
      {
         tl.LogMessage("SetPark", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SetPark");
      }

      public PierSide SideOfPier
      {
         get
         {
            PierSide value = PierSide.pierUnknown;
            switch (Settings.AscomCompliance.SideOfPier)
            {
               case SideOfPierOption.Pointing:
                  value = SOP_Pointing(_CurrentPosition.ObservedAxes.DecAxis.Radians);
                  break;
               case SideOfPierOption.Physical:
                  value = SOP_Physical(_CurrentPosition.Equatorial.RightAscension);
                  break;
               case SideOfPierOption.None:
                  value = PierSide.pierUnknown;
                  break;
               case SideOfPierOption.V124g:
                  value = SOP_Dec(_CurrentPosition.ObservedAxes.DecAxis.Radians);
                  break;
            }
            tl.LogMessage("SideOfPier", "Get - " + value.ToString());
            return value;
         }
         set
         {
            tl.LogMessage("SideOfPier Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("SideOfPier", true);
         }
      }

      public double SiderealTime
      {
         get
         {
            lock (_CurrentPosition)
            {
               RefreshCurrentPosition();
               double lst = _CurrentPosition.LocalApparentSiderialTime.Value;
               tl.LogMessage("SiderealTime", "Get - " + _AscomToolsCurrentPosition.Util.HoursToHMS(lst));
               return lst;
            }
         }
      }

      public double SiteElevation
      {
         get
         {
            LogMessage("SiteElevation", "Get {0}", _Controller.ObservatoryElevation);
            return _Controller.ObservatoryElevation;
         }
         set
         {
            LogMessage("SiteElevation", "Set {0}", value);
            if (_Controller.ObservatoryElevation == value)
            {
               return;
            }
            _AscomToolsCurrentPosition.Transform.SiteElevation = value;
            _Controller.ObservatoryElevation = value;
            _CurrentPosition.Refresh(_AscomToolsCurrentPosition, DateTime.Now);
         }
      }

      public double SiteLatitude
      {
         get
         {
            LogMessage("SiteLatitude", "Get {0}", _Controller.ObservatoryLocation.Latitude.Value);
            return _Controller.ObservatoryLocation.Latitude.Value;
         }
         set
         {
            LogMessage("SiteLatitude", "Set {0}", value);
            if (_Controller.ObservatoryLocation.Latitude.Value == value)
            {
               return;
            }
            _AscomToolsCurrentPosition.Transform.SiteLatitude = value;
            _Controller.ObservatoryLocation.Latitude.Value = value;
            // See if the controller is at it's park position and if so set RA/Dec
            _CurrentPosition.Refresh(_AscomToolsCurrentPosition, DateTime.Now);
         }
      }


      public double SiteLongitude
      {
         get
         {
            LogMessage("SiteLongitude", "Get {0}", _Controller.ObservatoryLocation.Longitude.Value);
            return _Controller.ObservatoryLocation.Longitude.Value;
         }
         set
         {
            LogMessage("SiteLongitude", "Set {0}", value);
            if (_Controller.ObservatoryLocation.Longitude.Value == value)
            {
               return;
            }
            _AscomToolsCurrentPosition.Transform.SiteLongitude = value;
            _Controller.ObservatoryLocation.Longitude.Value = value;
            _CurrentPosition.Refresh(_AscomToolsCurrentPosition, DateTime.Now);
         }
      }

      private void InitialiseCurrentPosition()
      {
         DateTime now = DateTime.Now;
         AltAzCoordinate altAzPosition = new AltAzCoordinate(SiteLatitude, 0.0);
         AxisPosition axisPosition = new AxisPosition(_Controller.MCGetAxisPosition(AXISID.AXIS1)
                                    , _Controller.MCGetAxisPosition(AXISID.AXIS2));
         _CelestialPolePosition = new MountCoordinate(altAzPosition, axisPosition, _AscomToolsCelestialPole, now);
         _CurrentPosition = new MountCoordinate(altAzPosition, axisPosition, _AscomToolsCurrentPosition, now);
      }


      /// <summary>
      /// Refreshes the NCP and current positions if more than 500ms have elapsed since the last refresh.
      /// </summary>
      private void RefreshCurrentPosition()
      {
         DateTime now = DateTime.Now;
         //if (now - _CurrentPosition.SyncTime > new TimeSpan(0, 0, 0, 0, 500))
         //{
         // Refresh the Celestial pole position to update the RA and observed time.
         _CelestialPolePosition.Refresh(_AscomToolsCelestialPole, now);
         // Get the axis positions from the controller/hardware.
         AxisPosition axisPosition = new AxisPosition(_Controller.MCGetAxisPosition(AXISID.AXIS1)
                                    , _Controller.MCGetAxisPosition(AXISID.AXIS2));
         // Calculate change in axis angle
         AxisPosition delta = axisPosition - _CelestialPolePosition.ObservedAxes;
         // Great equatorial difference
         EquatorialCoordinate deltaCoordinate = new EquatorialCoordinate(HourAngle.RadiansToHours(axisPosition[0]), Angle.RadiansToDegrees(axisPosition[1]));
         // Determine RA/Dec by adding the difference to the current celestial pole RA/Dec
         EquatorialCoordinate newPosition = _CelestialPolePosition.Equatorial + deltaCoordinate;
         _CurrentPosition = new MountCoordinate(newPosition, axisPosition, _AscomToolsCurrentPosition, now);
         // Check if slew finished
         if (_IsSlewing && (axisPosition == _TargetPosition.ObservedAxes))
         {
            _IsSlewing = false;
         }
         //}
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

      public void SlewToCoordinates(double RightAscension, double Declination)
      {
         LogMessage("SlewToCoordinates", "RA:{0}/Dec:{1}", _AscomToolsCurrentPosition.Util.HoursToHMS(RightAscension, "h", "m", "s"), _AscomToolsCurrentPosition.Util.DegreesToDMS(Declination, ":", ":"));
         EquatorialCoordinate target = new EquatorialCoordinate(RightAscension, Declination);
         RefreshCurrentPosition();
         AxisPosition targetAxisPosition = _CelestialPolePosition.Equatorial.GetRelativeAxisPositionOf(target);
         _IsSlewing = true;
         _TargetPosition = new MountCoordinate(target, targetAxisPosition, _AscomToolsTargetPosition, _CelestialPolePosition.SyncTime);
         System.Diagnostics.Debug.WriteLine($"Slew RA to { targetAxisPosition.RAAxis.Degrees } degrees");
         System.Diagnostics.Debug.WriteLine($"Slew Dec to { targetAxisPosition.DecAxis.Degrees } degrees");

         _Controller.MCAxisSlewTo(AXISID.AXIS1, targetAxisPosition[0]);    // Target position in radians
         _Controller.MCAxisSlewTo(AXISID.AXIS2, targetAxisPosition[1]);    // Target position in radians

      }

      public void SlewToCoordinatesAsync(double RightAscension, double Declination)
      {
         tl.LogMessage("SlewToCoordinatesAsync", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SlewToCoordinatesAsync");
      }

      public void SlewToTarget()
      {
         tl.LogMessage("SlewToTarget", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SlewToTarget");
      }

      public void SlewToTargetAsync()
      {
         tl.LogMessage("SlewToTargetAsync", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SlewToTargetAsync");
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

      public void SyncToCoordinates(double RightAscension, double Declination)
      {
         tl.LogMessage("SyncToCoordinates", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SyncToCoordinates");
      }

      public void SyncToTarget()
      {
         tl.LogMessage("SyncToTarget", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SyncToTarget");
      }

      public double TargetDeclination
      {
         get
         {
            tl.LogMessage("TargetDeclination Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("TargetDeclination", false);
         }
         set
         {
            tl.LogMessage("TargetDeclination Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("TargetDeclination", true);
         }
      }

      public double TargetRightAscension
      {
         get
         {
            tl.LogMessage("TargetRightAscension Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("TargetRightAscension", false);
         }
         set
         {
            tl.LogMessage("TargetRightAscension Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("TargetRightAscension", true);
         }
      }

      public bool Tracking
      {
         get
         {
            bool tracking = true;
            tl.LogMessage("Tracking", "Get - " + tracking.ToString());
            return tracking;
         }
         set
         {
            tl.LogMessage("Tracking Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("Tracking", true);
         }
      }

      public DriveRates TrackingRate
      {
         get
         {
            tl.LogMessage("TrackingRate Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("TrackingRate", false);
         }
         set
         {
            tl.LogMessage("TrackingRate Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("TrackingRate", true);
         }
      }

      public ITrackingRates TrackingRates
      {
         get
         {
            ITrackingRates trackingRates = new TrackingRates();
            tl.LogMessage("TrackingRates", "Get - ");
            foreach (DriveRates driveRate in trackingRates)
            {
               tl.LogMessage("TrackingRates", "Get - " + driveRate.ToString());
            }
            return trackingRates;
         }
      }

      public DateTime UTCDate
      {
         get
         {
            DateTime utcDate = DateTime.UtcNow;
            tl.LogMessage("UTCDate", "Get - " + String.Format("MM/dd/yy HH:mm:ss", utcDate));
            return utcDate;
         }
         set
         {
            tl.LogMessage("UTCDate Set", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("UTCDate", true);
         }
      }

      public void Unpark()
      {
         tl.LogMessage("Unpark", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("Unpark");
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



      /// <summary>
      /// Log helper function that takes formatted strings and arguments
      /// </summary>
      /// <param name="identifier"></param>
      /// <param name="message"></param>
      /// <param name="args"></param>
      internal static void LogMessage(string identifier, string message, params object[] args)
      {
         var msg = string.Format(message, args);
         tl.LogMessage(identifier, msg);
      }
      #endregion
   }
}
