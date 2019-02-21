/*
BSD 2-Clause License

Copyright (c) 2019, Philip Crompton
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this
  list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE. 
*/

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
            "Lunatic:SetSiteTemperature",
            "Lunatic:SetPolarScopeBrightness"
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
               TelescopeSettingsProvider.Current.SaveSettings();
               break;

            case "Lunatic:SetSiteTemperature":
               _AscomToolsCurrentPosition.Transform.SiteTemperature = Convert.ToDouble(values[0]);
               break;

            case "Lunatic:SetPolarScopeBrightness":
               if (!Connected)
               {
                  throw new ASCOM.NotConnectedException("Astro EQ is not connected.");
               }
               LogMessage("ProcessCustomAction", "{0} - {0}", actionName, actionParameters);
               Controller.MCSetPolarScopeBrightness(Convert.ToInt32(values[0]));
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
               result = Controller.IsConnected;
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
