using ASCOM.LunaticAstroEQ.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.LunaticAstroEQ.Core
{
   [Flags]
   public enum AxisState
   {
      Stopped = 0x0001,             // The axis is in a fully stopped state
      Slewing = 0x0002,             // The axis is in constant speed operation
      Slewing_To = 0x0004,          // The axis is in the process of running to the specified target position
      Slewing_Forward = 0x0008,     // The axis runs forward
      Slewing_Highspeed = 0x0010,   // The axis is in high-speed operation
      Not_Initialised = 0x0020      // MC controller has not been initialized, axis is not initialized.
   }

   // Two-axis telescope code
   public enum AxisId
   {
      Axis1_RA,
      Axis2_DEC,
      Both_Axes,
      Aux_RA_Encoder,
      Aux_DEC_Encoder
   };


   [TypeConverter(typeof(EnumTypeConverter))]
   public enum BaudRate
   {
      [Description("4800")]
      Baud4800 = 4800,
      [Description("9600")]
      Baud9600 = 9600,
      [Description("11520")]
      Baud115200 = 115200,
      [Description("128000")]
      Baud128000 = 128000,
   }

   [TypeConverter(typeof(EnumTypeConverter))]
   public enum TimeOutOption
   {
      [Description("1000")]
      TO1000 = 1000,
      [Description("2000")]
      TO2000 = 2000
   }

   [TypeConverter(typeof(EnumTypeConverter))]
   public enum RetryOption
   {
      [Description("Once")]
      Once = 1,
      [Description("Twice")]
      Twice = 2
   }

   [TypeConverter(typeof(EnumTypeConverter))]
   public enum PulseGuidingOption
   {
      [Description("ASCOM Pulse Guiding")]
      ASCOM,
      [Description("ST-4 Pulse Guiding")]
      ST4
   }

   [TypeConverter(typeof(EnumTypeConverter))]
   public enum ParkStatus
   {
      [Description("Unparked")]
      Unparked,
      [Description("Parked")]
      Parked,
      [Description("Parking")]
      Parking,
      [Description("Unparking")]
      Unparking,
      [Description("At home")]
      AtHome
   }

   [TypeConverter(typeof(EnumTypeConverter))]
   public enum TrackingStatus
   {
      [Description("Off")]
      Off,
      [Description("Sidereal")]
      Sidereal,
      [Description("Lunar")]
      Lunar,
      [Description("Solar")]
      Solar,
      [Description("Custom")]
      Custom
   }

   [TypeConverter(typeof(EnumTypeConverter))]
   public enum HemisphereOption
   {
      [Description("North")]
      Northern,
      [Description("South")]
      Southern
   }

   public enum SlewButton
   {
      Stop,
      North,
      East,
      South,
      West
   }

   public enum AxisDirection
   {
      [Description("Forward")]
      Forward,
      [Description("Reverse")]
      Reverse
   }

   [TypeConverter(typeof(EnumTypeConverter))]
   public enum SyncModeOption
   {
      [Description("Dialog Based")]
      Dialog,
      [Description("Append on Sync")]
      AppendOnSync
   }

   [TypeConverter(typeof(EnumTypeConverter))]
   public enum SyncAlignmentModeOptions
   {
      [Description("3-point + nearest star")]
      ThreePoint,
      [Description("Nearest star")]
      NearestStar
   }

}
