/*
-- BSD 2-Clause License

Copyright(c) 2019, Philip Crompton
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
DISCLAIMED.IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE. 
*/

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using ASCOM.LunaticAstroEQ.Core;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Resources;
using System.Reflection;

namespace Lunatic.TelescopeController.ViewModel
{
   /// <summary>
   /// This class contains static references to all the view models in the
   /// application and provides an entry point for the bindings.
   /// </summary>
   public class ViewModelLocator
   {

      private ResourceManager _Announcements = null;

      public ResourceManager Announcements
      {
         get
         {
            if (_Announcements == null)
            {
               _Announcements = new ResourceManager("Announcements", Assembly.GetExecutingAssembly());
            }
            return _Announcements;
         }
      }

      /// <summary>
      /// Initializes a new instance of the ViewModelLocator class.
      /// </summary>
      public ViewModelLocator()
      {
         ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

         if (ViewModelBase.IsInDesignModeStatic)
         {
            // Create design time view services and models
            SimpleIoc.Default.Register<ISettingsProvider<TelescopeControlSettings>, SettingsProvider>();
         }
         else
         {
            // Create run time view services and models
            SimpleIoc.Default.Register<ISettingsProvider<TelescopeControlSettings>, SettingsProvider>();
         }

         SimpleIoc.Default.Register<MainViewModel>();
      }



      public MainViewModel Main
      {
         get
         {
            return ServiceLocator.Current.GetInstance<MainViewModel>();
         }
      }

      public static void Cleanup()
      {
         MainViewModel mv = ServiceLocator.Current.GetInstance<MainViewModel>();
         mv.Cleanup();
         SimpleIoc.Default.Unregister<MainViewModel>();
         SimpleIoc.Default.Unregister<ISettingsProvider<TelescopeControlSettings>>();
      }
   }
}
