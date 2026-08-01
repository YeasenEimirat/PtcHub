using System.Collections.Generic;

namespace PtcHub.API.BLL
{
    // ============================================================
    //  فهرس المساقات على السيرفر — كود المساق ← السنة/السنوات اللي بينتمي لها
    //  مولّد من wwwroot/courses-index.js. لازم يبقى متوافق معه.
    //  ليش على السيرفر كمان؟ لأن فلترة الواجهة لحالها مش حماية:
    //  أي حد بيقدر يبعت POST /api/files بكود مساق سنة تانية من الـ console.
    // ============================================================
    public static class CourseCatalog
    {
        private static readonly Dictionary<string, byte[]> _map =
            new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "ACD0 3150", new byte[] { 1 } },
            { "ACD0 3151", new byte[] { 1 } },
            { "ACD0 3157", new byte[] { 1 } },
            { "ACD0 3158", new byte[] { 1 } },
            { "ACD0 3159", new byte[] { 1 } },
            { "ACD0 3262", new byte[] { 1 } },
            { "ACD0 3266", new byte[] { 2 } },
            { "ACD0 3267", new byte[] { 2 } },
            { "ACD0 3370", new byte[] { 2 } },
            { "ACD0 4159", new byte[] { 1 } },
            { "ACD0 4264", new byte[] { 2 } },
            { "BUS0 3451", new byte[] { 2 } },
            { "CMP0 3315", new byte[] { 1 } },
            { "EEE0 1151", new byte[] { 1 } },
            { "EEE0 1554", new byte[] { 4 } },
            { "EEE0 3200", new byte[] { 3 } },
            { "EEE0 3352", new byte[] { 2 } },
            { "EEE0 3555", new byte[] { 4 } },
            { "EEE1 1151", new byte[] { 2 } },
            { "EEE1 1253", new byte[] { 2 } },
            { "EEE1 1255", new byte[] { 2 } },
            { "EEE1 1356", new byte[] { 2 } },
            { "EEE1 3258", new byte[] { 1 } },
            { "EEE1 3259", new byte[] { 1 } },
            { "EEE1 3356", new byte[] { 2 } },
            { "EEE3 3350", new byte[] { 2 } },
            { "EEE3 3498", new byte[] { 3 } },
            { "EEE4 3253", new byte[] { 2 } },
            { "EEE4 3254", new byte[] { 2 } },
            { "EEE4 3354", new byte[] { 2 } },
            { "EEE4 3356", new byte[] { 3 } },
            { "EEE4 3358", new byte[] { 3 } },
            { "EEE4 3360", new byte[] { 3 } },
            { "EEE4 3455", new byte[] { 3 } },
            { "EEE4 3461", new byte[] { 3 } },
            { "EEE4 3465", new byte[] { 3 } },
            { "EEE4 3469", new byte[] { 4 } },
            { "EEE4 3490", new byte[] { 4 } },
            { "EEE4 3491", new byte[] { 4 } },
            { "EEE4 3576", new byte[] { 4 } },
            { "EEE4 3584", new byte[] { 3 } },
            { "EEE4 3595", new byte[] { 3 } },
            { "EEE4 3596", new byte[] { 3 } },
            { "EEE4 4150", new byte[] { 1 } },
            { "EEE6 3585", new byte[] { 4 } },
            { "EEEX 35XX", new byte[] { 3, 4 } },
            { "MEE0 1151", new byte[] { 1 } },
        };

        // بترجّع السنوات اللي فيها هذا المساق (فاضية لو الكود غير معروف)
        public static byte[] GetYears(string? courseCode)
        {
            if (string.IsNullOrWhiteSpace(courseCode))
                return Array.Empty<byte>();

            return _map.TryGetValue(courseCode.Trim(), out byte[]? years)
                ? years
                : Array.Empty<byte>();
        }

        // هل هذا المساق ضمن نطاق سنة معيّنة؟
        public static bool BelongsToYear(string? courseCode, byte year)
        {
            return Array.IndexOf(GetYears(courseCode), year) >= 0;
        }

        // هل الكود موجود في الخطة أصلاً؟
        public static bool IsKnown(string? courseCode)
        {
            return GetYears(courseCode).Length > 0;
        }
    }
}
