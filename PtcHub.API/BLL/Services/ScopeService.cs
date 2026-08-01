using PtcHub.API.Models;
using PtcHub.API.Repositories;

namespace PtcHub.API.BLL.Services
{
    // ============================================================
    //  ScopeService — المرجع الوحيد لسؤال: "هذا المستخدم مسؤول عن أي سنة؟"
    //  كل خدمة حسّاسة (الإعلانات / الملفات / الطلاب) بتناديه قبل ما تنفّذ.
    //  الفكرة: مركزية القرار في مكان واحد بدل تكرار الشروط في كل خدمة.
    // ============================================================
    public class ScopeService
    {
        private readonly ProfileRepository _repo;

        public ScopeService(ProfileRepository repo)
        {
            _repo = repo;
        }

        // ===== قراءة نطاق مستخدم من القاعدة =====
        public UserScope Resolve(Guid userId)
        {
            string role = "student";
            byte year = 0, scopeYear = 0;

            bool found = _repo.GetUserScope(userId, ref role, ref year, ref scopeYear);

            if (!found)
                throw new AppException("الجلسة غير صالحة، سجّل الدخول من جديد.", 401);

            return new UserScope
            {
                UserId = userId,
                Role = role,
                Year = year == 0 ? null : year,
                ScopeYear = scopeYear == 0 ? null : scopeYear
            };
        }

        // ===== لازم يكون طاقم (مشرف أو أدمن) =====
        public UserScope RequireStaff(Guid userId)
        {
            UserScope scope = Resolve(userId);

            if (!scope.IsStaff)
                throw new AppException("ما عندك صلاحية لهذا الإجراء.", 403);

            return scope;
        }

        // ===== لازم يكون أدمن عام (بلا نطاق سنة) =====
        // إدارة الصلاحيات محصورة بالأدمن العام: بدون هيك، أدمن سنة تانية
        // بيقدر يرفّع حاله لأدمن عام ويكسر كل التقسيم.
        public UserScope RequireSuperAdmin(Guid userId)
        {
            UserScope scope = Resolve(userId);

            if (!scope.IsSuperAdmin)
                throw new AppException("هذا الإجراء للأدمن العام فقط.", 403);

            return scope;
        }

        // ===== هل هذه السنة داخل نطاق هذا المستخدم؟ =====
        // الأدمن العام: كل السنوات. الطاقم المحدود: سنته فقط.
        public static void EnsureYearAllowed(UserScope scope, byte? year, string what)
        {
            if (!scope.IsScopedStaff)
                return;   // أدمن عام → ما في تقييد

            if (year != scope.ScopeYear)
            {
                throw new AppException(
                    $"صلاحيتك محدودة بـ{UserScope.YearName(scope.ScopeYear)}، فما بتقدر تتحكّم بـ{what}.",
                    403);
            }
        }
    }
}
