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
using System.Collections.Concurrent;
using System.Threading;
using AppComponents.Extensions.EnumEx;

namespace AppComponents
{
    public abstract class DictionaryConfigurationBase : IConfig
    {
        protected ConcurrentDictionary<string, object> _configurationCache;
        protected object _initializationSync = new object();
        protected long _initialized;

        #region IConfig Members

        public bool Get(Enum id, bool defaultValue)
        {
            MaybeInitialize();
            if (!_configurationCache.ContainsKey(id.EnumName()))
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
            MaybeInitialize();
            if (!_configurationCache.ContainsKey(id.EnumName()))
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
            MaybeInitialize();
            if (!_configurationCache.ContainsKey(id.EnumName()))
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

            MaybeInitialize();
            object val;
            if (!_configurationCache.TryGetValue(id.EnumName(), out val))
            {
                throw new SettingNotFoundException(string.Format("{0} could not find setting {1}", GetType().FullName,
                                                                 id.EnumName()));
            }

            if (val.GetType() == typeof (T))
                return (T) val;

            return (T) Convert.ChangeType(val, typeof (T));
        }

        public bool SettingExists(Enum id)
        {
            MaybeInitialize();
            return _configurationCache.ContainsKey(id.EnumName());
        }


        public bool Get(string id, bool defaultValue)
        {
            if (!_configurationCache.ContainsKey(id.EnumName()))
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
            if (!_configurationCache.ContainsKey(id.EnumName()))
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
            if (!_configurationCache.ContainsKey(id.EnumName()))
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
            MaybeInitialize();
            object val;
            if (!_configurationCache.TryGetValue(id, out val))
            {
                throw new SettingNotFoundException(string.Format("{0} could not find setting {1}", GetType().FullName,
                                                                 id));
            }

            if (val.GetType() == typeof(T))
                return (T)val;

            return (T)Convert.ChangeType(val, typeof(T));
        }

        public bool SettingExists(string id)
        {
            MaybeInitialize();
            return _configurationCache.ContainsKey(id);
                
        }

        #endregion

        public abstract void FillDictionary();

        protected void MaybeInitialize()
        {
            if (_initialized == 0)
            {
                lock (_initializationSync)
                {
                    if (_initialized == 0)
                    {
                        Interlocked.Increment(ref _initialized);
                        _configurationCache = new ConcurrentDictionary<string, object>();
                        FillDictionary();
                    }
                }
            }
        }
    }
}