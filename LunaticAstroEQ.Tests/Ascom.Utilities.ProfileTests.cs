using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LunaticAstroEQ.Tests
{
   [TestClass]
   public class AscomProfile
   {
      [TestMethod]
      public void TestOLERegister()
      {
         Type ProfileType = Type.GetTypeFromProgID("ASCOM.Utilities.Profile");
         dynamic ProfileInst = Activator.CreateInstance(ProfileType);
         ProfileInst.DeviceType = "Telescope";
         ProfileInst.Register("ASCOM.LunaticAstroEQ.Telescope.BetaTest.Telescope", "AstroEQ & Synta telescope mounts(Beta release)");
      }

      [TestMethod]
      public void TestOLEUnRegister()
      {
         Type ProfileType = Type.GetTypeFromProgID("ASCOM.Utilities.Profile");
         dynamic ProfileInst = Activator.CreateInstance(ProfileType);
         ProfileInst.DeviceType = "Telescope";
         ProfileInst.Unregister("ASCOM.LunaticAstroEQ.Telescope.BetaTest.Telescope");
      }

   }
}
