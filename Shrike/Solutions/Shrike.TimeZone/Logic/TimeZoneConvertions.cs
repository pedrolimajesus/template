using System;
using System.Linq;

namespace Shrike.TimeZone.Logic
{
    public class TimeZoneConvertions
    {
        // This will return the Windows zone that matches the IANA zone, if one exists.
        public string IanaToWindows(string ianaZoneId)
        {
            var tzdbSource = NodaTime.TimeZones.TzdbDateTimeZoneSource.Default;
            var mappings = tzdbSource.WindowsMapping.MapZones;

            var item = mappings.FirstOrDefault(x => x.TzdbIds.Contains(ianaZoneId, StringComparer.OrdinalIgnoreCase));
            if (item == null)
            {
                if (ianaZoneId == "Etc/UTC")
                {
                    return "UTC";
                }
            }
            return item == null ? null : item.WindowsId;
        }

        // This will return the "primary" IANA zone that matches the given windows zone.
        public string WindowsToIana(string windowsZoneId)
        {
            var tzdbSource = NodaTime.TimeZones.TzdbDateTimeZoneSource.Default;
            var tzi = TimeZoneInfo.FindSystemTimeZoneById(windowsZoneId);

            tzi = TimeZoneInfo.FindSystemTimeZoneById(tzi.StandardName);//in order to find a maptime zone id correctly, case should match

            return tzdbSource.MapTimeZoneId(tzi);
        }
    }
}