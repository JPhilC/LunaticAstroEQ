/*
BSD 2-Clause License

Copyright (c) 2019, Philip Crompton, Email: phil@lunaticsoftware.org
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

using ASCOM.LunaticAstroEQ.Core;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;

namespace ASCOM.LunaticAstroEQ.Controller
{
   public class SettingsManager : ISettingsProvider<ControllerSettings>
   {
      private static ControllerSettings _Settings = null;

      private object _Lock = new object();
#if BETA
      private const string CONFIG_SETTINGS_FILENAME = "AstroEQController_BetaTest.config";
#else
      private const string CONFIG_SETTINGS_FILENAME = "AstroEQController.config";
#endif


      #region Version info ...
      private static string _CompanyName = null;
      public static string CompanyName
      {
         get
         {
            if (_CompanyName == null)
            {
               LoadAssemblyInfo();
            }
            return _CompanyName;
         }
      }

      private static string _Copyright = null;
      public static string Copyright
      {
         get
         {
            if (_Copyright == null)
            {
               LoadAssemblyInfo();
            }
            return _Copyright;
         }
      }
      private static string _Comments = null;
      public static string Comments
      {
         get
         {
            if (_Comments == null)
            {
               LoadAssemblyInfo();
            }
            return _Comments;
         }
      }
      private static int? _MajorVersion = null;
      public static int MajorVersion
      {
         get
         {
            if (_MajorVersion == null)
            {
               LoadAssemblyInfo();
            }
            return _MajorVersion.Value;
         }
      }
      private static int? _MinorVersion = null;
      public static int MinorVersion
      {
         get
         {
            if (_MinorVersion == null)
            {
               LoadAssemblyInfo();
            }
            return _MinorVersion.Value;
         }
      }

      private static string _UserSettingsFolder = null;
      public static string UserSettingsFolder
      {
         get
         {
            if (_UserSettingsFolder == null)
            {
               LoadAssemblyInfo();
               // Ensure that the folder exists
               _UserSettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), CompanyName);
               Directory.CreateDirectory(_UserSettingsFolder);
            }
            return _UserSettingsFolder;
         }
      }
      /// <summary>
      /// Override to set values for:
      /// _CompanyName
      /// _Copyright
      /// _Comment
      /// _MajorVersion
      /// _MinorVersion
      /// </summary>
      private static void LoadAssemblyInfo()
      {
         FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
         _CompanyName = versionInfo.CompanyName;
         _Copyright = versionInfo.LegalCopyright;
         _Comments = versionInfo.Comments;
         _MajorVersion = versionInfo.ProductMajorPart;
         _MinorVersion = versionInfo.ProductMinorPart;
      }

#endregion



      public SettingsManager()
      {
         if (_Settings == null)
         {
            LoadSettings();
         }
      }

      public ControllerSettings Settings
      {
         get
         {
            return SettingsManager._Settings;
         }
      }


      /// <summary>
      /// Loads any previously saved settings
      /// </summary>
      private void LoadSettings()
      {
         lock (_Lock)
         {
            string settingsFile = Path.Combine(UserSettingsFolder, CONFIG_SETTINGS_FILENAME);
            if (File.Exists(settingsFile))
            {
               using (StreamReader sr = new StreamReader(settingsFile))
               {
                  JsonSerializer serializer = new JsonSerializer();
                  _Settings = (ControllerSettings)serializer.Deserialize(sr, typeof(ControllerSettings));
               }
            }
            if (_Settings == null)
            {
               _Settings = new ControllerSettings();   // Initilise with default values.
            }
         }
      }

      /// <summary>
      /// Saves the current settings to user storage
      /// </summary>
      [SuppressMessage("Microsoft.Usage", "CA2202: Do not dispose objects multiple times")]
      public void SaveSettings()
      {
         lock (_Lock)
         {
            string settingsFile = Path.Combine(UserSettingsFolder, CONFIG_SETTINGS_FILENAME);
            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;
            using (StreamWriter sw = new StreamWriter(settingsFile))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
               {
                  writer.Formatting = Newtonsoft.Json.Formatting.Indented;
                  serializer.Serialize(writer, _Settings);
               }
            }
         }
      }


   }
}
