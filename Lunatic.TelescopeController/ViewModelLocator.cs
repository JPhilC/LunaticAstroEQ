using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using ASCOM.LunaticAstroEQ.Core;
using Microsoft.Practices.ServiceLocation;
using System;

namespace Lunatic.TelescopeController.ViewModel
{
   /// <summary>
   /// This class contains static references to all the view models in the
   /// application and provides an entry point for the bindings.
   /// </summary>
   public class ViewModelLocator
   {
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
