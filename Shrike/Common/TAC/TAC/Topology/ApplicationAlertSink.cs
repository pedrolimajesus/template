using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using log4net;

namespace AppComponents.Topology
{

    

    public class ApplicationAlertSink: IApplicationAlert
    {
        private ConcurrentQueue<NodeAlert> _alerts = new ConcurrentQueue<NodeAlert>();
        private ConcurrentDictionary<int, DateTime>  _duplicateTracking = new ConcurrentDictionary<int, DateTime>();
        private ILog _log;
        private DebugOnlyLogger _dbLog;
        private ApplicationNodeRegistry _reg;

        private const int ExpireSeconds = 3600;

        public ApplicationAlertSink(string company, string product)
        {
            _reg = new ApplicationNodeRegistry(company, product);
        }

        public void BeginLogging()
        {
            _log = ClassLogger.Create(GetType());
            _dbLog = DebugOnlyLogger.Create(_log);
        }

        public IEnumerable<NodeAlert> GetNewAlerts()
        {
            var retval = _alerts.ToArray();
            NodeAlert dummy;
            while (_alerts.TryDequeue(out dummy));
            return retval;
        }

        public void RaiseAlert(ApplicationAlertKind kind, params object[] details)
        {
            BeginLogging();
            ExpireDups();

            var jsonDetail = JsonConvert.SerializeObject(details);

            var alert = new NodeAlert
                {
                    Id = Guid.NewGuid().ToString(),
                    ComponentOrigin = _reg.ComponentType,
                    Detail = jsonDetail,
                    EventTime = DateTime.UtcNow,
                    Handled = false,
                    Kind = kind
                };

            var hash = AlertHash(alert);

            if (!_duplicateTracking.ContainsKey(hash))
            {
                _duplicateTracking.TryAdd(hash, DateTime.UtcNow);
                _alerts.Enqueue(alert);
                _log.ErrorFormat("Application alert: {0}",jsonDetail);
                System.Diagnostics.Debug.WriteLine(jsonDetail);
            }
            else
            {
                _log.DebugFormat("app alert {0} is duplicate, discarding", hash);
            }
        }

        private void ExpireDups()
        {
            var keys = _duplicateTracking.Keys;
            foreach (var key in keys)
            {
                DateTime dupTime;
                if (_duplicateTracking.TryGetValue(key, out dupTime))
                {
                    var elapsed = DateTime.UtcNow - dupTime;
                    if (elapsed.TotalSeconds > ExpireSeconds)
                    {
                        _dbLog.InfoFormat("app alert for {0} from {1} expired", key, dupTime);
                        _duplicateTracking.TryRemove(key, out dupTime);
                    }
                }
            }
        }

        public static int AlertHash(NodeAlert item)
        {
            return Hash.GetCombinedHashCodeForHashes(item.Detail.GetHashCode(), item.Kind.GetHashCode());
        }
    }
}
