using GalaSoft.MvvmLight.Threading;
using Lunatic.TelescopeController.ViewModel;
using System.Threading;
using System.Windows;

namespace Lunatic.TelescopeController
{
   /// <summary>
   /// Interaction logic for App.xaml
   /// </summary>
   public partial class App : Application
   {
      DeviceNotificationService _DeviceNotificationService = null;

      private void Application_Startup(object sender, StartupEventArgs e)
      {
         DispatcherHelper.Initialize();
         Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture;

         
      }

      private void Application_Exit(object sender, ExitEventArgs e)
      {
         if (_DeviceNotificationService != null)
         {
            _DeviceNotificationService.Shutdown();
         }
         ViewModelLocator.Cleanup();
      }

      private void Application_Activated(object sender, System.EventArgs e)
      {
         // Activate the USB watching service the first time the application is activated.
         if (_DeviceNotificationService == null)
         {
            _DeviceNotificationService = DeviceNotificationService.Instance;
         }
      }
   }
}
