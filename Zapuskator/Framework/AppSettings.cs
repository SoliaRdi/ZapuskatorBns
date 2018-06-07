using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using DeployLX.CodeVeil.CompileTime.v5;
using Jot;
using Jot.DefaultInitializer;

namespace Zapuskator.Framework
{
    [Obfuscate(false)]
    public static class Services
    {

        public static StateTracker Tracker = new StateTracker();

        public static Profiles Profiles = new Profiles();
    }

    [Serializable]
    [Obfuscate(false)]
    public class ModOption : INotifyPropertyChanged
    {
        private string _name;
        private string _description;
        private string _path = "";
        private bool _enabled;
        private bool _isFolder;

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyOfPropertyChange([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ModOption(string name, string description,bool isFolder=false)
        {
            Name = name;
            Description = description;
            IsFolder = isFolder;
        }

        [Trackable]
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                NotifyOfPropertyChange();
            }
        }

        [Trackable]
        public bool IsFolder
        {
            get { return _isFolder; }
            set
            {
                _isFolder = value;
                NotifyOfPropertyChange();
            }
        }

        [Trackable]
        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
                NotifyOfPropertyChange();
            }
        }

        [Trackable]
        public string Path
        {
            get { return _path; }
            set
            {
                _path = value;
                NotifyOfPropertyChange();
            }
        }

        [Trackable]
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                _enabled = value;
                NotifyOfPropertyChange();
            }
        }
    }

    [Serializable]
    [Obfuscate(false)]
    public class GeneralSettings : INotifyPropertyChanged
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyOfPropertyChange([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _uid = "0";
        private string _cookie = "-";
        private string _chromePath = "-";
        private string _authorization = "-";
        private string _hwid = "-";
        private string _expiresAt = "-";
        private string _macrosMouse = "";
        private string _macrosKeyboard = "";
        private string _loopMacros = "";
        private string _loopMacros2 = "";
        private string _loopMacros3 = "";
        private string _loopMacros4 = "";
        private string _bnsPath = "";
        private int _bnsPriority = 0;
        private bool _bnsUseAllCores = false;
        private bool _bnsNoTxStream = false;
        private bool _normalLaunch = false;
        private int _pingPosX = 10;
        private int _pingPosY = 10;

        [Trackable]
        public string Uid
        {
            get { return _uid; }
            set
            {
                _uid = value;
                NotifyOfPropertyChange();
            }
        }

        [Trackable]
        public string Cookie
        {
            get { return _cookie; }
            set
            {
                _cookie = value;
                NotifyOfPropertyChange(nameof(Cookie));
            }
        }

        [Trackable]
        public string Chrome_Path
        {
            get { return _chromePath; }
            set
            {
                _chromePath = value;
                NotifyOfPropertyChange(nameof(Chrome_Path));
            }
        }

        [Trackable]
        public string Authorization
        {
            get { return _authorization; }
            set
            {
                _authorization = value;
                NotifyOfPropertyChange(nameof(Authorization));
            }
        }

        [Trackable]
        public string HWID
        {
            get { return _hwid; }
            set
            {
                _hwid = value;
                NotifyOfPropertyChange(nameof(HWID));
            }
        }

        [Trackable]
        public string ExpiresAt
        {
            get { return _expiresAt; }
            set
            {
                _expiresAt = value;
                NotifyOfPropertyChange(nameof(ExpiresAt));
            }
        }

        [Trackable]
        public string MacrosMouse
        {
            get { return _macrosMouse; }
            set
            {
                _macrosMouse = value;
                NotifyOfPropertyChange(nameof(MacrosMouse));
            }
        }

        [Trackable]
        public string MacrosKeyboard
        {
            get { return _macrosKeyboard; }
            set
            {
                _macrosKeyboard = value;
                NotifyOfPropertyChange(nameof(MacrosKeyboard));
            }
        }

        [Trackable]
        public string LoopMacros
        {
            get { return _loopMacros; }
            set
            {
                _loopMacros = value;
                NotifyOfPropertyChange(nameof(LoopMacros));
            }
        }

        [Trackable]
        public string LoopMacros2
        {
            get { return _loopMacros2; }
            set
            {
                _loopMacros2 = value;
                NotifyOfPropertyChange(nameof(LoopMacros2));
            }
        }

        [Trackable]
        public string LoopMacros3
        {
            get { return _loopMacros3; }
            set
            {
                _loopMacros3 = value;
                NotifyOfPropertyChange(nameof(LoopMacros3));
            }
        }

        [Trackable]
        public string LoopMacros4
        {
            get { return _loopMacros4; }
            set
            {
                _loopMacros4 = value;
                NotifyOfPropertyChange(nameof(LoopMacros4));
            }
        }

        [Trackable]
        public string BnsPath
        {
            get { return _bnsPath; }
            set
            {
                _bnsPath = value;
                NotifyOfPropertyChange(nameof(BnsPath));
            }
        }

        [Trackable]
        public int BnsPriority
        {
            get { return _bnsPriority; }
            set
            {
                _bnsPriority = value;
                NotifyOfPropertyChange(nameof(BnsPriority));
            }
        }

        [Trackable]
        public bool BnsUseAllCores
        {
            get { return _bnsUseAllCores; }
            set
            {
                _bnsUseAllCores = value;
                NotifyOfPropertyChange(nameof(BnsUseAllCores));
            }
        }

        [Trackable]
        public bool BnsNoTxStream
        {
            get { return _bnsNoTxStream; }
            set
            {
                _bnsNoTxStream = value;
                NotifyOfPropertyChange(nameof(BnsNoTxStream));
            }
        }

        [Trackable]
        public bool NormalLaunch
        {
            get { return _normalLaunch; }
            set
            {
                _normalLaunch = value;
                NotifyOfPropertyChange(nameof(NormalLaunch));
            }
        }

        [Trackable]
        public bool Summoner { get; set; } = false;

        [Trackable]
        public bool Assasin { get; set; } = false;

        [Trackable]
        public bool Gunner { get; set; } = false;

        [Trackable]
        public bool Destroyer { get; set; } = false;

        [Trackable]
        public bool Force { get; set; } = false;

        [Trackable]
        public bool Kungfu { get; set; } = false;

        [Trackable]
        public bool BM { get; set; } = false;

        [Trackable]
        public bool LinBM { get; set; } = false;

        [Trackable]
        public bool Warlock { get; set; } = false;

        [Trackable]
        public bool SF { get; set; } = false;

        [Trackable]
        public bool Soulburn { get; set; } = false;

        [Trackable]
        public bool DisableScreen { get; set; } = false;

        [Trackable]
        public int PingPosX
        {
            get { return _pingPosX; }
            set
            {
                _pingPosX = value;
                NotifyOfPropertyChange();
            }
        }

        [Trackable]
        public int PingPosY
        {
            get { return _pingPosY; }
            set
            {
                _pingPosY = value;
                NotifyOfPropertyChange();
            }
        }
    }
    [Serializable]
    [Obfuscate(false)]
    public class Profiles:INotifyPropertyChanged
    {
        private AppSettings _currentProfile;
        private int _lastUsedProfile;

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyOfPropertyChange([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public Profiles()
        {
            AllProfiles = new ObservableCollection<AppSettings>();
        }

        [Trackable]
        public int LastUsedProfile
        {
            get { return _lastUsedProfile; }
            set { _lastUsedProfile = value;
                NotifyOfPropertyChange();
            }
        }

        public AppSettings CurrentProfile
        {
            get { return _currentProfile; }
            set
            {
                _currentProfile = value;
                NotifyOfPropertyChange();
            }
        }

        [Trackable]
        public ObservableCollection<AppSettings> AllProfiles { get; set; }
    }

    [Serializable]
    [Obfuscate(false)]
    public class AppSettings:INotifyPropertyChanged
    {
        private string _name;

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyOfPropertyChange([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public AppSettings()
        {
            ModOptions = new ObservableCollection<ModOption>();
            GeneralSettings = new GeneralSettings();
            XmlSettings = new XmlSettings();
            Peaches = new Peaches();
        }

        public string Name
        {
            get { return _name; }
            set {
                _name = value;
                NotifyOfPropertyChange();
            }
        }

        [Trackable]
        public ObservableCollection<ModOption> ModOptions { get; set; }

        [Trackable]
        public GeneralSettings GeneralSettings { get; set; }

        [Trackable]
        public XmlSettings XmlSettings { get; set; }

        [Trackable]
        public Peaches Peaches { get; set; }

    }

    [Serializable]
    [Obfuscate(false)]
    public class Peaches:INotifyPropertyChanged
    {
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyOfPropertyChange([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private int _server=0;
        private bool _sync=false;
        private bool _notifications;

        [Trackable]
        public int Server
        {
            get { return _server; }
            set
            {
                _server = value;
                NotifyOfPropertyChange();
            }
        }

        [Trackable]
        public bool Sync
        {
            get { return _sync; }
            set
            {
                _sync = value;
                NotifyOfPropertyChange();
            }
        }

        [Trackable]
        public bool Notifications
        {
            get { return _notifications; }
            set
            {
                _notifications = value;
                NotifyOfPropertyChange();
            }
        }
    }
    [Obfuscate(false)]
    public class XmlSettings
    {
        [Trackable]
        public bool DpsMod { get; set; }
        [Trackable]
        public bool RatingMod { get; set; }
        [Trackable]
        public bool FastSkillChange { get; set; }
        [Trackable]
        public bool FastItems { get; set; }
        [Trackable]
        public bool FastRes { get; set; }
        [Trackable]
        public bool InviseIgnore { get; set; }
        [Trackable]
        public bool NoTooltips { get; set; }
        [Trackable]
        public bool NoBoast { get; set; }
        [Trackable]
        public bool AfkMaster { get; set; }
        [Trackable]
        public bool FastClose { get; set; }
        [Trackable]
        public bool OptimizedMode { get; set; }
        [Trackable]
        public bool NoLobbyAnim { get; set; }
        [Trackable]
        public bool WarlockLich { get; set; }
    }
}