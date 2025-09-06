using System;
using System.Runtime.InteropServices;

namespace BarcopoloWebApi.Helper
{
    public static class TehranDateTime
    {
        private static readonly TimeZoneInfo TehranTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Iran Standard Time" : "Asia/Tehran");

        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TehranTimeZone);

        public static DateTime Convert(DateTime dateTime) =>
            dateTime.Kind == DateTimeKind.Utc
                ? TimeZoneInfo.ConvertTimeFromUtc(dateTime, TehranTimeZone)
                : TimeZoneInfo.ConvertTime(dateTime, TehranTimeZone);
    }
}