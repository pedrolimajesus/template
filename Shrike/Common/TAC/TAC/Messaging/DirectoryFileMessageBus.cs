using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AppComponents.ControlFlow;
using AppComponents.Extensions.EnumEx;
using AppComponents.Extensions.EnumerableEx;
using AppComponents.Messaging.Declarations;
using AppComponents.RandomNumbers;
 using Newtonsoft.Json;

namespace AppComponents.Messaging
{
   
  
   


        public class FDMessageListener : IMessageListener
        {
            private readonly FDMessageInbox _q;
            private readonly string _bus;
            private readonly string _exchange;
            private readonly string _queueName;

            private readonly Dictionary<Type, Action<object, CancellationToken, IMessageAcknowledge>> _sinks =
                new Dictionary<Type, Action<object, CancellationToken, IMessageAcknowledge>>();

            private CancellationToken _ct;
            private CancellationTokenSource _cts;
            private bool _isDisposed;
            private Task _listening;


            public FDMessageListener()
            {
                var config = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
                _bus = config[MessageListenerLocalConfig.HostConnectionString];
                _exchange = config[MessageListenerLocalConfig.ExchangeName];
                _queueName = config[MessageListenerLocalConfig.QueueName];
                _q = new FDMessageInbox(_bus,_exchange,_queueName);

                _cts = new CancellationTokenSource();
            }

            #region IMessageListener Members

            public void Listen(params KeyValuePair<Type, Action<object, CancellationToken, IMessageAcknowledge>>[] listener)
            {
                if (null != _listening)
                {
                    _cts.Cancel();
                    _listening.Wait();
                    _listening.Dispose();

                    _cts = new CancellationTokenSource();
                }

                _sinks.Clear();
                foreach (var sink in listener)
                    _sinks.Add(sink.Key, sink.Value);

                _ct = _cts.Token;

                _listening = Task.Factory.StartNew(Listen, this, _ct);
            }

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    _isDisposed = true;
                    _cts.Cancel();
                    _listening.Wait();
                    _cts.Dispose();
                    _listening.Dispose();


                }
            }

            #endregion

            private static void Listen(object that)
            {
                var @this = (FDMessageListener)that;

                while (!@this._ct.IsCancellationRequested)
                {
                    var messages = @this._q.WaitForMessages(TimeSpan.FromSeconds(10.0));
                    if (messages.EmptyIfNull().Any())
                    {
                        foreach (var env in messages)
                        {
                            var msg = env.Decode();

                            if (@this._sinks.ContainsKey(env.MessageType))
                            {
                                var sink = @this._sinks[env.MessageType];
                                sink(msg, @this._cts.Token, new FDQueueAcknowledge(env, @this._bus, @this._exchange, @this._queueName));
                            }
                            else
                            {
                                
                                env.Lock.Release();
                                env.Lock.Dispose();

                            }
                        }
                    }
                }
            }




        }

        

        internal class FDQueueAcknowledge : IMessageAcknowledge, IDisposable
        {
            private readonly FDMessageEnvelope _message;
            private readonly string _bus;
            private readonly string _exchange;
            private readonly string _queueName;


            public FDQueueAcknowledge(FDMessageEnvelope message, string bus, string exchange, string queueName)
            {
                _message = message;
                _bus = bus;
                _exchange = exchange;
                _queueName = queueName;

            }

            #region IMessageAcknowledge Members


            void DeleteMessage()
            {
                var qPath = FDMessageBus.GetQueuePath(_bus, _exchange, _queueName);
                var fn = Path.Combine(qPath, string.Format("{0}.msg", _message.Id));
                var fi = new FileInfo(fn);
                if(fi.Exists)
                    fi.Delete();
                

            }

            public void MessageAcknowledged()
            {
                DeleteMessage();

                _message.Lock.Release();
                _message.Lock.Dispose();

                _isDisposed = true;
            }

            public void MessageAbandoned()
            {

                _message.Lock.Release();
                _message.Lock.Dispose();

                _isDisposed = true;
            }

            public void MessageRejected()
            {
                var ia = Catalog.Factory.Resolve<IApplicationAlert>();
                ia.RaiseAlert(ApplicationAlertKind.System, "Message rejected during processing", _message);

                DeleteMessage();

                _message.Lock.Release();
                _message.Lock.Dispose();

                _isDisposed = true;
            }

            #endregion

            private bool _isDisposed = false;
            public void Dispose()
            {
                if (!_isDisposed)
                {
                    _message.Lock.Release();
                    _message.Lock.Dispose();
                }
            }
        }

        public class FDMessagePublisher : IMessagePublisher
        {
            private readonly FDMessageBus _bus;
            private readonly string _exchangeName;
            private ExchangeTypes _exchangeType;
            private bool _isDisposed;
            private List<IQueueSpecifier> _queues;
            private string _busName;


            public FDMessagePublisher()
            {
                var cfg = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
                _busName = cfg[MessagePublisherLocalConfig.HostConnectionString];
                _exchangeName = cfg[MessagePublisherLocalConfig.ExchangeName];


                _bus = Catalog.Preconfigure().Add(MessageBusSpecifierLocalConfig.HostConnectionString,_busName)
                        .ConfiguredCreate(()=> new FDMessageBus());
            }

            #region IMessagePublisher Members

            public void Send<T>(T msg, string routeKey)
            {
                RefreshQueues();

                var matchingQueues = MessageExchangeDeclaration.BindMessageToQueues(routeKey, _exchangeType, _queues);
                foreach (var q in matchingQueues)
                {
                    var outbox = new FDMessageOutbox(_busName, _exchangeName, q);
                    outbox.Enqueue(msg);
                    outbox.Send();

                }
            }

            public void Send<T>(T msg, Enum routeKey)
            {
                Send(msg, routeKey.EnumName());
            }

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    _isDisposed = true;
                    _bus.Dispose();
                }
            }

            #endregion

            private void RefreshQueues()
            {
                var ex = _bus.SpecifyExchange(_exchangeName);
                if (null != ex)
                {
                    _queues = ex.Queues.ToList();
                    _exchangeType = ex.ExchangeType;
                }
                else
                {
                    _queues = new List<IQueueSpecifier>();
                }
            }
        }


        public class FDMessageBus : IMessageBusSpecifier
        {

            private bool _isDisposed;
            private string _busPath;

            public FDMessageBus()
            {
                var cf = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
                _busPath = cf[MessageBusSpecifierLocalConfig.HostConnectionString];
            }

            public static string GetExchangePath(string basePath, string name)
            {
                return Path.Combine(basePath, name);
            }

            public static string GetQueuePath(string basePath, string exchange, string queue)
            {
                return Path.Combine(basePath, exchange, queue);
            }

            public string GetExchangePath(string name)
            {
                return GetExchangePath(_busPath, name);
            }

            public string GetQueuePath(string exchange, string queue)
            {
                return GetQueuePath(_busPath, exchange, queue);
            }

            public static IDistributedMutex GetExchangeMutex(string name)
            {
                return Catalog.Preconfigure().Add(DistributedMutexLocalConfig.Name, string.Format("Exchange{0}",name)).ConfiguredResolve<IDistributedMutex>();
            }

            public static IDistributedMutex GetQueueMutex(string exchange, string name)
            {
                return Catalog.Preconfigure().Add(DistributedMutexLocalConfig.Name, string.Format("Exchange{0}Queue{1}", exchange, name)).ConfiguredResolve<IDistributedMutex>();
            }

            public static IDistributedMutex GetMessageMutex(string exchange, string name, string messageId)
            {
                return
                    Catalog.Preconfigure()
                           .Add(DistributedMutexLocalConfig.Name,
                                string.Format("Exchange{0}Queue{1}Msg{2}", exchange, name, messageId))
                           .ConfiguredResolve<IDistributedMutex>();
            }


            #region IMessageBusSpecifier Members

            public IMessageBusSpecifier DeclareExchange(string exchangeName, ExchangeTypes exchangeType)
            {
                var exchangePath = GetExchangePath(exchangeName);

                var mtx = GetExchangeMutex(exchangeName);

                var di = new DirectoryInfo(exchangePath);

                using (mtx)
                {
                    if (mtx.Wait(TimeSpan.FromSeconds(30)))
                    {
                        if (di.Exists)
                            return this;

                        di.Create();
                        using (
                            var sw =
                                File.CreateText(Path.Combine(exchangePath,
                                                             string.Format("{0}.type", exchangeType.EnumName()))))
                        {
                            sw.Flush();
                            sw.Close();
                        }

                        mtx.Release();
                    }

                    

                }

              

                return this;

            }

            public IMessageBusSpecifier DeclareExchange(Enum exchangeName, ExchangeTypes exchangeType)
            {
                return DeclareExchange(exchangeName.EnumName(), exchangeType);

            }

            public IMessageBusSpecifier DeleteExchange(string exchangeName)
            {
                var exchangePath = GetExchangePath(exchangeName);

                var mtx = GetExchangeMutex(exchangeName);

                var di = new DirectoryInfo(exchangePath);

                using (mtx)
                {
                    if (mtx.Wait(TimeSpan.FromSeconds(30)))
                    {
                        di.Delete(true);
                        mtx.Release();
                    }
                }

                return this;
            }

            public IMessageBusSpecifier DeleteExchange(Enum exchangeName)
            {
                return DeleteExchange(exchangeName.EnumName());
            }

            public IExchangeSpecifier SpecifyExchange(string exchangeName)
            {
                var exchangePath = GetExchangePath(exchangeName);
                var mtx = GetExchangeMutex(exchangeName);
                var di = new DirectoryInfo(exchangePath);
                using (mtx)
                {

                    if (mtx.Wait(TimeSpan.FromSeconds(30)) && di.Exists)
                    {
                        var typeFile = di.EnumerateFiles("*.type").FirstOrDefault();
                        if (null != typeFile)
                        {
                            var typestr = typeFile.Name.Split('.').FirstOrDefault();
                            ExchangeTypes type;
                            if(!string.IsNullOrEmpty(typestr) && Enum.TryParse(typestr, out type))
                               return new FDExchange(this, exchangeName, type);
                        }

                        
                    }
                }

                return null;
                
            }

            public IExchangeSpecifier SpecifyExchange(Enum exchangeName)
            {
                return SpecifyExchange(exchangeName.EnumName());
            }

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    _isDisposed = true;
                }
            }

            #endregion

            
            
        }


        public class FDExchange : IExchangeSpecifier
        {
            private string _exchangeName;
            private readonly FDMessageBus _parent;
            private readonly ExchangeTypes _exchangeType;

            internal FDExchange(FDMessageBus parent, string exchangeName, ExchangeTypes exchangeType)
            {
                _parent = parent;
                _exchangeName = exchangeName;
                _exchangeType = exchangeType;
            }

            #region IExchangeSpecifier Members

            public string Name
            {
                get { return _exchangeName; }
            }

            public ExchangeTypes ExchangeType
            {
                get { return _exchangeType; }
            }

            public IExchangeSpecifier DeclareQueue(string queueName, params string[] boundRoutes)
            {
                var qPath = _parent.GetQueuePath(_exchangeName, queueName);
                var qMtx = FDMessageBus.GetQueueMutex(_exchangeName, queueName);
                var exPath = _parent.GetExchangePath(_exchangeName);
                var exMtx = FDMessageBus.GetExchangeMutex(_exchangeName);

                using(exMtx)
                using(qMtx)
                {
                    if (exMtx.Wait(TimeSpan.FromSeconds(30)) && qMtx.Wait(TimeSpan.FromSeconds(30)))
                    {
                        var di = new DirectoryInfo(qPath);
                        if (di.Exists)
                            return this;
                        di.Create();

                        var fi = new FileInfo(Path.Combine(qPath, "routes.json"));
                        using (var sw = fi.CreateText())
                        {
                            var q = new MessageQueueDeclaration {Name = queueName};
                            q.Bindings.AddRange(boundRoutes);
                            var text = JsonConvert.SerializeObject(q);
                            sw.Write(text);
                            sw.Flush();
                            sw.Close();
                        }

                    }

                    

                }

               
                return this;
            }

            public IExchangeSpecifier DeclareQueue(Enum queueName, params string[] boundRoutes)
            {
                return DeclareQueue(queueName.EnumName(), boundRoutes);
            }

            public IExchangeSpecifier DeleteQueue(string queueName)
            {
                var qPath = _parent.GetQueuePath(_exchangeName, queueName);
                var qMtx = FDMessageBus.GetQueueMutex(_exchangeName, queueName);
                var exPath = _parent.GetExchangePath(_exchangeName);
                var exMtx = FDMessageBus.GetExchangeMutex(_exchangeName);

                using(exMtx)
                using (qMtx)
                {
                    if (exMtx.Wait(TimeSpan.FromSeconds(30)) && qMtx.Wait(TimeSpan.FromSeconds(30)))
                    {
                        var di = new DirectoryInfo(qPath);
                        if (!di.Exists)
                            return this;

                        di.Delete(true);
                    }
                }
                return this;
            }

            public IExchangeSpecifier DeleteQueue(Enum queueName)
            {
                return DeleteQueue(queueName.EnumName());
            }

            IQueueSpecifier GetQueueInfo(string queueName)
            {
                
                var qPath = _parent.GetQueuePath(_exchangeName, queueName);

                var qMtx = FDMessageBus.GetQueueMutex(_exchangeName, queueName);
                using (qMtx)
                {
                    if (qMtx.Wait(TimeSpan.FromSeconds(120)))
                    {
                        var di = new DirectoryInfo(qPath);
                        if (!di.Exists)
                            return null;

                        var fi = new FileInfo(Path.Combine(qPath, "routes.json"));
                        if(fi.Exists)
                        using (var sr = fi.OpenText())
                        {
                            var text = sr.ReadToEnd();
                            var mqd = JsonConvert.DeserializeObject<MessageQueueDeclaration>(text);
                            return new FDQueueSpecifier(mqd);
                        }
                    }
                }


                return null;

            }

            public IQueueSpecifier SpecifyQueue(string queueName)
            {
                var exPath = _parent.GetExchangePath(_exchangeName);
                var exMtx = FDMessageBus.GetExchangeMutex(_exchangeName);

                using(exMtx)
                {
                    if (exMtx.Wait(TimeSpan.FromSeconds(30)) )
                    {
                        return GetQueueInfo(queueName);
                    }
                }

                return null;
            }

            public IQueueSpecifier SpecifyQueue(Enum queueName)
            {
                return SpecifyQueue(queueName.EnumName());
            }


            public IEnumerable<IQueueSpecifier> Queues
            {
                get
                {
                    var exPath = _parent.GetExchangePath(_exchangeName);
                    var exMtx = FDMessageBus.GetExchangeMutex(_exchangeName);

                    using (exMtx)
                    {
                        if (exMtx.Wait(TimeSpan.FromSeconds(30)))
                        {
                            var di = new DirectoryInfo(exPath);
                            if (!di.Exists)
                                return Enumerable.Empty<IQueueSpecifier>();

                            var qdirs = di.EnumerateDirectories();
                            return qdirs.Select(qi => GetQueueInfo(qi.Name)).Where(it => null != it).ToArray();
                        }
                    }

                    return Enumerable.Empty<IQueueSpecifier>();
                }
            }

            #endregion
        }

        public class FDQueueSpecifier : IQueueSpecifier
        {
            private readonly MessageQueueDeclaration _queueSpec;

            public FDQueueSpecifier(MessageQueueDeclaration queueSpec)
            {
                _queueSpec = queueSpec;
            }

            #region IQueueSpecifier Members

            public string Name
            {
                get { return _queueSpec.Name; }
            }

            public IEnumerable<string> Bindings
            {
                get { return _queueSpec.Bindings; }
            }

            #endregion
        }

        public class FDMessageEnvelope
        {
            public string Id { get; set; }
            public object Data { get; set; }
            public Type MessageType { get; set; }

            [JsonIgnore]
            public IDistributedMutex Lock { get; set; }
            
            public object Decode()
            {
                return Data;
            }
        }

        public class FDMessageOutbox
        {

            private readonly ConcurrentQueue<object> _pending = new ConcurrentQueue<object>();

            private readonly object _sendLock = new object();
        
            private bool _isDisposed;
            private Timer _sendTimer;
            private JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };


            public FDMessageOutbox(string busName, string exchangeName, string queueName)
            {
                _busName = busName;
                _exchangeName = exchangeName;
                _queueName = queueName;
               
            }

            

            #region IMessageOutbox Members

            public void Dispose()
            {
                if (_isDisposed)
                    return;

                _isDisposed = true;

                if (null != _sendTimer)
                {
                    _sendTimer.Dispose();
                    _sendTimer = null;
                }


            }

            
            

            public bool Pending
            {
                get { return _pending.Any(); }
            }

            public void Enqueue<T>(T item)
            {
                _pending.Enqueue(item);
            }

            private const int BatchLimit = 100;
            private string _busName;
            private string _exchangeName;
            private string _queueName;
            public void Send()
            {
                lock (_sendLock)
                {
                    while (_pending.Any())
                    {
                        var qPath = FDMessageBus.GetQueuePath(_busName, _exchangeName, _queueName);
                        var di = new DirectoryInfo(qPath);
                        if (!di.Exists)
                            throw new FileNotFoundException();

                        //var qMtx = FDMessageBus.GetQueueMutex(_exchangeName, _queueName);

                        //using (qMtx)
                        {
                            //if (qMtx.Wait(TimeSpan.FromSeconds(120)))
                            {
                                

                                object item;
                                while (_pending.TryDequeue(out item))
                                {
                                    var id = Guid.NewGuid().ToString();
                                    var msgPath = Path.Combine(qPath, string.Format("{0}.msg", id));
                                    using (var sw = File.CreateText(msgPath))
                                    {
                                        var text = JsonConvert.SerializeObject(new FDMessageEnvelope { Id=id, Data = item, MessageType = item.GetType()}, _jsonSerializerSettings);
                                        sw.Write(text);
                                        sw.Flush();
                                        sw.Close();
                                    }
                                }
                            }
                        }


                    }
                }
            }


            public void AutomaticSend()
            {
                AutomaticSend(TimeSpan.FromSeconds(5.0));
            }

            public void AutomaticSend(TimeSpan sendDuration)
            {
                if (null != _sendTimer)
                    return;

                _sendTimer = new Timer(CheckSend, this, 0L, (long)sendDuration.TotalMilliseconds);
            }

            #endregion

            private static void CheckSend(object obj)
            {
                var that = (FDMessageOutbox)obj;
                that.Send();
            }
        }


        public class FDMessageInbox
        {


            private readonly Guid _reservationId = Guid.NewGuid(); // yeah. Each inbox reserves messages
            private JsonSerializerSettings _serializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

            public FDMessageInbox(string busName, string exchangeName, string queueName)
            {
                _busName = busName;
                _exchangeName = exchangeName;
                _queueName = queueName;
                _rnd = GoodSeedRandom.Create();
            }

            

            #region IMessageInbox Members


            private static readonly TimeSpan CheckSpan = TimeSpan.FromSeconds(2.0);
            private readonly Random _rnd;

            private FDMessageEnvelope MaybeReserveOne()
            {
                var qPath = FDMessageBus.GetQueuePath(_busName, _exchangeName, _queueName);
                foreach (var it in _messageNames)
                {
                    if(_taken.Contains(it))
                        continue;
                    

                    var mmtx = FDMessageBus.GetMessageMutex(_exchangeName, _queueName, it);
                    
                    
                    if (mmtx.Open())
                    {
                        try
                        {


                            var fn = Path.Combine(qPath, it);
                            var fi = new FileInfo(fn);
                            _taken.Add(it);
                            if (fi.Exists)
                            {
                                using (var sr = fi.OpenText())
                                {
                                    var text = sr.ReadToEnd();
                                    var retval = JsonConvert.DeserializeObject<FDMessageEnvelope>(text,
                                                                                                  _serializerSettings);
                                    if (null != retval)
                                    {
                                        retval.Lock = mmtx;
                                        
                                        return retval;
                                    }

                                }
                       
                            }


                            mmtx.Release();
                        }
                        catch // not finally, because if we can open the message, control of the mutex passes 
                              // to the caller
                        {
                            
                            mmtx.Release();
                            
                        }

                        

                       
                    }


                    mmtx.Dispose();
                    
                }


                return null;

            }

            
            private string _busName;
            private string _exchangeName;
            private string _queueName;
            private List<string> _messageNames = new List<string>();  
            private List<string> _taken = new List<string>(); 

            private void GetMessageNames()
            {
                _messageNames.Clear();
                

                var qPath = FDMessageBus.GetQueuePath(_busName, _exchangeName, _queueName);
                
                var di = new DirectoryInfo(qPath);
                if (!di.Exists)
                    return;

                _messageNames.AddRange(di.EnumerateFiles("*.msg").Select(it=>it.Name));
                    
                
            }

            public IEnumerable<FDMessageEnvelope> WaitForMessages(TimeSpan duration)
            {

                var timeout = DateTime.UtcNow + duration;
                FDMessageEnvelope reserved = null;

                _taken.Clear();
                while (DateTime.UtcNow < timeout && null == reserved)
                {
                    GetMessageNames();
                    reserved = MaybeReserveOne();
                    if (null == reserved)
                        Thread.Sleep(CheckSpan);
                }

                var retval = new List<FDMessageEnvelope>();


                while (null != reserved)
                {
                    retval.Add(reserved);
                    reserved = MaybeReserveOne();

                    if (null == reserved)
                    {
                        GetMessageNames();
                        reserved = MaybeReserveOne();
                    }
                }


                return retval;
            }

            #endregion
        }
    }

