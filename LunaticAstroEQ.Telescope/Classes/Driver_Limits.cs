using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.LunaticAstroEQ
{
   partial class Telescope
   {
      private double LimitWest;
      private double LimitEast;

      private bool _CheckLimitsActive;
      public bool CheckLimitsActive
      {
         get
         {
            return _CheckLimitsActive;
         }
         private set
         {
            _CheckLimitsActive = value;
         }
      }

      public bool LimitsActive
      {
         get
         {
            if (!CheckLimitsActive)
            {
               return false;
            }
            else
            {
               if (LimitEast == 0 || LimitWest == 0)
               {
                  return false;
               }
               else
               {
                  return true;
               }
            }
         }
      }
   }
}
