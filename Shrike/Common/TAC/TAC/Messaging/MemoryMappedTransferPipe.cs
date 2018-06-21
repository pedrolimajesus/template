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
using System.Threading;
using AppComponents.Files;

namespace AppComponents.Messaging
{
    public class MemoryMappedTransferPipe : IDisposable
    {
        private readonly InterProcessLockedMemoryMappedFileStream _xfer;
        private bool _isDisposed;


        public MemoryMappedTransferPipe(string name, int capacity)
        {
            _xfer = new InterProcessLockedMemoryMappedFileStream(name, capacity);
        }

        public WaitHandle PipeMessage
        {
            get { return _xfer.WrittenEvent; }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _xfer.Dispose();
            }
        }

        #endregion

        public void ReadPipe(Action<Stream> pipeReader)
        {
            using (var ms = new MemoryStream())
            {
                _xfer.AtomicAction(mmfs =>
                                       {
                                           mmfs.Seek(0, SeekOrigin.Begin);
                                           mmfs.CopyTo(ms);
                                           mmfs.SetLength(0);
                                           mmfs.WrittenEvent.Reset();
                                       });

                ms.Seek(0, SeekOrigin.Begin);
                pipeReader(ms);
            }
        }


        public void AppendPipe(Action<InterProcessLockedMemoryMappedFileStream> pipeWriter)
        {
            _xfer.AtomicAction(mmfs =>
                                   {
                                       pipeWriter(mmfs);
                                       mmfs.WrittenEvent.Set();
                                   });
        }
    }
}