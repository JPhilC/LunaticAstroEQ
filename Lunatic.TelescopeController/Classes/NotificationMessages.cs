using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lunatic.TelescopeController
{
   

   public class ErrorNotificationMessage : NotificationMessage
   {
      public ErrorNotificationMessage(string notification) : base(notification) { }
      public ErrorNotificationMessage(object sender, string notification) : base(sender, notification) { }
      public ErrorNotificationMessage(object sender, object target, string notification) : base(sender, target, notification) { }
   }
}
