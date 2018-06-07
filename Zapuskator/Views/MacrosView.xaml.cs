using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using DeployLX.CodeVeil.CompileTime.v5;
using Interceptor;
using Zapuskator.Framework;
using Zapuskator.Properties;

namespace Zapuskator.Views
{
    /// <summary>
    ///     Interaction logic for MacrosView.xaml
    /// </summary>
    
    public partial class MacrosView : Window
    {
        // Using a DependencyProperty as the backing store for ContentAssisteSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ContentAssistSourceProperty =
            DependencyProperty.Register("ContentAssistSource", typeof(List<string>), typeof(MainView), new UIPropertyMetadata(new List<string>()));

        // Using a DependencyProperty as the backing store for ContentAssistTriggers.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ContentAssistTriggersProperty =
            DependencyProperty.Register("ContentAssistTriggers", typeof(List<char>), typeof(MainView), new UIPropertyMetadata(new List<char>()));

        private List<string> _variables;

        public MacrosView()
        {
            InitializeComponent();
            DataContext = this;
            foreach (var i in Variables)
            {
                ContentAssistSource.Add(i);
                ContentAssistTriggers.Add(i[0]);
            }

            ContentAssistSource.Add("off");
        }

        private List<string> Variables
        {
            get
            {
                if (_variables == null || _variables.Count == 0)
                {
                    _variables = Enum.GetNames(typeof(MouseState)).Where(x => !x.Contains("WHEEL")).ToList();
                    _variables.AddRange(Enum.GetNames(typeof(Keys)).ToList());
                }

                return _variables;
            }
            set => _variables = value;
        }

        public List<string> ContentAssistSource
        {
            get => (List<string>) GetValue(ContentAssistSourceProperty);
            set => SetValue(ContentAssistSourceProperty, value);
        }


        public List<char> ContentAssistTriggers
        {
            get => (List<char>) GetValue(ContentAssistTriggersProperty);
            set => SetValue(ContentAssistTriggersProperty, value);
        }

        private void SaveMacros2(object sender, RoutedEventArgs e)
        {
            Services.Tracker.RunAutoPersist();
        }
    }
}