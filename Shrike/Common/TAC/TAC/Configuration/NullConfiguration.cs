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

namespace AppComponents.Configuration
{
    public class NullConfiguration : IConfig
    {
        #region IConfig Members

        public string this[Enum id]
        {
            get { throw new SettingNotFoundException(); }
        }

        public bool Get(Enum id, bool defaultValue)
        {
            return defaultValue;
        }

        public int Get(Enum id, int defaultValue)
        {
            return defaultValue;
        }

        public string Get(Enum id, string defaultValue)
        {
            return defaultValue;
        }

        public T Get<T>(Enum id)
        {
            throw new SettingNotFoundException();
        }

        public bool SettingExists(Enum id)
        {
            return false;
        }

        public string this[string id]
        {
            get { throw new SettingNotFoundException(); }
        }

        public bool Get(string id, bool defaultValue)
        {
            return defaultValue;
        }

        public int Get(string id, int defaultValue)
        {
            return defaultValue;
        }

        public string Get(string id, string defaultValue)
        {
            return defaultValue;
        }

        public T Get<T>(string id)
        {
            throw new SettingNotFoundException();
        }

        public bool SettingExists(string id)
        {
            return false;
        }

        #endregion
    }
}