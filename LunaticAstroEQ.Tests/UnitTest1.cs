using System;
using ASCOM.DeviceInterface;
using ASCOM.LunaticAstroEQ;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LunaticAstroEQ.Tests
{
   [TestClass]
   public class InitialisationTests
   {
      [TestMethod]
      public void CreateInstanceTest()
      {

         Type driverType = Type.GetTypeFromProgID("ASCOM.LunaticAstroEQ.Telescope");
         ITelescopeV3 driver = (ITelescopeV3)Activator.CreateInstance(driverType);

         Assert.IsNotNull(driver);
      }
   }
}
