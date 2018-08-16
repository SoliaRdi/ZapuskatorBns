using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AutoUpdaterDotNET;
using Caliburn.Micro;
using Interceptor;
using Jot;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PaxScript.Net;
using STO.Framework;
using WebSocketSharp;
using Zapuskator.Framework;
using Zapuskator.Views;
using Action = System.Action;
using DeployLX.CodeVeil.CompileTime.v5;
namespace Zapuskator.ViewModels
{
    
    public class MainViewModel : Conductor<IScreen>.Collection.AllActive, IHandle<string>
    {
        public static WebSocket ws = new WebSocket("wss://127.0.0.1:9443/ws");
        public static ManagementEventWatcher startWatch;
        public readonly IDialogCoordinator _dialogCoordinator;
        private readonly IWindowManager _windowManager;
        public string _logData;
        
        public MainViewModel(IEventAggregator eventAggregator, IWindowManager windowManager)
        {
            _windowManager = windowManager;
            eventAggregator.Subscribe(this);
            SettingsViewModel = IoC.Get<GameSettingsViewModel>();
            ModsViewModel = IoC.Get<ModsViewModel>();
            XmlEditorViewModel = IoC.Get<XmlEditorViewModel>();
            PeachesEventViewModel = IoC.Get<PeachesEventViewModel>();
            _dialogCoordinator = DialogCoordinator.Instance;
            if (Services.Profiles.AllProfiles.Count == 0)
            {
                var path = Path.Combine(Environment.GetEnvironmentVariable("AppData"), "Microsoft", "Zapuskator", "AppSettings.json");
                if (File.Exists(path)) //Миграция при введении профилей. Удалить через 2 версии после 1.*.0.3
                {
                    StateTracker tr = new StateTracker();
                    AppSettings obj = new AppSettings();
                    tr.Configure(obj).Apply();
                    obj.Name = "Профиль 1";
                    Services.Profiles.AllProfiles.Add(obj);
                    File.Delete(path);
                }
                else
                {
                    Services.Profiles.AllProfiles.Add(new AppSettings {Name = "Профиль 1"});
                }
            }
            else
            {
                File.Delete(Path.Combine(Environment.GetEnvironmentVariable("AppData"), "Microsoft", "Zapuskator", "AppSettings.json"));
            }

            Services.Profiles.CurrentProfile = Services.Profiles.AllProfiles.ElementAt(Services.Profiles.LastUsedProfile);
            for (var i = 0; i < 4; i++)
            {
                ScriptSharing.ts[i] = new CancellationTokenSource();
                ScriptSharing.ct[i] = ScriptSharing.ts[i].Token;
                ScriptSharing.MacrosEnabled[i] = false;
            }

            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            ws.Origin = "https://ru.4game.com";
            ws.OnMessage += (s, a) =>
            {
                if (a.Data.Contains("error")) LogEvent("\nSocket: " + a.Data + "\n");

                if (a.Data.Contains("appIdentity"))
                {
                    LogEvent("Получен HWID.");
                    var json = JObject.Parse(a.Data);
                    json = JObject.Parse(json["data"].ToString());
                    Services.Profiles.CurrentProfile.GeneralSettings.HWID = (json["hwId"].ToString(Formatting.None) + ":" + json["appId"].ToString(Formatting.None)).Replace("\"", "");
                }

                if (a.Data.Contains("service started"))
                {
                    LogEvent("Соединение установлено.");
                    ws.Send("{\"name\":\"getFeature\"}");
                    ws.Send("{\"name\":\"getVersions\"}");
                    ws.Send("{\"name\":\"getStatus\",\"data\":\"{ }\"}");
                }
            };
            ws.Connect();
            ScriptSharing.input.OnKeyPressed += Input_OnKeyPressed;
            ScriptSharing.input.OnMousePressed += Input_OnMousePressed;
            var query = new SelectQuery("Win32_SystemDriver")
            {
                Condition = "Name = 'keyboard' OR Name = 'mouse'"
            };
            var searcher = new ManagementObjectSearcher(query);
            var drivers = searcher.Get();

            if (drivers.Count < 2)
                DriverInstalled = false;
            else
                DriverInstalled = true;
            if (!Directory.Exists("bnsfiles"))
                Directory.CreateDirectory("bnsfiles");
            if (!Directory.Exists("bnsfiles\\mods"))
                Directory.CreateDirectory("bnsfiles\\mods");
            if (!Directory.Exists("bnsfiles\\backup"))
                Directory.CreateDirectory("bnsfiles\\backup");
        }

        public static Dictionary<MouseState, dynamic> MouseRules { get; } = new Dictionary<MouseState, dynamic>();
        public static Dictionary<Keys, dynamic> KeyboardRules { get; } = new Dictionary<Keys, dynamic>();
        public Action TheMethod { get; set; }
        public GameSettingsViewModel SettingsViewModel { get; set; }
        public ModsViewModel ModsViewModel { get; set; }
        public XmlEditorViewModel XmlEditorViewModel { get; set; }
        public PeachesEventViewModel PeachesEventViewModel { get; set; }
        public bool DriverInstalled { get; set; }

        public string LogData
        {
            get => _logData;
            set
            {
                _logData = value;
                NotifyOfPropertyChange(() => LogData);
            }
        }
        
        public void Handle(string message)
        {
            switch (message)
            {
                case "GetHWID":
                    GetHWID();
                    break;
                case "ApplyAnimSet":
                    ApplyAnimSet();
                    break;
                default:
                    LogEvent(message);
                    break;
            }
        }
        
        public void BnsWatcher(object sender, EventArrivedEventArgs e)
        {
            var ClientPath = "\"" + Services.Profiles.CurrentProfile.GeneralSettings.BnsPath;
            startWatch.Stop();
            ClientPath += Services.Profiles.CurrentProfile.GeneralSettings.BnsPriority == 0 ? "\\bin\\Client.exe\"" : "\\bin64\\Client.exe\"";
            LogEvent("Параметры получены.");
            KillBnS();
            var FrostPath = ((ManagementBaseObject) e.NewEvent["TargetInstance"])["ExecutablePath"].ToString();
            var paramBns = ((ManagementBaseObject) e.NewEvent["TargetInstance"])["CommandLine"].ToString();
            paramBns = paramBns.Substring(paramBns.LastIndexOf("/username", StringComparison.Ordinal));
            var sliced = paramBns.Split(' ').ToList();
            var ind = sliced.FindIndex(row => row.Contains("Client.exe"));
            sliced[ind] = ClientPath;
            paramBns = string.Join(" ", sliced);
            var addParamBns = " -lite:2";
            if (Services.Profiles.CurrentProfile.GeneralSettings.BnsUseAllCores) addParamBns += " -USEALLAVAILABLECORES";
            if (Services.Profiles.CurrentProfile.GeneralSettings.BnsNoTxStream) addParamBns += " -NOTEXTURESTREAMING";
            var info = new ProcessStartInfo(FrostPath, paramBns.Replace("-lite:2", addParamBns));
            var process = Process.Start(info);
            if (process != null) process.PriorityClass = ProcessPriorityClass.Normal;
            LogEvent("Запускаем игру");
        }
        
        public bool CanLaunchBns()
        {
            return ws.ReadyState == WebSocketState.Open;
        }
        
        public async void LaunchBns()
        {
            KillBnS();
            if (!Services.Profiles.CurrentProfile.GeneralSettings.NormalLaunch)
            {
                if (!Helpers.IsAdministrator())
                    if (MessageDialogResult.Affirmative == await _dialogCoordinator.ShowMessageAsync(this, "Ошибка", "Необходим запуск от имени администратора."))
                        return;

                RemoveAnimations();
                try
                {
                    startWatch?.Stop();
                    var scope = new ManagementScope(@"\\.\root\CIMV2");
                    var queryString =
                        "SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Process' AND TargetInstance.Name='bns.exe'";
                    startWatch = new ManagementEventWatcher(scope,
                        new WqlEventQuery(queryString));
                    startWatch.EventArrived
                        += BnsWatcher;
                    startWatch.Start();
                }
                catch (Exception)
                {
                    startWatch?.Stop();
                    LogEvent("Не хватает прав администратора на выполнение операции.");
                    return;
                }
            }

            if (string.IsNullOrEmpty(Services.Profiles.CurrentProfile.GeneralSettings.HWID) || string.IsNullOrEmpty(Services.Profiles.CurrentProfile.GeneralSettings.Cookie) || string.IsNullOrEmpty(Services.Profiles.CurrentProfile.GeneralSettings.Authorization))
                LogEvent("Некоторые поля не заполнены");
            string responseString = "";
            try
            {
                using (var handler = new HttpClientHandler {UseCookies = false})
                using (var client = new HttpClient(handler))
                {
                    var values = new Dictionary<string, string>
                    {
                        {"device", Services.Profiles.CurrentProfile.GeneralSettings.HWID}
                    };

                    var content = new StringContent("{\"device\":\"" + Services.Profiles.CurrentProfile.GeneralSettings.HWID + "\"}");
                    client.DefaultRequestHeaders
                        .Accept
                        .Add(new MediaTypeWithQualityHeaderValue("*/*")); //ACCEPT header
                    client.DefaultRequestHeaders
                        .AcceptEncoding
                        .Add(new StringWithQualityHeaderValue("gzip")); //ACCEPT header, , br
                    client.DefaultRequestHeaders
                        .AcceptEncoding
                        .Add(new StringWithQualityHeaderValue("deflate"));
                    client.DefaultRequestHeaders
                        .AcceptEncoding
                        .Add(new StringWithQualityHeaderValue("br"));
                    client.DefaultRequestHeaders
                        .AcceptLanguage
                        .Add(new StringWithQualityHeaderValue("ru")); //ACCEPT header
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    client.DefaultRequestHeaders.Add("Cookie", Services.Profiles.CurrentProfile.GeneralSettings.Cookie);
                    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/65.0.3325.181 Safari/537.36");
                    client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", Services.Profiles.CurrentProfile.GeneralSettings.Authorization);
                    var response = await client.PostAsync("https://api2.4game.com/users/" + Services.Profiles.CurrentProfile.GeneralSettings.Uid + "/apps/22/sign-in-codes", content);
                    responseString = await response.Content.ReadAsStringAsync();

                    var jsonResponse = JObject.Parse(responseString);
                    jsonResponse = JObject.Parse(jsonResponse["signInCode"].ToString());
                    if ((bool) jsonResponse["isEncrypted"].ToObject(typeof(bool)))
                    {
                        var jsonData = new JObject
                        {
                            ["serviceId"] = 22,
                            ["accountId"] = int.Parse(jsonResponse["accountId"].ToString()),
                            ["login"] = (string) jsonResponse["serviceLogin"],
                            ["payloadEncrypted"] = (string) jsonResponse["code"],
                            ["serviceEnvironment"] = "live",
                            ["status"] = "installed",
                            ["readyStage"] = 2
                        };
                        var jsonSend = new JObject
                        {
                            ["name"] = "playEncrypted",
                            ["data"] = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(jsonData.ToString(Formatting.None)))
                        };
                        LogEvent("Получены данные для авторизации.");
                        LogEvent("Отправляем данные на сокет.");
                        ws.Send(jsonSend.ToString(Formatting.None));
                    }
                    else
                    {
                        var jsonData = new JObject
                        {
                            ["serviceId"] = 22,
                            ["accountId"] = int.Parse(jsonResponse["accountId"].ToString()),
                            ["login"] = (string) jsonResponse["serviceLogin"],
                            ["password"] = (string) jsonResponse["code"],
                            ["serviceEnvironment"] = "live",
                            ["status"] = "installed",
                            ["readyStage"] = 2
                        };
                        var jsonSend = new JObject
                        {
                            ["name"] = "play",
                            ["data"] = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(jsonData.ToString(Formatting.None)))
                        };
                        LogEvent("Получены данные для авторизации.");
                        LogEvent("Отправляем данные на сокет.");
                        ws.Send(jsonSend.ToString(Formatting.None));
                    }
                }
            }
            catch (Exception ex)
            {
                LogEvent("Ошибка подключения. Возможно стоит обновить bearer." + ex.Message);
                LogEvent(responseString);
                startWatch.Stop();
            }
        }
        
        public void KillBnS()
        {
            try
            {
                LogEvent("Закрываем активные процессы BnS.");
                foreach (var proc in Process.GetProcessesByName("Client"))
                    proc.Kill();
                Thread.Sleep(100);
                foreach (var proc in Process.GetProcessesByName("bns"))
                    proc.Kill();
            }
            catch (Exception ex)
            {
                LogEvent(ex.Message);
            }
        }
        
        public void GetHWID()
        {
            LogEvent("Запрос на HWID.");
            ws.Send("{\"name\":\"getAppIdentity\"}");
        }
        
        public void LogEvent(string text)
        {
            LogData += Environment.NewLine + text;
        }
        
        public void RemoveAnimations()
        {
            try
            {
                if (!Directory.Exists("bnsfiles"))
                    Directory.CreateDirectory("bnsfiles");
                if (Services.Profiles.CurrentProfile.GeneralSettings.BnsPriority == 0)
                {
                    if (File.Exists(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\bin\Frost\bns_platform_x64"))
                        File.Delete(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\bin\Frost\bns_platform_x64");
                    if (!File.Exists(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\bin\Frost\bns_platform_x32"))
                        File.Create(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\bin\Frost\bns_platform_x32");
                }
                else
                {
                    if (File.Exists(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\bin\Frost\bns_platform_x32"))
                        File.Delete(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\bin\Frost\bns_platform_x32");
                    if (!File.Exists(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\bin\Frost\bns_platform_x64"))
                        File.Create(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\bin\Frost\bns_platform_x64");
                }

                foreach (var mod in Services.Profiles.CurrentProfile.ModOptions)
                    if (!string.IsNullOrEmpty(mod.Path))
                        try
                        {
                            if (!mod.IsFolder)
                                if (mod.Enabled)
                                {
                                    if (File.Exists(Path.Combine(mod.Path + @"\CookedPC", mod.Name)) && !File.Exists(Path.Combine(@"bnsfiles\backup", mod.Name)))
                                        File.Move(Path.Combine(mod.Path + @"\CookedPC", mod.Name), Path.Combine(@"bnsfiles\backup", mod.Name));
                                    File.Copy(Path.Combine("bnsfiles\\mods", mod.Name), Path.Combine(mod.Path + @"\CookedPC", mod.Name), true);
                                }
                                else
                                {
                                    if (File.Exists(Path.Combine(mod.Path + @"\CookedPC", mod.Name)) && File.Exists(Path.Combine(@"bnsfiles\backup", mod.Name)))
                                    {
                                        File.Delete(Path.Combine(mod.Path + @"\CookedPC", mod.Name));
                                        File.Move(Path.Combine(@"bnsfiles\backup", mod.Name), Path.Combine(mod.Path + @"\CookedPC", mod.Name));
                                    }
                                }
                            else if (mod.Enabled)
                            {
                                var backupPath = Path.Combine("bnsfiles", "backup", mod.Name);
                                var modPath = Path.Combine("bnsfiles", "mods", mod.Name);
                                if (!Directory.Exists(backupPath))
                                    Directory.CreateDirectory(backupPath);
                                DirectoryInfo dir = new DirectoryInfo(modPath);
                                FileInfo[] files = dir.GetFiles();
                                foreach (var i in files)
                                {
                                    if (File.Exists(Path.Combine(mod.Path + @"\CookedPC", i.Name)) && !File.Exists(Path.Combine(backupPath, i.Name)))
                                        File.Move(Path.Combine(mod.Path + @"\CookedPC", i.Name), Path.Combine(backupPath, i.Name));
                                    File.Copy(Path.Combine(modPath, i.Name), Path.Combine(mod.Path + @"\CookedPC", i.Name), true);
                                }

                            }
                            else
                            {
                                var backupPath = Path.Combine("bnsfiles", "backup", mod.Name);
                                var modPath = Path.Combine("bnsfiles", "mods", mod.Name);
                                if (!Directory.Exists(backupPath))
                                    Directory.CreateDirectory(backupPath);
                                DirectoryInfo dir = new DirectoryInfo(modPath);
                                FileInfo[] files = dir.GetFiles();
                                foreach (var i in files)
                                {
                                    if (File.Exists(Path.Combine(mod.Path + @"\CookedPC", i.Name)) && File.Exists(Path.Combine(backupPath, i.Name)))
                                    {
                                        File.Delete(Path.Combine(mod.Path + @"\CookedPC", i.Name));
                                        File.Move(Path.Combine(backupPath, i.Name), Path.Combine(mod.Path + @"\CookedPC", i.Name));
                                    }
                                }

                                
                            }
                        }
                        catch { }

                #region remove skills

                //
                if (!Services.Profiles.CurrentProfile.ModOptions.Any(x => x.Enabled && x.Name.Contains("00007916.upk")))
                    if (Services.Profiles.CurrentProfile.GeneralSettings.Assasin)
                    {
                        if (File.Exists(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00007916.upk"))
                        {
                            if (File.Exists(@"bnsfiles\00007916.upk")) File.Delete(@"bnsfiles\00007916.upk");
                            File.Move(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00007916.upk", @"bnsfiles\00007916.upk");
                        }
                    }
                    else
                    {
                        if (File.Exists(@"bnsfiles\00007916.upk") && !File.Exists(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00007916.upk"))
                            File.Move(@"bnsfiles\00007916.upk", Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00007916.upk");
                    }

                //
                if (!Services.Profiles.CurrentProfile.ModOptions.Any(x => x.Enabled && x.Name.Contains("00007917.upk")))
                    if (Services.Profiles.CurrentProfile.GeneralSettings.Summoner)
                    {
                        if (File.Exists(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00007917.upk"))
                        {
                            if (File.Exists(@"bnsfiles\00007917.upk")) File.Delete(@"bnsfiles\00007917.upk");
                            File.Move(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00007917.upk", @"bnsfiles\00007917.upk");
                        }
                    }
                    else
                    {
                        if (File.Exists(@"bnsfiles\00007917.upk") && !File.Exists(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00007917.upk"))
                            File.Move(@"bnsfiles\00007917.upk", Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00007917.upk");
                    }

                //
                if (!Services.Profiles.CurrentProfile.ModOptions.Any(x => x.Enabled && x.Name.Contains("00007915.upk")))
                    if (Services.Profiles.CurrentProfile.GeneralSettings.Gunner)
                    {
                        if (File.Exists(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00007915.upk"))
                        {
                            if (File.Exists(@"bnsfiles\00007915.upk")) File.Delete(@"bnsfiles\00007915.upk");
                            File.Move(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00007915.upk", @"bnsfiles\00007915.upk");
                        }
                    }
                    else
                    {
                        if (File.Exists(@"bnsfiles\00007915.upk") && !File.Exists(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00007915.upk"))
                            File.Move(@"bnsfiles\00007915.upk", Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00007915.upk");
                    }

                //
                if (!Services.Profiles.CurrentProfile.ModOptions.Any(x => x.Enabled && x.Name.Contains("00007914.upk")))
                    if (Services.Profiles.CurrentProfile.GeneralSettings.Destroyer)
                    {
                        if (File.Exists(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00007914.upk"))
                        {
                            if (File.Exists(@"bnsfiles\00007914.upk")) File.Delete(@"bnsfiles\00007914.upk");
                            File.Move(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00007914.upk", @"bnsfiles\00007914.upk");
                        }
                    }
                    else
                    {
                        if (File.Exists(@"bnsfiles\00007914.upk") && !File.Exists(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00007914.upk"))
                            File.Move(@"bnsfiles\00007914.upk", Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00007914.upk");
                    }

                //
                if (!Services.Profiles.CurrentProfile.ModOptions.Any(x => x.Enabled && x.Name.Contains("00007911.upk")))
                    if (Services.Profiles.CurrentProfile.GeneralSettings.BM)
                    {
                        if (File.Exists(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00007911.upk"))
                        {
                            if (File.Exists(@"bnsfiles\00007911.upk")) File.Delete(@"bnsfiles\00007911.upk");
                            File.Move(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00007911.upk", @"bnsfiles\00007911.upk");
                        }
                    }
                    else
                    {
                        if (File.Exists(@"bnsfiles\00007911.upk") && !File.Exists(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00007911.upk"))
                            File.Move(@"bnsfiles\00007911.upk", Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00007911.upk");
                    }

                //
                if (!Services.Profiles.CurrentProfile.ModOptions.Any(x => x.Enabled && x.Name.Contains("00018601.upk")))
                    if (Services.Profiles.CurrentProfile.GeneralSettings.LinBM)
                    {
                        if (File.Exists(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00018601.upk"))
                        {
                            if (File.Exists(@"bnsfiles\00018601.upk")) File.Delete(@"bnsfiles\00018601.upk");
                            File.Move(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00018601.upk", @"bnsfiles\00018601.upk");
                        }
                    }
                    else
                    {
                        if (File.Exists(@"bnsfiles\00018601.upk") && !File.Exists(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00018601.upk"))
                            File.Move(@"bnsfiles\00018601.upk", Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00018601.upk");
                    }

                //
                if (!Services.Profiles.CurrentProfile.ModOptions.Any(x => x.Enabled && x.Name.Contains("00007913.upk")))
                    if (Services.Profiles.CurrentProfile.GeneralSettings.Force)
                    {
                        if (File.Exists(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00007913.upk"))
                        {
                            if (File.Exists(@"bnsfiles\00007913.upk")) File.Delete(@"bnsfiles\00007913.upk");
                            File.Move(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00007913.upk", @"bnsfiles\00007913.upk");
                        }
                    }
                    else
                    {
                        if (File.Exists(@"bnsfiles\00007913.upk") && !File.Exists(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00007913.upk"))
                            File.Move(@"bnsfiles\00007913.upk", Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00007913.upk");
                    }

                //
                if (!Services.Profiles.CurrentProfile.ModOptions.Any(x => x.Enabled && x.Name.Contains("00007912.upk")))
                    if (Services.Profiles.CurrentProfile.GeneralSettings.Kungfu)
                    {
                        if (File.Exists(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00007912.upk"))
                        {
                            if (File.Exists(@"bnsfiles\00007912.upk")) File.Delete(@"bnsfiles\00007912.upk");
                            File.Move(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00007912.upk", @"bnsfiles\00007912.upk");
                        }
                    }
                    else
                    {
                        if (File.Exists(@"bnsfiles\00007912.upk") && !File.Exists(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00007912.upk"))
                            File.Move(@"bnsfiles\00007912.upk", Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00007912.upk");
                    }

                //
                if (!Services.Profiles.CurrentProfile.ModOptions.Any(x => x.Enabled && x.Name.Contains("00023439.upk")))
                    if (Services.Profiles.CurrentProfile.GeneralSettings.Warlock)
                    {
                        if (File.Exists(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00023439.upk"))
                        {
                            if (File.Exists(@"bnsfiles\00023439.upk")) File.Delete(@"bnsfiles\00023439.upk");
                            File.Move(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00023439.upk", @"bnsfiles\00023439.upk");
                        }
                    }
                    else
                    {
                        if (File.Exists(@"bnsfiles\00023439.upk") && !File.Exists(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00023439.upk"))
                            File.Move(@"bnsfiles\00023439.upk", Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00023439.upk");
                    }

                //
                if (!Services.Profiles.CurrentProfile.ModOptions.Any(x => x.Enabled && x.Name.Contains("00034408.upk")))
                    if (Services.Profiles.CurrentProfile.GeneralSettings.SF)
                    {
                        if (File.Exists(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00034408.upk"))
                        {
                            if (File.Exists(@"bnsfiles\00034408.upk")) File.Delete(@"bnsfiles\00034408.upk");
                            File.Move(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00034408.upk", @"bnsfiles\00034408.upk");
                        }
                    }
                    else
                    {
                        if (File.Exists(@"bnsfiles\00034408.upk") && !File.Exists(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00034408.upk"))
                            File.Move(@"bnsfiles\00034408.upk", Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00034408.upk");
                    }

                if (!Services.Profiles.CurrentProfile.ModOptions.Any(x => x.Enabled && x.Name.Contains("00010869.upk")))
                    if (Services.Profiles.CurrentProfile.GeneralSettings.Soulburn)
                    {
                        if (File.Exists(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00010869.upk"))
                        {
                            if (File.Exists(@"bnsfiles\00010869.upk")) File.Delete(@"bnsfiles\00010869.upk");
                            File.Move(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00010869.upk", @"bnsfiles\00010869.upk");
                        }
                    }
                    else
                    {
                        if (File.Exists(@"bnsfiles\00010869.upk") && !File.Exists(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00010869.upk"))
                            File.Move(@"bnsfiles\00010869.upk", Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\bns\CookedPC\00010869.upk");
                    }

                if (!Services.Profiles.CurrentProfile.ModOptions.Any(x => x.Enabled && (x.Name.Contains("Loading.pkg") || x.Name.Contains("00009368.upk"))))
                    if (Services.Profiles.CurrentProfile.GeneralSettings.DisableScreen)
                    {
                        if (File.Exists(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\Local\INNOVA\RUSSIAN\CookedPC\Loading.pkg"))
                        {
                            if (File.Exists(@"bnsfiles\Loading.pkg")) File.Delete(@"bnsfiles\Loading.pkg");
                            File.Move(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\Local\INNOVA\RUSSIAN\CookedPC\Loading.pkg", @"bnsfiles\Loading.pkg");
                        }

                        if (File.Exists(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\Local\INNOVA\RUSSIAN\CookedPC\00009368.upk"))
                        {
                            if (File.Exists(@"bnsfiles\00009368.upk")) File.Delete(@"bnsfiles\00009368.upk");
                            File.Move(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\Local\INNOVA\RUSSIAN\CookedPC\00009368.upk", @"bnsfiles\00009368.upk");
                        }
                    }
                    else
                    {
                        if (File.Exists(@"bnsfiles\Loading.pkg") && !File.Exists(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\Local\INNOVA\RUSSIAN\CookedPC\Loading.pkg"))
                            File.Move(@"bnsfiles\Loading.pkg", Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\Local\INNOVA\RUSSIAN\CookedPC\Loading.pkg");
                        if (File.Exists(@"bnsfiles\00009368.upk") && !File.Exists(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\Local\INNOVA\RUSSIAN\CookedPC\00009368.upk"))
                            File.Move(@"bnsfiles\00009368.upk", Services.Profiles.CurrentProfile.GeneralSettings.BnsPath + @"\contents\Local\INNOVA\RUSSIAN\CookedPC\00009368.upk");
                    }

                #endregion
            }
            catch (Exception e)
            {
                LogEvent(e.Message);
            }
        }

        
        public void ApplyAnimSet()
        {
            RemoveAnimations();
            LogEvent("Изменения внесены.");
        }
        
        public void CloseBnsButton()
        {
            KillBnS();
        }
        
        public void Reconnect()
        {
            LogEvent("Переподключение.");
            Task.Factory.StartNew(() =>
            {
                ws.Close();
                ws.Connect();
                NotifyOfPropertyChange("CanLaunchBns");
            });
        }
        
        public void CleanMem()
        {
            bool GameStarted = false;
            if (GameStarted == true)
            {
                LogEvent("Cleaning Memory...");
                GCSettings.LatencyMode = GCLatencyMode.LowLatency;
                long before = GC.GetTotalMemory(false);
                LogEvent("Before: " + before / 1024 + " MB");
                GC.AddMemoryPressure(before);
                GC.Collect(2, GCCollectionMode.Optimized, false);
                long after = GC.GetTotalMemory(false);
                GC.RemoveMemoryPressure(after);
                LogEvent("After: " + after / 1024 + " MB");
                LogEvent("Freed: " + (before - after) / 1024 + " MB");
                LogEvent("Memory Cleaned");
            }
            else
            {
                GCSettings.LatencyMode = GCLatencyMode.Interactive;
                long before = GC.GetTotalMemory(true);
                GC.AddMemoryPressure(before);
                GC.Collect(2, GCCollectionMode.Forced, false);
                long after = GC.GetTotalMemory(true);
                GC.RemoveMemoryPressure(after);
                LogEvent("Освобождено: " + (before - after) / 1024 + " MB");
            }
        }
        
        public void ManageProfiles()
        {
            _windowManager.ShowDialog(IoC.Get<ManageProfilesViewModel>());
        }
        
        public void ChangeProfile()
        {
            Services.Profiles.CurrentProfile = Services.Profiles.AllProfiles.ElementAt(Services.Profiles.LastUsedProfile);
            Services.Profiles.CurrentProfile = Services.Profiles.AllProfiles.ElementAt(Services.Profiles.LastUsedProfile);
        }

        #region Flyouts

        public void HelpTab(int index)
        {
            _windowManager.ShowWindow(new HelpViewModel(index));
        }

        #endregion
        
        public void DisposeNotifier()
        {
            PeachesEventViewModel.Notifier?.Dispose();
            SettingsViewModel.Notifier?.Dispose();
        }


        #region Macros
        
        public void InstallDriver()
        {
            var info = new ProcessStartInfo("install-driver.exe", "/install");
            Process.Start(info);
        }
        private int ModuleName = 0;
        
        public async void ToggleMacros()
        {
            if (!DriverInstalled)
            {
                var res = await _dialogCoordinator.ShowMessageAsync(this, "Драйвер не установлен", "Установить драйвер макроса? Возможно потребуется перезагрузка компьютера.", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings {AffirmativeButtonText = "Да", NegativeButtonText = "Нет"});
                if (res == MessageDialogResult.Affirmative)
                {
                    InstallDriver();
                    return;
                }

                return;
            }
            
            if (!ScriptSharing.InterceptorEnabled)
            {
                ScriptSharing.input.KeyboardFilterMode = KeyboardFilterMode.All;
                ScriptSharing.input.MouseFilterMode = MouseFilterMode.INTERCEPTION_FILTER_MOUSE_ALL & ~MouseFilterMode.INTERCEPTION_FILTER_MOUSE_MOVE & ~MouseFilterMode.INTERCEPTION_FILTER_MOUSE_LEFT_BUTTON_DOWN & ~MouseFilterMode.INTERCEPTION_FILTER_MOUSE_LEFT_BUTTON_UP & ~MouseFilterMode.INTERCEPTION_FILTER_MOUSE_HWHEEL;
                ScriptSharing.input.ClickDelay = 5;
                ScriptSharing.input.KeyPressDelay = 5;
                MouseRules.Clear();

                var task1 = Task.Factory.StartNew(() =>
                {
                    var script = new PaxScripter();
                    script.DiscardError();
                    script.OnChangeState += (sender, e) =>
                    {
                        if (e.OldState == ScripterState.Init)
                        {
                            sender.AddModule(ModuleName.ToString());
                            sender.AddCode(ModuleName++.ToString(), ProcessScript(0));
                        }
                        else if (sender.HasErrors)
                        {
                            LogEvent(sender.Error_List[0].Message);
                            sender.Dispose();
                        }
                    };
                    ScriptSharing.MacrosScript[0] = script;
                });
                var task2 = Task.Factory.StartNew(() =>
                {
                    var script = new PaxScripter();
                    script.DiscardError();
                    script.OnChangeState += (sender, e) =>
                    {
                        if (e.OldState == ScripterState.Init)
                        {
                            sender.AddModule(ModuleName.ToString());
                            sender.AddCode(ModuleName++.ToString(), ProcessScript(1));
                        }
                        else if (sender.HasErrors)
                        {
                            LogEvent(sender.Error_List[0].Message);
                            sender.Dispose();
                        }
                    };
                    ScriptSharing.MacrosScript[1] = script;
                });
                var task3 = Task.Factory.StartNew(() =>
                {
                    var script = new PaxScripter();
                    script.DiscardError();
                    script.OnChangeState += (sender, e) =>
                    {
                        if (e.OldState == ScripterState.Init)
                        {
                            sender.AddModule(ModuleName.ToString());
                            sender.AddCode(ModuleName++.ToString(), ProcessScript(2));
                        }
                        else if (sender.HasErrors)
                        {
                            LogEvent(sender.Error_List[0].Message);
                            sender.Dispose();
                        }
                    };
                    ScriptSharing.MacrosScript[2] = script;
                });
                var task4 = Task.Factory.StartNew(() =>
                {
                    var script = new PaxScripter();
                    script.DiscardError();
                    script.OnChangeState += (sender, e) =>
                    {
                        if (e.OldState == ScripterState.Init)
                        {
                            sender.AddModule(ModuleName.ToString());
                            sender.AddCode(ModuleName++.ToString(), ProcessScript(3));
                        }
                        else if (sender.HasErrors)
                        {
                            LogEvent(sender.Error_List[0].Message);
                            sender.Dispose();
                        }
                    };
                    ScriptSharing.MacrosScript[3] = script;
                });
                Task.WaitAll(task1, task2, task3, task4);
                foreach (var str in Regex.Replace(Services.Profiles.CurrentProfile.GeneralSettings.MacrosMouse.Replace(" ", string.Empty), @"/\*(.*?)\*/", string.Empty).Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries))
                {
                    var firstStep = str.Split('=');
                    if (firstStep.Length == 2)
                    {
                        var step2left = firstStep[0];
                        var step2right = firstStep[1].Split('|');
                        if (Enum.GetNames(typeof(MouseState)).Contains(step2left))
                            if (step2right.Length == 2)
                            {
                                if (Enum.GetNames(typeof(Keys)).Contains(step2right[0]) && Enum.GetNames(typeof(KeyState)).Contains(step2right[1]))
                                    MouseRules.Add(
                                        (MouseState) Enum.Parse(typeof(MouseState), step2left),
                                        new KeyStroke {Code = (Keys) Enum.Parse(typeof(Keys), step2right[0]), State = (KeyState) Enum.Parse(typeof(KeyState), step2right[1].Replace("Down", "E0").Replace("Up", "E1"))});
                            }
                            else
                            {
                                if (Enum.GetNames(typeof(Keys)).Contains(step2right[0]))
                                {
                                    MouseRules.Add(
                                        (MouseState) Enum.Parse(typeof(MouseState), step2left),
                                        new KeyStroke {Code = (Keys) Enum.Parse(typeof(Keys), step2right[0]), State = KeyState.TermsrvVKPacket});
                                }
                                else if (Enum.GetNames(typeof(MouseState)).Contains(step2right[0]))
                                {
                                    MouseRules.Add(
                                        (MouseState) Enum.Parse(typeof(MouseState), step2left),
                                        new MouseStroke {State = (MouseState) Enum.Parse(typeof(MouseState), step2right[0])});
                                }
                                else if (step2right[0].Contains("Macros"))
                                {
                                    var temp = step2left.Replace("Up", "").Replace("Down", "");
                                    var index = Regex.Match(step2right[0], @"\d+").Value;
                                    if (!step2right[0].Contains("ToggleMode"))
                                        MouseRules.Add(
                                            (MouseState) Enum.Parse(typeof(MouseState), temp + "Up"),
                                            "MacrosOff" + index);

                                    MouseRules.Add(
                                        (MouseState) Enum.Parse(typeof(MouseState), temp + "Down"),
                                        "MacrosOn" + index);
                                }

                                else if (step2right[0] == "Off")
                                {
                                    MouseRules.Add(
                                        (MouseState) Enum.Parse(typeof(MouseState), step2left),
                                        "Off");
                                }
                            }
                    }
                }

                KeyboardRules.Clear();
                foreach (var str in Regex.Replace(Services.Profiles.CurrentProfile.GeneralSettings.MacrosKeyboard.Replace(" ", string.Empty), @"/\*(.*?)\*/", string.Empty).Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries))
                {
                    var firstStep = str.Split('=');
                    if (firstStep.Length == 2)
                    {
                        var step2left = firstStep[0];
                        var step2right = firstStep[1];
                        if (Enum.GetNames(typeof(Keys)).Contains(step2left))
                            if (Enum.GetNames(typeof(Keys)).Contains(step2right))
                                KeyboardRules.Add(
                                    (Keys) Enum.Parse(typeof(Keys), step2left),
                                    (Keys) Enum.Parse(typeof(Keys), step2right));
                            else if (step2right.Contains("Macros"))
                                KeyboardRules.Add(
                                    (Keys) Enum.Parse(typeof(Keys), step2left),
                                    step2right);
                            else if (step2right == "Off")
                                KeyboardRules.Add(
                                    (Keys) Enum.Parse(typeof(Keys), step2left),
                                    step2right);
                    }
                }

                ScriptSharing.input.Load();
            }
            else
            {
                foreach (var i in ScriptSharing.MacrosScript) i.Dispose();

                ScriptSharing.input.Unload();
            }

            ScriptSharing.InterceptorEnabled = !ScriptSharing.InterceptorEnabled;
        }
        
        public void Input_OnMousePressed(object sender, MousePressedEventArgs e)
        {
            if (MouseRules.ContainsKey(e.State))
            {
                var value = MouseRules[e.State];
                if (value is string)
                {
                    if ((value as string).Contains("MacrosOn"))
                    {
                        try
                        {
                            var index = int.Parse((value as string).Substring(8)) - 1;
                            if (!ScriptSharing.MacrosEnabled[index])
                            {
                                var macros = new Task(() => { ScriptSharing.MacrosScript[index].Run(RunMode.Run); });
                                ScriptSharing.MacrosEnabled[index] = true;
                                macros.Start();
                            }
                            else
                            {
                                ScriptSharing.ts[index].Cancel();
                            }
                        }
                        catch (Exception ex)
                        {
                            var writer = new StreamWriter(File.Open("errorlog.txt", FileMode.Append));
                            writer.WriteLine(ex.Message + ex.StackTrace);
                            Environment.Exit(-1);
                        }

                        e.Handled = true;
                        return;
                    }

                    if ((value as string).Contains("MacrosOff"))
                    {
                        var index = int.Parse((value as string).Substring(9)) - 1;
                        if (ScriptSharing.MacrosEnabled[index])
                            ScriptSharing.ts[index].Cancel();
                        e.Handled = true;
                        return;
                    }

                    if (value == "Off")
                    {
                        e.Handled = true;
                        return;
                    }
                }

                if (value is KeyStroke stroke)
                {
                    if (stroke.State == KeyState.TermsrvVKPacket)
                        ScriptSharing.input.SendKey(stroke.Code);
                    else if (stroke.State == KeyState.E0)
                        ScriptSharing.input.SendKey(stroke.Code, KeyState.Down);
                    else
                        ScriptSharing.input.SendKey(stroke.Code, KeyState.Up);
                    e.Handled = true;
                    return;
                }

                if (value is MouseStroke MouseStroke)
                {
                    ScriptSharing.input.SendMouseEvent(MouseStroke.State);
                    e.Handled = true;
                }
            }
        }

        
        public void Input_OnKeyPressed(object sender, KeyPressedEventArgs e)
        {
            
            if (KeyboardRules.ContainsKey(e.Key))
            {
                var value = KeyboardRules[e.Key];
                if (value is string)
                {
                    if ((value as string).Contains("Macros") && e.State == KeyState.Down)
                    {
                        try
                        {
                            var index = int.Parse((value as string).Substring(6)) - 1;
                            if (!ScriptSharing.MacrosEnabled[index])
                            {
                                var macros = new Task(() => { ScriptSharing.MacrosScript[index].Run(RunMode.Run); });
                                ScriptSharing.MacrosEnabled[index] = true;
                                macros.Start();
                            }
                        }
                        catch (Exception ex)
                        {
                            var writer = new StreamWriter(File.Open("errorlog.txt", FileMode.Append));
                            writer.WriteLine(ex.Message + ex.StackTrace);
                            Environment.Exit(-1);
                        }

                        e.Handled = true;
                        return;
                    }

                    if ((value as string).Contains("Macros") && e.State == KeyState.Up)
                    {
                        var index = int.Parse((value as string).Substring(6)) - 1;
                        if (ScriptSharing.MacrosEnabled[index])
                            ScriptSharing.ts[index].Cancel();
                        e.Handled = true;
                        return;
                    }

                    if (value == "Off")
                    {
                        e.Handled = true;
                        return;
                    }
                }

                if (value is Keys stroke)
                {
                    e.Key = stroke;
                    e.Handled = true;
                }
            }
        }

        
        public void MacrosSettings()
        {
            var w = new MacrosView();
            w.Show();
        }
        
        public string ProcessScript(int activeMacros)
        {
            string macros;
            switch (activeMacros)
            {
                case 0:
                    macros = Services.Profiles.CurrentProfile.GeneralSettings.LoopMacros;
                    break;
                case 1:
                    macros = Services.Profiles.CurrentProfile.GeneralSettings.LoopMacros2;
                    break;
                case 2:
                    macros = Services.Profiles.CurrentProfile.GeneralSettings.LoopMacros3;
                    break;
                case 3:
                    macros = Services.Profiles.CurrentProfile.GeneralSettings.LoopMacros4;
                    break;
                default:
                    macros = Services.Profiles.CurrentProfile.GeneralSettings.LoopMacros;
                    break;
            }

            var tmp = @"   
                
                " + macros + @"
                Zapuskator.ViewModels.ScriptSharing.MacrosEnabled[index] = false;
            ";
            var resultScript = "";
            foreach(string str in tmp.Split(Environment.NewLine.ToArray()))
            {
                string toRpl="";
                if (!str.ContainsAny("KeyState.Up", "_Up"))
                {
                    toRpl = str.Replace("Send", "stopToken" + Environment.NewLine + "Send").Replace("stopToken", @"
if (Zapuskator.ViewModels.ScriptSharing.ct[index].IsCancellationRequested)
{
    Zapuskator.ViewModels.ScriptSharing.ts[index] = new System.Threading.CancellationTokenSource();
    Zapuskator.ViewModels.ScriptSharing.ct[index]  = Zapuskator.ViewModels.ScriptSharing.ts[index].Token;
    Zapuskator.ViewModels.ScriptSharing.MacrosEnabled.SetValue(false,index);
    break;
}
                    ") + Environment.NewLine;

                }
                else
                    toRpl = str;
                resultScript += toRpl.Replace("index", activeMacros.ToString()).Replace("KeyState", "Interceptor.KeyState").Replace("Keys", "Interceptor.Keys").Replace("MouseState", "Interceptor.MouseState").Replace("SendKey", "Zapuskator.ViewModels.ScriptSharing.input.SendKey").Replace("SendMouseEvent", "Zapuskator.ViewModels.ScriptSharing.input.SendMouseEvent").Replace("Sleep", "System.Threading.Thread.Sleep");

            }

            return resultScript;
        }

        #endregion


        #region Updater
        
        private void AutoUpdater_ApplicationExitEvent()
        {
            File.Copy("ReplaceZapuskator.exe", @"bnsfiles\ReplaceZapuskator.exe", true);
            Process.Start(@"bnsfiles\ReplaceZapuskator.exe");
            Environment.Exit(1);
        }
        
        public void CheckForUpdates()
        {
            AutoUpdater.ApplicationExitEvent += AutoUpdater_ApplicationExitEvent;
            Task.Factory.StartNew(() =>
            {
                try
                {
                    AutoUpdater.ShowSkipButton = false;
                    AutoUpdater.RunUpdateAsAdmin = true;
                    AutoUpdater.Start(@"https://zapuskator-bns.azurewebsites.net/updates/UpdateInfo.xml");
                }
                catch (Exception ex)
                {
                    LogEvent(ex.Message);
                }
            });
        }

        #endregion
    }
    
    public static class ScriptSharing
    {
        public static Input input = new Input();
        public static bool[] MacrosEnabled = new bool[4];
        public static CancellationTokenSource[] ts = new CancellationTokenSource[4];
        public static CancellationToken[] ct = new CancellationToken[4];
        public static bool InterceptorEnabled;
        public static PaxScripter[] MacrosScript = new PaxScripter[4];
    }
}