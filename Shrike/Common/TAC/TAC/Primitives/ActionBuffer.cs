using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;


namespace AppComponents.Primitives
{
    public class ActionBuffer<T>: IDisposable
    {
        private TimeSpan _timeOut;
        private int _maxBuffer;

        private Timer _timer;
        private object _bufferLock = new object();
        private List<T> _items = new List<T>();
        private Action<IEnumerable<T>> _action;

        public ActionBuffer(Action<IEnumerable<T>> action, int timeOutSeconds = 30, int maxBufferSize = 1024)
        {
             _maxBuffer = maxBufferSize;
            _timeOut = TimeSpan.FromSeconds(timeOutSeconds);

            if (timeOutSeconds > 0)
            {
                _timer = new Timer(CheckSend, this, 0L, (long) _timeOut.TotalMilliseconds);
            }

        }

        private static void CheckSend(object obj)
        {
            var that = (ActionBuffer<T>)obj;
            that.Timeout();
        }

        public ActionBuffer<T> Add(T item)
        {
            int count = 0;
            lock (_bufferLock)
            {
                _items.Add(item);
                count = _items.Count;
            }

            if(count > _maxBuffer)
                SendBuffer();

            return this;
        }

        public ActionBuffer<T> AddRange(IEnumerable<T> things)
        {
            int count = 0;
            lock (_bufferLock)
            {
                _items.AddRange(things);
                count = _items.Count;
            }

            if (count > _maxBuffer)
                SendBuffer();

            return this;
        }

        public void Timeout()
        {
            SendBuffer();
        }

        private void SendBuffer()
        {


            T[] things;
            lock (_bufferLock)
            {
                if (!_items.Any())
                    return;

                things = _items.ToArray();
                _items.Clear();
            }

            _action(things);
        }


        private bool _isDisposed;
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                if (_timer != null)
                    _timer.Dispose();
            }
            
        }
    }
}
