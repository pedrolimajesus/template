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

namespace AppComponents
{
    public enum WorkspaceLocalConfig
    {
        WorkspaceName,
        OptionalConfigurationHostName
    }


    public interface IWorkspaceData
    {
        bool Exists(Enum key);
        bool Exists(string key);

        T Get<T>(Enum key);
        T Get<T>(Enum key, T defaultValue);
        T Get<T>(string key);
        T Get<T>(string key, T defaultValue);

        void Put<T>(Enum key, T value);
        void Put<T>(string key, T value);

        void Remove(Enum key);
        void Remove(string key);

        
    }

    public interface IWorkspace : IDisposable
    {
        string Name { get; set; }

        void Batch(Action<IWorkspaceData> batchAction);
        bool DeleteWorkspace();
        void RegisterKeyOverloads(params Tuple<string, string>[] aliases);
        
    }
}