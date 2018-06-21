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
    public class FilePersistentStore<TKey> : PersistentStoreBase<TKey>
    {
        private readonly string _basePath;
        private readonly string _logPath;
        private readonly string _name;
        private readonly string _suffix;
        private readonly bool _writeThrough;

        private FileStream _log;


        public FilePersistentStore(string basePath, string name, string suffix, bool writeThrough)
        {
            _basePath = basePath;
            _name = name;
            _suffix = suffix;
            _writeThrough = writeThrough;

            _logPath = Path.Combine(basePath, name + suffix);

            MaybeCleanUpRename(_logPath);

            IsCreated = !File.Exists(_logPath);
            OpenFiles();
        }

        protected override Stream Log
        {
            get { return _log; }
        }

        private void OpenFiles()
        {
            _log = new FileStream(_logPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 4096,
                                  _writeThrough
                                      ? FileOptions.WriteThrough | FileOptions.SequentialScan
                                      : FileOptions.SequentialScan);
        }

        private void MaybeCleanUpRename(string logPath)
        {
            string renamed = logPath + ".rename";
            if (File.Exists(renamed) == false)
                return;

            if (File.Exists(logPath))
                File.Delete(renamed);
            else
                File.Move(renamed, logPath);
        }

        public override void ReplaceAtomically(Stream newLog)
        {
            var newStream = (FileStream) newLog;
            var tempName = newStream.Name;

            newStream.Flush();

            newLog.Dispose();
            newStream.Dispose();
            _log.Dispose();


            var renamed = _logPath + ".rename";
            File.Move(_logPath, renamed);
            File.Move(tempName, _logPath);
            File.Delete(renamed);

            OpenFiles();
        }

        public override Stream ProvideTempStream()
        {
            var tempFile = Path.Combine(_basePath, Path.GetFileName(Path.GetTempFileName()));
            return File.Open(tempFile, FileMode.Create, FileAccess.ReadWrite);
        }

        public override void FlushLog()
        {
            _log.Flush(_writeThrough);
        }

        public override StoreState ProvideState()
        {
            return new StoreState
                       {
                           Path = _basePath,
                           Prefix = _name
                       };
        }

        public override void SetCapacity(int size)
        {
        }

        protected override Stream ReadOnlyClonedStream()
        {
            return new FileStream(_logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public override void Dispose()
        {
            Action disposeParent = base.Dispose;
            Write(_ => _log.Dispose());
            disposeParent();
        }

        public void Delete()
        {
            File.Delete(_logPath);
        }
    }
}