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

using System.IO;

namespace AppComponents.Data
{
    public class MemoryPersistentStore<TKey> : PersistentStoreBase<TKey>
    {
        private MemoryStream _log;

        public MemoryPersistentStore()
        {
            _log = new MemoryStream();
            IsCreated = true;
        }

        public MemoryPersistentStore(byte[] data)
        {
            _log = new MemoryStream(data);
            IsCreated = true;
        }


        protected override Stream Log
        {
            get { return _log; }
        }

        protected override Stream ReadOnlyClonedStream()
        {
            var memoryStream = _log;
            var buffer = memoryStream.GetBuffer();
            return new MemoryStream(buffer, 0, buffer.Length, false);
        }

        public override void ReplaceAtomically(Stream newLog)
        {
            _log = (MemoryStream) newLog;
        }

        public override Stream ProvideTempStream()
        {
            return new MemoryStream();
        }

        public override void FlushLog()
        {
        }

        public override StoreState ProvideState()
        {
            return new StoreState {Log = ((MemoryStream) Log).ToArray(),};
        }

        public override void SetCapacity(int size)
        {
            _log.Capacity = size;
        }
    }
}