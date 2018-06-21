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
using AppComponents.Extensions.EnumEx;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace AppComponents.Azure
{
    /// <summary>
    ///   Implements the <see cref="IConfig" /> interface using Azure configuration settings.
    /// </summary>
    public class AzureConfig : IConfig
    {
        #region IConfig Members

        public bool Get(string id, bool defaultValue)
        {
            try
            {
                return Get<bool>(id);
            }
            catch (RoleEnvironmentException)
            {
                return defaultValue;
            }
        }

        public int Get(string id, int defaultValue)
        {
            try
            {
                return Get<int>(id);
            }
            catch (RoleEnvironmentException)
            {
                return defaultValue;
            }
        }


        public string Get(string id, string defaultValue)
        {
            Debug.Assert(RoleEnvironment.IsAvailable);
            try
            {
                return RoleEnvironment.GetConfigurationSettingValue(id);
            }
            catch (RoleEnvironmentException)
            {
                return defaultValue;
            }
        }

        public string this[string id]
        {
            get
            {
                Debug.Assert(RoleEnvironment.IsAvailable);
                return RoleEnvironment.GetConfigurationSettingValue(id);
            }
        }


        public T Get<T>(string id)
        {
            Debug.Assert(RoleEnvironment.IsAvailable);
            var configData = RoleEnvironment.GetConfigurationSettingValue(id);
            return (T) Convert.ChangeType(configData, typeof (T));
        }


        public bool SettingExists(string id)
        {
            bool available = false;
            try
            {
                RoleEnvironment.GetConfigurationSettingValue(id);
                available = true;
            }
            catch (RoleEnvironmentException)
            {
            }

            return available;
        }

        public string this[Enum id]
        {
            get { return this[id.EnumName()]; }
        }

        public bool Get(Enum id, bool defaultValue)
        {
            return Get(id.EnumName(), defaultValue);
        }

        public int Get(Enum id, int defaultValue)
        {
            return Get(id.EnumName(), defaultValue);
        }

        public string Get(Enum id, string defaultValue)
        {
            return Get(id.EnumName(), defaultValue);
        }

        public T Get<T>(Enum id)
        {
            return Get<T>(id.EnumName());
        }

        public bool SettingExists(Enum id)
        {
            return SettingExists(id.EnumName());
        }

        #endregion

        
    }
}