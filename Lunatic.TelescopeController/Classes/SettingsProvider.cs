using ASCOM.LunaticAstroEQ.Core;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;

namespace Lunatic.TelescopeController
{
   public class SettingsProvider : ISettingsProvider<TelescopeControlSettings>
   {
      private static TelescopeControlSettings _Settings = null;

      private const string CONFIG_SETTINGS_FILENAME = "TelescopeControl.config";

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
         _MajorVersion = versionInfo.ProductMajorPart;
         _MinorVersion = versionInfo.ProductMinorPart;
      }

      #endregion



      public SettingsProvider()
      {
         if (_Settings == null)
         {
            LoadSettings();
         }
      }


      public TelescopeControlSettings Settings
      {
         get
         {
            return SettingsProvider._Settings;
         }
      }


      /// <summary>
      /// Loads any previously saved settings
      /// </summary>
      private void LoadSettings()
      {
         string settingsFile = Path.Combine(UserSettingsFolder, CONFIG_SETTINGS_FILENAME);
         if (File.Exists(settingsFile))
         {
            using (StreamReader sr = new StreamReader(settingsFile))
            {
               JsonSerializer serializer = new JsonSerializer();
               _Settings = (TelescopeControlSettings)serializer.Deserialize(sr, typeof(TelescopeControlSettings));
            }
         }
         if (_Settings == null)
         {
            _Settings = new TelescopeControlSettings();     // Initialise with default values.
         }
      }

      /// <summary>
      /// Saves the current settings to user storage
      /// </summary>
      [SuppressMessage("Microsoft.Usage", "CA2202: Do not dispose objects multiple times")]
      public void SaveSettings()
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
