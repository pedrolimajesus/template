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
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace AppComponents.Files
{
    public class PageLockedMemoryMappedFileStream : Stream, IDisposable
    {
        private readonly long _capacity;
        private readonly MemoryMappedViewAccessor _header;
        private readonly Mutex _lengthMutex;
        private readonly string _mapName;
        private readonly MemoryMappedFile _mmf;
        private readonly Stream _mmfstr;
        private readonly string _mutexTemplate;
        private readonly int _pageSize;
        private bool _isDisposed;

        public PageLockedMemoryMappedFileStream(string mapName, long capacity, int pageSize = 4096)
        {
            _mapName = mapName;
            const int streamHeaderSize = 1024;
            _mmf = MemoryMappedFile.CreateOrOpen(mapName, capacity + streamHeaderSize, MemoryMappedFileAccess.ReadWrite,
                                                 MemoryMappedFileOptions.DelayAllocatePages,
                                                 null, HandleInheritability.Inheritable);
            _pageSize = pageSize;

            _header = _mmf.CreateViewAccessor(0, sizeof (long));
            _mmfstr = _mmf.CreateViewStream(streamHeaderSize, capacity);

            _mutexTemplate = string.Format("PLMMFS//{0}//Page{{0:0000000000000}}", mapName);
            _lengthMutex = new Mutex(false, string.Format("PLMMFS//{0}//Length", mapName));
            _capacity = capacity;
        }


        public string MapName
        {
            get { return _mapName; }
        }

        public int PageSize
        {
            get { return _pageSize; }
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
                long retval;

                try
                {
                    _lengthMutex.WaitOne();
                    retval = _header.ReadInt64(0);
                }
                finally
                {
                    _lengthMutex.ReleaseMutex();
                }

                return retval;
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
                _header.Dispose();
                _lengthMutex.Dispose();
                _mmfstr.Dispose();
                _mmf.Dispose();
            }
        }

        #endregion

        public override void Flush()
        {
            _mmfstr.Flush();
        }


        private PageSpan CreateSpanningFromHere(int count)
        {
            return new PageSpan
                       {
                           _startPage = (int) ((_mmfstr.Position/_pageSize)),
                           _firstSpanLength = (int) (_mmfstr.Position%_pageSize),
                           _lastPage = (int) (((_mmfstr.Position + count)/_pageSize)),
                           _bytesLeft = count
                       };
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var ps = CreateSpanningFromHere(count);
            var readLength = ps._firstSpanLength;
            var bytesRead = 0;

            var currentRead = 1;

            using (LockPages(ps._startPage, ps._lastPage))
            {
                for (var eachPage = ps._startPage; eachPage <= ps._lastPage && currentRead > 0; eachPage++)
                {
                    currentRead = _mmfstr.Read(buffer, offset, readLength);


                    offset += currentRead;
                    bytesRead += currentRead;

                    ps._bytesLeft -= currentRead;
                    readLength = ps._bytesLeft < _pageSize ? ps._bytesLeft : _pageSize;
                }
            }

            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _mmfstr.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            if (value > Capacity)
                throw new InvalidOperationException("Length exceeds capacity.");

            try
            {
                _lengthMutex.WaitOne();
                _header.Write(0, value);
            }
            finally
            {
                _lengthMutex.ReleaseMutex();
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var ps = CreateSpanningFromHere(count);
            var writeLength = ps._firstSpanLength;

            try
            {
                _lengthMutex.WaitOne();
                var currentLength = _header.ReadInt64(0);
                if (_mmfstr.Position + count > currentLength)
                {
                    currentLength = _mmfstr.Position + count;
                    _header.Write(0, currentLength);
                }
            }
            finally
            {
                _lengthMutex.ReleaseMutex();
            }

            using (LockPages(ps._startPage, ps._lastPage))
            {
                for (var eachPage = ps._startPage; eachPage <= ps._lastPage; eachPage++)
                {
                    _mmfstr.Write(buffer, offset, writeLength);
                    offset += writeLength;
                    ps._bytesLeft -= _pageSize;
                    writeLength = ps._bytesLeft < _pageSize ? ps._bytesLeft : _pageSize;
                }
            }

            _mmfstr.Flush();
        }

        public void Append(byte[] buffer, int offset, int count)
        {
            try
            {
                _lengthMutex.WaitOne();
                var currentPos = _header.ReadInt64(0);
                _mmfstr.Position = currentPos;
                _header.Write(0, currentPos + count);
            }
            finally
            {
                _lengthMutex.ReleaseMutex();
            }

            var ps = CreateSpanningFromHere(count);
            var writeLength = ps._firstSpanLength;

            using (LockPages(ps._startPage, ps._lastPage))
            {
                for (var eachPage = ps._startPage; eachPage <= ps._lastPage; eachPage++)
                {
                    _mmfstr.Write(buffer, offset, writeLength);


                    offset += writeLength;
                    ps._bytesLeft -= _pageSize;
                    writeLength = ps._bytesLeft < _pageSize ? ps._bytesLeft : _pageSize;
                }
            }

            _mmfstr.Flush();
        }


        public IDisposable LockPage(int pageNumber)
        {
            var theLock = new Mutex(false, string.Format(_mutexTemplate, pageNumber));
            theLock.WaitOne();
            return Disposable.Create(theLock.ReleaseMutex);
        }

        public IDisposable LockPages(int start, int end)
        {
            var pageLocks = new List<IDisposable>();
            for (var each = 0; each <= end; each++)
                pageLocks.Add(LockPage(each));

            return Disposable.Create(() => ReleaseLocks(pageLocks));
        }

        private static void ReleaseLocks(IEnumerable<IDisposable> locks)
        {
            foreach (var pl in locks)
                pl.Dispose();
        }

        public IDisposable LockAllPages()
        {
            var sz = _mmfstr.Length;
            var pages = sz/_pageSize;
            if (pages > 16777216)
                throw new OutOfMemoryException();

            var pageLocks = new List<Mutex>((int) pages);
            for (var each = 0; each != pages; each++)
            {
                var m = new Mutex(false, string.Format(_mutexTemplate, each));
                m.WaitOne();
                pageLocks.Add(m);
            }

            var globalLock = pageLocks.ToArray();

            return Disposable.Create(() =>
                                         {
                                             foreach (var m in globalLock)
                                                 m.ReleaseMutex();
                                         });
        }

        #region Nested type: PageSpan

        private struct PageSpan
        {
            public int _bytesLeft;
            public int _firstSpanLength;
            public int _lastPage;
            public int _startPage;
        }

        #endregion
    }
}