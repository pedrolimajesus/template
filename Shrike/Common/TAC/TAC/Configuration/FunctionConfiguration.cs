using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppComponents.Extensions.EnumEx;

namespace AppComponents.Configuration
{
    public class FunctionConfiguration: IConfig
    {
        private ConcurrentDictionary<string, Func<object>> _configurationFunctions = new ConcurrentDictionary<string, Func<object>>();  

       

        public FunctionConfiguration()
        {
            
        }

       public FunctionConfiguration RegisterSetting(string id, Func<object> fetcher)
       {
           _configurationFunctions.TryAdd(id, fetcher);
           return this;
       }

       public FunctionConfiguration RegisterSetting(Enum id, Func<object> fetcher)
       {
           return RegisterSetting(id.EnumName(), fetcher);
       }

       #region IConfig Members

        public bool Get(Enum id, bool defaultValue)
        {
            if (!_configurationFunctions.ContainsKey(id.EnumName()))
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
            if (!_configurationFunctions.ContainsKey(id.EnumName()))
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
            if (!_configurationFunctions.ContainsKey(id.EnumName()))
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
            Func<object> fetcher;
            if (!_configurationFunctions.TryGetValue(id.EnumName(), out fetcher))
            {
                throw new SettingNotFoundException(string.Format("{0} could not find setting {1}", GetType().FullName,
                                                                 id.EnumName()));
            }

            val = fetcher();

            if (val.GetType() == typeof (T))
                return (T) val;

            return (T) Convert.ChangeType(val, typeof (T));
        }

        public bool SettingExists(Enum id)
        {
            
            return _configurationFunctions.ContainsKey(id.EnumName());
        }


        public bool Get(string id, bool defaultValue)
        {
            if (!_configurationFunctions.ContainsKey(id.EnumName()))
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
            if (!_configurationFunctions.ContainsKey(id.EnumName()))
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
            if (!_configurationFunctions.ContainsKey(id.EnumName()))
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
            Func<object> fetcher;
            if (!_configurationFunctions.TryGetValue(id, out fetcher))
            {
                throw new SettingNotFoundException(string.Format("{0} could not find setting {1}", GetType().FullName,
                                                                 id));
            }

            val = fetcher();
            if (val.GetType() == typeof(T))
                return (T)val;

            return (T)Convert.ChangeType(val, typeof(T));
        }

        public bool SettingExists(string id)
        {
            
            return _configurationFunctions.ContainsKey(id);
        }

        #endregion
    }
}
