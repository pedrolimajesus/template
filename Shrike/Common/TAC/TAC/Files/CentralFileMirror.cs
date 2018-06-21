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
using AppComponents.Extensions.StringEx;

namespace AppComponents
{
    public class CentralFileMirror : ILocalFileMirror
    {
        private IDictionary<string, string> _configuration;
        public CentralFileMirror()
        {
            
            _configuration = Catalog.Factory.Resolve<IConfig>()[CommonConfiguration.LocalFileMirror].ParseInitialization();
            
        }

        #region ILocalFileMirror Members

        public string SourcePath
        {
            get { return _configuration["SourcePath"]; }
        }

        public string TargetFolder
        {
            get { return _configuration["TargetFolder"]; }
        }

        public string TargetPath
        {
            get { return _configuration["TargetPath"]; }
        }

        #endregion
    }
}