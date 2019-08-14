using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace Lunatic.TelescopeController
{
   public class DeviceNotificationService
   {

      public static string USBDeviceAddedNotification = "DD41AD30-12BD-45D5-A34E-6462AE8982D4";
      public static string USBDeviceRemovedNotification = "59303F25-5444-44BC-829A-8B791A5090B4";

      #region Singleton pattern ...
      private static DeviceNotificationService _Instance = null;

      public static DeviceNotificationService Instance
      {
         get
         {
            if (_Instance == null)
            {
               _Instance = new DeviceNotificationService();
            }
            return _Instance;
         }
      }

      #endregion

      private IntPtr m_hNotifyDevNode;
      private System.Windows.Window _ParentWindow;

      private DeviceNotificationService()
      {
      }

      public void Start()
      {
         _ParentWindow = Application.Current.MainWindow;

         HwndSource source = PresentationSource.FromVisual(_ParentWindow) as HwndSource;
         source.AddHook(WndProc);

         Guid hidGuid = new Guid("4d1e55b2-f16f-11cf-88cb-001111000030");     // Game Controller
         //Guid usbXpressGuid = new Guid("3c5e1462-5695-4e18-876b-f3f3d08aaf18");
         //Guid cp210xGuid = new Guid("993f7832-6e2d-4a0f-b272-e2c78e74f93e");
         //Guid newCP210xGuid = new Guid("a2a39220-39f4-4b88-aecb-3d86a35dc748");
         //Guid usbDeviceGuid = new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED");  // USB Serial Ports

         RegisterNotification(hidGuid);
      }

      public void Shutdown()
      {
         if (_ParentWindow != null)
         {
            System.Diagnostics.Debug.WriteLine("DeviceNotificationService shutting down.");
            UnregisterNotification();
            _ParentWindow = null;
            _Instance = null;
            System.Diagnostics.Debug.WriteLine("DeviceNotificationService shutdown complete.");
         }
      }

      private void RegisterNotification(Guid guid)
      {
         DeviceBroadcastStructures.DEV_BROADCAST_DEVICEINTERFACE devIF = new DeviceBroadcastStructures.DEV_BROADCAST_DEVICEINTERFACE();
         IntPtr devIFBuffer;

         // Set to HID GUID
         devIF.dbcc_size = Marshal.SizeOf(devIF);
         devIF.dbcc_devicetype = DeviceBroadcastStructures.DBT_DEVTYP_DEVICEINTERFACE;
         devIF.dbcc_reserved = 0;
         devIF.dbcc_classguid = guid;

         // Allocate a buffer for DLL call
         devIFBuffer = Marshal.AllocHGlobal(devIF.dbcc_size);

         // Copy devIF to buffer
         Marshal.StructureToPtr(devIF, devIFBuffer, true);

         // Register for HID device notifications
         m_hNotifyDevNode = DeviceBroadcastStructures.RegisterDeviceNotification((new WindowInteropHelper(_ParentWindow)).Handle, devIFBuffer, DeviceBroadcastStructures.DEVICE_NOTIFY_WINDOW_HANDLE);

         // Copy buffer to devIF
         Marshal.PtrToStructure(devIFBuffer, devIF);

         // Free buffer
         Marshal.FreeHGlobal(devIFBuffer);
      }

      // Unregister HID device notification
      private void UnregisterNotification()
      {
         uint ret = DeviceBroadcastStructures.UnregisterDeviceNotification(m_hNotifyDevNode);
      }

      protected IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
      {
         // Intercept the WM_DEVICECHANGE message
         if (msg == DeviceBroadcastStructures.WM_DEVICECHANGE)
         {
            // Get the message event type
            int nEventType = wParam.ToInt32();

            // Check for devices being connected or disconnected
            if (nEventType == DeviceBroadcastStructures.DBT_DEVICEARRIVAL ||
                nEventType == DeviceBroadcastStructures.DBT_DEVICEREMOVECOMPLETE)
            {
               DeviceBroadcastStructures.DEV_BROADCAST_HDR hdr = new DeviceBroadcastStructures.DEV_BROADCAST_HDR();

               // Convert lparam to DEV_BROADCAST_HDR structure
               Marshal.PtrToStructure(lParam, hdr);

               if (hdr.dbch_devicetype == DeviceBroadcastStructures.DBT_DEVTYP_DEVICEINTERFACE)
               {
                  DeviceBroadcastStructures.DEV_BROADCAST_DEVICEINTERFACE_1 devIF = new DeviceBroadcastStructures.DEV_BROADCAST_DEVICEINTERFACE_1();

                  // Convert lparam to DEV_BROADCAST_DEVICEINTERFACE structure
                  Marshal.PtrToStructure(lParam, devIF);

                  // Get the device path from the broadcast message
                  string devicePath = new string(devIF.dbcc_name);

                  // Remove null-terminated data from the string
                  int pos = devicePath.IndexOf((char)0);
                  if (pos != -1)
                  {
                     devicePath = devicePath.Substring(0, pos);
                  }

                  // An HID device was connected or removed
                  if (nEventType == DeviceBroadcastStructures.DBT_DEVICEREMOVECOMPLETE)
                  {
                     System.Diagnostics.Debug.WriteLine("Device \"" + devicePath + "\" was removed");
                     Messenger.Default.Send<NotificationMessage>(new NotificationMessage(DeviceNotificationService.USBDeviceRemovedNotification));
                  }
                  else if (nEventType == DeviceBroadcastStructures.DBT_DEVICEARRIVAL)
                  {
                     System.Diagnostics.Debug.WriteLine("Device \"" + devicePath + "\" arrived");
                     Messenger.Default.Send<NotificationMessage>(new NotificationMessage(DeviceNotificationService.USBDeviceAddedNotification));
                  }
               }
            }
         }
         return IntPtr.Zero;
      }

   }
}
