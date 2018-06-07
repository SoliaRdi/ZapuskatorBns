using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DeployLX.CodeVeil.CompileTime.v5;
using ToastNotifications.Core;
using Zapuskator.ViewModels;

namespace Zapuskator.Views
{
    /// <summary>
    /// Interaction logic for PingNotificationView.xaml
    /// </summary>
    
    public partial class PingNotificationView : NotificationDisplayPart
    {
        private CustomNotificationViewModel _customNotification;
        public PingNotificationView(CustomNotificationViewModel customNotification)
        {
            _customNotification = customNotification;
            DataContext = customNotification;
            InitializeComponent();
        }
    }
}
