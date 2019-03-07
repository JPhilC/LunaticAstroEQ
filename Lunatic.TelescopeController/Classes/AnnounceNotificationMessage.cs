using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;

namespace Lunatic.TelescopeController
{
    public class AnnounceNotificationMessage:NotificationMessage
    {
        public AnnounceNotificationMessage(string notification) : base(notification) { }
    }
}
