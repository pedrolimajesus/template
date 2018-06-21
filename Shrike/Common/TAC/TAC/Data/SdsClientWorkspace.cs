using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppComponents.Extensions.EnumEx;

namespace AppComponents.Data
{
    public class SdsClientWorkspace : IWorkspace
    {
        public SdsClientWorkspace()
        {
        }

        public string Name
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public void Batch(Action<IWorkspaceData> batchAction)
        {
            throw new NotImplementedException();
        }

        public bool DeleteWorkspace()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }


        public void RegisterKeyOverloads(params Tuple<string, string>[] aliases)
        {
            throw new NotImplementedException();
        }
    }

    internal class SdsWorkspaceData
    {
        public SdsWorkspaceData()
        {
            Data = new ConcurrentDictionary<string, object>();
            Aliases = new ConcurrentDictionary<string, string>();
        }

        public string Name { get; set; }

        public ConcurrentDictionary<string, object> Data { get; set; }
        public ConcurrentDictionary<string, string> Aliases { get; set; }

        public string AliasKey(string given)
        {
            if (Data.ContainsKey(given))
                return given;
            if (Aliases.ContainsKey(given))
                return Aliases[given];

            return given;
        }
    }


    internal class SdsWorkspaceDataProxy : IWorkspaceData
    {
        private readonly WorkspaceData _wd;

        public SdsWorkspaceDataProxy(WorkspaceData wd)
        {
            _wd = wd;
        }

        #region IWorkspaceData Members

        public bool Exists(Enum key)
        {
            return _wd.Data.ContainsKey(key.EnumName());
        }

        public T Get<T>(Enum key)
        {
            if (_wd.Data.ContainsKey(key.EnumName()))
                return (T)_wd.Data[key.EnumName()];
            return default(T);
        }

        public T Get<T>(Enum key, T defaultValue)
        {
            if (_wd.Data.ContainsKey(key.EnumName()))
                return (T)_wd.Data[key.EnumName()];
            return defaultValue;
        }

        public void Put<T>(Enum key, T value)
        {
            _wd.Data[key.EnumName()] = value;
        }

        public void Remove(Enum key)
        {
            object _;
            _wd.Data.TryRemove(key.EnumName(), out _);
        }

        public bool Exists(string key)
        {
            throw new NotImplementedException();
        }

        public T Get<T>(string key)
        {
            throw new NotImplementedException();
        }

        public T Get<T>(string key, T defaultValue)
        {
            throw new NotImplementedException();
        }

        public void Put<T>(string key, T value)
        {
            throw new NotImplementedException();
        }

        public void Remove(string key)
        {
            throw new NotImplementedException();
        }
        #endregion


       
    }

    
}
