using System;
using ASCOM.LunaticAstroEQ.Core;
using ASCOM.LunaticAstroEQ.Core.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LunaticAstroEQ.Tests
{
   [TestClass]
   public class Investigations
   {
      private AscomTools _Tools;
      private DateTime _Now;

      [TestInitialize]
      public void Initialize()
      {
         _Now = new DateTime(2019, 1, 25, 11, 47, 40);      // Gives HA of approx 20.
         _Tools = new AscomTools();
         _Tools.Transform.SiteElevation = 192;
         _Tools.Transform.SiteLatitude = 52.667;
         _Tools.Transform.SiteLongitude = -1.333;
         _Tools.Transform.SiteTemperature = 15.0;
      }

      [TestCleanup]
      public void Cleanup()
      {
         _Tools.Dispose();
      }

      [TestMethod]
      public void TestingHA()
      {
         double ra = 0.0;
         double ha = 0.0;
         HourAngle lst = new HourAngle(AstroConvert.LocalApparentSiderealTime(_Tools.Transform.SiteLongitude,_Now));
         System.Diagnostics.Debug.WriteLine($"LST = {lst.Value}");
         System.Diagnostics.Debug.WriteLine("\tRA\t\t\tHA");
         System.Diagnostics.Debug.WriteLine("\t==\t\t\t==");

         for (int i = 0; i < 24; i++) {
            ra = i * 1.0;
            ha = AstroConvert.RangeHA(ra - lst);
            System.Diagnostics.Debug.WriteLine($"\t{ra}\t\t{ha}");
         }

         Assert.IsTrue(true);
      }
   }
}
