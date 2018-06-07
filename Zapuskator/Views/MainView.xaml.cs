
using System;
using System.Windows;
using System.Windows.Controls;
using AutoUpdaterDotNET;
using DeployLX.CodeVeil.CompileTime.v5;
using MahApps.Metro.Controls;


namespace Zapuskator.Views
{
     
    public partial class MainView : MetroWindow
    {
        public MainView()
        {
            InitializeComponent();
        }

        private void LogData_TextChanged(object sender, TextChangedEventArgs e)
        {
            (sender as TextBox).ScrollToEnd();
        }
        private void ToggleFlyout(int index)
        {
            var flyout = this.Flyouts.Items[index] as Flyout;
            if (flyout == null)
            {
                return;
            }

            flyout.IsOpen = !flyout.IsOpen;
        }
        private void ShowHelp(object sender, RoutedEventArgs e)
        {
            this.ToggleFlyout(0);
        }
    }
}