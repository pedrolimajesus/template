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
using System.Threading;

namespace AppComponents.Extensions.Time
{
    public static class DateTimeExtensions
    {
        public static string GetFormatedTicks(this DateTime dateTime)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:D19}", dateTime.Ticks);
        }

        public static string GetFormattedInvertedTicks(this DateTime dateTime)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:D19}", dateTime.Ticks - DateTime.MaxValue.Ticks);
        }

        public static bool TimeIsUp(this DateTime startTime, TimeSpan max)
        {
            return (DateTime.UtcNow - startTime) > max;
        }


        public static void MaybeTimeOutOrCancel(DateTime startTime, TimeSpan max, CancellationToken ct)
        {
            if (TimeIsUp(startTime, max))
                throw new TimeoutException();
            ct.ThrowIfCancellationRequested();
        }
    }
}