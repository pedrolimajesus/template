using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace AppComponents.Messaging
{

    public enum PassiveListenerLocalConfig
    {
        OuterListener
    }

    public interface IPassiveMessageDispatch
    {
        void InvokeCollectedMessages();
    }

    public class PassiveListener: IMessageListener, IPassiveMessageDispatch
    {
        private IMessageListener _outerListener;
        private Dictionary<Type, Action<object, CancellationToken, IMessageAcknowledge>> _sinks = new Dictionary<Type, Action<object, CancellationToken, IMessageAcknowledge>>();
        private Queue<Tuple<object,IMessageAcknowledge>> _collected = new Queue<Tuple<object, IMessageAcknowledge>>(); 

        private object _lock = new object();

        public PassiveListener()
        {
            var cf = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            _outerListener = cf.Get<IMessageListener>(PassiveListenerLocalConfig.OuterListener);
        }
        
        public void Listen(params KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>[] listener)
        {
            foreach (var keyValuePair in listener)
            {
                _sinks.Add(keyValuePair.Key,keyValuePair.Value);
            }

           var converted =
                _sinks.Select(it => new KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>
                                        (it.Key,
                                         (object obj, CancellationToken ct, IMessageAcknowledge ack) =>
                                         this.Collect(obj, ct, ack))).ToArray();

            _outerListener.Listen(converted);
        }

        private void Collect(object obj, CancellationToken ct, IMessageAcknowledge ack)
        {
            if(_sinks.ContainsKey(obj.GetType()))
                lock(_lock) _collected.Enqueue(Tuple.Create(obj,ack));
        }

        public void InvokeCollectedMessages()
        {
            lock (_lock)
            {
                using (var cts = new CancellationTokenSource())
                {
                    while (_collected.Any())
                    {
                        var msg = _collected.Dequeue();
                        _sinks[msg.Item1.GetType()](msg.Item1, cts.Token, msg.Item2);
                    }

                }
            }
        }

        public void Dispose()
        {
            _outerListener.Dispose();
        }

        
    }
}
