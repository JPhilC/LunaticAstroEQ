using ASCOM.LunaticAstroEQ.Core.Geometry;
using ASCOM.LunaticAstroEQ.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.LunaticAstroEQ.Controller
{
   [ComVisible(false)]
   public class ControllerSettings : DataObjectBase
   {

      /// <summary>
      /// The start position of the mount
      /// </summary>
      public LatLongCoordinate ObservatoryLocation { get; set; } = new LatLongCoordinate();

      public double ObservatoryElevation { get; set; }

   }
}
