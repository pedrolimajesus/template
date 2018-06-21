using Shrike.TimeZone.Contracts;
using Shrike.TimeZone.Logic;
using System;
using System.Linq;
using System.Web;
using Shrike.ExceptionHandling.Exceptions;

namespace Shrike.TimeZone.Service
{
    public class HttpTimeZoneService : ITimeZoneService
    {
        private const string TimeZoneKey = "Timezone";

        #region Implementation of ITimeZoneService

        private readonly TimeZoneConvertions timeZoneBusinessLogic = new TimeZoneConvertions();

        private string GetTimezoneCookie()
        {
            var current = HttpContext.Current;

            if (current == null)
            {
                return string.Empty;
                //throw new BusinessLogicException("This TimeZone service is an implementation for a web application only.");
            }

            var timezoneCookie = current.Request.Cookies[TimeZoneKey];
            if (timezoneCookie == null) return null;

            var timezone = timezoneCookie.Value.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).First();

            return timeZoneBusinessLogic.IanaToWindows(timezone);
        }

        private static void StoreTimezoneToSession(string timeZoneCookieValue)
        {
            var current = HttpContext.Current;

            if (current != null && current.Session != null)
            {
                current.Session[TimeZoneKey] = timeZoneCookieValue;
            }
        }

        private const string DefaultZone = "UTC";

        public string GetCurrentWindowsTimeZoneId()
        {
            var current = HttpContext.Current;
            var timeZoneCookieValue = current == null || current.Session == null
                                          ? string.Empty
                                          : current.Session[TimeZoneKey] as string;

            if (string.IsNullOrEmpty(timeZoneCookieValue))
            {
                timeZoneCookieValue = this.GetTimezoneCookie();

                if (string.IsNullOrEmpty(timeZoneCookieValue))
                {
                    return DefaultZone;
                }

                StoreTimezoneToSession(timeZoneCookieValue);
            }

            return string.IsNullOrEmpty(timeZoneCookieValue) ? DefaultZone : timeZoneCookieValue;
        }

        private TimeZoneInfo GetCurrentWindowsTimeZone()
        {
            return TimeZoneInfo.FindSystemTimeZoneById(GetCurrentWindowsTimeZoneId());
        }

        public void StoreTimezoneToSession()
        {
            var timeZoneCookieValue = GetTimezoneCookie();
            StoreTimezoneToSession(timeZoneCookieValue);
        }

        public string ConvertUtcToLocal(DateTime dateTime, string format)
        {
            var localTime = this.ConvertUtcToLocal(dateTime);
            return localTime.ToString(format);
        }

        public DateTime ConvertUtcToLocal(DateTime dateTime)
        {
            var timezone = this.GetCurrentWindowsTimeZone();
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime, timezone);
            return localTime;
        }

        public DateTime ConvertLocalToUtc(string dateTimeString)
        {
            DateTime dateTime;
            if (!DateTime.TryParse(dateTimeString, out dateTime))
            {
                throw new BusinessLogicException("dateTimeString should be a valid datetime");
            }

            var utcDateTime = ConvertLocalToUtc(dateTime);
            return utcDateTime;
        }

        public DateTime ConvertLocalToUtc(DateTime local)
        {
            var timezone = GetCurrentWindowsTimeZone();
            var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(local, timezone);
            return utcDateTime;
        }

        /// <summary>
        /// Used with datetimes from browser obtained with js function date.getTime(), usually used on urls.
        /// </summary>
        /// <param name="jsTicks">ticks obtained from a date.getTime() function in javascript</param>
        /// <returns>a UTC datetime</returns>
        public DateTime ConvertFromUtcJsTicksToUtcDateTime(long jsTicks)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(jsTicks);
        }

        /// <summary>
        /// ticks should be local
        /// </summary>
        /// <param name="ticksString"></param>
        /// <returns></returns>
        public DateTime ConvertFromTicksToUtc(string ticksString)
        {
            long ticks;
            if (!long.TryParse(ticksString, out ticks))
            {
                throw new BusinessLogicException("ticks should be an integer");
            }

            var dateTime = new DateTime(ticks);
            var timezone = GetCurrentWindowsTimeZone();
            var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(dateTime, timezone);
            return utcDateTime;
        }

        public long ConvertToUnixTicks(DateTime dateTime)
        {
            var unixInitEpoch = new DateTime(1970, 1, 1);
            var inUtc = dateTime.ToUniversalTime();
            var ts = new TimeSpan(inUtc.Ticks - unixInitEpoch.Ticks);
            return (long)ts.TotalMilliseconds;
        }

        #endregion
    }
}
