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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Newtonsoft.Json;
using log4net;

namespace AppComponents
{
    public class Recurrence
    {
        private DebugOnlyLogger _dblog;
        private ILog _log;

        public Recurrence()
        {
            DaysOfMonth = null;
            DaysOfWeek = null;
            MonthsLater = 0;
            DaysLater = 0;
            HoursLater = 0;
            MinutesLater = 0;
            AtFixedTime = DateTime.MinValue;
            _log = ClassLogger.Create(GetType());
            _dblog = DebugOnlyLogger.Create(_log);
        }


        public int[] DaysOfMonth { get; set; }

        public int[] DaysOfWeek { get; set; }
        public int MonthsLater { get; set; }
        public int DaysLater { get; set; }
        public int HoursLater { get; set; }
        public int MinutesLater { get; set; }
        public DateTime AtFixedTime { get; set; }

        public static Recurrence OnDaysOfMonth(params int[] days)
        {
            Contract.Requires(days.All(d => d >= 1 && d <= 31));
            return new Recurrence {DaysOfMonth = days};
        }

        public static Recurrence OnDaysOfWeek(params DayOfWeek[] days)
        {
            return new Recurrence {DaysOfWeek = days.Distinct().Cast<int>().ToArray()};
        }

        public static Recurrence InMonths(int monthsFromNow)
        {
            return new Recurrence {MonthsLater = monthsFromNow};
        }

        public static Recurrence InDays(int daysFromNow)
        {
            return new Recurrence {DaysLater = daysFromNow};
        }

        public static Recurrence InHours(int hoursFromNow)
        {
            return new Recurrence {HoursLater = hoursFromNow};
        }

        public Recurrence SetToFixedTime(int hour, int minute)
        {
            AtFixedTime = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, hour, minute, 0);
            return this;
        }

        public DateTime GetNextRecurrence()
        {
            return GetNextRecurrence(DateTime.UtcNow);
        }


        public DateTime GetNextRecurrence(DateTime baseTime)
        {
            DateTime recurrence = baseTime;

            if (DaysOfMonth != null)
            {
                var findDay = (from d in DaysOfMonth where d > recurrence.Day select d).FirstOrDefault();
                if (findDay == 0)
                {
                    recurrence = recurrence.AddMonths(1);
                    findDay = (from d in DaysOfMonth where d > recurrence.Day select d).FirstOrDefault();
                }

                if (findDay == 0)
                {
                    var err = string.Format("Cannot find day of month recurrence for recurrence: {0}", ToString());
                    _log.Error(err);
                    throw new ArgumentOutOfRangeException(err);
                }

                if (findDay > DateTime.DaysInMonth(baseTime.Year, baseTime.Month))
                    findDay = DateTime.DaysInMonth(baseTime.Year, baseTime.Month);
                recurrence = new DateTime(recurrence.Year, recurrence.Month, findDay, recurrence.Hour, recurrence.Minute,
                                          recurrence.Second);
            }
            else if (DaysOfWeek != null)
            {
                if (!DaysOfWeek.All(d => d >= (int) DayOfWeek.Sunday && d <= (int) DayOfWeek.Saturday))
                {
                    var err = string.Format("Cannot find day of week recurrence for recurrence {0}", ToString());
                    _log.Error(err);
                    throw new ArgumentOutOfRangeException(err);
                }

                while (!DaysOfWeek.Contains((int) recurrence.DayOfWeek))
                    recurrence = recurrence.AddDays(1.0);
            }
            else
            {
                if (MonthsLater > 0) recurrence = recurrence.AddMonths(MonthsLater);
                if (DaysLater > 0) recurrence = recurrence.AddDays(DaysLater);
                if (HoursLater > 0) recurrence = recurrence.AddHours(HoursLater);
                if (MinutesLater > 0) recurrence = recurrence.AddMinutes(MinutesLater);
            }

            if (AtFixedTime != DateTime.MinValue)
            {
                recurrence = new DateTime(recurrence.Year, recurrence.Month, recurrence.Day, AtFixedTime.Hour,
                                          AtFixedTime.Minute, AtFixedTime.Second);
                if (recurrence < DateTime.UtcNow)
                    recurrence = recurrence.AddDays(1.0);
            }

            _dblog.InfoFormat("Recurrence {0} from base time {1} next {2}", ToString(), baseTime, recurrence);
            return recurrence;
        }
    }

    public class ScheduledItem
    {
        public const string MutexName = "scheduledjobs";

        public ScheduledItem(string uniqueName, string message, Type type, string route, DateTime time,
                             Recurrence r = null)
        {
            UniqueName = uniqueName;
            Message = message;
            Route = route;
            Type = type;

            Time = time;
            Recurrence = (r == null) ? string.Empty : JsonConvert.SerializeObject(r);
        }

        public ScheduledItem()
        {
        }


        public string Id { get; set; }
        public string Message { get; set; }
        public string Route { get; set; }
        public Type Type { get; set; }
        public DateTime Time { get; set; }
        public string Recurrence { get; set; }

        public string UniqueName { get; set; }
    }

    public interface IJobScheduler
    {
        void ScheduleJob<T>(T jobInfo, DateTime schedule, Recurrence r = null, string jobRoute = null) where T : class;

        void ScheduleJobOnlyOnce<T>(string uniqueName, T jobInfo, DateTime schedule, Recurrence r = null,
                                    string jobRoute = null) where T : class;

        IEnumerable<ScheduledItem> GetDue();

        IEnumerable<ScheduledItem> GetAll(); 

        void Reschedule(ScheduledItem item);

        void Cancel(ScheduledItem item);
    }

    
    
}