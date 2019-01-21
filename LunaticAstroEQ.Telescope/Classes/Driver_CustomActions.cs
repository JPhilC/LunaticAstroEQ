using ASCOM.LunaticAstroEQ.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.LunaticAstroEQ
{
   partial class Telescope
   {

      private List<string> _SupportedActions = new List<string>() {
            "Lunatic:SetUnparkPositions",
            "Lunatic:SetParkPositions",
            //"Lunatic:SetTrackUsingPEC",
            //"Lunatic:SetCheckRASync",
            //"Lunatic:SetAutoGuiderPortRates",
            "Lunatic:SetCustomTrackingRates",
            "Lunatic:SetSiteTemperature"
      };

      /// <summary>
      /// Processes customer actions
      /// </summary>
      /// <param name="actionName"></param>
      /// <param name="actionParameters">List of parameters delimited with '|' or tabs</param>
      /// <returns>Return value for Gets or OK.</returns>
      private string ProcessCustomAction(string actionName, string actionParameters)
      {
         string result = "OK";
         char[] delimiters = new char[] { '|', '\t' };
         string[] values = actionParameters.Split(delimiters);
         switch (actionName)
         {
            case "Lunatic:SetUnparkPositions":
               Settings.AxisUnparkPosition = new AxisPosition(Convert.ToInt32(values[0]), Convert.ToInt32(values[1]));
               break;

            case "Lunatic:SetParkPositions":
               Settings.AxisParkPosition = new AxisPosition(Convert.ToInt32(values[0]), Convert.ToInt32(values[1]));
               break;

            //case "Lunatic:SetTrackUsingPEC":
            //   Settings.TrackUsingPEC = Convert.ToBoolean(actionParameters);
            //   break;

            //case "Lunatic:SetCheckRASync":
            //   Settings.CheckRASync = Convert.ToBoolean(actionParameters);
            //   break;

            //case "Lunatic:SetAutoGuiderPortRates":
            //   Settings.RAAutoGuiderPortRate = (AutoguiderPortRate)Convert.ToInt32(values[0]);
            //   Settings.DECAutoGuiderPortRate = (AutoguiderPortRate)Convert.ToInt32(values[1]);
            //   _Mount.EQ_SetAutoguiderPortRate(AxisId.Axis1_RA, Settings.RAAutoGuiderPortRate);
            //   _Mount.EQ_SetAutoguiderPortRate(AxisId.Axis2_DEC, Settings.DECAutoGuiderPortRate);
            //   break;

            case "Lunatic:SetCustomTrackingRates":
               Settings.CustomTrackingRate[0] = Convert.ToDouble(values[0]);
               Settings.CustomTrackingRate[1] = Convert.ToDouble(values[1]);
               break;

            case "Lunatic:SetSiteTemperature":
               _AscomToolsCurrentPosition.Transform.SiteTemperature = Convert.ToDouble(values[0]);
               break;

            default:
               throw new ASCOM.ActionNotImplementedException("Action " + actionName + " is not implemented by this driver");
         }
         return result;
      }

      private bool ProcessCommandBool(string command, bool raw)
      {
         bool result = false;
         switch (command)
         {
            case "Lunatic:IsInitialised":
               result = _Controller.IsConnected;
               break;
            case "Lunatic:SetLimitsActive":
               CheckLimitsActive = raw;
               break;
            default:
               throw new ASCOM.DriverException(string.Format("CommandBool command is not recognised '{0}'.", command));

         }
         return result;
      }

      private string ProcessCommandString(string command, bool raw)
      {
         string result = "Error";
         switch (command)
         {
            case "Lunatic:GetParkStatus":
               result = ((int)Settings.ParkStatus).ToString();
               break;
            case "Lunatic:GetAxisPositions":
               result = _CurrentPosition.ObservedAxes.ToDegreesString();
               break;
            default:
               throw new ASCOM.DriverException(string.Format("CommandString command is not recognised '{0}'.", command));

         }
         return result;
      }
   }
}
