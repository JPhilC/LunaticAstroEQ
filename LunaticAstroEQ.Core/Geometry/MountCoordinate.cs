using ASCOM.DeviceInterface;
using System;
using System.Text;

namespace ASCOM.LunaticAstroEQ.Core.Geometry
{

   /// <summary>
   /// A class to tie together an equatorial coordinate, 
   /// calculated/theoretical mount axis positions at a given time
   /// and optionally the actual observered axis positions.
   /// </summary>
   public class MountCoordinate
   {
      public bool ForceMeridianFlip { get; set; } = false;


      public HemisphereOption Hemisphere { get; private set; } = HemisphereOption.Northern;


      public EquatorialCoordinate Equatorial { get; private set; }

      public AltAzCoordinate AltAzimuth { get; private set; }


      public AxisPosition ObservedAxes { get; private set; }


      /// <summary>
      /// The last time everything was syncronised 
      /// </summary>
      public DateTime SyncTime { get; private set; }


      public HourAngle LocalApparentSiderialTime { get; private set; }

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


      public MountCoordinate(EquatorialCoordinate equatorial, AxisPosition axisPosition, AscomTools tools, DateTime syncTime)
      {
         ObservedAxes = axisPosition;
         SyncTime = syncTime;
         LocalApparentSiderialTime = new HourAngle(AstroConvert.LocalApparentSiderealTime(tools.Transform.SiteLongitude, syncTime));
         if (tools.Transform.SiteLatitude < 0.0)
         {
            Hemisphere = HemisphereOption.Southern;
         }
         Equatorial = equatorial;
         this.UpdateAltAzimuth(tools, syncTime);
      }

      public MountCoordinate(AltAzCoordinate altAz, AxisPosition axisPosition, AscomTools tools, DateTime syncTime)
      {
         ObservedAxes = axisPosition;
         SyncTime = syncTime;
         AltAzimuth = altAz;
         LocalApparentSiderialTime = new HourAngle(AstroConvert.LocalApparentSiderealTime(tools.Transform.SiteLongitude, syncTime));
         if (tools.Transform.SiteLatitude < 0.0)
         {
            Hemisphere = HemisphereOption.Southern;
         }
         AltAzimuth = altAz;
         this.UpdateEquatorial(tools, syncTime);
      }




      /// <summary>
      /// Returns the AltAzimuth coordinate for the equatorial using the values
      /// currently set in the passed AscomTools instance.
      /// </summary>
      /// <param name="transform"></param>
      /// <returns></returns>
      public void UpdateAltAzimuth(AscomTools tools, DateTime syncTime)
      {
         tools.Transform.JulianDateTT = tools.Util.DateLocalToJulian(syncTime);
         tools.Transform.SetTopocentric(Equatorial.RightAscension, Equatorial.Declination);
         AltAzimuth = new AltAzCoordinate(tools.Transform.ElevationTopocentric, AstroConvert.RangeAzimuth(tools.Transform.AzimuthTopocentric));
      }

      /// <summary>
      /// Returns the RADec coordinate for the observed AltAzimuth using the values
      /// currently set in the passed AscomTools instance. Also sets the stored Equatorial
      /// </summary>
      /// <param name="transform"></param>
      /// <returns></returns>
      public void UpdateEquatorial(AscomTools tools, DateTime syncTime)
      {
         tools.Transform.JulianDateTT = tools.Util.DateLocalToJulian(syncTime);
         tools.Transform.SetAzimuthElevation(AltAzimuth.Azimuth, AltAzimuth.Altitude);
         Equatorial = new EquatorialCoordinate(tools.Transform.RATopocentric, tools.Transform.DECTopocentric);
      }



      public void Refresh(AscomTools tools, DateTime syncTime)
      {
         SyncTime = syncTime;
         LocalApparentSiderialTime = new HourAngle(AstroConvert.LocalApparentSiderealTime(tools.Transform.SiteLongitude, syncTime));
         Equatorial = new EquatorialCoordinate(GetRA(), GetDec());
         UpdateAltAzimuth(tools, syncTime);
      }


      public void MoveRADec(AxisPosition newAxisPosition, AscomTools tools, DateTime syncTime)
      {
         // double[] delta = ObservedAxes.GetDeltaTo(newAxisPosition);
         SyncTime = syncTime;
         LocalApparentSiderialTime = new HourAngle(AstroConvert.LocalApparentSiderealTime(tools.Transform.SiteLongitude, syncTime));
         // Apply the axis rotation to the new position.
         ObservedAxes = newAxisPosition;
         Equatorial = new EquatorialCoordinate(GetRA(), GetDec());
         UpdateAltAzimuth(tools, syncTime);
      }


      private double GetRA()
      {
         // Turn ACW +ve Observer position into CW angle
         double cwAngle = 360.0 - ObservedAxes[0];
         double ra = LocalApparentSiderialTime - 12.0 + HourAngle.DegreesToHours(cwAngle);

         if (Hemisphere == HemisphereOption.Northern)
         {
            if (ObservedAxes[1] > 180.0)
            {
               ra = ra + 12;
            }
         }
         else
         {
            System.Diagnostics.Debug.Assert(false, "Not tested for Southern Hemisphere");
            if (ObservedAxes[1] > 90.0 && ObservedAxes[1] <= 270.0)
            {
               ra = ra + 12;
            }
         }
         return AstroConvert.RangeRA(ra);
      }

      private double GetDec()
      {
         if (Hemisphere == HemisphereOption.Northern)
         {
            if (ObservedAxes[1] <= 180)
            {
               return 90.0 - ObservedAxes[1];
            }
            else
            {
               return ObservedAxes[1] - 270.0;
            }
         }
         else
         {
            System.Diagnostics.Debug.Assert(false, "Untested in Southern Hemisphere");
            if (ObservedAxes[1] <= 180)
            {
               return ObservedAxes[1] - 270.0;
            }
            else
            {
               return 90.0 - ObservedAxes[1];
            }
         }
      }

      public void MoveRADec(Angle[] delta, AscomTools tools, DateTime syncTime)
      {
         SyncTime = syncTime;
         LocalApparentSiderialTime = new HourAngle(AstroConvert.LocalApparentSiderealTime(tools.Transform.SiteLongitude, syncTime));
         // Refresh the Equatorial at the current position
         UpdateEquatorial(tools, syncTime);
         // Apply the axis rotation to the new position.
         double newRA = AstroConvert.RangeRA(LocalApparentSiderialTime + 12.0 + HourAngle.DegreesToHours(ObservedAxes[0] + delta[0]));
         double newDec = AddDec(Equatorial.Declination, delta[1]);
         ObservedAxes = ObservedAxes.RotateBy(delta);
         Equatorial = new EquatorialCoordinate(newRA, newDec);
         UpdateAltAzimuth(tools, syncTime);
      }


      public Angle[] GetRADecSlewAnglesTo(double targetRA, double targetDec)
      {

         double targetHA = AstroConvert.RangeHA(targetRA - LocalApparentSiderialTime);
         System.Diagnostics.Debug.WriteLine($"Target HA: {targetHA}");



         double deltaHrs = (targetRA - Equatorial.RightAscension) % 12.0;    // Mod 12 because a single axis position is two RA angles 12 hours apart.
         double deltaRa = HourAngle.HoursToDegrees(deltaHrs);
         double deltaDec = targetDec - Equatorial.Declination;
         Angle[] slewAngles = new Angle[] { new Angle(deltaRa), new Angle(deltaDec) };

         // Get the desired final axis position
         AxisPosition finalAxisPosition = this.ObservedAxes.RotateBy(slewAngles);
         // Get the SAFE (through the pole) angles to slew.
         slewAngles = this.ObservedAxes.GetSlewAnglesTo(finalAxisPosition);
         return new Angle[] { slewAngles[0], slewAngles[1] };
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


      private double AddDec(double original, double delta)
      {
         double result = original + delta;
         if (result > 90.0)
         {
            result = 180 - result;
         }
         if (result < -90.0)
         {
            result = -180 - result;
         }
         if (result > 90.0 || result < -90.0)
         {
            throw new ArgumentOutOfRangeException("delta", "AddDec can only handle small changes of declination.");
         }
         return result;
      }


      public AxisPosition CalculateTargetAxes(double targetRA, double targetDec, AscomTools tools)
      {
         bool flipDEC;
         double adjustedRA = targetRA;
         double targetHA = AstroConvert.RangeHA(targetRA - LocalApparentSiderialTime);
         if (targetHA < 0) // Target is to the west.
         {
            if (ForceMeridianFlip)
            {
               if (Hemisphere == HemisphereOption.Northern)
               {
                  flipDEC = false;
               }
               else
               {
                  flipDEC = true;
               }
               adjustedRA = targetRA;
            }
            else
            {
               if (Hemisphere == HemisphereOption.Northern)
               {
                  flipDEC = true;
               }
               else
               {
                  flipDEC = false;
               }
               adjustedRA = AstroConvert.RangeRA(targetRA - 12);
            }
         }
         else
         {
            if (ForceMeridianFlip)
            {
               if (Hemisphere == HemisphereOption.Northern)
               {
                  flipDEC = true;
               }
               else
               {
                  flipDEC = false;
               }
               adjustedRA = AstroConvert.RangeRA(targetRA - 12);
            }
            else
            {
               if (Hemisphere == HemisphereOption.Northern)
               {
                  flipDEC = false;
               }
               else
               {
                  flipDEC = true;
               }
               adjustedRA = targetRA;
            }
         }


         // Compute for Target RA/DEC angles
         Angle RAAxis = GetAxisPositionForRA(adjustedRA, 0.0);
         Angle DecAxis = GetAxisPositionForDec(targetDec, flipDEC);

         return new AxisPosition(RAAxis.Value, DecAxis.Value);
      }


      public void TestCalculateTargetAxes(double targetRA, double targetDec, AscomTools tools)
      {
         bool flipDEC;
         double adjustedRA = targetRA;
         double targetHA = AstroConvert.RangeHA(targetRA - LocalApparentSiderialTime);
         if (targetHA < 0) // Target is to the west.
         {
            if (ForceMeridianFlip)
            {
               if (Hemisphere == HemisphereOption.Northern)
               {
                  flipDEC = false;
               }
               else
               {
                  flipDEC = true;
               }
               adjustedRA = targetRA;
            }
            else
            {
               if (Hemisphere == HemisphereOption.Northern)
               {
                  flipDEC = true;
               }
               else
               {
                  flipDEC = false;
               }
               adjustedRA = AstroConvert.RangeRA(targetRA - 12);
            }
         }
         else
         {
            if (ForceMeridianFlip)
            {
               if (Hemisphere == HemisphereOption.Northern)
               {
                  flipDEC = true;
               }
               else
               {
                  flipDEC = false;
               }
               adjustedRA = AstroConvert.RangeRA(targetRA - 12);
            }
            else
            {
               if (Hemisphere == HemisphereOption.Northern)
               {
                  flipDEC = false;
               }
               else
               {
                  flipDEC = true;
               }
               adjustedRA = targetRA;
            }
         }

         System.Diagnostics.Debug.WriteLine($"{targetRA} -> {adjustedRA}");
      }


      #region RA calcs ...
      /// <summary>
      /// 
      /// </summary>
      /// <param name="targetRA">Target RA in hours</param>
      /// <param name="targetDec">Target Dec in degrees</param>
      /// <param name="longitude">Site longitude</param>
      /// <param name="hemisphere">Site hemisphere</param>
      /// <returns></returns>
      public Angle GetAxisPositionForRA(double targetRA, double targetDec)
      {
         double deltaRa = targetRA - LocalApparentSiderialTime;
         if (Hemisphere == HemisphereOption.Northern)
         {
            if (targetDec > 90.0 && targetDec <= 270.0)
            {
               deltaRa = deltaRa - 12.0;
            }
         }
         else
         {
            if (targetDec > 90.0 && targetDec <= 270)
            {
               deltaRa = deltaRa + 12.0;
            }
         }
         deltaRa = AstroConvert.RangeRA(deltaRa);

         return GetAngleFromHours(deltaRa);
      }


      public Angle GetAngleFromHours(double hourAngle)
      {
         double offset = 0.0;    // This may vary in the future if zero degrees is no longer at 12-0-clock
         hourAngle = AstroConvert.RangeRA(hourAngle - 6.0); // Renormalise from a perpendicular position
         double degrees;
         if (Hemisphere == HemisphereOption.Northern)
         {
            //if (hourAngle < 12)
            //{
            //   degrees = offset - HourAngle.HoursToDegrees(hourAngle);
            //}
            //else
            //{
               degrees = HourAngle.HoursToDegrees(hourAngle) + offset;
            //}
         }
         else
         {
            if (hourAngle < 12)
            {
               degrees = HourAngle.HoursToDegrees(hourAngle) + offset;
            }
            else
            {
               degrees = offset - HourAngle.HoursToDegrees(hourAngle);
            }
         }
         return AstroConvert.Range360Degrees(degrees);
      }

      #endregion


      #region Dec calcs ...

      public Angle GetAxisPositionForDec(double targetDec, bool flipDEC)
      {
         double angle = targetDec;
         if (flipDEC)
         {
            angle = 180.0 - targetDec;
         }
         return GetAngleFromDecDegrees(angle, flipDEC);
      }


      public Angle GetAngleFromDecDegrees(double angle, bool flipDEC)
      {
         double offset = -90.0;    // This may vary in the future if zero degrees is no longer at 12-0-clock
         double result = 0.0;
         if (Hemisphere == HemisphereOption.Southern)
         {
            angle = 360.0 - angle;
         }
         if (angle > 180.0 && !flipDEC)
         {
            result = offset - angle;
         }
         else
         {
            result = angle + offset;
         }
         return result;
      }

      #endregion


      public void DumpDebugInfo()
      {
         StringBuilder sb = new StringBuilder();
         sb.AppendLine("Mount data:");
         sb.AppendFormat("\tRA/Dec: {0}/{1}\n", Equatorial.RightAscension, Equatorial.Declination);
         sb.AppendFormat("\tAlt/Az: {0}/{1}\n", AltAzimuth.Altitude, AltAzimuth.Azimuth);
         sb.AppendFormat("\t  Axes: {0}/{1}\n", ObservedAxes[0], ObservedAxes[1]);
         System.Diagnostics.Debug.WriteLine(sb.ToString());
      }
   }


}
