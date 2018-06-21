using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shrike.TimeZone.Contracts
{
    public interface ITimeZoneService
    {
        string ConvertUtcToLocal(DateTime dateTime, string format);

        DateTime ConvertUtcToLocal(DateTime dateTime);

        DateTime ConvertLocalToUtc(string dateTimeString);

        DateTime ConvertLocalToUtc(DateTime local);

        /// <summary>
        /// Used with datetimes from browser obtained with js function date.getTime(), usually used on urls.
        /// </summary>
        /// <param name="jsTicks">ticks obtained from a date.getTime() function in javascript</param>
        /// <returns>a UTC datetime</returns>
        DateTime ConvertFromUtcJsTicksToUtcDateTime(long jsTicks);

        /// <summary>
        /// ticks should be local
        /// </summary>
        /// <param name="ticksString"></param>
        /// <returns></returns>
        DateTime ConvertFromTicksToUtc(string ticksString);

        long ConvertToUnixTicks(DateTime dateTime);

        string GetCurrentWindowsTimeZoneId();
    }
}
