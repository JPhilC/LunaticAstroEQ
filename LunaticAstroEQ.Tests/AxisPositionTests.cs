using System;
using ASCOM.LunaticAstroEQ.Core.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LunaticAstroEQ.Tests
{
   [TestClass]
   public class AxisPositionTests
   {
      [DataTestMethod]
      [DataRow(90.0, 90.0, 90.0, 90, 180.0, 180.0)]
      [DataRow(90.0, 90.0, 180.0, 180.0, 270.0, 270.0)]
      [DataRow(270.0, 270.0, 90.0, 90.0, 0.0, 0.0)]
      [DataRow(360.0, 360.0, 360.0, 360.0, 0.0, 0.0)]
      [DataRow(270.0, 270.0, 100.0, 100.0, 10.0, 10.0)]
      [DataRow(180.0, 180.0, -270.0, -270.0, 270.0, 270.0)]
      public void Addition(double a1_0, double a1_1, double a2_0, double a2_1, double e_0, double e_1)
      {
         AxisPosition pos1 = new AxisPosition(a1_0, a1_1);
         AxisPosition pos2 = new AxisPosition(a2_0, a2_1);
         AxisPosition expected = new AxisPosition(e_0, e_1);
         AxisPosition result = pos1 + pos2;
         Assert.AreEqual(expected, result);
      }


      [DataTestMethod]
      [DataRow(45.0, 315.0, 225.0, 45.0)]
      [DataRow(315.0, 45.0, 135.0, 315.0)]
      [DataRow(225.0, 135.0, 45.0, 225.0)]
      [DataRow(135.0, 85.0, 315.0, 275.0)]
      public void TestFlip(double a1_0, double a1_1, double e_0, double e_1)
      {
         AxisPosition pos1 = new AxisPosition(a1_0, a1_1);
         AxisPosition expected = new AxisPosition(e_0, e_1);
         AxisPosition result = pos1.Flip();
         Assert.AreEqual(expected, result);
      }

      [DataTestMethod]
      [DataRow(135.0, 225.0, 225.0, 135.0, -270.0, 270)]   // either side of 180
      [DataRow(45.0, 135.0, 135.0, 45.0, 90.0, -90)]     // both <180
      [DataRow(225.0, 315.0, 315.0, 225.0, 90.0, -90.0)] // both > 180
      [DataRow(45.0, 315.0, 315.0, 45.0, -90.0, 90.0)] // both either side of zero

      public void TestGetSlewAngleTo(double s_0, double s_1, double t_0, double t_1, double e_0, double e_1)
      {
         AxisPosition source = new AxisPosition(s_0, s_1);
         AxisPosition target = new AxisPosition(t_0, t_1);
         Angle[] expected = new Angle[] { new Angle(e_0), new Angle(e_1) };
         Angle[] result = source.GetSlewAnglesTo(target);
         Assert.IsTrue(expected[0] == result[0] && expected[1] == result[1]);
      }

      

   }
}
