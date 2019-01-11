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
   public partial class Telescope : ITelescopeV3
   // public partial class Telescope : ReferenceCountedObjectBase, ITelescopeV3
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
      /// Private variable to hold the connected state
      /// </summary>
      private bool connectedState;

      /// <summary>
      /// Private variable to hold an ASCOM Utilities object
      /// </summary>
      private Util utilities;

      /// <summary>
      /// Private variable to hold an ASCOM AstroUtilities object to provide the Range method
      /// </summary>
      private AstroUtils astroUtilities;

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

      internal TelescopeSettings Settings
      {
         get
         {
            return TelescopeSettingsProvider.Current.Settings;
         }
      }


      /// <summary>
      /// Initializes a new instance of the <see cref="LunaticAstroEQ"/> class.
      /// Must be public for COM registration.
      /// </summary>
      public Telescope()
      {
         driverID = Marshal.GenerateProgIdForType(this.GetType());
         driverDescription = GetDriverDescription();

         _Controller = AstroEQController.Instance;

         tl = new TraceLogger("", "LunaticAstroEQ");
         tl.Enabled = Settings.TracingState; // This will also load the settings as it is the first time it is accessed.

         tl.LogMessage("Telescope", "Starting initialisation");

         connectedState = false; // Initialise connected to false
         utilities = new Util(); //Initialise util object
         astroUtilities = new AstroUtils(); // Initialise astro utilities object
                                            //TODO: Implement your additional construction here

         tl.LogMessage("Telescope", "Completed initialisation");
      }

      private string GetDriverDescription()
      {
         string descr;
         ServedClassNameAttribute attr = this.GetType().GetCustomAttributes(typeof(ServedClassNameAttribute), true).FirstOrDefault() as ServedClassNameAttribute;
         if (attr != null)
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
            return new ArrayList();
         }
      }

      public string Action(string actionName, string actionParameters)
      {
         LogMessage("", "Action {0}, parameters {1} not implemented", actionName, actionParameters);
         throw new ASCOM.ActionNotImplementedException("Action " + actionName + " is not implemented by this driver");
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

      public bool CommandBool(string command, bool raw)
      {
         CheckConnected("CommandBool");
         string ret = CommandString(command, raw);
         // TODO decode the return string and return true or false
         // or
         throw new ASCOM.MethodNotImplementedException("CommandBool");
         // DO NOT have both these sections!  One or the other
      }

      public string CommandString(string command, bool raw)
      {
         CheckConnected("CommandString");
         // it's a good idea to put all the low level communication with the device here,
         // then all communication calls this function
         // you need something to ensure that only one command is in progress at a time

         throw new ASCOM.MethodNotImplementedException("CommandString");
      }

      public void Dispose()
      {
         // Clean up the tracelogger and util objects
         tl.Enabled = false;
         tl.Dispose();
         tl = null;
         utilities.Dispose();
         utilities = null;
         astroUtilities.Dispose();
         astroUtilities = null;
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
               connectedState = true;
               LogMessage("Connected Set", "Connecting to port {0}", Settings.COMPort);
               int connectionResult = _Controller.Connect(Settings.COMPort, (int)Settings.BaudRate, (int)Settings.Timeout, (int)Settings.Retry);
               if (connectionResult == Core.Constants.MOUNT_SUCCESS)
               {
                  connectedState = true;
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
               connectedState = false;
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
            tl.LogMessage("Altitude", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("Altitude", false);
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
            tl.LogMessage("AtPark", "Get - " + false.ToString());
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
            tl.LogMessage("Azimuth Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("Azimuth", false);
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
            double declination = 0.0;
            tl.LogMessage("Declination", "Get - " + utilities.DegreesToDMS(declination, ":", ":"));
            return declination;
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

      public void MoveAxis(TelescopeAxes Axis, double Rate)
      {
         tl.LogMessage("MoveAxis", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("MoveAxis");
      }

      public void Park()
      {
         tl.LogMessage("Park", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("Park");
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
            double rightAscension = 0.0;
            tl.LogMessage("RightAscension", "Get - " + utilities.HoursToHMS(rightAscension));
            return rightAscension;
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
            tl.LogMessage("SideOfPier Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("SideOfPier", false);
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
            // Now using NOVAS 3.1
            double siderealTime = 0.0;
            using (var novas = new ASCOM.Astrometry.NOVAS.NOVAS31())
            {
               var jd = utilities.DateUTCToJulian(DateTime.UtcNow);
               novas.SiderealTime(jd, 0, novas.DeltaT(jd),
                   ASCOM.Astrometry.GstType.GreenwichApparentSiderealTime,
                   ASCOM.Astrometry.Method.EquinoxBased,
                   ASCOM.Astrometry.Accuracy.Reduced, ref siderealTime);
            }

            // Allow for the longitude
            siderealTime += SiteLongitude / 360.0 * 24.0;

            // Reduce to the range 0 to 24 hours
            siderealTime = astroUtilities.ConditionRA(siderealTime);

            tl.LogMessage("SiderealTime", "Get - " + siderealTime.ToString());
            return siderealTime;
         }
      }

      public double SiteElevation
      {
         get
         {
            if (!Settings.SiteElevation.HasValue)
            {
               throw new ASCOM.ValueNotSetException("SiteElevation");
            }
            LogMessage("SiteElevation", "Get {0}", Settings.SiteElevation.Value);
            return Settings.SiteElevation.Value;
         }
         set
         {
            LogMessage("SiteElevation", "Set {0}", value);
            if (Settings.SiteElevation.HasValue && Settings.SiteElevation.Value == value)
            {
               return;
            }
            Settings.SiteElevation = value;
         }
      }

      public double SiteLatitude
      {
         get
         {
            if (!Settings.SiteLatitude.HasValue)
            {
               throw new ASCOM.ValueNotSetException("SiteLatitude");
            }
            LogMessage("SiteLatitude", "Get {0}", Settings.SiteLatitude.Value);
            return Settings.SiteLatitude.Value;
         }
         set
         {
            LogMessage("SiteLatitud", "Set {0}", value);
            if (Settings.SiteLatitude.HasValue && Settings.SiteLatitude.Value == value)
            {
               return;
            }
            Settings.SiteLatitude = value;
            Settings.StartAltitude = Settings.SiteLatitude.Value;
            TelescopeSettingsProvider.Current.SaveSettings();
         }
      }

      public double SiteLongitude
      {
         get
         {
            if (!Settings.SiteLongitude.HasValue)
            {
               throw new ASCOM.ValueNotSetException("SiteLongitude");
            }
            LogMessage("SiteLongitude", "Get {0}", Settings.SiteLongitude.Value);
            return Settings.SiteLongitude.Value;
         }
         set
         {
            LogMessage("SiteLongitude", "Set {0}", value);
            if (Settings.SiteLongitude.HasValue && Settings.SiteLongitude.Value == value)
            {
               return;
            }
            Settings.SiteLongitude = value;
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

      public void SlewToCoordinates(double RightAscension, double Declination)
      {
         tl.LogMessage("SlewToCoordinates", "Not implemented");
         throw new ASCOM.MethodNotImplementedException("SlewToCoordinates");
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

      public bool Slewing
      {
         get
         {
            tl.LogMessage("Slewing Get", "Not implemented");
            throw new ASCOM.PropertyNotImplementedException("Slewing", false);
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
