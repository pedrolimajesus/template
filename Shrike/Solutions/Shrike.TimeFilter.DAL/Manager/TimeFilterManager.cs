using AppComponents;
using Lok.Unik.ModelCommon.Aware;
using Shrike.TimeZone.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shrike.TimeFilter.DAL.Manager
{
    public class TimeFilterManager
    {
        public delegate bool TimeFilterComparisonDelegate(DateTime dateTimeToCompare);
        private const int SixMonths = 6;
        private const int OneMonth = 1;

        //dateTimeToCompare should be in UTC
        public static TimeFilterComparisonDelegate GetTimeFilterDateComparison(TimeCategories time)
        {
            var utcNow = DateTime.UtcNow;

            switch (time)
            {
                case TimeCategories.Today:
                    var timeZoneService = Catalog.Factory.Resolve<ITimeZoneService>();
                    var localNow = timeZoneService.ConvertUtcToLocal(utcNow);
                    var initDay = localNow.Date;
                    var initLocalDayInUtc = timeZoneService.ConvertLocalToUtc(initDay);

                    return dateTimeToCompare => ((initLocalDayInUtc <= dateTimeToCompare) && (dateTimeToCompare <= utcNow));

                case TimeCategories.LastMonth:
                    return dateTimeToCompare => (utcNow.AddMonths(-OneMonth) <= dateTimeToCompare && dateTimeToCompare <= utcNow);

                //last 6 months
                case TimeCategories.LastSixMonths:
                    return
                        dateTimeToCompare =>
                            (utcNow.AddMonths(-SixMonths) <= dateTimeToCompare && dateTimeToCompare <= utcNow);
                //all
                case TimeCategories.All:
                    return dateTimeToCompare => true;
                default:
                    throw new ArgumentOutOfRangeException("time");
            }
        }
    }
}