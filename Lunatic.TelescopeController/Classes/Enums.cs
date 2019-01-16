using ASCOM.LunaticAstroEQ.Controls;
using System;
using System.ComponentModel;

namespace Lunatic.TelescopeController
{
   [Flags]
   public enum Modules
   {
      None = 0,           // 0000 0000 0000
      ReducedSlew = 1,           // 0000 0000 0001
      Slew = 1 << 1,      // 0000 0000 0010
      MountPosition = 1 << 2,      // 0000 0000 0100
      Tracking = 1 << 3,      // 0000 0000 1000
      ParkStatus = 1 << 4,      // 0000 0001 0000
      Expander = 1 << 5,      // 0000 0010 0000
      AxisPosition = 1 << 6,      // 0000 0100 0000
      MessageCentre = 1 << 7,      // 0000 1000 0000
      PEC = 1 << 8,      // 0001 0000 0000
      PulseGuide = 1 << 9       // 0010 0000 0000
   }

   /// <summary>
   /// Used to control the main form component visiblity
   /// </summary>
   public enum DisplayMode
   {
      /// <summary>
      /// Just shows the reduced slew buttons.
      /// </summary>
      ReducedSlew = Modules.ReducedSlew,
      /// <summary>
      /// Shows the mount position, Slew Controls, Track Rate, Park Status and expander.
      /// </summary>
      MountPosition = Modules.MountPosition | Modules.Slew | Modules.Tracking | Modules.ParkStatus | Modules.Expander,
      /// <summary>
      /// Shows axis position, Slew Controls, Track Rate, Park Status and expander.
      /// </summary>
      AxisPosition = Modules.AxisPosition | Modules.Slew | Modules.Tracking | Modules.ParkStatus | Modules.Expander,
      /// <summary>
      /// Shows message centre, Slew Controls, Track Rate, Park Status and expander.
      /// </summary>
      MessageCentre = Modules.MessageCentre | Modules.Slew | Modules.Tracking | Modules.ParkStatus | Modules.Expander,
      /// <summary>
      /// Shows PEC panel, Track Rate, Park Status and expander.
      /// </summary>
      PEC = Modules.PEC | Modules.Tracking | Modules.ParkStatus | Modules.Expander,
      /// <summary>
      /// Shows pulse guide centre, Slew Controls, Track Rate, Park Status and expander.
      /// </summary>
      PulseGuideMonitor = Modules.PulseGuide | Modules.Tracking | Modules.ParkStatus | Modules.Expander,
   }

   [TypeConverter(typeof(EnumTypeConverter))]
   public enum TrackingMode
   {
      [Description("Solar")]
      Solar,
      [Description("Sidereal")]
      Sidereal,
      [Description("Sidereal with PEC")]
      SiderealPEC,
      [Description("Lunar")]
      Lunar,
      [Description("Custom")]
      Custom,
      [Description("Not tracking")]
      Stop
   }

}
