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
using System.IO.MemoryMappedFiles;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace AppComponents.Files
{
    public class InterProcessLockedMemoryMappedFileStream : Stream, IDisposable
    {
        private readonly long _capacity;
        private readonly MemoryMappedViewAccessor _header;
        private readonly string _mapName;
        private readonly MemoryMappedFile _mmf;
        private readonly Stream _mmfstr;
        private readonly Mutex _objectMutex;
        private readonly EventWaitHandle _writeSignal;
        private bool _haveLock;
        private bool _isDisposed;

        public InterProcessLockedMemoryMappedFileStream(string mapName, long capacity)
        {
            _mapName = mapName;
            const int streamHeaderSize = 1024;
            _mmf = MemoryMappedFile.CreateOrOpen(mapName, capacity + streamHeaderSize, MemoryMappedFileAccess.ReadWrite,
                                                 MemoryMappedFileOptions.None,
                                                 null, HandleInheritability.Inheritable);


            _header = _mmf.CreateViewAccessor(0, streamHeaderSize);
            _mmfstr = _mmf.CreateViewStream(streamHeaderSize, capacity);

            var id = string.Format("Global\\PLMMFS-ObLock-{0}", mapName);

            var mutexAccessRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
            var mutexSecurity = new MutexSecurity();
            mutexSecurity.AddAccessRule(mutexAccessRule);
            _objectMutex = new Mutex(false, id);
            _objectMutex.SetAccessControl(mutexSecurity);
            
            _writeSignal = new EventWaitHandle(false, EventResetMode.ManualReset,
                                               string.Format("Global\\PLMMFS-Written-{0}", mapName));
            var eventSecurity = new EventWaitHandleSecurity();
            var eventAccessRule = new EventWaitHandleAccessRule(
                new SecurityIdentifier(WellKnownSidType.WorldSid, null), 
                EventWaitHandleRights.FullControl, 
                AccessControlType.Allow);
            eventSecurity.AddAccessRule(eventAccessRule);
            _writeSignal.SetAccessControl(eventSecurity);

            _capacity = capacity;
        }


        public string MapName
        {
            get { return _mapName; }
        }

        public EventWaitHandle WrittenEvent
        {
            get { return _writeSignal; }
        }

        public long Capacity
        {
            get { return _capacity; }
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get
            {
                using (Lock())
                {
                    return _header.ReadInt64(0);
                }
            }
        }

        public override long Position
        {
            get { return _mmfstr.Position; }

            set { _mmfstr.Position = value; }
        }

        public Stream UnsafeStream
        {
            get { return _mmfstr; }
        }

        #region IDisposable Members

        void IDisposable.Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _objectMutex.Dispose();
                _writeSignal.Dispose();
                _mmfstr.Dispose();
                _header.Dispose();
                _mmf.Dispose();
            }
        }

        #endregion

        public override void Flush()
        {
            _mmfstr.Flush();
        }


        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead;

            using (Lock())
            {
                var currentLength = _header.ReadInt64(0);
                long readCount = count;
                if (_mmfstr.Position + count > currentLength)
                    readCount = currentLength - _mmfstr.Position;
                count = (int) readCount;

                bytesRead = _mmfstr.Read(buffer, offset, count);
            }

            return bytesRead;
        }

        public void AtomicAction(Action<InterProcessLockedMemoryMappedFileStream> action)
        {
            using (Lock())
            {
                action(this);
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _mmfstr.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            if (value > Capacity)
                throw new InvalidOperationException("Length exceeds capacity.");

            using (Lock())
            {
                _header.Write(0, value);
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            using (Lock())
            {
                if (_mmfstr.Position + count > Capacity)
                    throw new EndOfStreamException();

                var currentLength = _header.ReadInt64(0);
                if (_mmfstr.Position + count > currentLength)
                {
                    currentLength = _mmfstr.Position + count;
                    _header.Write(0, currentLength);
                }


                _mmfstr.Write(buffer, offset, count);
                _mmfstr.Flush();
            }
        }

        public void Append(byte[] buffer, int offset, int count)
        {
            using (Lock())
            {
                var currentPos = _header.ReadInt64(0);
                _mmfstr.Position = currentPos;
                if (_mmfstr.Position + count > Capacity)
                    throw new EndOfStreamException();

                _header.Write(0, currentPos + count);


                _mmfstr.Write(buffer, offset, count);
                _mmfstr.Flush();
            }
        }

        public bool MaybeAppend(byte[] buffer, int offset, int count)
        {
            using (Lock())
            {
                var currentPos = _header.ReadInt64(0);
                _mmfstr.Position = currentPos;
                if (currentPos + count > _mmfstr.Length)
                    return false;

                _header.Write(0, currentPos + count);

                if (_mmfstr.Position + count > Capacity)
                    throw new EndOfStreamException();
                _mmfstr.Write(buffer, offset, count);
                _mmfstr.Flush();
            }

            return true;
        }

        public IDisposable Lock()
        {
            if (_haveLock)
            {
                return Disposable.Create(() => { });
            }

            _objectMutex.WaitOne();
            _haveLock = true;
            return Disposable.Create(() =>
                                         {
                                             _haveLock = false;
                                             _objectMutex.ReleaseMutex();
                                         });
        }
    }
}