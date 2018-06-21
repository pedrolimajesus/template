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
using AppComponents.Extensions.EnumEx;
using AppComponents.Extensions.EnumerableEx;

namespace AppComponents.Data
{
    internal class WorkspaceData
    {
        public WorkspaceData()
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


    internal class MemoryWorkspaceDataProxy : IWorkspaceData
    {
        private readonly WorkspaceData _wd;

        public MemoryWorkspaceDataProxy(WorkspaceData wd)
        {
            _wd = wd;
        }

        #region IWorkspaceData Members

        public bool Exists(Enum key)
        {
            return Exists(key.EnumName());
        }

        public T Get<T>(Enum key)
        {
            return Get<T>(key.EnumName());
        }

        public T Get<T>(Enum key, T defaultValue)
        {
            return Get<T>(key.EnumName(), defaultValue);
        }

        public void Put<T>(Enum key, T value)
        {
            Put<T>(key.EnumName(), value);
        }

        public void Remove(Enum key)
        {
            Remove(key.EnumName());
        }

        

        public bool Exists(string key)
        {
            return _wd.Data.ContainsKey(_wd.AliasKey(key));
        }

        public T Get<T>(string key)
        {
            if (_wd.Data.ContainsKey(_wd.AliasKey(key)))
                return (T)_wd.Data[_wd.AliasKey(key)];
            return default(T);
        }

        public T Get<T>(string key, T defaultValue)
        {
            if (_wd.Data.ContainsKey(_wd.AliasKey(key)))
                return (T)_wd.Data[_wd.AliasKey(key)];
            return defaultValue;
        }

        public void Put<T>(string key, T value)
        {
            _wd.Data[_wd.AliasKey(key)] = value;
        }

        public void Remove(string key)
        {
            object _;
            _wd.Data.TryRemove(_wd.AliasKey(key), out _);
        }

        #endregion


    }

    public class MemoryWorkspace : IWorkspace
    {
        private static readonly ConcurrentDictionary<string, WorkspaceData> _workspaces =
            new ConcurrentDictionary<string, WorkspaceData>();

        private readonly WorkspaceData _wsData;
        private string _name;

        public MemoryWorkspace()
        {
            var cf = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            _name = cf[WorkspaceLocalConfig.WorkspaceName];

            if (!_workspaces.TryGetValue(_name, out _wsData))
            {
                _wsData = new WorkspaceData {Name = _name};
                _workspaces.TryAdd(_name, _wsData);
            }
        }

        #region IWorkspace Members

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public void Batch(Action<IWorkspaceData> batchAction)
        {
            batchAction(new MemoryWorkspaceDataProxy(_wsData));
        }

        public bool DeleteWorkspace()
        {
            WorkspaceData _;
            return _workspaces.TryRemove(_name, out _);
        }

        public void Dispose()
        {
        }

        public void RegisterKeyOverloads(params Tuple<string, string>[] aliases)
        {
            aliases.ForEach(alias => _wsData.Aliases.TryAdd(alias.Item1, alias.Item2));
        }

        #endregion





        
    }
}