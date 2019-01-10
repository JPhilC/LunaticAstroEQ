using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASCOM.LunaticAstroEQ.Controller
{
   public static class BasicMath
   {
      public const double RAD1 = Math.PI / 180;
      public static double AngleDistance(double ang1, double ang2)
      {
         ang1 = UniformAngle(ang1);
         ang2 = UniformAngle(ang2);

         double d = ang2 - ang1;

         return UniformAngle(d);
      }
      public static double UniformAngle(double Source)
      {
         Source = Source % (Math.PI * 2);
         if (Source > Math.PI)
            return Source - 2 * Math.PI;
         if (Source < -Math.PI)
            return Source + 2 * Math.PI;
         return Source;
      }

      public static double DegToRad(double Degree) { return (Degree / 180 * Math.PI); }
      public static double RadToDeg(double Rad) { return (Rad / Math.PI * 180.0); }
      public static double RadToMin(double Rad) { return (Rad / Math.PI * 180.0 * 60.0); }
      public static double RadToSec(double Rad) { return (Rad / Math.PI * 180.0 * 60.0 * 60.0); }

   }
}
