using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using DeployLX.CodeVeil.CompileTime.v5;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;
using Zapuskator.Framework;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

namespace Zapuskator.ViewModels
{
    public class ModsViewModel : Screen

    {
        private readonly IEventAggregator _eventAggregator;
        public IDialogCoordinator _dialogCoordinator;
        private readonly IWindowManager _windowManager;
        private string _loadingPath;
        private string _loadingImgFolder;

        public string LoadingPath
        {
            get { return _loadingPath; }
            set
            {
                _loadingPath = value;
                NotifyOfPropertyChange();
            }
        }

        public string LoadingImgFolder
        {
            get { return _loadingImgFolder; }
            set
            {
                _loadingImgFolder = value;
                NotifyOfPropertyChange();
            }
        }

        public string Header
        {
            get
            {
                var count = Services.Profiles.CurrentProfile.ModOptions.Count();
                var active = Services.Profiles.CurrentProfile.ModOptions.Count(x => x.Enabled);
                return "Пользовательских модификаций:" + active + "/" + count;
            }
        }

        public ModsViewModel(IEventAggregator eventAggregator, IWindowManager windowManager)
        {
            _eventAggregator = eventAggregator;
            _dialogCoordinator = DialogCoordinator.Instance;
            _windowManager = windowManager;
        }

        public void RefreshFiles()
        {
            try
            {
                _eventAggregator.PublishOnUIThread("Получение списка модов.");
                DirectoryInfo d = new DirectoryInfo(@"bnsfiles\mods");
                FileInfo[] Files = d.GetFiles("*.upk");
                foreach (var i in Files)
                {
                    if (Services.Profiles.CurrentProfile.ModOptions.All(x => x.Name != i.Name))
                    {
                        Services.Profiles.CurrentProfile.ModOptions.Add(new ModOption(i.Name, ""));
                    }
                }

                foreach (var i in Services.Profiles.CurrentProfile.ModOptions.Where(x => !x.IsFolder).ToArray())
                {
                    if (Files.All(x => x.Name != i.Name))
                    {
                        if (File.Exists(Path.Combine(@"bnsfiles\backup", i.Name)))
                            File.Move(Path.Combine(@"bnsfiles\backup", i.Name), Path.Combine(i.Path, i.Name));
                        Services.Profiles.CurrentProfile.ModOptions.Remove(i);
                    }
                }

                foreach (var i in Directory.GetDirectories(@"bnsfiles\mods"))
                {
                    DirectoryInfo dir = new DirectoryInfo(i);
                    FileInfo[] lFiles = dir.GetFiles();
                    if (Services.Profiles.CurrentProfile.ModOptions.Select(a => a.Name).Intersect(lFiles.Select(b => b.Name)).ToList().Count > 0)
                    {
                        _eventAggregator.PublishOnUIThread("Конфликт одинаковых модификаций");
                    }
                    else
                    {
                        if (Services.Profiles.CurrentProfile.ModOptions.All(x => x.Name != dir.Name))
                            Services.Profiles.CurrentProfile.ModOptions.Add(new ModOption(dir.Name, "", true));
                    }
                }

                foreach (var i in Services.Profiles.CurrentProfile.ModOptions.Where(x => x.IsFolder).ToArray())
                {
                    if (!Directory.Exists(Path.Combine("bnsfiles", "mods", i.Name)))
                    {
                        Services.Profiles.CurrentProfile.ModOptions.Remove(i);
                    }
                }

                NotifyOfPropertyChange(nameof(Header));
            }
            catch (Exception ex)
            {
                _eventAggregator.PublishOnUIThread(ex.Message);
            }
        }

        public void AddFolderPath(object context)
        {
            var obj = context as ModOption;
            var dialog = new CommonOpenFileDialog {IsFolderPicker = true};
            if (string.IsNullOrWhiteSpace(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath))
            {
                _eventAggregator.PublishOnUIThread("Не указана папка игры");
            }
            else
            {
                dialog.InitialDirectory = Services.Profiles.CurrentProfile.GeneralSettings.BnsPath;
                dialog.RestoreDirectory = false;
                try
                {
                    if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        Services.Profiles.CurrentProfile.ModOptions.First(x => obj != null && x.Name == obj.Name).Path = dialog.FileName;
                    }
                }
                catch (Exception)
                {
                    _eventAggregator.PublishOnUIThread("Не удалось открыть диалог");
                }
            }
        }

        public async void AlertWings(bool val)
        {
            await _dialogCoordinator.ShowMessageAsync(this, "Предупреждение", "Опция крылья ведет к потере эффектов скилов у кота.");
        }

        public void ApplyAnimSet()
        {
            _eventAggregator.PublishOnUIThread("ApplyAnimSet");
        }

        public void GetMods()
        {
            _windowManager.ShowDialog(IoC.Get<DownloadModsListViewModel>());
        }

        public void LoadingPkgPath()
        {
            var dialog = new CommonOpenFileDialog {IsFolderPicker = false};
            try
            {
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                    LoadingPath = dialog.FileName;
            }
            catch (Exception)
            {
                _eventAggregator.PublishOnUIThread("Не удалось открыть диалог");
            }
        }

        public void LoadingImgsPath()
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };
            try
            {
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                    LoadingImgFolder = dialog.FileName;
            }
            catch (Exception)
            {
                _eventAggregator.PublishOnUIThread("Не удалось открыть диалог");
            }
        }

        public async void UnpackLoading()
        {
            string _LoadingPath = LoadingPath;
            await Task.Factory.StartNew(() =>
            {
                try
                {
                    if (!Directory.Exists(Path.Combine("bnsfiles", "Loading")))
                        Directory.CreateDirectory(Path.Combine("bnsfiles", "Loading"));
                    if (!string.IsNullOrEmpty(_LoadingPath) && File.Exists(_LoadingPath))
                        using (var s = new ZipInputStream(File.OpenRead(_LoadingPath)))
                        {
                            s.Password = "abcde12#";
                            ZipEntry theEntry;
                            while ((theEntry = s.GetNextEntry()) != null)
                            {
                                _eventAggregator.PublishOnUIThread("Извлечение " + theEntry.Name);


                                var directoryName = Path.GetDirectoryName(theEntry.Name);
                                var fileName = Path.GetFileName(theEntry.Name);

                                if (!string.IsNullOrEmpty(directoryName))
                                    Directory.CreateDirectory(directoryName);

                                if (fileName != string.Empty)
                                    using (var streamWriter = File.Create(Path.Combine("bnsfiles", "Loading", theEntry.Name)))
                                    {
                                        var data = new byte[2048];
                                        while (true)
                                        {
                                            var size = s.Read(data, 0, data.Length);
                                            if (size > 0)
                                                streamWriter.Write(data, 0, size);
                                            else
                                                break;
                                        }
                                    }
                            }
                            _eventAggregator.PublishOnUIThread("Файлы извлечены в bnsfiles/Loading");
                        }
                    else
                        _eventAggregator.PublishOnUIThread("Файлы не найден");
                }
                catch (Exception ex)
                {
                    _eventAggregator.PublishOnUIThread("Ошибка" + ex.Message);
                }
            });
        }

        public void RepackLoading()
        {
            string _LoadingImgFolder = LoadingImgFolder;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    if (!Directory.Exists(Path.Combine("bnsfiles", "Loading")))
                       UnpackLoading();
                    var d = new DirectoryInfo(Path.Combine("bnsfiles", "Loading"));
                    var files = d.GetFiles();
                    if (files.Length < 10)
                    {
                        UnpackLoading();
                        files = d.GetFiles();
                    }

                    var r = new DirectoryInfo(_LoadingImgFolder);
                    var replaceFiles = r.GetFiles();
                    var filerCount = replaceFiles.Length;
                    var i = 0;
                    foreach (var f in files)
                    {
                        File.Copy(replaceFiles[i++].FullName, f.FullName, true);
                        Console.WriteLine(f.Name);
                        if (filerCount == i) i = 0;
                    }


                    var filenames = Directory.GetFiles(Path.Combine("bnsfiles", "Loading"));
                    using (var s = new ZipOutputStream(File.Create("Loading.pkg")))
                    {
                        s.Password = "abcde12#";
                        s.SetLevel(9);
                        var buffer = new byte[4096];
                        foreach (var file in filenames)
                        {
                            var entry = new ZipEntry(Path.GetFileName(file)) { DateTime = DateTime.Now };
                            s.PutNextEntry(entry);
                            _eventAggregator.PublishOnUIThread("Сжатие " + entry.Name);
                            using (var fs = File.OpenRead(file))
                            {
                                int sourceBytes;
                                do
                                {
                                    sourceBytes = fs.Read(buffer, 0, buffer.Length);
                                    s.Write(buffer, 0, sourceBytes);
                                } while (sourceBytes > 0);
                            }
                        }
                        s.Finish();
                        s.Close();
                    }
                    _eventAggregator.PublishOnUIThread("Файл Loading.pkg создан в текущей папке");
                }
                catch (Exception ex)
                {
                    _eventAggregator.PublishOnUIThread("Ошибка" + ex.Message);
                }
            });
        }
    }
}