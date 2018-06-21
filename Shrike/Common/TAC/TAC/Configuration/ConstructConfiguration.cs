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
using System.Collections.Generic;
using AppComponents.Configuration;
using AppComponents.Extensions.EnumEx;

namespace AppComponents
{
    public class ConstructConfiguration : IConfig
    {
        private Dictionary<string, object> _configuration = new Dictionary<string, object>();
        private IConfig _default;

        public ConstructConfiguration()
        {
            try
            {
                _default = Catalog.Factory.Resolve<IConfig>();
            }
            catch (Exception ex)
            {
                _default = new NullConfiguration();
            }
        }

        #region IConfig Members

        public bool Get(Enum id, bool defaultValue)
        {
            if (!_configuration.ContainsKey(id.EnumName()))
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
            if (!_configuration.ContainsKey(id.EnumName()))
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
            if (!_configuration.ContainsKey(id.EnumName()))
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
            object val;
            if (!_configuration.TryGetValue(id.EnumName(), out val))
            {
                return _default.Get<T>(id);
            }

            if (val.GetType() == typeof (T))
                return (T) val;

            if (val is T)
                return (T) val;

            return (T) Convert.ChangeType(val, typeof (T));
        }

        public bool SettingExists(Enum id)
        {
            return _configuration.ContainsKey(id.EnumName());
        }

        public bool Get(string id, bool defaultValue)
        {
            if (!_configuration.ContainsKey(id.EnumName()))
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
            if (!_configuration.ContainsKey(id.EnumName()))
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
            if (!_configuration.ContainsKey(id.EnumName()))
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
            object val;
            if (!_configuration.TryGetValue(id.EnumName(), out val))
            {
                return _default.Get<T>(id);
            }

            if (val.GetType() == typeof(T))
                return (T)val;

            if (val is T)
                return (T)val;

            return (T)Convert.ChangeType(val, typeof(T));
        }

        public bool SettingExists(string id)
        {
            return _configuration.ContainsKey(id);
        }

        #endregion

        public ConstructConfiguration Add(string key, object configValue)
        {
            _configuration.Add(key, configValue);
            return this;
        }

        public ConstructConfiguration Add(Enum key, object configValue)
        {
            _configuration.Add(key.EnumName(), configValue);
            return this;
        }

        public T ConfiguredResolve<T>(string name = null) where T : class
        {
            if (null == name)
                name = string.Empty;

            var retval = Catalog.Factory.Resolve<T>(name);
            Catalog.ResolveBoundConfiguration();
            return retval;
        }

        public T ConfiguredResolve<T>(Enum id) where T : class
        {
            var retval = Catalog.Factory.Resolve<T>(id);
            Catalog.ResolveBoundConfiguration();
            return retval;
        }

        public T ConfiguredCreate<T>(Func<T> creator) where T : class
        {
            var retval = creator();
            Catalog.ResolveBoundConfiguration();
            return retval;
        }
    }
}