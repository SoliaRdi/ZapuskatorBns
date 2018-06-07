using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using Zapuskator.Properties;
using System.Configuration;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DeployLX.CodeVeil.CompileTime.v5;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Zapuskator
{
    /// <summary>
    ///     Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        
        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var writer = new StreamWriter(File.Open("errorlog.txt", FileMode.Append));
            writer.WriteLine(JsonConvert.SerializeObject(e));
            writer.Flush();
            writer.Close();
            Environment.Exit(1);
        }

        private void Application_Activated(object sender, EventArgs e)
        {
           AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception) e.ExceptionObject;
            var writer = new StreamWriter(File.Open("errorlog.txt", FileMode.Append));
            writer.WriteLine(JsonConvert.SerializeObject(ex));
            writer.Flush();
            writer.Close();
            Environment.Exit(1);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var license = new LicenseHelper();
            license.Required();
        }

 
        private void Application_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            
        }
    }

    public class Program
    {
        

        [STAThread]
        public static void Main()
        {
            
           // AppDomain.CurrentDomain.AssemblyResolve += OnResolveAssembly;
            App.Main();
            
        }


        //private static Assembly OnResolveAssembly(object sender, ResolveEventArgs args)
        //{
        //    Assembly executingAssembly = Assembly.GetExecutingAssembly();
        //    AssemblyName assemblyName = new AssemblyName(args.Name);

        //    string path = assemblyName.Name + ".dll";
        //    if (assemblyName.CultureInfo.Equals(CultureInfo.InvariantCulture) == false)
        //    {
        //        path = String.Format(@"{0}\{1}", assemblyName.CultureInfo, path);
        //    }

        //    using (Stream stream = executingAssembly.GetManifestResourceStream(path))
        //    {
        //        if (stream == null)
        //            return null;

        //        byte[] assemblyRawBytes = new byte[stream.Length];
        //        stream.Read(assemblyRawBytes, 0, assemblyRawBytes.Length);
        //        return Assembly.Load(assemblyRawBytes);
        //    }
        //}
    }
}