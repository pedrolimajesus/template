using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AppComponents.Topology
{
    public interface IPerfCounterSpec
    {
        PerfCounterGatherer AddCategory(string categoryName);

        PerfCounterGatherer AddMetric(
            string metricName,
            string counterCategory,
            string counterName,
            string instanceName,
            string counterFormat = null);
    }

    public class PerfCounterGatherer: GathererBase, IDisposable, IPerfCounterSpec
    {
        private PerformanceCounterNodeMetrics _pcNodeMetrics = new PerformanceCounterNodeMetrics();

        public void RecordActivity(int activity)
        {
            _activityLevel = activity;
        }

        public PerfCounterGatherer(string company, string product)
            :base(company,product)
        {
            
        }


        public PerfCounterGatherer AddCategory(string categoryName)
        {
            _pcNodeMetrics.AddCategory(categoryName);
            return this;
        }

        public PerfCounterGatherer AddMetric(
            string metricName,
            string counterCategory,
            string counterName,
            string instanceName,
            string counterFormat = null)
        {
            _pcNodeMetrics.AddMetric(metricName, counterCategory, counterName, instanceName, counterFormat);
            return this;
        }

        

        protected override List<NodeMetric> DoGatherMetrics()
        {
            return _pcNodeMetrics.GetCurrentMetrics().ToList();
        }

        private bool _isDisposed = false;
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _pcNodeMetrics.Dispose();
            }
        }
    }
}
