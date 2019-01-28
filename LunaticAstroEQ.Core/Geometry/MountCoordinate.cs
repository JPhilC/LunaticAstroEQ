using ASCOM.DeviceInterface;
using System;

namespace ASCOM.LunaticAstroEQ.Core.Geometry
{

   /// <summary>
   /// A class to tie together an equatorial coordinate, 
   /// calculated/theoretical mount axis positions at a given time
   /// and optionally the actual observered axis positions.
   /// </summary>
   public class MountCoordinate
   {
      public enum MasterCoordinateEnum
      {
         Equatorial,
         AltAzimuth
      }

      private EquatorialCoordinate _Equatorial;
      private AltAzCoordinate _AltAzimuth;
      private AxisPosition _AxesPosition;
      private DateTime _SyncTime;
      private HourAngle _LocalApparentSiderialTime;

      /// <summary>
      /// Held for reference so that when a refresh is requested we know which coordinate
      /// is the master.
      /// </summary>
      private MasterCoordinateEnum _MasterCoordinate;

      public MasterCoordinateEnum MasterCoordinate
      {
         get
         {
            return _MasterCoordinate;
         }
         private set
         {
            _MasterCoordinate = value;
         }
      }

      public EquatorialCoordinate Equatorial
      {
         get
         {
            return _Equatorial;
         }
         private set
         {
            _Equatorial = value;
         }
      }

      public AltAzCoordinate AltAzimuth
      {
         get
         {
            return _AltAzimuth;
         }
         private set
         {
            _AltAzimuth = value;
         }
      }


      public AxisPosition ObservedAxes
      {
         get
         {
            return _AxesPosition;
         }
         private set
         {
            _AxesPosition = value;
         }
      }


      /// <summary>
      /// The last time everything was syncronised 
      /// </summary>
      public DateTime SyncTime
      {
         get
         {
            return _SyncTime;
         }
         private set
         {
            _SyncTime = value;
         }
      }


      public HourAngle LocalApparentSiderialTime
      {
         get
         {
            return _LocalApparentSiderialTime;
         }
         private set
         {
            _LocalApparentSiderialTime = value;
         }
      }

      /// <summary>
      /// Returns the pointing side of Pier as required by ASCOM
      /// </summary>
      public PierSide PointingSideOfPier
      {
         get
         {
            if (AltAzimuth != null)
            {
               if (AltAzimuth.Azimuth > 180.0)
               {
                  return PierSide.pierEast;
               }
               else
               {
                  return PierSide.pierWest;
               }
            }
            else
            {
               return PierSide.pierUnknown;
            }
         }
      }

      /// <summary>
      /// Returns which side of the pier the Dec axis would be pointing if
      /// if the RA axis were at the 12-o-clock
      /// </summary>
      public PierSide PhysicalSideOfPier
      {
         get
         {
            if (Equatorial != null)
            {
               return GetPhysicalSideOfPier(ObservedAxes[0]);
            }
            else
            {
               return PierSide.pierUnknown;
            }
         }
      }


      /// <summary>
      /// Initialise a mount coordinate with Ra/Dec strings 
      /// </summary>
      /// <param name="ra">A right ascension string</param>
      /// <param name="dec">declination string</param>
      /// <param name="localTime">The local time of the observation</param>
      public MountCoordinate(string ra, string dec) : this(new EquatorialCoordinate(ra, dec))
      {
         _MasterCoordinate = MasterCoordinateEnum.Equatorial;
      }

      /// <summary>
      /// Initialise a mount coordinate with Ra/Dec strings 
      /// </summary>
      /// <param name="ra">A right ascension string</param>
      /// <param name="dec">declination string</param>
      /// <param name="localTime">The local time of the observation</param>
      public MountCoordinate(string ra, string dec, AltAzCoordinate altAz) : this(new EquatorialCoordinate(ra, dec))
      {
         _MasterCoordinate = MasterCoordinateEnum.Equatorial;
         AltAzimuth = altAz;
      }

      /// <summary>
      /// Simple initialisation with an equatorial coordinate
      /// </summary>
      private MountCoordinate(EquatorialCoordinate equatorial)
      {
         _Equatorial = equatorial;
         _MasterCoordinate = MasterCoordinateEnum.Equatorial;
      }

      /// <summary>
      /// Simple initialisation with an altAzimuth coordinate
      /// </summary>
      private MountCoordinate(AltAzCoordinate altAz)
      {
         _AltAzimuth = altAz;
         _MasterCoordinate = MasterCoordinateEnum.AltAzimuth;
      }

      /// <summary>
      /// Initialisation with an equatorial coordinate, a transform instance using the current time
      /// </summary>
      public MountCoordinate(EquatorialCoordinate equatorial, AscomTools tools) : this(equatorial, tools, DateTime.Now)
      {
      }

      /// <summary>
      /// Initialisation with an equatorial coordinate, a transform instance and the local time
      /// which then means that the AltAzimunth at the time is available.
      /// </summary>
      public MountCoordinate(EquatorialCoordinate equatorial, AscomTools tools, DateTime localTime) : this(equatorial)
      {
         this.UpdateAltAzimuth(tools, localTime);
      }

      /// <summary>
      /// Initialise a mount coordinate with Ra/Dec strings and axis positions in radians.
      /// </summary>
      /// <param name="altAz">The AltAzimuth coordinate for the mount</param>
      /// <param name="suggested">The suggested position for the axes (e.g. via a star catalogue lookup)</param>
      /// <param name="localTime">The local time of the observation</param>
      public MountCoordinate(string ra, string dec, AxisPosition axisPosition, AscomTools tools, DateTime currentTime) : this(new EquatorialCoordinate(ra, dec))
      {
         _AxesPosition = axisPosition;
         Equatorial = new EquatorialCoordinate(ra, dec);
         this.UpdateAltAzimuth(tools, currentTime);
         _MasterCoordinate = MasterCoordinateEnum.Equatorial;
      }

      /// <summary>
      /// Initialisation with an equatorial coordinate, a transform instance and the local julian time (corrected)
      /// which then means that the AltAzimunth at the time is available.
      /// </summary>
      public MountCoordinate(EquatorialCoordinate equatorial, AxisPosition axisPosition, AscomTools tools, DateTime currentTime) : this(equatorial)
      {
         _AxesPosition = axisPosition;
         this.UpdateAltAzimuth(tools, currentTime);

      }

      /// <summary>
      /// Initialisation with an equatorial coordinate, a transform instance the current time
      /// </summary>
      public MountCoordinate(AltAzCoordinate altAz, AscomTools tools) : this(altAz, tools, DateTime.Now)
      {
      }

      /// <summary>
      /// Initialisation with an equatorial coordinate, a transform instance and the local time
      /// which then means that the AltAzimunth at the time is available.
      /// </summary>
      public MountCoordinate(AltAzCoordinate altAz, AscomTools tools, DateTime localTime) : this(altAz)
      {
         this.UpdateEquatorial(tools, localTime);
      }


      /// <summary>
      ///  Use to initialise a mount coordinate to an AltAz with an option force 0-360 value for the declination.
      ///  Used when initialising the Celestial pole coordinate.
      /// </summary>
      /// <param name="altAz"></param>
      /// <param name="axisPosition"></param>
      /// <param name="tools"></param>
      /// <param name="currentTime"></param>
      /// <param name="forcedDecValue"></param>
      public MountCoordinate(AltAzCoordinate altAz, AxisPosition axisPosition, AscomTools tools, DateTime currentTime) : this(altAz)
      {
         _AxesPosition = axisPosition;
         this.UpdateEquatorial(tools, currentTime);
      }



      /// <summary>
      /// Returns the AltAzimuth coordinate for the equatorial using the values
      /// currently set in the passed AscomTools instance.
      /// </summary>
      /// <param name="transform"></param>
      /// <returns></returns>
      public AltAzCoordinate GetAltAzimuth(AscomTools tools)
      {
         tools.Transform.SetTopocentric(_Equatorial.RightAscension, _Equatorial.Declination);
         //tools.Transform.Refresh();
         AltAzCoordinate coord = new AltAzCoordinate(tools.Transform.ElevationTopocentric, AstroConvert.RangeAzimuth(tools.Transform.AzimuthTopocentric));
         return coord;
      }

      /// <summary>
      /// Returns the AltAzimuth coordinate for the equatorial using the values
      /// currently set in the passed AscomTools instance.
      /// </summary>
      /// <param name="transform"></param>
      /// <returns></returns>
      public AltAzCoordinate UpdateAltAzimuth(AscomTools tools, DateTime currentTime)
      {
         _SyncTime = currentTime;
         _LocalApparentSiderialTime = new HourAngle(AstroConvert.LocalApparentSiderealTime(tools.Transform.SiteLongitude, currentTime));
         tools.Transform.JulianDateTT = tools.Util.DateLocalToJulian(currentTime);
         tools.Transform.SetTopocentric(_Equatorial.RightAscension, _Equatorial.Declination);
         AltAzimuth = new AltAzCoordinate(tools.Transform.ElevationTopocentric, AstroConvert.RangeAzimuth(tools.Transform.AzimuthTopocentric));
         return AltAzimuth;
      }

      /// <summary>
      /// Returns the RADec coordinate for the observed AltAzimuth using the values
      /// currently set in the passed AscomTools instance. Also sets the stored Equatorial
      /// </summary>
      /// <param name="transform"></param>
      /// <returns></returns>
      public EquatorialCoordinate UpdateEquatorial(AscomTools tools, DateTime currentTime)
      {
         _SyncTime = currentTime;
         _LocalApparentSiderialTime = new HourAngle(AstroConvert.LocalApparentSiderealTime(tools.Transform.SiteLongitude, currentTime));
         tools.Transform.JulianDateTT = tools.Util.DateLocalToJulian(currentTime);
         tools.Transform.SetAzimuthElevation(_AltAzimuth.Azimuth, _AltAzimuth.Altitude);
         double ra = tools.Transform.RATopocentric;
         double declination = tools.Transform.DECTopocentric;
         double decAxisValue = AstroConvert.DecTo360(declination, tools.Transform.SiteLatitude, ObservedAxes.RAAxis, _AltAzimuth.Altitude, _AltAzimuth.Azimuth);
         _Equatorial = new EquatorialCoordinate(tools.Transform.RATopocentric, decAxisValue);
         return _Equatorial;
      }



      public void Refresh(AscomTools tools, DateTime currentTime)
      {
         _SyncTime = currentTime;
         _LocalApparentSiderialTime = new HourAngle(AstroConvert.LocalApparentSiderealTime(tools.Transform.SiteLongitude, currentTime));
         tools.Transform.JulianDateTT = tools.Util.DateLocalToJulian(currentTime);
         if (_MasterCoordinate == MasterCoordinateEnum.Equatorial)
         {
            // Update the AltAzimuth
            tools.Transform.SetTopocentric(_Equatorial.RightAscension.Value, _Equatorial.Declination.Value);
            //tools.Transform.Refresh();
            this.AltAzimuth = new AltAzCoordinate(tools.Transform.ElevationTopocentric, tools.Transform.AzimuthTopocentric);
         }
         else
         {
            // Update the Equatorial
            tools.Transform.SetAzimuthElevation(_AltAzimuth.Azimuth.Value, _AltAzimuth.Altitude.Value);
            double declination = tools.Transform.DECTopocentric;
            double decAxisValue = AstroConvert.DecTo360(declination, tools.Transform.SiteLatitude, ObservedAxes.RAAxis, _AltAzimuth.Altitude, _AltAzimuth.Azimuth);
            this.Equatorial = new EquatorialCoordinate(tools.Transform.RATopocentric, decAxisValue);
         }
      }

      public void Refresh(EquatorialCoordinate equatorial, AxisPosition axisPosition, AscomTools tools, DateTime currentTime)
      {
         _Equatorial = equatorial;
         _AxesPosition = axisPosition;
         this.UpdateAltAzimuth(tools, currentTime);
         _MasterCoordinate = MasterCoordinateEnum.Equatorial;

      }

      public void Refresh(AltAzCoordinate altAz, AxisPosition axisPosition, AscomTools tools, DateTime currentTime)
      {
         _AltAzimuth = altAz;
         _AxesPosition = axisPosition;
         this.UpdateEquatorial(tools, currentTime);
         _MasterCoordinate = MasterCoordinateEnum.AltAzimuth;
      }


      public double[] GetSlewAnglesTo(EquatorialCoordinate target)
      {
         double[] slewAngles = new double[] { 0.0D, 0.0D };
         double[] deltaAxis = this.Equatorial.GetAxisOffsetTo(target);
         // Get the desired final axis position
         AxisPosition finalAxisPosition = this.ObservedAxes.RotateBy(deltaAxis);
         // Get the SAFE (through the pole) angles to slew.
         slewAngles = this.ObservedAxes.GetSlewAnglesTo(finalAxisPosition);
         return slewAngles;
      }

      public void MoveToAxisPosition(AxisPosition axisPosition, AscomTools tools, DateTime currentTime)
      {
         double[] delta = ObservedAxes.GetDeltaTo(axisPosition);
         if (_MasterCoordinate == MasterCoordinateEnum.Equatorial)
         {
            //// Work out if the RA needs to change as DEC passes through the pole
            AxisPosition nextPosition = this.ObservedAxes.RotateBy(delta);
            _SyncTime = currentTime;
            _LocalApparentSiderialTime = new HourAngle(AstroConvert.LocalApparentSiderealTime(tools.Transform.SiteLongitude, currentTime));
            double newRA = _LocalApparentSiderialTime + 12.0 + HourAngle.DegreesToHours(nextPosition[0]);
            _Equatorial = new EquatorialCoordinate(HourAngle.Range24(newRA), _Equatorial.DeclinationAxis.Value + delta[1]);
            _AxesPosition = nextPosition;
            tools.Transform.SetTopocentric(_Equatorial.RightAscension.Value, _Equatorial.Declination.Value);
            //tools.Transform.Refresh();
            this.AltAzimuth = new AltAzCoordinate(tools.Transform.ElevationTopocentric, tools.Transform.AzimuthTopocentric);
         }
         else
         {
            throw new NotImplementedException();
         }
      }


            /// <summary>
      /// 
      /// </summary>
      /// <param name="RaAxisPosition">DEC axis position in degrees</param>
      /// <returns></returns>
      private PierSide GetPhysicalSideOfPier(double raAxisPosition)
      {
         // Fudge to work around proble caused by un-initised doubles
         return (raAxisPosition >= 0.0 && raAxisPosition <= 180.0) ? PierSide.pierEast : PierSide.pierWest;
      }

   }

}
