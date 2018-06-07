using System;
using System.Collections.Generic;
using Caliburn.Micro;
using System.Windows;
using System.Windows.Data;
using DeployLX.CodeVeil.CompileTime.v5;
using MahApps.Metro.Controls.Dialogs;
using Zapuskator.Framework;
using Zapuskator.ViewModels;

namespace Zapuskator
{
    [Obfuscate(false)]
    public class AppBootstrapper : BootstrapperBase
    {
        public AppBootstrapper()
        {
            Initialize();
        }
        private readonly SimpleContainer _container =
            new SimpleContainer();

        protected override object GetInstance(Type serviceType, string key)
        {
            return _container.GetInstance(serviceType, key);
        }

        protected override IEnumerable<object> GetAllInstances(Type serviceType)
        {
            return _container.GetAllInstances(serviceType);
        }

        protected override void BuildUp(object instance)
        {
            _container.BuildUp(instance);
        }
        protected override void Configure()
        {

            Services.Tracker.Configure(Services.Profiles).Apply();
            _container.Instance(_container);
            _container.Singleton<IWindowManager, WindowManager>();
            _container.Singleton<IEventAggregator, EventAggregator>();
            _container.PerRequest<MainViewModel>()
                .PerRequest<GameSettingsViewModel>()
                .PerRequest<ModsViewModel>()
                .PerRequest<XmlEditorViewModel>()
                .PerRequest<PeachesEventViewModel>()
                .PerRequest<ManageProfilesViewModel>()
                .PerRequest<DownloadModsListViewModel>();
            

        }
        protected override void OnStartup(object sender, StartupEventArgs e)
        {

            DisplayRootViewFor<MainViewModel>();

        }
    }

    public class NullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return value ?? (value = 0);
        }
    }
}