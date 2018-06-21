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
using System.Globalization;
using System.Linq;

namespace AppComponents
{
    public class TemporalId
    {
        private string _id;

        public TemporalId()
        {
            Initialize(DateTime.UtcNow, Guid.NewGuid());
        }

        public TemporalId(DateTime time)
        {
            Initialize(time, Guid.NewGuid());
        }

        public TemporalId(DateTime time, Guid subId)
        {
            Initialize(time, subId);
        }

        public TemporalId(DateTime time, Guid subId, bool ascending)
        {
            Initialize(time, subId, ascending);
        }


        public TemporalId(string id)
        {
            _id = id;
        }

        public string Id
        {
            get { return _id; }
        }

        public DateTime Time
        {
            get { return ExtractTime(_id); }
        }

        private void Initialize(DateTime time, Guid subId, bool ascending = false)
        {
            long ticks = ascending ? time.Ticks : DateTime.MaxValue.Ticks - time.Ticks; // most recent first.
            _id = ticks.ToString("d19", CultureInfo.InvariantCulture) + "_" + subId.ToString();
        }


        public void WrapId(string id)
        {
            // provide this method instead of a property setter
            // so other classes, serialization, ORM, etc won't 
            // improperly set id arbitrarily.
            _id = id;
        }

        public override string ToString()
        {
            return Id;
        }

        public static DateTime ExtractTime(string id)
        {
            try
            {
                long ticks = long.Parse(id.Split('_').First().Trim(), CultureInfo.InvariantCulture);
                return new DateTime(ticks);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        public static Guid ExtractSubId(string id)
        {
            try
            {
                return new Guid(id.Split('_').Skip(1).Single().Trim());
            }
            catch (Exception)
            {
                return Guid.Empty;
            }
        }


        public static TimeSpan GetGap(string a, string b)
        {
            return (ExtractTime(a) - ExtractTime(b)).Duration();
        }


        public static Func<string, string, bool> FuncDetectRangeGapFor(TimeSpan ts)
        {
            return (a, b) => GetGap(a, b) > ts;
        }


        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof (TemporalId))
                return false;

            var comp = obj as TemporalId;

            return string.Compare(Id, comp.Id) == 0;
        }

        public bool Equals(TemporalId other)
        {
            if (other == null)
                return false;

            return string.Compare(Id, other.Id) == 0;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }


        public static bool operator ==(TemporalId a, TemporalId b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (null == a || null == b)
                return false;

            return string.Compare(a.Id, b.Id) == 0;
        }

        public static bool operator !=(TemporalId a, TemporalId b)
        {
            return !(a == b);
        }
    }
}