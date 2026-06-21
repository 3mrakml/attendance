using System;
using Microsoft.AspNetCore.Http;

namespace Attendence_System.Helpers
{
#pragma warning disable RS0030 // Do not use banned APIs
    public static class AppTime
    {
        private static IHttpContextAccessor? _httpContextAccessor;

        public static void Configure(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public static DateTime Now
        {
            get
            {
                // محاولة قراءة المنطقة الزمنية للمستخدم من الكوكيز
                var tzId = _httpContextAccessor?.HttpContext?.Request?.Cookies["UserTimeZone"];

                if (!string.IsNullOrWhiteSpace(tzId))
                {
                    try
                    {
                        var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
                        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
                    }
                    catch
                    {
                        // في حالة فشل التحويل، ننتقل للخيارات الافتراضية
                    }
                }

                // كخيار افتراضي نعتمد توقيت مصر
                try
                {
                    var tz = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
                    return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
                }
                catch (TimeZoneNotFoundException)
                {
                    try
                    {
                        var tz = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
                        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
                    }
                    catch
                    {
                        // الملاذ الأخير
                        return DateTime.Now;
                    }
                }
            }
        }

        public static DateTime Today => Now.Date;
    }
}
