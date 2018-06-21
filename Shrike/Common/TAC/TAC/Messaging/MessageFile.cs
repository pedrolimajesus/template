using System;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;


namespace AppComponents.Messaging
{

    public enum MessageFileConfig
    {
        Path,
        Name,
        OptionalKeepAliveTimeoutMinutes
    }


    public class FileMessageExchange
    {
        public string EndpointType { get; set; }
        public string InstanceId { get; set; }
        public string InboxId { get; set; }
        public string Path { get; set; }
        public DateTime LastKeepAlive { get; set; }

        public FileMessageExchange()
        {
            LastKeepAlive = DateTime.UtcNow;
        }


        public MessageFileListener CreateListener()
        {
            return Catalog.Preconfigure()
                .Add(MessageFileConfig.Path, Path)
                .Add(MessageFileConfig.Name, InboxId)
                .ConfiguredCreate(() => new MessageFileListener());

        }

        public MessageFilePublisher CreatePublisher()
        {
            return Catalog.Preconfigure()
               .Add(MessageFileConfig.Path, Path)
               .Add(MessageFileConfig.Name, InboxId)
               .ConfiguredCreate(() => new MessageFilePublisher());
        }
    }

    public class FileMessageBus
    {
        const string BusData = "FileMessageBus.json";
        Mutex _fileMutex;
        string _filePath;
        string _fileName;
        TimeSpan _longestAlive;

        public FileMessageBus()
        {
            var cf = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            _filePath = cf[MessageFileConfig.Path];
            _fileName = cf.Get(MessageFileConfig.Name, BusData);

            double timeoutSeconds = double.Parse(cf.Get(MessageFileConfig.OptionalKeepAliveTimeoutMinutes, "15.0"));
            _longestAlive = TimeSpan.FromMinutes(timeoutSeconds);

            var fmn = string.Format("Global\\{{{0}}}",_fileName);
            _fileMutex = new Mutex(false, fmn);

            var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
            var securitySettings = new MutexSecurity();
            securitySettings.AddAccessRule(allowEveryoneRule);
            _fileMutex.SetAccessControl(securitySettings);
        }

        public void KeepAlive(string instanceId)
        {
            ExchangeFile(
                     ef =>
                     {
                         var item = ef.SingleOrDefault(ex => ex.InstanceId == instanceId);
                         if (item == null)
                             return false;
                         item.LastKeepAlive = DateTime.UtcNow;
                         return true;
                     }
                );
        }

        public FileMessageExchange AllocateExchange(string endpointType, string instanceId)
        {
            var fme = new FileMessageExchange
            {
                LastKeepAlive = DateTime.UtcNow,
                Path = _filePath,
                EndpointType = endpointType,
                InstanceId = instanceId,
                InboxId = string.Format("{0}.json", instanceId)
            };
            ExchangeFile(
                    ef =>
                    {
                        ef.Add(fme);
                        return true;
                    });
            return fme;
        }

        public void DeallocateExchange(string instanceId)
        {
            ExchangeFile(
                     ef =>
                     {
                         var item = ef.SingleOrDefault(ex => ex.InstanceId == instanceId);
                         if (item == null)
                             return false;
                         ef.Remove(item);
                         return true;
                     }
                );
        }

        public IEnumerable<FileMessageExchange> GetExchanges()
        {
            List<FileMessageExchange> retval = new List<FileMessageExchange>();

            ExchangeFile(
                ef =>
                {
                    retval.AddRange(ef);
                    return false;
                }
                );

            return retval;
        }

        private void ReleaseMutex()
        {
            _fileMutex.ReleaseMutex();
        }


        private void ExchangeFile(Func<List<FileMessageExchange>, bool> exchOp)
        {
            using (Disposable.Create(ReleaseMutex))
            {
                _fileMutex.WaitOne();

                string exchData = string.Empty;

                using (FileStream file = File.Open(
                    Path.Combine(_filePath, _fileName),
                    FileMode.Append,
                    FileAccess.ReadWrite,
                    FileShare.Read))
                {
                    using (var reader = new StreamReader(file))
                        exchData = reader.ReadToEnd();



                    List<FileMessageExchange> exchanges;

                    if (!string.IsNullOrEmpty(exchData))

                        exchanges = JsonConvert.DeserializeObject<List<FileMessageExchange>>(exchData);
                    else
                        exchanges = new List<FileMessageExchange>();

                    var writeFile = exchOp(exchanges);

                    var expirationTime = DateTime.UtcNow - _longestAlive;
                    var expired = exchanges.Where(ex => ex.LastKeepAlive > expirationTime).ToArray();

                    if (expired.Any())
                    {
                        writeFile = true;
                        foreach (var it in expired)
                            exchanges.Remove(it);
                    }



                    if (writeFile)
                    {


                        var jsStr = JsonConvert.SerializeObject(exchanges);
                        
                        using (var sw = new StreamWriter(file))
                        {
                            sw.Write(jsStr);
                        }

                    }


                }

            }

        }



    }




    public class FileMessageEnvelope
    {
        public string Key { get; set; }
        public Type MessageType { get; set; }
        public object Data { get; set; }
    }


    public class MessageFileListener
    {
        Mutex _fileMutex;
        string _filePath;
        string _fileName;
        Queue<FileMessageEnvelope> _inbox = new Queue<FileMessageEnvelope>();
        long _runFlag = 0;
        object _inboxLock = new object();


        public MessageFileListener()
        {
            var cf = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            _filePath = cf[MessageFileConfig.Path];
            _fileName = cf[MessageFileConfig.Name];
            var fmn = string.Format("Global\\{{{0}}}", _fileName);
            _fileMutex = new Mutex(false, fmn);

            var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
            var securitySettings = new MutexSecurity();
            securitySettings.AddAccessRule(allowEveryoneRule);
            _fileMutex.SetAccessControl(securitySettings);
        }

        public void Stop()
        {
            var running = Interlocked.Read(ref _runFlag);
            if (running != 0)
                Interlocked.Decrement(ref _runFlag);
        }

        public void Listen()
        {
            var running = Interlocked.Read(ref _runFlag);
            if (running == 0)
            {
                Interlocked.Increment(ref _runFlag);
                var thread = new Thread(ReadFromFile);
            }
        }

        public FileMessageEnvelope Dequeue()
        {
            lock (_inboxLock)
            {

                return _inbox.Count != 0 ? _inbox.Dequeue() : null;
            }
        }

        public bool Any()
        {
            lock (_inboxLock)
            {
                return _inbox.Count != 0;
            }
        }

        private FileMessageEnvelope[] _emptyMsgs = { };
        public IEnumerable<FileMessageEnvelope> DequeueAll()
        {
            lock (_inboxLock)
            {
                if (_inbox.Count == 0)
                    return _emptyMsgs;

                var msgs = _inbox.ToArray();
                _inbox.Clear();
                return msgs;
            }
        }

        public event EventHandler ReceivedMessage;

        private void ReleaseMutex()
        {
            _fileMutex.ReleaseMutex();
        }



        private void ReadFromFile()
        {
            FileSystemWatcher fsw = new FileSystemWatcher
            {
                Path = _filePath,
                Filter = _fileName
            };


            bool stop = false;
            while (!stop)
            {
                try
                {

                    string msgs = string.Empty;
                    fsw.WaitForChanged(WatcherChangeTypes.Changed);


                    using (Disposable.Create(ReleaseMutex))
                    {
                        if (_fileMutex.WaitOne(TimeSpan.FromSeconds(2.0)))
                        {
                            using (FileStream file = File.Open(
                                Path.Combine(_filePath, _fileName),
                                FileMode.Open,
                                FileAccess.ReadWrite,
                                FileShare.Write))
                            {


                                using (var reader = new StreamReader(file))
                                    msgs = reader.ReadToEnd();


                                file.SetLength(0);
                                file.Flush();
                                file.Close();
                            }


                        }
                        else
                            msgs = string.Empty;
                    }

                    if (!string.IsNullOrEmpty(msgs))
                    {
                        var messages = JsonConvert.DeserializeObject<List<FileMessageEnvelope>>(msgs);

                        lock (_inboxLock)
                        {
                            foreach (var m in messages)
                                _inbox.Enqueue(m);

                        }

                        if (ReceivedMessage != null)
                            ReceivedMessage(this, new EventArgs());

                    }

                    var running = Interlocked.Read(ref _runFlag);
                    if (running == 0)
                        stop = true;
                }
                catch (Exception)
                {
                }
            }
        }



    }

    public class MessageFilePublisher
    {
        Mutex _fileMutex;
        string _filePath;
        string _fileName;

        Queue<FileMessageEnvelope> _outbox = new Queue<FileMessageEnvelope>();

        object _outboxLock = new object();

        public MessageFilePublisher()
        {
            var cf = Catalog.Factory.Resolve<IConfig>(SpecialFactoryContexts.Routed);
            _filePath = cf[MessageFileConfig.Path];
            _fileName = cf[MessageFileConfig.Name];
            var fmn = string.Format("Global\\{{{0}}}", _fileName);
            _fileMutex = new Mutex(false, fmn);

            var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
            var securitySettings = new MutexSecurity();
            securitySettings.AddAccessRule(allowEveryoneRule);
            _fileMutex.SetAccessControl(securitySettings);
        }

        public void Enqueue(FileMessageEnvelope msg)
        {
            lock (_outboxLock)
            {
                _outbox.Enqueue(msg);

            }
        }


        public void Send()
        {
            int count = 0;
            lock (_outboxLock)
                count = _outbox.Count;
            if (count > 0)
                WriteToFile();
        }

        private void ReleaseMutex()
        {
            _fileMutex.ReleaseMutex();
        }

        private void WriteToFile()
        {
            using (Disposable.Create(ReleaseMutex))
            {
                _fileMutex.WaitOne();

                string msgs = string.Empty;
                using (FileStream file = File.Open(
                    Path.Combine(_filePath, _fileName),
                    FileMode.Append,
                    FileAccess.ReadWrite,
                    FileShare.Read))
                {
                    using (var reader = new StreamReader(file))
                        msgs = reader.ReadToEnd();



                    List<FileMessageEnvelope> messages;

                    if (!string.IsNullOrEmpty(msgs))
                        messages = JsonConvert.DeserializeObject<List<FileMessageEnvelope>>(msgs);
                    else
                        messages = new List<FileMessageEnvelope>();


                    lock (_outboxLock)
                    {
                        if (_outbox.Count != 0)
                        {

                            messages.AddRange(_outbox.ToArray());
                            var jsStr = JsonConvert.SerializeObject(messages);
                            
                            using (StreamWriter sw = new StreamWriter(file))
                            {
                                sw.Write(jsStr);
                                sw.Flush();

                            }

                            file.Flush();
                            file.Close();
                           

                            _outbox.Clear();
                        }
                    }


                }

            }

        }


    }
}

