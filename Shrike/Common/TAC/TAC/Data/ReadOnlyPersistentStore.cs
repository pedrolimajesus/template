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
using System.IO;

namespace AppComponents.Data
{
    public class ReadOnlyPersistentStore<TKey> : PersistentStoreBase<TKey>
    {
        private readonly string _basePath;
        private readonly string _logPath;
        private readonly string _name;
        private readonly string _suffix;

        private Stream _log;

        public ReadOnlyPersistentStore(string basePath, string name, string suffix)
        {
            _basePath = basePath;
            _name = name;
            _logPath = Path.Combine(_basePath, name + suffix);

            OpenFiles();
        }

        protected override Stream Log
        {
            get { return _log; }
        }

        private void OpenFiles()
        {
            _log = ReadOnlyClonedStream();
        }

        public override void ReplaceAtomically(Stream newLog)
        {
            throw new NotImplementedException();
        }

        public override Stream ProvideTempStream()
        {
            throw new NotImplementedException();
        }

        public override void FlushLog()
        {
            throw new NotImplementedException();
        }

        public override StoreState ProvideState()
        {
            return new StoreState
                       {
                           Path = _logPath,
                           Prefix = _name
                       };
        }

        public override void SetCapacity(int size)
        {
        }

        protected override Stream ReadOnlyClonedStream()
        {
            return new FileStream(_logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096,
                                  FileOptions.SequentialScan);
        }

        public override void Dispose()
        {
            _log.Dispose();
            base.Dispose();
        }
    }
}