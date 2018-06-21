using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AppComponents.Topology
{
    public abstract class GathererBase: IApplicationNodeGatherer, IApplicationAlert
    {
        protected ApplicationAlertSink _sink;
        protected List<NodeMetric> _currentMetrics = new List<NodeMetric>();
        protected int _activityLevel;

        public GathererBase(string company, string product)
        {
            _sink = new ApplicationAlertSink(company, product);
        }

        public IEnumerable<NodeMetric> GatherMetrics()
        {
            _currentMetrics = DoGatherMetrics();
            var retval = _currentMetrics.ToArray();
            _currentMetrics.Clear();
            return retval;
        }

        protected abstract List<NodeMetric> DoGatherMetrics();
 
        public IEnumerable<NodeAlert> NewAlerts()
        {
            return _sink.GetNewAlerts();
        }

        public int ActivityLevel
        {
            get { return _activityLevel; }
        }

        public void RaiseAlert(ApplicationAlertKind kind, params object[] details)
        {
            _sink.RaiseAlert(kind,details);
        }
    }
}
