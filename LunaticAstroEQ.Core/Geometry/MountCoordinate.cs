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
         LocalApparentSiderialTime = new HourAngle(AstroConvert.LocalApparentSiderealTime(tools.Transform.SiteLongitude, syncTime));
         if (tools.Transform.SiteLatitude < 0.0)
         {
            Hemisphere = HemisphereOption.Southern;
         }
         AltAzimuth = altAz;
         this.UpdateEquatorial(tools, syncTime);
      }


      public MountCoordinate(AxisPosition axisPosition, AscomTools tools, DateTime syncTime)
      {
         ObservedAxes = axisPosition;
         SyncTime = syncTime;
         LocalApparentSiderialTime = new HourAngle(AstroConvert.LocalApparentSiderealTime(tools.Transform.SiteLongitude, syncTime));
         if (tools.Transform.SiteLatitude < 0.0)
         {
            Hemisphere = HemisphereOption.Southern;
         }
         Equatorial = new EquatorialCoordinate(GetRA(ObservedAxes), GetDec(ObservedAxes));
         this.UpdateAltAzimuth(tools, syncTime);
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
         Equatorial = new EquatorialCoordinate(GetRA(ObservedAxes), GetDec(ObservedAxes));
         UpdateAltAzimuth(tools, syncTime);
      }


      public void MoveRADec(AxisPosition newAxisPosition, AscomTools tools, DateTime syncTime)
      {
         // double[] delta = ObservedAxes.GetDeltaTo(newAxisPosition);
         SyncTime = syncTime;
         LocalApparentSiderialTime = new HourAngle(AstroConvert.LocalApparentSiderealTime(tools.Transform.SiteLongitude, syncTime));
         // Apply the axis rotation to the new position.
         ObservedAxes = newAxisPosition;
         Equatorial = new EquatorialCoordinate(GetRA(ObservedAxes), GetDec(ObservedAxes));
         UpdateAltAzimuth(tools, syncTime);
      }




      //public void MoveRADec(Angle[] delta, AscomTools tools, DateTime syncTime)
      //{
      //   SyncTime = syncTime;
      //   LocalApparentSiderialTime = new HourAngle(AstroConvert.LocalApparentSiderealTime(tools.Transform.SiteLongitude, syncTime));
      //   // Refresh the Equatorial at the current position
      //   UpdateEquatorial(tools, syncTime);
      //   // Apply the axis rotation to the new position.
      //   ObservedAxes = ObservedAxes.RotateBy(delta);
      //   Equatorial = new EquatorialCoordinate(GetRA(ObservedAxes), GetDec(ObservedAxes));
      //   UpdateAltAzimuth(tools, syncTime);
      //}


      //public Angle[] GetRADecSlewAnglesTo(double targetRA, double targetDec, AscomTools tools)
      //{
      //   AxisPosition targetAxisPosition = GetAxisPositionForRADec(targetRA, targetDec, tools);
      //   return ObservedAxes.GetSlewAnglesTo(targetAxisPosition);
      //}


      #region Side of Pier calculations
      /// <summary>
      /// Returns the pointing side of Pier as required by ASCOM
      /// </summary>
      public PierSide GetPointingSideOfPier(bool swapSideOfPier)
      {
         PierSide pointingSOP = PierSide.pierUnknown;
         if (ObservedAxes != null)
         {
            if (ObservedAxes[1] > 180.0)  //  (ObservedAxes[1] <= 90 || ObservedAxes[1] >= 270.0)
            {
               if (swapSideOfPier)
               {
                  pointingSOP = PierSide.pierWest;
               }
               else
               {
                  pointingSOP = PierSide.pierEast;
               }
            }
            else
            {
               if (swapSideOfPier)
               {
                  pointingSOP = PierSide.pierEast;
               }
               else
               {
                  pointingSOP = PierSide.pierWest;
               }
            }
            if (Hemisphere == HemisphereOption.Southern)
            {
               if (pointingSOP == PierSide.pierWest)
               {
                  pointingSOP = PierSide.pierEast;
               }
               else
               {
                  pointingSOP = PierSide.pierWest;
               }
            }
         }
         return pointingSOP;
      }


      /// <summary>
      /// Gets the mounts physical side of pier.
      /// </summary>
      /// <param name="raHours"></param>
      /// <param name="swapSideOfPier"></param>
      /// <returns></returns>
      public PierSide GetPhysicalSideOfPier(double raHours, bool swapSideOfPier)
      {
         PierSide physicalSOP = PierSide.pierUnknown;
         double ha;

         ha = AstroConvert.RangeHA(raHours - 6.0);
         if (swapSideOfPier)
         {
            physicalSOP = (ha >= 0.0 ? PierSide.pierWest : PierSide.pierEast);
         }
         else
         {
            physicalSOP = (ha >= 0.0 ? PierSide.pierEast : PierSide.pierWest);
         }
         return physicalSOP;
      }


      public PierSide GetDecSideOfPier(double dec)
      {
         PierSide decSOP = PierSide.pierUnknown;
         dec = Math.Abs(dec - 180);
         if (dec <= 90.0)
         {
            decSOP = PierSide.pierEast;
         }
         else
         {
            decSOP = PierSide.pierWest;
         }
         return decSOP;
      }

      #endregion


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


      public AxisPosition GetAxisPositionForRADec(double targetRA, double targetDec, AscomTools tools)
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
         // System.Diagnostics.Debug.WriteLine($"RA/Dec:{targetHA}/{targetDec} Axes:{ RAAxis.Value}/{ DecAxis.Value} FlipDec: {flipDEC}");
         return new AxisPosition(RAAxis.Value, DecAxis.Value, flipDEC);
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
      public HourAngle GetRA(AxisPosition axes)
      {
         double tempRA_hours = GetHourAngleFromAngle(axes.RAAxis.Value);
         double tRa = LocalApparentSiderialTime + tempRA_hours;
         double tHa = AstroConvert.RangeHA(tRa);
         double dec = GetDec(axes);
         System.Diagnostics.Debug.Write($"{axes.RAAxis.Value}/{axes.DecAxis.Value}\t{dec}\t{tHa}\t{tRa}");
         if (Hemisphere == HemisphereOption.Northern)
         {
            if (axes.DecFlipped)
            {
               tRa = tRa - 12.0;
               System.Diagnostics.Debug.Write("\t tRa - tRa - 12");
            }
         }
         else
         {
            System.Diagnostics.Debug.Assert(false, "GetRA is not tested for Southern Hemisphere");
            if (axes.DecAxis.Value > 180)
            {
               tRa = tRa + 12.0;
            }
         }
         return new HourAngle(AstroConvert.RangeRA(tRa));
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="targetRA">Target RA in hours</param>
      /// <param name="targetDec">Target Dec in degrees</param>
      /// <param name="longitude">Site longitude</param>
      /// <param name="hemisphere">Site hemisphere</param>
      /// <returns></returns>
      private Angle GetAxisPositionForRA(double targetRA, double targetDec)
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

         return GetAngleFromHourAngle(deltaRa);
      }

      private double GetHourAngleFromAngle(Angle raAxisAngle)
      {
         double hours = HourAngle.DegreesToHours(raAxisAngle.Value);
         if (Hemisphere == HemisphereOption.Northern)
         {
            return AstroConvert.RangeRA(hours + 6.0);
         }
         else
         {
            return AstroConvert.RangeRA((24.0 - hours) + 6.0);
         }

      }

      private Angle GetAngleFromHourAngle(double hourAngle)
      {
         double ha = 0.0;
         double degrees;
         if (Hemisphere == HemisphereOption.Northern)
         {
            ha = AstroConvert.RangeRA(hourAngle - 6.0); // Renormalise from a perpendicular position
            degrees = HourAngle.HoursToDegrees(ha);
         }
         else
         {
            System.Diagnostics.Debug.Assert(false, "GetAngleFromHours not tested for Southern Hemisphere");
            ha = AstroConvert.RangeRA((24 - hourAngle) - 6.0); // Renormalise from a perpendicular position
            degrees = HourAngle.HoursToDegrees(ha);
         }
         return AstroConvert.Range360Degrees(degrees);
      }

      #endregion


      #region Dec calcs ...
      public Angle GetDec(AxisPosition axes)
      {
         double dec = GetDecDegreesFromAngle(axes.DecAxis.Value);
         return new Angle(AstroConvert.RangeDec(dec));
      }



      private Angle GetAxisPositionForDec(double targetDec, bool flipDEC)
      {
         double angle = targetDec;
         if (flipDEC)
         {
            angle = 180.0 - targetDec;
         }
         return GetAngleFromDecDegrees(angle, flipDEC);
      }



      private double GetDecDegreesFromAngle(Angle decAxisAngle)
      {
         double i = 0.0;
         if (decAxisAngle.Value >= 180)
         {
            i = decAxisAngle.Value - 270.0;
         }
         else
         {
            i = 90.0 - decAxisAngle.Value;
         }
         if (Hemisphere == HemisphereOption.Northern)
         {
            return AstroConvert.Range360Degrees(i);
         }
         else
         {
            return AstroConvert.Range360Degrees(360.0 - i);
         }
      }

      private Angle GetAngleFromDecDegrees(double angle, bool flipDEC)
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
         // This routine works clockwise so need to convert to Anti-clockwise
         result = AstroConvert.Range360Degrees(360 + result);
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
