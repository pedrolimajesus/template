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
using System.Linq;

namespace AppComponents
{
    public class AggregateConfiguration : IConfig
    {
        private IConfig[] _configurations;

        public AggregateConfiguration(params IConfig[] configurations)
        {
            _configurations = configurations;
        }

        #region IConfig Members

        public bool Get(Enum id, bool defaultValue)
        {
            if (!SettingExists(id))
                return defaultValue;

            try
            {
                return Get<bool>(id);
            }
            catch
            {
                return defaultValue;
            }
        }

        public int Get(Enum id, int defaultValue)
        {
            if (!SettingExists(id))
                return defaultValue;

            try
            {
                return Get<int>(id);
            }
            catch
            {
                return defaultValue;
            }
        }

        public string Get(Enum id, string defaultValue)
        {
            if (!SettingExists(id))
                return defaultValue;

            try
            {
                return Get<string>(id);
            }
            catch
            {
                return defaultValue;
            }
        }

        public string this[Enum id]
        {
            get { return Get<string>(id); }
        }

        public T Get<T>(Enum id)
        {
            try
            {
                return _configurations.First(c => c.SettingExists(id)).Get<T>(id);
            }
            catch
            {
                throw new SettingNotFoundException(string.Format("Setting not found: {0}", id));
            }
        }

        public bool SettingExists(Enum id)
        {
            return _configurations.Any(c => c.SettingExists(id));
        }

        public bool Get(string id, bool defaultValue)
        {
            if (!SettingExists(id))
                return defaultValue;

            try
            {
                return Get<bool>(id);
            }
            catch
            {
                return defaultValue;
            }
        }

        public int Get(string id, int defaultValue)
        {
            if (!SettingExists(id))
                return defaultValue;

            try
            {
                return Get<int>(id);
            }
            catch
            {
                return defaultValue;
            }
        }

        public string Get(string id, string defaultValue)
        {
            if (!SettingExists(id))
                return defaultValue;
            
            try
            {
                return Get<string>(id);
            }
            catch
            {
                return defaultValue;
            }
        }

        public string this[string id]
        {
            get { return Get<string>(id); }
        }

        public T Get<T>(string id)
        {
            try
            {
                var configations = _configurations.First(c => c.SettingExists(id));
                var retval = configations.Get<T>(id);
                return retval;
            }
            catch
            {
                throw new SettingNotFoundException(string.Format("Setting not found: {0}", id));
            }
        }

        public bool SettingExists(string id)
        {
            return _configurations.Any(c => c.SettingExists(id));
        }
        

        #endregion


       
    }
}