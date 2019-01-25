using System;
using ASCOM.LunaticAstroEQ.Core.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LunaticAstroEQ.Tests
{
   [TestClass]
   public class AngleTests
   {
      [DataTestMethod]
      [DataRow(0.0, 90.0, 90.0)]
      [DataRow(90.0, 90.0, 180.0)]
      [DataRow(180.0, 90.0, 270.0)]
      [DataRow(270.0, 90.0, 360.0)]
      [DataRow(270.0, 100.0, 370.0)]
      [DataRow(225.0, -180.0, 45.0)]
      [DataRow(225.0, -270.0, -45.0)]
      public void AnglePlusAngle(double v1, double v2, double expected)
      {
         Angle ang1 = new Angle(v1);
         Angle ang2 = new Angle(v2);
         Angle result = ang1 + ang2;
         Assert.AreEqual(expected, result.Value);
      }

      [DataTestMethod]
      [DataRow(0.0, 90.0, -90.0)]
      [DataRow(90.0, 90.0, 0.0)]
      [DataRow(180.0, 90.0, 90.0)]
      [DataRow(270.0, 90.0, 180.0)]
      [DataRow(270.0, 100.0, 170.0)]
      [DataRow(225.0, -180.0, 405.0)]
      [DataRow(225.0, -270.0, 495.0)]
      public void AngleMinusAngle(double v1, double v2, double expected)
      {
         Angle ang1 = new Angle(v1);
         Angle ang2 = new Angle(v2);
         Angle result = ang1 - ang2;
         Assert.AreEqual(expected, result.Value);
      }

      [DataTestMethod]
      [DataRow(0.0, 6.0, 6.0)]
      [DataRow(6.0, 6.0, 12.0)]
      [DataRow(12.0, 6.0, 18.0)]
      [DataRow(18.0, 6.0, 24.0)]
      [DataRow(18.0, 7.0, 25.0)]
      [DataRow(15.0, 12.0, 27.0)]
      [DataRow(15.0, -18.0, -3.0)]
      public void HourAnglePlusHourAngle(double v1, double v2, double expected)
      {
         HourAngle ang1 = new HourAngle(v1);
         HourAngle ang2 = new HourAngle(v2);
         HourAngle result = ang1 + ang2;
         Assert.AreEqual(expected, result.Value);
      }

      [DataTestMethod]
      [DataRow(0.0, 6.0, -6.0)]
      [DataRow(6.0, 6.0, 0.0)]
      [DataRow(12.0, 6.0, 6.0)]
      [DataRow(18.0, 6.0, 12.0)]
      [DataRow(18.0, 7.0, 11.0)]
      [DataRow(15.0, 12.0, 3.0)]
      [DataRow(15.0, -18.0, 33.0)]
      public void HourAngleMinusHourAngle(double v1, double v2, double expected)
      {
         HourAngle ang1 = new HourAngle(v1);
         HourAngle ang2 = new HourAngle(v2);
         HourAngle result = ang1 - ang2;
         Assert.AreEqual(expected, result.Value);
      }


   }
}
