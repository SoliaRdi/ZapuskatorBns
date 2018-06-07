using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Timers;
using System.Windows;
using Caliburn.Micro;
using DeployLX.CodeVeil.CompileTime.v5;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using STO.Framework;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;
using Zapuskator.Framework;

namespace Zapuskator.ViewModels
{
    
    public class GameSettingsViewModel : Screen
    {
        private readonly IEventAggregator _eventAggregator;
        public IDialogCoordinator _dialogCoordinator;
        public Notifier Notifier;

        public int SelectedServer
        {
            get { return _selectedServer; }
            set { _selectedServer = value;
                NotifyOfPropertyChange();
            }
        }

        public GameSettingsViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            _dialogCoordinator = DialogCoordinator.Instance;
        }

        public bool PingEnabled { get; set; }

        public List<Tuple<string, string>> getCookieChrome(string strHost)
        {
            var list = new List<Tuple<string, string>>();
            var strPath = Services.Profiles.CurrentProfile.GeneralSettings.Chrome_Path;
            try
            {
                File.Copy(strPath, Path.Combine(Path.GetTempPath() ,"Cookies.cookie"), true);
                strPath = Path.Combine(Path.GetTempPath(),"Cookies.cookie");
                var strDb = "Data Source=" + strPath + ";pooling=false";

                using (var conn = new SQLiteConnection(strDb))
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT name,encrypted_value,datetime(expires_utc / 1000000 + (strftime('%s', '1601-01-01')), 'unixepoch') FROM cookies WHERE host_key LIKE '%4game.com%';";
                        try
                        {
                            conn.Open();
                        }
                        catch (Exception ex)
                        {
                            _eventAggregator.PublishOnUIThread("Не удалось считать файл cookie");
                            _eventAggregator.PublishOnUIThread(ex.Message);
                            return null;
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            var i = 0;

                            while (reader.Read())
                            {
                                if (reader.GetString(0) == "inn-user") Services.Profiles.CurrentProfile.GeneralSettings.ExpiresAt = "Обновить cookie: \n" + reader[2];

                                i++;

                                var plainText = UnencryptCookie((byte[]) reader[1]);

                                list.Add(Tuple.Create(reader.GetString(0), plainText));
                            }
                        }

                        conn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                _eventAggregator.PublishOnUIThread(ex.Message);
            }


            return list;
        }

        public void ChromeFilePath()
        {
            var dialog = new OpenFileDialog();
            var path = Environment.GetEnvironmentVariable("LocalAppData") + @"\Google\Chrome\User Data\Default";
            dialog.InitialDirectory = Directory.Exists(path) ? path : Environment.GetEnvironmentVariable("LocalAppData");

            dialog.RestoreDirectory = false;
            dialog.Filter = "Cookies (Cookies*)|*Cookies*";
            try
            {
                if (dialog.ShowDialog() == true)
                    Services.Profiles.CurrentProfile.GeneralSettings.Chrome_Path = dialog.FileName;

                ReloadCookie();
                if (string.IsNullOrWhiteSpace(Services.Profiles.CurrentProfile.GeneralSettings.HWID))
                    _eventAggregator.PublishOnUIThread("GetHWID");
            }
            catch (Exception)
            {
                _eventAggregator.PublishOnUIThread("Не удалось открыть диалог");
            }
        }

        public void BnsFilePath()
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            try
            {
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                    Services.Profiles.CurrentProfile.GeneralSettings.BnsPath = dialog.FileName;
            }
            catch (Exception)
            {
                _eventAggregator.PublishOnUIThread("Не удалось открыть диалог");
            }
        }

        public void SaveSettings()
        {
            Services.Tracker.RunAutoPersist();
        }


        public void ReloadCookie()
        {
            try
            {
                if (string.IsNullOrEmpty(Services.Profiles.CurrentProfile.GeneralSettings.Chrome_Path) || !File.Exists(Services.Profiles.CurrentProfile.GeneralSettings.Chrome_Path))
                {
                    _eventAggregator.PublishOnUIThread("Не указан или не найден файл cookie");
                    return;
                }

                var CookieData = getCookieChrome("4game");
                if (CookieData == null)
                {
                    _eventAggregator.PublishOnUIThread("Cookie пустой");
                    return;
                }

                var Cookie = CookieData.Where(x => x.Item1.EqualsAny("_ym_uid", "4g.tid", "__auc", "_ga", "inn-user","pid", "pid-priority", "inn-user-p")).GroupBy(x => x.Item1).Select(y => y.First());
                if (!Cookie.Any())
                {
                    _eventAggregator.PublishOnUIThread("Не найдены данные 4game");
                    return;
                }

                var login = Cookie.Aggregate(new StringBuilder(),
                    (sb, kvp) => sb.AppendFormat("{0} {1} = {2}",
                        sb.Length > 0 ? ";" : "", kvp.Item1, kvp.Item2),
                    sb => sb.ToString());

                Services.Profiles.CurrentProfile.GeneralSettings.Cookie = login;
                Services.Profiles.CurrentProfile.GeneralSettings.Authorization = "bearer " + Cookie.First(x => x.Item1 == "inn-user").Item2;
                _eventAggregator.PublishOnUIThread("Данные cookie получены.");
            }
            catch (Exception ex)
            {
                _eventAggregator.PublishOnUIThread(ex.Message);
            }
        }

        public string UnencryptCookie(byte[] data)
        {
            var encryptedData = data;
            byte[] decodedData = null;
            try
            {
                decodedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.LocalMachine);
            }
            catch (Exception ex)
            {
                _eventAggregator.PublishOnUIThread(ex.Message);
                return null;
            }

            var plainText = Encoding.ASCII.GetString(decodedData);
            return plainText;
        }

        #region ping

        private Timer timer1;
        private int _selectedServer;

        public void TogglePing()
        {
            string Ip = "109.105.133.68";
            switch (SelectedServer)
            {
                case 0: Ip = "109.105.133.68";
                    break;
                case 1:
                    Ip = "109.105.133.64";
                    break;
                case 2:
                    Ip = "109.105.133.65";
                    break;
                case 3:
                    Ip = "109.105.133.67"; 
                    break;
                case 4:
                    Ip = "109.105.133.66"; 
                    break; 
                case 5:
                    Ip = "109.105.133.69";
                    break; 
            }
            if (!PingEnabled)
            {
                Notifier = new Notifier(cfg =>
                {
                    cfg.PositionProvider = new PrimaryScreenPositionProvider(
                        Corner.TopLeft,
                        Services.Profiles.CurrentProfile.GeneralSettings.PingPosX,
                        Services.Profiles.CurrentProfile.GeneralSettings.PingPosY);
                    cfg.DisplayOptions.TopMost = true;

                    cfg.LifetimeSupervisor = new CountBasedLifetimeSupervisor(MaximumNotificationCount.UnlimitedNotifications());

                    cfg.Dispatcher = Application.Current.Dispatcher;
                });
                
                timer1 = new Timer();
                var pinger = new Ping();
                
                var pingViewModel = new CustomNotificationViewModel("");
                Notifier.Notify<CustomNotificationViewModel>(() => pingViewModel);
                timer1.Elapsed += (e, s) =>
                {
                    try
                    {
                        var reply = pinger.Send(Ip);
                        if (reply?.Status == IPStatus.Success)
                            pingViewModel.Message = reply?.RoundtripTime.ToString() + "ms";
                        else
                            pingViewModel.Message = "ERR";
                        timer1.Start();
                    }
                    catch (PingException)
                    {
                        pingViewModel.Message = "ERR";
                        timer1.Start();
                    }
                };
                timer1.Interval = 1000;// in miliseconds
                timer1.AutoReset = false;
                timer1.Start();
                PingEnabled = true;
            }
            else
            {
                PingEnabled = false;
                timer1.Stop();
                timer1.Dispose();
                Notifier.Dispose();
            }
        }
        #endregion
    }
}