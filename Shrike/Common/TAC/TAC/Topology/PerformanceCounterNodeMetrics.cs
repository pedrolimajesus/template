using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AppComponents.Topology
{


    public class PerformanceCounterNodeMetrics: IDisposable
    {
        private class CounterToMetric
        {
            public string Category { get; set; }
            public string MetricName { get; set; }
            public string CounterCategory { get; set; }
            public string CounterName { get; set; }
            public string CounterFormat { get; set; }
            public string InstanceName { get; set; }
        }

        private List<CounterToMetric> _counterToMetrics = new List<CounterToMetric>();
        private string _currentCategory = string.Empty;
        private Dictionary<string, PerformanceCounter> _performanceCounters = new Dictionary<string, PerformanceCounter>();

        public PerformanceCounterNodeMetrics AddCategory(string categoryName)
        {
            _currentCategory = categoryName;
            return this;
        }

        public PerformanceCounterNodeMetrics AddMetric(
            string metricName,
            string counterCategory,
            string counterName,
            string instanceName,
            string counterFormat = null)
        {
            _counterToMetrics.Add(new CounterToMetric
                {
                    Category = _currentCategory,
                    MetricName = metricName,
                    CounterCategory = counterCategory,
                    CounterName = counterName,
                    CounterFormat = counterFormat,
                    InstanceName = instanceName
                });
            return this;
        }

        public IEnumerable<NodeMetric> GetCurrentMetrics()
        {
            return from cm in _counterToMetrics
                   let format = cm.CounterFormat ?? "{0}"
                   select new NodeMetric
                       {
                           MetricName = cm.MetricName,
                           Category = cm.CounterCategory,
                           DisplayCategory = cm.Category,
                           Value = string.Format(format, 
                                GetCounter(cm.CounterCategory, cm.CounterName, cm.InstanceName)
                                .NextValue())
                       };
        }

        private PerformanceCounter GetCounter(string category, string name, string instanceName)
        {
            string key = category + "|" + name;
            if (!_performanceCounters.ContainsKey(key))
            {
                var pc = new PerformanceCounter(category, name );
                pc.InstanceName = instanceName;
                _performanceCounters[key] = pc;
            }

            return _performanceCounters[key];
        }


        private bool _isDisposed = false;
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                foreach (var pc in _performanceCounters.Values)
                {
                    pc.Dispose();
                }

                _performanceCounters.Clear();
            }

        }
    }


}
