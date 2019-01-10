using ASCOM.Astrometry.Transform;
using ASCOM.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
         set
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

      /// <summary>
      /// Initialise a mount coordinate with Ra/Dec strings 
      /// </summary>
      /// <param name="ra">A right ascension string</param>
      /// <param name="dec">declination string</param>
      /// <param name="localTime">The local time of the observation</param>
      public MountCoordinate(string ra, string dec):this(new EquatorialCoordinate(ra, dec))
      {
         _MasterCoordinate = MasterCoordinateEnum.Equatorial;
      }

      /// <summary>
      /// Simple initialisation with an equatorial coordinate
      /// </summary>
      public MountCoordinate(EquatorialCoordinate equatorial) 
      {
         _Equatorial = equatorial;
         _MasterCoordinate = MasterCoordinateEnum.Equatorial;
      }

      /// <summary>
      /// Simple initialisation with an altAzimuth coordinate
      /// </summary>
      public MountCoordinate(AltAzCoordinate altAz)
      {
         _AltAzimuth = altAz;
         _MasterCoordinate = MasterCoordinateEnum.AltAzimuth;
      }

      /// <summary>
      /// Initialisation with an equatorial coordinate, a transform instance and the local julian time (corrected)
      /// which then means that the AltAzimunth at the time is available.
      /// </summary>
      public MountCoordinate(EquatorialCoordinate equatorial, Transform transform, DateTime localTime):this(equatorial)
      {
         _AltAzimuth = this.GetAltAzimuth(transform, localTime);
      }

      /// <summary>
      /// Initialise a mount coordinate with Ra/Dec strings and axis positions in radians.
      /// </summary>
      /// <param name="altAz">The AltAzimuth coordinate for the mount</param>
      /// <param name="suggested">The suggested position for the axes (e.g. via a star catalogue lookup)</param>
      /// <param name="localTime">The local time of the observation</param>
      public MountCoordinate(string ra, string dec, AxisPosition axisPosition, Transform transform, DateTime localTime) : this(new EquatorialCoordinate(ra, dec))
      {
         _SyncTime = localTime;
         _Equatorial = new EquatorialCoordinate(ra, dec);
         _AltAzimuth = this.GetAltAzimuth(transform, localTime);
         _AxesPosition = axisPosition;
         _MasterCoordinate = MasterCoordinateEnum.Equatorial;
      }

      /// <summary>
      /// Initialisation with an equatorial coordinate, a transform instance and the local julian time (corrected)
      /// which then means that the AltAzimunth at the time is available.
      /// </summary>
      public MountCoordinate(EquatorialCoordinate equatorial, AxisPosition axisPosition, Transform transform, DateTime localTime) : this(equatorial)
      {
         _AltAzimuth = this.GetAltAzimuth(transform, localTime);
         _AxesPosition = axisPosition;
         _SyncTime = localTime;

      }

      /// <summary>
      /// Initialisation with an equatorial coordinate, a transform instance and the local julian time (corrected)
      /// which then means that the AltAzimunth at the time is available.
      /// </summary>
      public MountCoordinate(AltAzCoordinate altAz, Transform transform, DateTime localTime) : this(altAz)
      {
         _Equatorial = this.GetEquatorial(transform, localTime);
      }

      public MountCoordinate(AltAzCoordinate altAz, AxisPosition axisPosition, Transform transform, DateTime localTime) : this(altAz)
      {
         _Equatorial = this.GetEquatorial(transform, localTime);
         _AxesPosition = axisPosition;
         _SyncTime = localTime;
      }



      /// <summary>
      /// Returns the AltAzimuth coordinate for the equatorial using the values
      /// currently set in the passed Transform instance.
      /// </summary>
      /// <param name="transform"></param>
      /// <returns></returns>
      public AltAzCoordinate GetAltAzimuth(Transform transform)
      {
         transform.SetTopocentric(_Equatorial.RightAscension, _Equatorial.Declination);
         transform.Refresh();
         AltAzCoordinate coord = new AltAzCoordinate(transform.ElevationTopocentric, transform.AzimuthTopocentric);
         return coord;
      }

      /// <summary>
      /// Returns the AltAzimuth coordinate for the equatorial using the values
      /// currently set in the passed Transform instance.
      /// </summary>
      /// <param name="transform"></param>
      /// <returns></returns>
      public AltAzCoordinate GetAltAzimuth(Transform transform, DateTime currentTime)
      {
         transform.JulianDateUTC = AstroConvert.DateLocalToJulian(currentTime);
         transform.SetTopocentric(_Equatorial.RightAscension, _Equatorial.Declination);
         // transform.Refresh();
         AltAzCoordinate coord = new AltAzCoordinate(transform.ElevationTopocentric, transform.AzimuthTopocentric);
         return coord;
      }

      /// <summary>
      /// Returns the RADec coordinate for the observed AltAzimuth using the values
      /// currently set in the passed Transform instance.
      /// </summary>
      /// <param name="transform"></param>
      /// <returns></returns>
      public EquatorialCoordinate GetEquatorial(Transform transform, DateTime currentTime)
      {
         transform.JulianDateUTC = AstroConvert.JulianDateUTC(currentTime);
         transform.SetAzimuthElevation(_AltAzimuth.Azimuth, _AltAzimuth.Altitude);
         transform.Refresh();
         EquatorialCoordinate coord = new EquatorialCoordinate(transform.RATopocentric, transform.DECTopocentric);
         return coord;
      }


      public void SetObservedAxis(AxisPosition axisPosition, DateTime observationTime)
      {
         _AxesPosition = axisPosition;
         _SyncTime = observationTime;
      }

      public void Refresh(Transform transform, double julianDateUTC)
      {
         transform.JulianDateUTC = julianDateUTC;
         if (_MasterCoordinate == MasterCoordinateEnum.Equatorial) {
            // Update the AltAzimuth
            transform.SetTopocentric(_Equatorial.RightAscension, _Equatorial.Declination);
            transform.Refresh();
            _AltAzimuth = new AltAzCoordinate(transform.ElevationTopocentric, transform.AzimuthTopocentric);
         }
         else {
            // Update the Equatorial
            transform.SetAzimuthElevation(_AltAzimuth.Azimuth, _AltAzimuth.Altitude);
            transform.Refresh();
            _Equatorial = new EquatorialCoordinate(transform.RATopocentric, transform.DECTopocentric);
         }
      }

      public void Refresh(EquatorialCoordinate equatorial, AxisPosition axisPosition, Transform transform, DateTime localTime)
      {
         _Equatorial = equatorial;
         _AxesPosition = axisPosition;
         _SyncTime = localTime;
         _AltAzimuth = this.GetAltAzimuth(transform, localTime);
         _MasterCoordinate = MasterCoordinateEnum.Equatorial;

      }

      public void Refresh(AltAzCoordinate altAz, AxisPosition axisPosition, Transform transform, DateTime localTime)
      {
         _AltAzimuth = altAz;
         _AxesPosition = axisPosition;
         _SyncTime = localTime;
         _Equatorial = this.GetEquatorial(transform, localTime);
         _MasterCoordinate = MasterCoordinateEnum.AltAzimuth;
      }

   }

}
