// // 
// //  Copyright 2012 David Gressett
// // 
// //    Licensed under the Apache License, Version 2.0 (the "License");
// //    you may not use this file except in compliance with the License.
// //    You may obtain a copy of the License at
// // 
// //        http://www.apache.org/licenses/LICENSE-2.0
// // 
// //    Unless required by applicable law or agreed to in writing, software
// //    distributed under the License is distributed on an "AS IS" BASIS,
// //    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// //    See the License for the specific language governing permissions and
// //    limitations under the License.

using System;
using System.Diagnostics;
using System.Linq;
using AppComponents.Extensions.EnumerableEx;
using AppComponents.Extensions.ExceptionEx;
using AppComponents.Raven;

using Raven.Client.Indexes;
using Raven.Client.Linq;


namespace AppComponents
{
    using global::Raven.Imports.Newtonsoft.Json;

    public class JobDocumentsScheduler : IJobScheduler
    {
        private const string _notUnique = "sys_not_unique";
        private JsonSerializerSettings _settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

        #region IJobScheduler Members

        public void ScheduleJob<T>(T jobInfo, DateTime schedule, Recurrence r = null, string jobRoute = "")
            where T : class
        {
            Debug.Assert(null != jobInfo);

            var log = ClassLogger.Create(typeof (JobDocumentsScheduler));
            log.InfoFormat("Scheduling job: {0} at time {1}, recurring: {2}, route = {3}", jobInfo, schedule,
                           r == null ? string.Empty : r.ToString(), jobRoute);

            
            log.InfoFormat("Scheduling job: {0} at time {1}, recurring: {2}, route = {3}", jobInfo, schedule,
                            r == null ? string.Empty : r.ToString(), jobRoute);

            using (var ds = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                ds.Store(new ScheduledItem(_notUnique, JsonConvert.SerializeObject(jobInfo, _settings), typeof (T),
                                            jobRoute, schedule, r));
                ds.SaveChanges();
            }
                
        }

        public void ScheduleJobOnlyOnce<T>(string uniqueName, T jobInfo, DateTime schedule, Recurrence r = null,
                                           string jobRoute = null) where T : class
        {
            Debug.Assert(null != jobInfo);

            bool exists;
            using (var ds = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                exists = ds.Query<ScheduledItem>().Any(i => i.UniqueName == uniqueName);
            }

            if (!exists)
            {
                var log = ClassLogger.Create(typeof (JobDocumentsScheduler));

                
                log.InfoFormat("Scheduling job: {0} at time {1}, recurring: {2}, route = {3}", jobInfo, schedule,
                                r == null ? string.Empty : r.ToString(), jobRoute);

                using (var ds = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
                {
                    ds.Store(new ScheduledItem(uniqueName, JsonConvert.SerializeObject(jobInfo, _settings),
                                                typeof (T), jobRoute, schedule, r));
                    ds.SaveChanges();
                }
                    
            }
        }

        #endregion

        


        public System.Collections.Generic.IEnumerable<ScheduledItem> GetDue()
        {
            using (var ds = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {
                

                var due =
                    (from si in ds.Query<ScheduledItem>() where si.Time < DateTime.UtcNow select si).
                        GetAllUnSafe().EmptyIfNull();

                return due;
            }
        }


        public void Reschedule(ScheduledItem item)
        {
            try
            {

            
                using (var ds = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
                {
                    var loadedItem = ds.Load<ScheduledItem>(DataDocument.GetDocumentId(item));

                    if (!string.IsNullOrWhiteSpace(loadedItem.Recurrence))
                    {
                        Recurrence r = JsonConvert.DeserializeObject<Recurrence>(loadedItem.Recurrence);
                        DateTime nextOccurence = r.GetNextRecurrence();

                    
                        ds.Store(
                            new ScheduledItem(loadedItem.UniqueName, loadedItem.Message, loadedItem.Type, loadedItem.Route,
                                              nextOccurence, r));
                    }
                
                    ds.Delete(loadedItem);

                    ds.SaveChanges();
                
            }

            }
            catch (Exception ex)
            {
                var aa = Catalog.Factory.Resolve<IApplicationAlert>();
                aa.RaiseAlert(ApplicationAlertKind.System, ex.TraceInformation());

            }
        }


        public System.Collections.Generic.IEnumerable<ScheduledItem> GetAll()
        {
            using (var ds = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
            {


                var due =
                    (from si in ds.Query<ScheduledItem>() select si).
                        GetAllUnSafe().EmptyIfNull();

                return due;
            }
        }

        public void Cancel(ScheduledItem item)
        {
            try
            {


                using (var ds = DocumentStoreLocator.ResolveOrRoot(CommonConfiguration.CoreDatabaseRoute))
                {
                    var loadedItem = ds.Load<ScheduledItem>(DataDocument.GetDocumentId(item));


                    ds.Delete(loadedItem);

                    ds.SaveChanges();

                }

            }
            catch (Exception ex)
            {
                var aa = Catalog.Factory.Resolve<IApplicationAlert>();
                aa.RaiseAlert(ApplicationAlertKind.System, ex.TraceInformation());

            }
        }
    }

    


    

    public class ScheduledItem_ByUniqueName : AbstractIndexCreationTask<ScheduledItem>
    {
        public ScheduledItem_ByUniqueName()
        {
            Map = items => from item in items select new {item.UniqueName};
        }
    }

    public class ScheduledItem_ByTime : AbstractIndexCreationTask<ScheduledItem>
    {
        public ScheduledItem_ByTime()
        {
            Map = items => from item in items select new {item.Time};
        }
    }
}