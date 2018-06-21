// // 
// //  Copyright 2012 David Gressett
// // 
// //    Licensed under the Apache License, Version 2.0 (the "License");
// //    you may not use this file except in compliance with the License.
// //    You may obtain a copy of the License at
// // 
// //        http://www.apache.org/licenses/LICENSE-2.0
// // 
// //    Unless required by applicable law or agreed to in writing, software
// //    distributed under the License is distributed on an "AS IS" BASIS,
// //    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// //    See the License for the specific language governing permissions and
// //    limitations under the License.

using System;
using System.Configuration;
using System.IO;
using System.Timers;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using log4net;

namespace AppComponents.Azure
{
    /// <summary>
    ///   Implements the <see cref="ILocalFileMirror" /> interface using azure local file storage to mirror azure a container from blob storage. Uses the CommonConfiguration.LocalFileMirror configuration setting as the configuration string in the form 'sourcepath;targetfolder;resourceconfig' where resourceconfig is the local file storage resource in azure. Change the contents of the file 'Version.txt' in your blob container to force a recopy of the mirror.
    /// </summary>
    public class AzureLocalFileMirror : ILocalFileMirror
    {
        private bool _configInitialized;
        private DebugOnlyLogger _dblog;

        private bool _initialized;
        private ILog _log;
        private LocalResource _mirrorStorage;
        private string _targetPath = string.Empty;
        private Timer _timer;

        public AzureLocalFileMirror()
        {
            _log = ClassLogger.Create(GetType());
            _dblog = DebugOnlyLogger.Create(_log);
            InitializeConfig();
        }

        private string ResourceConfig { get; set; }

        #region ILocalFileMirror Members

        public string TargetFolder { get; private set; }

        public string TargetPath
        {
            get
            {
                Initialize();
                return _targetPath;
            }

            private set { _targetPath = value; }
        }


        public string SourcePath { get; private set; }

        #endregion

        private void InitializeConfig()
        {
            if (!_configInitialized)
            {
                _configInitialized = true;
                IConfig config = Catalog.Factory.Resolve<IConfig>();
                var configstr = config[CommonConfiguration.LocalFileMirror];
                string[] configs = configstr.Split(';');

                if (configs.Length != 3)
                {
                    var es = string.Format("Bad local file mirror config: {0}", configstr);
                    _log.Error(es);
                    var on = Catalog.Factory.Resolve<IApplicationAlert>();
                    on.RaiseAlert(ApplicationAlertKind.Services, es);
                    throw new SettingsPropertyNotFoundException(
                        "Malformed or Missing Configuration Settings for LocalFileMirror");
                }

                SourcePath = configs[0];
                TargetFolder = configs[1];
                ResourceConfig = configs[2];
            }
        }

        private void Initialize()
        {
            if (!_initialized)
            {
                _initialized = true;
                InitializeConfig();

                _mirrorStorage = RoleEnvironment.GetLocalResource(ResourceConfig);

                _targetPath = _mirrorStorage.RootPath + TargetFolder;

                _timer = new Timer(1000*60*5);
                _timer.AutoReset = true;
                _timer.Elapsed += _timer_Elapsed;

                MirrorCopy();

                _timer.Enabled = true;
            }
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            MirrorCopy();
        }

        private void MirrorCopy()
        {
            if (!Directory.Exists(_targetPath))
                Directory.CreateDirectory(_targetPath);


            var blobs = Client.FromConfig().ForBlobs();
            var source = blobs.GetContainerReference(SourcePath);
            source.CreateIfNotExist();

            var versionBlob = source.GetBlobReference("Version.txt");
            string versionString = versionBlob.DownloadText();
            if (string.IsNullOrEmpty(versionString))
            {
                var es = string.Format("Local File Mirror source in blob container {0} does not have blob Version.txt",
                                       SourcePath);
                _log.Error(es);
                IApplicationAlert on = Catalog.Factory.Resolve<IApplicationAlert>();
                on.RaiseAlert(ApplicationAlertKind.Unknown, es);

                versionString = Guid.NewGuid().ToString();
            }

            string completedFile = TargetPath + "\\LFMComplete.txt";


            if (!File.Exists(completedFile) || File.ReadAllText(completedFile) != versionString)
            {
                var theBlobs = source.ListBlobs();

                foreach (IListBlobItem b in theBlobs)
                {
                    var fileBlob = source.GetBlobReference(b.Uri.ToString());
                    string filename = Path.GetFileName(b.Uri.ToString());
                    File.Delete(filename);
                    fileBlob.DownloadToFile(_targetPath + "\\" + filename);
                }

                File.WriteAllText(completedFile, versionString);
            }
        }
    }
}