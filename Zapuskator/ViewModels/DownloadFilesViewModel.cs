using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Cache;
using AutoUpdaterDotNET;
using Caliburn.Micro;
using MahApps.Metro.Controls.Dialogs;
using Zapuskator.Framework;
using System.Linq;
using DeployLX.CodeVeil.CompileTime.v5;

namespace Zapuskator.ViewModels
{
    
    public class DownloadFilesViewModel : Screen
    {
        private readonly string _downloadURL, _downloadPath;
        private readonly IEventAggregator _eventAggregator;
        public IDialogCoordinator _dialogCoordinator;
        private float _progressValue;
        private string _tempFile;
        private ModOption _option;
        private MyWebClient _webClient;
        private string _title;

        public string Title
        {
            get { return _title; }
            set { _title = value;
                NotifyOfPropertyChange();
            }
        }

        public DownloadFilesViewModel(IEventAggregator eventAggregator, string downloadURL, string downloadPath,ModOption option)
        {
            _eventAggregator = eventAggregator;
            _dialogCoordinator = DialogCoordinator.Instance;
            _downloadURL = downloadURL;
            _downloadPath = downloadPath;
            _option = option;
            Title ="Скачивание "+ _option.Name;
            _webClient = new MyWebClient { CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore) };

            if (AutoUpdater.Proxy != null) _webClient.Proxy = AutoUpdater.Proxy;

            var uri = new Uri(_downloadURL);

            _tempFile = string.IsNullOrEmpty(_downloadPath) ? Path.GetTempFileName() : Path.Combine(_downloadPath, $"{Guid.NewGuid().ToString()}.tmp");

            _webClient.DownloadProgressChanged += OnDownloadProgressChanged;

            _webClient.DownloadFileCompleted += WebClientOnDownloadFileCompleted;

            _webClient.DownloadFileAsync(uri, _tempFile);
            
        }

        public float ProgressValue
        {
            get => _progressValue;
            set
            {
                _progressValue = value;
                NotifyOfPropertyChange();
            }
        }


        private void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            ProgressValue = e.ProgressPercentage;
        }

        private void WebClientOnDownloadFileCompleted(object sender, AsyncCompletedEventArgs asyncCompletedEventArgs)
        {
            if (asyncCompletedEventArgs.Cancelled) return;

            if (asyncCompletedEventArgs.Error != null)
            {
                _eventAggregator.PublishOnUIThread("Ошибка:" + asyncCompletedEventArgs.Error.GetType() + " - " + asyncCompletedEventArgs.Error.Message);

                _webClient = null;
                TryClose();
                return;
            }


            string fileName;
            var contentDisposition = _webClient.ResponseHeaders["Content-Disposition"] ?? string.Empty;
            if (string.IsNullOrEmpty(contentDisposition))
            {
                fileName = Path.GetFileName(_webClient.ResponseUri.LocalPath);
            }
            else
            {
                fileName = TryToFindFileName(contentDisposition, "filename=");
                if (string.IsNullOrEmpty(fileName)) fileName = TryToFindFileName(contentDisposition, "filename*=UTF-8''");
            }

            var tempPath = Path.Combine(string.IsNullOrEmpty(_downloadPath) ? Path.GetTempPath() : _downloadPath, fileName);

            try
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);

                File.Move(_tempFile, tempPath);
                _option.Path = Path.Combine(Services.Profiles.CurrentProfile.GeneralSettings.BnsPath, _option.Path);
                if (Services.Profiles.CurrentProfile.ModOptions.Count(x => x.Name == _option.Name) == 0)
                {

                    Services.Profiles.CurrentProfile.ModOptions.Add(_option);
                }
                else
                {
                    var mod = Services.Profiles.CurrentProfile.ModOptions.First(x => x.Name == _option.Name);
                    mod.Path = _option.Path;
                    mod.Description = _option.Description;
                }
            }
            catch (Exception e)
            {
                _eventAggregator.PublishOnUIThread(e.Message + " Type^" + e.GetType());
                _webClient = null;
                TryClose();
                return;
            }
            

            TryClose();
        }

        private static string TryToFindFileName(string contentDisposition, string lookForFileName)
        {
            var fileName = string.Empty;
            if (!string.IsNullOrEmpty(contentDisposition))
            {
                var index = contentDisposition.IndexOf(lookForFileName, StringComparison.CurrentCultureIgnoreCase);
                if (index >= 0)
                    fileName = contentDisposition.Substring(index + lookForFileName.Length);
                if (fileName.StartsWith("\""))
                {
                    var file = fileName.Substring(1, fileName.Length - 1);
                    var i = file.IndexOf("\"", StringComparison.CurrentCultureIgnoreCase);
                    if (i != -1) fileName = file.Substring(0, i);
                }
            }

            return fileName;
        }

    }

    /// <inheritdoc />
    public class MyWebClient : WebClient
    {
        /// <summary>
        ///     Response Uri after any redirects.
        /// </summary>
        public Uri ResponseUri;

        /// <inheritdoc />
        protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
        {
            var webResponse = base.GetWebResponse(request, result);
            ResponseUri = webResponse.ResponseUri;
            return webResponse;
        }
    }
}