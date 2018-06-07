using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DeployLX.CodeVeil.CompileTime.v5;
using ToastNotifications.Core;
using Zapuskator.Views;
namespace Zapuskator.ViewModels
{
    
    public class CustomNotificationViewModel : NotificationBase, INotifyPropertyChanged
    {
        private PingNotificationView _displayPart;

        public override NotificationDisplayPart DisplayPart => _displayPart ?? (_displayPart = new PingNotificationView(this));

        public CustomNotificationViewModel(string message)
        {
            Message = message;
        }

        private string _message;
        public string Message
        {
            get
            {
                return _message;
            }
            set
            {
                _message = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
