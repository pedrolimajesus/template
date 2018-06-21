using System;
using System.IO;
using AppComponents.Data;
using Microsoft.Win32;

namespace AppComponents.Topology
{
    public class ApplicationNodeRegistry
    {
        private string _settingsPath;
        private IniFile _settings;
        private string _company;
        private string _appName;
        private const string Section = "Settings";
        private bool _initialized = false;

        public ApplicationNodeRegistry(string company, string appName)
        {
            _company = company;
            _appName = appName;
        }

        private void MaybeInitialize()
        {
            if (!_initialized)
            {
                _initialized = true;

                var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

                if (string.IsNullOrEmpty(programData))
                    throw new FileLoadException("Cannot open program data folder.");

                _settingsPath = Path.Combine(programData, _company, _appName);
                if (!Directory.Exists(_settingsPath))
                    Directory.CreateDirectory(_settingsPath);

                _settingsPath = Path.Combine(_settingsPath, "ApplicationNodeSettings.ini");
                _settings = new IniFile(_settingsPath);
            }
        }

        public void Delete()
        {
            MaybeInitialize();

            File.Delete(_settingsPath);
        }

        public string Id
        {
            get
            {
                MaybeInitialize();
                return _settings.IniReadValue(Section, "Id"); 
            }

            set
            {
                MaybeInitialize();
                _settings.IniWriteValue(Section, "Id", value);
                
            }
        }

        public  string ComponentType
        {
            get
            {
                MaybeInitialize();
                var t = _settings.IniReadValue(Section, "ComponentType", "none");
                return t ?? string.Empty;
            }
            set
            {
                MaybeInitialize();
                _settings.IniWriteValue(Section, "ComponentType", value);
            }
        }

        public DateTime InstallDate
        {
            get
            {
                MaybeInitialize();
                var dts= _settings.IniReadValue(Section,"InstallDate", DateTime.UtcNow.ToString());
                return DateTime.Parse(dts);
            }

            set { MaybeInitialize(); _settings.IniWriteValue(Section, "InstallDate", value.ToString()); }

        }

        public string Version 
        {
            get { MaybeInitialize(); return _settings.IniReadValue(Section, "Version", "none"); }

            set
            {
                MaybeInitialize(); _settings.IniWriteValue(Section, "Version", value);
            }
        }

        public string RequiredDB
        {
            get { MaybeInitialize(); return _settings.IniReadValue(Section, "DBVersion", "none"); }

            set
            {
                MaybeInitialize(); _settings.IniWriteValue(Section, "DBVersion", value);
            }
        }

        public string DBConnection
        {
            get { MaybeInitialize(); return _settings.IniReadValue(Section, "Database", "none"); }

            set
            {
                MaybeInitialize(); _settings.IniWriteValue(Section, "Database", value);
            }
        }

        public string DBUser
        {
            get { MaybeInitialize(); return _settings.IniReadValue(Section, "DatabaseUN", "none"); }

            set
            {
                MaybeInitialize(); _settings.IniWriteValue(Section, "DatabaseUN", value);
            }
        }


        public string DBPassword
        {
            get { MaybeInitialize(); return _settings.IniReadValue(Section, "DatabaseKey", "none"); }

            set
            {
                MaybeInitialize(); _settings.IniWriteValue(Section, "DatabaseKey", value);
            }
        }

        public string RootDB
        {
            get { MaybeInitialize(); return _settings.IniReadValue(Section, "DefaultDB", "none"); }
            set
            {
                MaybeInitialize(); _settings.IniWriteValue(Section, "DefaultDB", value);
            }

        }

        public string FileShare
        {
            get { MaybeInitialize(); return _settings.IniReadValue(Section, "FileShare", "none"); }
            set { MaybeInitialize(); _settings.IniWriteValue(Section, "FileShare", value); }
        }
    }
}
