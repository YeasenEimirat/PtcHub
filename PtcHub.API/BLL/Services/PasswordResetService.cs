using System.Security.Cryptography;
using PtcHub.API.Repositories;

namespace PtcHub.API.BLL.Services
{
    // ============================================================
    //  PasswordResetService — تصفير كلمة السر بطريقتين:
    //
    //  1) من لوحة التحكم: الأدمن بيولّد كلمة مؤقتة وبيعطيها للطالب.
    //     → ResetByAdmin()
    //
    //  2) نسيت كلمة السر: الطالب بيطلب OTP على إيميله وبيدخله.
    //     → RequestOtp() ثم VerifyOtpAndReset()
    //
    //  الطريقتين بيحدّثوا TokenVersion → كل التوكنات القديمة بتنرفض.
    // ============================================================
    public class PasswordResetService
    {
        private readonly ProfileRepository _repo;
        private readonly ScopeService _scope;
        private readonly EmailService _email;
        private readonly ILogger<PasswordResetService> _logger;

        // OTP مؤقت بالذاكرة — بسيط ومناسب لخادم واحد.
        // المفتاح = الإيميل بحروف صغيرة
        private static readonly Dictionary<string, OtpEntry> _otpStore = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object _lock = new();

        public PasswordResetService(ProfileRepository repo, ScopeService scope,
            EmailService email, ILogger<PasswordResetService> logger)
        {
            _repo = repo;
            _scope = scope;
            _email = email;
            _logger = logger;
        }

        // ============================================================
        //  1) تصفير من لوحة التحكم — بيرجّع كلمة مؤقتة للأدمن
        // ============================================================
        public string ResetByAdmin(Guid actingAdminId, Guid targetUserId)
        {
            var acting = _scope.RequireStaff(actingAdminId);

            // مسؤول السنة بيصفّر لطلاب نطاقه بس
            if (acting.IsScopedStaff)
            {
                var target = _scope.Resolve(targetUserId);

                if (target.Year != acting.ScopeYear)
                    throw new AppException(
                        $"صلاحيتك محدودة ب{Models.UserScope.YearName(acting.ScopeYear)}.", 403);
            }

            // كلمة مؤقتة: 8 أحرف وأرقام — سهلة النسخ عبر واتساب
            string tempPassword = GenerateReadablePassword();
            string hash = BCrypt.Net.BCrypt.HashPassword(tempPassword);

            if (!_repo.UpdatePasswordAndForceChange(targetUserId, hash))
                throw new AppException("المستخدم غير موجود.", 404);

            _logger.LogInformation("أدمن {Admin} صفّر كلمة سر المستخدم {Target}",
                actingAdminId, targetUserId);

            return tempPassword;
        }

        // ============================================================
        //  2a) طلب OTP — بيبعت رمز على الإيميل
        // ============================================================
        public async Task RequestOtp(string email)
        {
            email = email.Trim().ToLower();

            // ما بنكشف إذا الإيميل موجود أو لأ — نفس الرد دايماً
            Guid id = Guid.Empty;
            string fullName = "", studentId = "", passwordHash = "", role = "";
            byte year = 0;

            bool found = _repo.GetProfileByEmail(email,
                ref id, ref fullName, ref studentId, ref passwordHash, ref year, ref role);

            if (!found)
            {
                _logger.LogWarning("طلب OTP لإيميل غير موجود: {Email}", email);
                return;  // ما بنرمي خطأ — حتى ما نكشف الحسابات
            }

            // حدّ: OTP واحد كل ٩٠ ثانية لنفس الإيميل
            lock (_lock)
            {
                if (_otpStore.TryGetValue(email, out var existing) &&
                    existing.CreatedAt.AddSeconds(90) > DateTime.UtcNow)
                {
                    throw new AppException("انتظر قليلاً قبل ما تطلب رمز جديد.", 429);
                }
            }

            string otp = GenerateOtp();

            lock (_lock)
            {
                // تنضيف المنتهية كل طلب
                CleanExpired();

                _otpStore[email] = new OtpEntry
                {
                    HashedOtp = BCrypt.Net.BCrypt.HashPassword(otp),
                    UserId = id,
                    Attempts = 0,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15)
                };
            }

            await _email.SendPasswordResetOtpAsync(email, fullName, otp);

            _logger.LogInformation("OTP انبعت لـ {Email}", email);
        }

        // ============================================================
        //  2b) التحقّق من الـ OTP وتعيين كلمة سر جديدة
        // ============================================================
        public void VerifyOtpAndReset(string email, string otp, string newPassword)
        {
            email = email.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
                throw new AppException("كلمة السر يجب أن تكون ٨ أحرف على الأقل.");

            OtpEntry? entry;

            lock (_lock)
            {
                if (!_otpStore.TryGetValue(email, out entry))
                    throw new AppException("الرمز غير صالح أو منتهي.");
            }

            // منتهي؟
            if (DateTime.UtcNow > entry.ExpiresAt)
            {
                lock (_lock) { _otpStore.Remove(email); }
                throw new AppException("الرمز منتهي. اطلب رمز جديد.");
            }

            // حدّ محاولات: ٥ محاولات خاطئة → الرمز بينلغى
            if (entry.Attempts >= 5)
            {
                lock (_lock) { _otpStore.Remove(email); }
                throw new AppException("عدد المحاولات تجاوز الحدّ. اطلب رمز جديد.");
            }

            entry.Attempts++;

            if (!BCrypt.Net.BCrypt.Verify(otp, entry.HashedOtp))
                throw new AppException($"الرمز غير صحيح. بقي {5 - entry.Attempts} محاولات.");

            // نجح ✔ — نحدّث كلمة السر و نلغي كل التوكنات القديمة
            string hash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            if (!_repo.UpdatePassword(entry.UserId, hash))
                throw new AppException("تعذّر تحديث كلمة السر.", 500);

            lock (_lock) { _otpStore.Remove(email); }

            _logger.LogInformation("كلمة سر {Email} تغيّرت عبر OTP", email);
        }

        // ============================================================
        //  3) تغيير كلمة السر (المستخدم يعرف القديمة)
        // ============================================================
        public void ChangePassword(Guid userId, string currentPassword, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
                throw new AppException("كلمة السر الجديدة يجب أن تكون ٨ أحرف على الأقل.");

            // نقرأ الهاش الحالي
            string storedHash = _repo.GetPasswordHash(userId)
                ?? throw new AppException("المستخدم غير موجود.", 404);

            // نتحقّق من القديمة
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, storedHash))
                throw new AppException("كلمة السر الحالية غير صحيحة.", 401);

            string hash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            if (!_repo.UpdatePassword(userId, hash))
                throw new AppException("تعذّر تحديث كلمة السر.", 500);

            _logger.LogInformation("المستخدم {UserId} غيّر كلمة سره بنفسه", userId);
        }

        // ============================================================
        //  أدوات مساعدة
        // ============================================================

        // رمز OTP من ٦ أرقام — آمن تشفيرياً
        private static string GenerateOtp()
        {
            return RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        }

        // كلمة مؤقتة مقروءة: 8 أحرف وأرقام بلا أحرف ملتبسة
        private static string GenerateReadablePassword()
        {
            const string chars = "abcdefghjkmnpqrstuvwxyz23456789";
            return string.Create(8, chars, (span, pool) =>
            {
                for (int i = 0; i < span.Length; i++)
                    span[i] = pool[RandomNumberGenerator.GetInt32(pool.Length)];
            });
        }

        // تنضيف الـ OTPs المنتهية — بينادى مع كل طلب جديد
        private static void CleanExpired()
        {
            var expired = _otpStore
                .Where(kv => DateTime.UtcNow > kv.Value.ExpiresAt)
                .Select(kv => kv.Key)
                .ToList();

            foreach (var key in expired)
                _otpStore.Remove(key);
        }

        private class OtpEntry
        {
            public string HashedOtp { get; set; } = "";
            public Guid UserId { get; set; }
            public int Attempts { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime ExpiresAt { get; set; }
        }
    }
}
