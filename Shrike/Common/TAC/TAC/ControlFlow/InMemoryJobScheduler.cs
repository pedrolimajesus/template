using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace AppComponents.ControlFlow
{
    public class InMemoryJobScheduler: IJobScheduler
    {

        private ConcurrentList<ScheduledItem> _jobs = new ConcurrentList<ScheduledItem>();
        private const string _notUnique = "sys_not_unique";
        private JsonSerializerSettings _settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };


        public InMemoryJobScheduler()
        {
        }

        public void ScheduleJob<T>(T jobInfo, DateTime schedule, Recurrence r = null, string jobRoute = null) where T : class
        {
            var si = new ScheduledItem(_notUnique, JsonConvert.SerializeObject(jobInfo, _settings), typeof (T),
                                       jobRoute, schedule, r);
            _jobs.Add(si);
        }

        public void ScheduleJobOnlyOnce<T>(string uniqueName, T jobInfo, DateTime schedule, Recurrence r = null, string jobRoute = null) where T : class
        {
            var exists = true;

            exists = (from si in _jobs where si.UniqueName == uniqueName select si).Any();
            
            

            if (!exists)
            {
                
                var si = new ScheduledItem(uniqueName, JsonConvert.SerializeObject(jobInfo, _settings),
                                  typeof (T), jobRoute, schedule, r);
                _jobs.Add(si);

                

            }
        }

        public IEnumerable<ScheduledItem> GetDue()
        {
            //return _jobs;
            return from si in _jobs where si.Time < DateTime.UtcNow select si;
        }

        public void Reschedule(ScheduledItem item)
        {
            if (!string.IsNullOrWhiteSpace(item.Recurrence))
            {
                Recurrence r = JsonConvert.DeserializeObject<Recurrence>(item.Recurrence);
                DateTime nextOccurence = r.GetNextRecurrence();

                var si = new ScheduledItem(item.UniqueName, item.Message, item.Type, item.Route,
                                           nextOccurence, r);
                _jobs.Add(si);
                
            }

            _jobs.Remove(item);
        }



        public IEnumerable<ScheduledItem> GetAll()
        {
            return _jobs;
        }

        public void Cancel(ScheduledItem item)
        {
            _jobs.Remove(item);
        }
    }
}
