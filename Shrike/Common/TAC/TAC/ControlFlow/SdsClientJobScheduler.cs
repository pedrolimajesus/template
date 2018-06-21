using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace AppComponents.ControlFlow
{
    public class SdsClientJobScheduler: IJobScheduler
    {
        public SdsClientJobScheduler()
        {
        }

        public void ScheduleJob<T>(T jobInfo, DateTime schedule, Recurrence r = null, string jobRoute = null) where T : class
        {
            
        }

        public void ScheduleJobOnlyOnce<T>(string uniqueName, T jobInfo, DateTime schedule, Recurrence r = null, string jobRoute = null) where T : class
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ScheduledItem> GetDue()
        {
            throw new NotImplementedException();
        }

        public void Reschedule(ScheduledItem item)
        {
            throw new NotImplementedException();
        }


        public IEnumerable<ScheduledItem> GetAll()
        {
            throw new NotImplementedException();
        }

        public void Cancel(ScheduledItem item)
        {
            throw new NotImplementedException();
        }
    }
}
