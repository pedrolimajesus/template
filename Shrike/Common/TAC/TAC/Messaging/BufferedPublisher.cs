using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using AppComponents.Extensions.EnumEx;

namespace AppComponents.Messaging
{
    public enum BufferedPublisherLocalConfig
    {
        ForwardTo,
        OptionalTimeoutSeconds,
        OptionalMaximumBuffer
    }

    public class BufferedPublisher: IMessagePublisher
    {
        private class BufferedMessage
        {
            public object Data { get; set; }
            public string Route { get; set; }
        }

        private IMessagePublisher _forwardPublisher;
        private TimeSpan _timeOut;
        private int _maxBuffer;
        private const int DefaultMaximumBuffer = 4096;
        private const int DefaultTimeoutSeconds = 10;
        private Timer _timer;
        private object _bufferLock = new object();
        private List<BufferedMessage> _messages = new List<BufferedMessage>(); 


        public BufferedPublisher()
        {
            var cf = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            _forwardPublisher = cf.Get<IMessagePublisher>(BufferedPublisherLocalConfig.ForwardTo);
            _maxBuffer = cf.Get(BufferedPublisherLocalConfig.OptionalMaximumBuffer, DefaultMaximumBuffer);
            _timeOut = TimeSpan.FromSeconds(cf.Get(BufferedPublisherLocalConfig.OptionalTimeoutSeconds, DefaultTimeoutSeconds));
            _timer = new Timer(CheckSend, this, 0L, (long) _timeOut.TotalMilliseconds);
        }

        private static void CheckSend(object obj)
        {
            var that = (BufferedPublisher)obj;
            that.Timeout();
        }

        private void SendBuffer()
        {
            

            BufferedMessage[] msgs;
            lock (_bufferLock)
            {
                if (!_messages.Any())
                    return;

                msgs = _messages.ToArray();
                _messages.Clear();
            }

            foreach (var msg in msgs)
            {
                _forwardPublisher.Send(msg.Data,msg.Route);
            }
        }

        public void Timeout()
        {
            SendBuffer();
        }

        public void Send<T>(T msg, string routeKey)
        {
            int count = 0;
            lock (_bufferLock)
            {
                _messages.Add(new BufferedMessage { Data = msg, Route = routeKey});
                count = _messages.Count;
            }

            if(count>_maxBuffer)
                SendBuffer();
        }

        public void Send<T>(T msg, Enum routeKey)
        {
            Send(msg, routeKey.EnumName());
        }

        private bool _isDisposed = false;
        public void Dispose()
        {
            lock(_bufferLock)
            if (!_isDisposed)
            {
                _isDisposed = true;
                _forwardPublisher.Dispose();

                _timer.Dispose();
                _timer = null;
            }
            
        }
    }
}
