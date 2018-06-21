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
using AppComponents.Extensions.EnumEx;
//using Newtonsoft.Json;
using Raven.Client;


namespace AppComponents.Raven
{
    using global::Raven.Imports.Newtonsoft.Json;

    internal class WorkspaceDocument
    {
        public WorkspaceDocument()
        {
            Data = new Dictionary<string, string>();
            Aliases = new Dictionary<string, string>();
        }

        [DocumentIdentifier]
        public string Name { get; set; }

        public Dictionary<string, string> Data { get; set; }
        public Dictionary<string, string> Aliases { get; set; }

        public string AliasKey(string given)
        {
            if (Data.ContainsKey(given))
                return given;
            if (Aliases.ContainsKey(given))
                return Aliases[given];

            return given;
        }
    }


    internal class WorkspaceDataProxy: IWorkspaceData
    {
        private readonly WorkspaceDocument _wd;

        public WorkspaceDataProxy(WorkspaceDocument wd)
        {
            _wd = wd;
        }

        public bool Exists(string key)
        {
            return _wd.Data.ContainsKey(_wd.AliasKey(key));
        }

        public T Get<T>(string key)
        {
            if (_wd.Data.ContainsKey(key.EnumName()))
                return JsonConvert.DeserializeObject<T>(_wd.Data[_wd.AliasKey(key)]);
            return default(T);

        }

        public T Get<T>(string key, T defaultValue)
        {
            if (_wd.Data.ContainsKey(key.EnumName()))
                return JsonConvert.DeserializeObject<T>(_wd.Data[_wd.AliasKey(key)]);
            return defaultValue;
        }

        public void Put<T>(string key, T value)
        {
            _wd.Data[_wd.AliasKey(key)] = JsonConvert.SerializeObject(value);
        }

        public void Remove(string key)
        {
            _wd.Data.Remove(_wd.AliasKey(key));
        }

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


        
    }

    public class DocumentDistributedWorkspace : IWorkspace
    {

        private string _name;
        

        public DocumentDistributedWorkspace()
        {
            var cf = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            _name = cf[WorkspaceLocalConfig.WorkspaceName];
            
        }

        #region IWorkspace Members

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public void Batch(Action<IWorkspaceData> batchAction)
        {
            using (var dc = DocumentStoreLocator.ResolveOrRoot(WorkspaceLocalConfig.OptionalConfigurationHostName))
            {
                var data = dc.Load<WorkspaceDocument>(_name);
                if (null == data)
                {
                    data = new WorkspaceDocument() {Name = _name};
                    
                }

                batchAction(new WorkspaceDataProxy(data));
                dc.Store(data);

                dc.SaveChanges();
            }
        }

        public bool DeleteWorkspace()
        {
            using (var dc = DocumentStoreLocator.ResolveOrRoot(WorkspaceLocalConfig.OptionalConfigurationHostName))
            {
                var wsd = dc.Load<WorkspaceDocument>(_name);
                if (null != wsd)
                {
                    dc.Delete(wsd);
                    dc.SaveChanges();
                    return true;
                }
            }

            return false;
        }

        public void Dispose()
        {
            
        }

        public void RegisterKeyOverloads(params Tuple<string, string>[] aliases)
        {
            using (var dc = DocumentStoreLocator.ResolveOrRoot(WorkspaceLocalConfig.OptionalConfigurationHostName))
            {
                var data = dc.Load<WorkspaceDocument>(_name);
                if (null == data)
                {
                    data = new WorkspaceDocument() { Name = _name };

                }

                foreach(var alias in aliases)
                    data.Aliases.Add(alias.Item1, alias.Item2);
                dc.Store(data);

                dc.SaveChanges();
            }
        }

        #endregion


        
    }
}