﻿using GalaSoft.MvvmLight.Threading;
using Lunatic.TelescopeController.ViewModel;
using System.Windows;

namespace Lunatic.TelescopeController
{
   /// <summary>
   /// Interaction logic for App.xaml
   /// </summary>
   public partial class App : Application
   {
      private void Application_Startup(object sender, StartupEventArgs e)
      {
         DispatcherHelper.Initialize();
      }
      private void Application_Exit(object sender, ExitEventArgs e)
      {
         ViewModelLocator.Cleanup();
      }
   }
}
