using System;
using ASCOM.LunaticAstroEQ.Core.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LunaticAstroEQ.Tests
{
   [TestClass]
   public class EquatorialCoordinateTests
   {

      [DataRow(90.0, 90.0, 6.0, 90.0)]
      [DataRow(180.0, -90.0, 12.0, 270.0)]
      [DataRow(270.0, 135.0, 18.0, 135.0)]
      [DataRow(360.0, 270.0, 0.0, 270.0)]
      [DataRow(375.0, 375.0, 1.0, 15.0)]
      [DataTestMethod]
      public void TestFromAxisDelta(double dRa, double dDec, double rRa, double rDec)
      {
         EquatorialCoordinate datum = new EquatorialCoordinate(0.0, 0.0);
         double[] delta = new double[] { dRa, dDec };
         EquatorialCoordinate result = datum.SlewBy(delta);
         EquatorialCoordinate expected = new EquatorialCoordinate(rRa, rDec);

         Assert.AreEqual(expected, result);

      }
   }
}
