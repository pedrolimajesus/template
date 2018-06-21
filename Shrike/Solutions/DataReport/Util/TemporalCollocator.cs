namespace Shrike.Data.Reports.Util
{
    using System;

    public class TemporalCollocator
    {
        private DateTime _now;
        private DateTime _thisMinute;
        private DateTime _thisHour;
        private DateTime _nextHour;
        private DateTime _tomorrow;
        private DateTime _atMidnight;
        private DateTime _nextWeek;
        private DateTime _nextMonth;
        private DateTime _firstOfMonth;
        private DateTime _lastDayQuarter;
        private DateTime _nextYear;
        private DateTime _nextQuarterHour;
        private DateTime _nextQuarterDay;
        private DateTime _next5Minutes;

        private DateTime _startOfHour;
        private DateTime _previousYear;
        private DateTime _startPreviousQuarter;
        private DateTime _yesterday;
        private DateTime _todayMidnight;
        private DateTime _startOfWeek;
        private DateTime _lastMonth;
        private DateTime _firstOfThisMonth;
        private DateTime _lastOfThisMonth;
        private DateTime _lastDayPrevQuarter;
        private DateTime _firstOfYear;
        private DateTime _startOfQuarterHour;
        private DateTime _startOfQuarterDay;
        private DateTime _startOf5Minutes;

        public DateTime Now
        {
            get { return this._now; }
            set { this._now = value; }
        }

        public DateTime Next5Minutes
        {
            get { return this._next5Minutes; }
            set { this._next5Minutes = value; }
        }

        public DateTime ThisMinute
        {
            get { return this._thisMinute; }
            set { this._thisMinute = value; }
        }

        public DateTime ThisHour
        {
            get { return this._thisHour; }
            set { this._thisHour = value; }
        }

        public DateTime NextHour
        {
            get { return this._nextHour; }
            set { this._nextHour = value; }
        }

        public DateTime Tomorrow
        {
            get { return this._tomorrow; }
            set { this._tomorrow = value; }
        }

        public DateTime AtMidnight
        {
            get { return this._atMidnight; }
            set { this._atMidnight = value; }
        }

        public DateTime NextWeek
        {
            get { return this._nextWeek; }
            set { this._nextWeek = value; }
        }

        public DateTime NextMonth
        {
            get { return this._nextMonth; }
            set { this._nextMonth = value; }
        }

        public DateTime FirstOfMonth
        {
            get { return this._firstOfMonth; }
            set { this._firstOfMonth = value; }
        }

        public DateTime LastDayQuarter
        {
            get { return this._lastDayQuarter; }
            set { this._lastDayQuarter = value; }
        }

        public DateTime NextYear
        {
            get { return this._nextYear; }
            set { this._nextYear = value; }
        }

        public DateTime NextQuarterHour
        {
            get { return this._nextQuarterHour; }
            set { this._nextQuarterHour = value; }
        }

        public DateTime NextQuarterDay
        {
            get { return this._nextQuarterDay; }
            set { this._nextQuarterDay = value; }
        }

        public DateTime StartOfHour
        {
            get { return this._startOfHour; }
            set { this._startOfHour = value; }
        }

        public DateTime PreviousYear
        {
            get { return this._previousYear; }
            set { this._previousYear = value; }
        }

        public DateTime StartPreviousQuarter
        {
            get { return this._startPreviousQuarter; }
            set { this._startPreviousQuarter = value; }
        }

        public DateTime Yesterday
        {
            get { return this._yesterday; }
            set { this._yesterday = value; }
        }

        public DateTime TodayMidnight
        {
            get { return this._todayMidnight; }
            set { this._todayMidnight = value; }
        }

        public DateTime StartOfWeek
        {
            get { return this._startOfWeek; }
            set { this._startOfWeek = value; }
        }

        public DateTime LastMonth
        {
            get { return this._lastMonth; }
            set { this._lastMonth = value; }
        }

        public DateTime FirstOfThisMonth
        {
            get { return this._firstOfThisMonth; }
            set { this._firstOfThisMonth = value; }
        }

        public DateTime LastOfThisMonth
        {
            get { return this._lastOfThisMonth; }
            set { _lastOfThisMonth = value; }
        }

        public DateTime LastDayPrevQuarter
        {
            get { return this._lastDayPrevQuarter; }
            set { this._lastDayPrevQuarter = value; }
        }

        public DateTime FirstOfYear
        {
            get { return this._firstOfYear; }
            set { this._firstOfYear = value; }
        }

        public DateTime StartOfQuarterHour
        {
            get { return this._startOfQuarterHour; }
            set { this._startOfQuarterHour = value; }
        }

        public DateTime StartOfQuarterDay
        {
            get { return this._startOfQuarterDay; }
            set { this._startOfQuarterDay = value; }
        }

        public DateTime StartOf5Minutes
        {
            get { return this._startOf5Minutes; }
            set { this._startOf5Minutes = value; }
        }

        public void CollocateTime(DateTime when)
        {
            this._now = when;
            this._thisMinute = new DateTime(this._now.Year, this._now.Month, this._now.Day, this._now.Hour, this._now.Minute, 0, 0);
            this._thisHour = new DateTime(this._now.Year, this._now.Month, this._now.Day, this._now.Hour, 0, 0, 0);
            this._nextHour = this._thisHour + TimeSpan.FromHours(1.0);
            this._startOfHour = this._thisHour;

            this._tomorrow = this._now + TimeSpan.FromDays(1.0);
            this._previousYear = this._now.AddYears(-1);
            this._startPreviousQuarter =
                GetQuarterStartingDate(GetQuarterStartingDate(this._now) - TimeSpan.FromDays(1.0));
            this._yesterday = this._now - TimeSpan.FromDays(1.0);
            this._atMidnight = new DateTime(this._tomorrow.Year, this._tomorrow.Month, this._tomorrow.Day, 0, 0, 0);
            this._todayMidnight = new DateTime(this._now.Year, this._now.Month, this._now.Day, 0, 0, 0);
            this._nextWeek = Next(this._atMidnight, DayOfWeek.Sunday);
            this._startOfWeek = Previous(this._atMidnight, DayOfWeek.Sunday);

            this._nextMonth = this._now.AddMonths(1);
            this._lastMonth = this._now.AddMonths(-1);
            this._firstOfMonth = new DateTime(this._nextMonth.Year, this._nextMonth.Month, 1, 0, 0, 0);
            this._firstOfThisMonth = new DateTime(this._now.Year, this._now.Month, 1, 0, 0, 0);
            this._lastOfThisMonth = this._firstOfMonth - TimeSpan.FromDays(1.0);

            this._lastDayQuarter = this.GetQuarterStartingDate(this._now).AddMonths(3).AddDays(-1);
            this._lastDayPrevQuarter = this.GetQuarterStartingDate(this._now).AddDays(-1);

            this._nextYear = new DateTime(this._now.Year + 1, 1, 1, 0, 0, 0);
            this._firstOfYear = new DateTime(this._now.Year, 1, 1, 1, 0, 0, 0);

            var fiveMinuteMark = (Math.Floor(this._thisMinute.Minute / 5.0) + 1.0) * 5.0;
            var fiveMinuteDifference = fiveMinuteMark - this._thisMinute.Minute;
            this._next5Minutes = this._thisMinute + TimeSpan.FromMinutes(fiveMinuteDifference);

            fiveMinuteMark = Math.Floor(this._thisMinute.Minute / 5.0) * 5.0;
            this._startOf5Minutes = new DateTime(this._now.Year, this._now.Month, this._now.Day, this._now.Hour, (int)fiveMinuteMark, 0, 0);

            var quarterHourMark = (Math.Floor(this._thisMinute.Minute / 15.0) + 1.0) * 15.0;
            var quarterHourDifference = quarterHourMark - this._thisMinute.Minute;
            this._nextQuarterHour = this._thisMinute + TimeSpan.FromMinutes(quarterHourDifference);

            quarterHourMark = (Math.Floor(this._thisMinute.Minute / 15.0)) * 15.0;
            this._startOfQuarterHour = new DateTime(this._now.Year, this._now.Month, this._now.Day, this._now.Hour, (int)quarterHourMark, 0, 0);

            var quarterDayMark = (Math.Floor(this._thisHour.Hour / 6.0) + 1.0) * 6.0;
            var quarterDayDifference = quarterDayMark - this._thisHour.Hour;
            this._nextQuarterDay = this._thisHour + TimeSpan.FromHours(quarterDayDifference);

            quarterDayMark = (Math.Floor(this._thisHour.Hour / 6.0)) * 6.0;
            this._startOfQuarterDay = new DateTime(this._now.Year, this._now.Month, this._now.Day, (int)quarterDayMark, 0, 0, 0);

        }

        int GetQuarterName(DateTime myDate)
        {
            return (int)Math.Ceiling(myDate.Month / 3.0);
        }

        DateTime GetQuarterStartingDate(DateTime myDate)
        {
            return new DateTime(myDate.Year, (3 * this.GetQuarterName(myDate)) - 2, 1);
        }

        public void CollocateUtcNow()
        {
            this.CollocateTime(DateTime.UtcNow);
        }

        private static DateTime Next(DateTime from, DayOfWeek dayOfWeek)
        {
            int start = (int)from.DayOfWeek;
            int target = (int)dayOfWeek;
            if (target <= start)
                target += 7;
            return from.AddDays(target - start);
        }

        private static DateTime Previous(DateTime from, DayOfWeek dayOfWeek)
        {
            int start = (int)from.DayOfWeek;
            int target = (int)dayOfWeek;

            if (target >= start)
                target -= 7;
            int dayDiff = target - start;

            return new DateTime(from.Year, from.Month, from.Day, 0, 0, 0) + TimeSpan.FromDays(dayDiff);
        }

    }
}
