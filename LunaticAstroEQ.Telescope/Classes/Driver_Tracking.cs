using ASCOM.DeviceInterface;
using ASCOM.LunaticAstroEQ.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.LunaticAstroEQ
{
   public partial class Telescope
   {

      #region Side of pier stuff ...
      /// <summary>
      /// Returns the Side of Pier based on the Declination axis position in radians
      /// V1.24g mode - not ASCOM but folks seem to like it!
      /// </summary>
      /// <param name="decAxisPosition"></param>
      /// <returns></returns>
      private PierSide SOP_Dec(double decAxisPosition)
      {
         double dec = Math.Abs(decAxisPosition - Math.PI);
         return ((dec <= Core.Constants.HALF_PI) ? PierSide.pierEast : PierSide.pierWest);

      }


      /// <summary>
      /// Physical Side of Pier
      /// this is what folks expect side of pier to be - but it won't work in ASCOM land.
      /// </summary>
      /// <param name="hourAngle"></param>
      /// <returns></returns>
      private PierSide SOP_Physical(double hourAngle)
      {
         double ha = AstroConvert.RangeRA(hourAngle);
         if (Settings.AscomCompliance.SwapPhysicalSideOfPier)
         {
            return (ha >= 0 ? PierSide.pierWest : PierSide.pierEast);
         }
         else
         {
            return ((ha >= 0) ? PierSide.pierEast : PierSide.pierWest);
         }
      }

      /// <summary>
      /// Returns the side of pier defined by the dec axis position in radians.
      /// Not the side of pier at all - but that's what ASCOM in their widsom chose to call it - duh!
      /// </summary>
      /// <param name="decAxisPosition"></param>
      /// <returns></returns>
      private PierSide SOP_Pointing(double decAxisPosition)
      {
         PierSide result;
         if (decAxisPosition <= Core.Constants.HALF_PI || Core.Constants.ONEANDHALF_PI >= 270)
         {
            if (Settings.AscomCompliance.SwapPointingSideOfPier)
            {
               result = PierSide.pierEast;
            }
            else
            {
               result = PierSide.pierWest;
            }
         }
         else
         {
            if (Settings.AscomCompliance.SwapPointingSideOfPier)
            {
               result = PierSide.pierWest;
            }
            else
            {
               result = PierSide.pierEast;
            }
         }


         //  in the south east is west and west is east!
         if (Hemisphere == HemisphereOption.Southern)
         {
            if (result == PierSide.pierWest)
            {
               result = PierSide.pierEast;
            }
            else
            {
               result = PierSide.pierWest;
            }
         }

         return result;
      }

      #endregion

   }
}
