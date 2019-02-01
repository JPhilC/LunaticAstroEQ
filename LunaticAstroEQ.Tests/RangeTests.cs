using System;
using ASCOM.LunaticAstroEQ.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LunaticAstroEQ.Tests
{
   [TestClass]
   public class RangeTests
   {

      [DataTestMethod]
      [DataRow(0.0, 0.0)]
      [DataRow(7.0, 7.0)]
      [DataRow(6.0, 6.0)]
      [DataRow(12.0, 12.0)]
      [DataRow(13.0, 13.0)]
      [DataRow(18.0, 18.0)]
      [DataRow(24.0, 0.0)]
      [DataRow(31.0, 7.0)]
      [DataRow(30.0, 6.0)]
      [DataRow(36.0, 12.0)]
      [DataRow(37.0, 13.0)]
      [DataRow(42.0, 18.0)]
      [DataRow(-6.0, 18.0)]
      [DataRow(-12.0, 12.0)]
      [DataRow(-13.0, 11.0)]
      [DataRow(-18.0, 6.0)]
      [DataRow(-19.0, 5.0)]
      [DataRow(-23.0, 1.0)]
      [DataRow(-30.0, 18.0)]
      [DataRow(-36.0, 12.0)]
      [DataRow(-37.0, 11.0)]
      [DataRow(-42.0, 6.0)]
      [DataRow(-43.0, 5.0)]
      [DataRow(-47.0, 1.0)]
      public void TestRangeRA(double value, double expected)
      {
         Assert.AreEqual(expected, AstroConvert.RangeRA(value));
      }


      [DataTestMethod]
      [DataRow(0.0, 0.0)]
      [DataRow(5.43334, 5.43334)]
      [DataRow(6.28318530, 6.28318530)]
      [DataRow(6.30000000, 0.0168146)]
      [DataRow(-6.30000000, 6.2663707)]
      public void TestRange2Pi(double value, double expected)
      {
         Assert.AreEqual(expected, AstroConvert.Range2Pi(value), 0.00001);
      }

      [DataTestMethod]
      [DataRow(0.0, 0.0)]
      [DataRow(340.0, 340.0)]
      [DataRow(360.0, 0.0)]
      [DataRow(370.0, 10.0)]
      [DataRow(-10.0, 350.0)]
      [DataRow(-350.0, 10.0)]
      [DataRow(-360.0, 0.0)]
      public void TestRange360(double value, double expected)
      {
         Assert.AreEqual(expected, AstroConvert.RangeAzimuth(value), 0.00001);
      }


      [DataTestMethod]
      [DataRow(0.0, 0.0)]
      [DataRow(170.0, 170.0)]
      [DataRow(180.0, 180.0)]
      [DataRow(190.0, -170.0)]
      [DataRow(275.0, -85.0)]
      [DataRow(350.0, -10.0)]
      [DataRow(-70.0, -70.0)]
      [DataRow(-170.0, -170.0)]
      [DataRow(-180.0, 180.0)]
      [DataRow(-190.0, 170.0)]
      [DataRow(-275.0, 85.0)]
      [DataRow(-350.0, 10.0)]
      [DataRow(0.0, 0.0)]
      [DataRow(530.0, 170.0)]
      [DataRow(540.0, 180.0)]
      [DataRow(550.0, -170.0)]
      [DataRow(635.0, -85.0)]
      [DataRow(710.0, -10.0)]
      [DataRow(-430.0, -70.0)]
      [DataRow(-530.0, -170.0)]
      [DataRow(-540.0, 180.0)]
      [DataRow(-550.0, 170.0)]
      [DataRow(-635.0, 85.0)]
      [DataRow(-710.0, 10.0)]


      public void TestRangeLatitude(double value, double expected)
      {
         Assert.AreEqual(expected, AstroConvert.RangeLatitude(value));
      }

      [DataTestMethod]
      [DataRow(0.0, 0.0)]
      [DataRow(30.0, 330.0)]
      [DataRow(90.0, 270.0)]
      [DataRow(120.0, 240.0)]
      [DataRow(150.0, 210.0)]
      [DataRow(180.0, 180.0)]
      [DataRow(-30.0, -330.0)]
      [DataRow(-90.0, -270.0)]
      [DataRow(-120.0, -240.0)]
      [DataRow(-150.0, -210.0)]
      [DataRow(-180.0, -180.0)]
      public void TestFlipDecAxisDegrees(double value, double expected)
      {
         Assert.AreEqual(expected, AstroConvert.FlipDecAxisDegrees(value));
      }

      [DataTestMethod]
      [DataRow(0.0, 180.0)]
      [DataRow(10.0, -170.0)]
      [DataRow(90.0, -90.0)]
      [DataRow(135.0, -45.0)]
      [DataRow(180.0, 0.0)]
      [DataRow(225.0, 45.0)]
      [DataRow(270.0, 90.0)]
      [DataRow(315, 135.0)]
      [DataRow(-10.0, 170.0)]
      [DataRow(-90.0, 90.0)]
      [DataRow(-135.0, 45.0)]
      [DataRow(-180.0, 0.0)]
      [DataRow(-225.0, -45.0)]
      [DataRow(-270.0, -90.0)]
      [DataRow(-315.0, -135.0)]
      public void TestFlipRAAxisDegrees(double value, double expected)
      {
         Assert.AreEqual(expected, AstroConvert.FlipRAAxisDegrees(value));
      }


      
   }
}
