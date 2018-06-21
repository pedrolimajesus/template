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
using System.Diagnostics;

namespace AppComponents
{
    public enum CommonConfiguration
    {
        AuthCodeSecret,
        ClassLogging,
        DefaultStorageConnection,
        DefaultBusConnection,
        DefaultDataConnection,
        DefaultDataDatabase,
        DefaultDataUser,
        DefaultDataPassword,
        DatabaseSecret,
        DefaultDownloadContainer,
        DistributedFileShare,
        CoreDatabaseRoute,
        LocalFileMirror,
        LogLevel,
        StoreType,
        EnableControlNumberOfTagsValidation
    }

    public static class ConfigString
    {
    }

    public static class ConfigurationValidator
    {
        private const string _missingKeyString = "missing a key from this configuration type";

        public static bool ValidateConfiguration(Type configType)
        {
            Debug.Assert(configType.IsEnum);
            IConfig config = Catalog.Factory.Resolve<IConfig>();
            string missingConfigs = string.Empty;

            foreach (var key in Enum.GetNames(configType))
            {
                Enum tkey = (Enum) Enum.Parse(configType, key);
                string val = config.Get(tkey, _missingKeyString);
                if (val == _missingKeyString)
                {
                    var ms = string.Format("Configuration validation: cannot retrieve key {0}", key);
                    CriticalLog.Always.WarnFormat(ms);
                    missingConfigs += ms;
                }
            }

            if (!string.IsNullOrEmpty(missingConfigs))
            {
                IApplicationAlert on = Catalog.Factory.Resolve<IApplicationAlert>();
                on.RaiseAlert(ApplicationAlertKind.Defect, missingConfigs);
            }

            return true;
        }
    }
}