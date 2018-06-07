using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using DeployLX.CodeVeil.CompileTime.v5;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using Zapuskator.Framework;

namespace Zapuskator.ViewModels
{
    
    public class DownloadModsListViewModel : Screen
    {
        private readonly IEventAggregator _eventAggregator;
        public IWindowManager _windowManager;
        public class ModToDownload
        {
            public string DownloadURL { get; set; } 
            public ModOption modOption { get; set; }
        }
        public List<ModToDownload> Mods { get; set; }
        public DownloadModsListViewModel(IEventAggregator eventAggregator, IWindowManager windowManager)
        {
            _eventAggregator = eventAggregator;
            _windowManager = windowManager;
            WebRequest request = WebRequest.Create("http://zapuskator-bns.azurewebsites.net/mods/modlist.txt");
            using (WebResponse jsonResponse = request.GetResponse())
            {
                StreamReader streamReader = new StreamReader(jsonResponse.GetResponseStream());
                String responseData = streamReader.ReadToEnd();
                Mods = JsonConvert.DeserializeObject<List<ModToDownload>>(responseData);
            }
        }

        public void DownloadMod(ModToDownload mod)
        {
            _windowManager.ShowDialog(new DownloadFilesViewModel(IoC.Get<IEventAggregator>(), mod.DownloadURL, "bnsfiles\\mods", mod.modOption));
        }
    }
}
