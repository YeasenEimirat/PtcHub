using System.Data;
using PtcHub.API.Models;
using PtcHub.API.Repositories;

namespace PtcHub.API.BLL.Services
{
    public class AdminService
    {
        private readonly ProfileRepository _repo;
        private readonly ScopeService _scope;

        public AdminService(ProfileRepository repo, ScopeService scope)
        {
            _repo = repo;
            _scope = scope;
        }

        // ===== قائمة الطلاب =====
        // الأدمن العام يشوف الكل، وأدمن/مشرف السنة يشوف طلاب سنته فقط.
        // الفلترة على القاعدة (WHERE) مش على الواجهة — حتى ما تنكشف البيانات في الـ JSON.
        public List<Profile> GetStudents(Guid actingUserId)
        {
            UserScope acting = _scope.RequireStaff(actingUserId);

            DataTable dt = _repo.GetAllProfiles(onlyYear: acting.ScopeYear);

            List<Profile> list = new List<Profile>();

            foreach (DataRow row in dt.Rows)
            {
                list.Add(new Profile
                {
                    Id = (Guid)row["Id"],
                    FullName = (string)row["FullName"],
                    StudentId = row["StudentId"] == DBNull.Value ? null : (string)row["StudentId"],
                    Email = (string)row["Email"],
                    Year = row["Year"] == DBNull.Value ? (byte?)null : Convert.ToByte(row["Year"]),
                    Role = (string)row["Role"],
                    ScopeYear = row["ScopeYear"] == DBNull.Value ? (byte?)null : Convert.ToByte(row["ScopeYear"]),
                    CreatedAt = (DateTime)row["CreatedAt"]
                });
            }

            return list;
        }

        // ===== تعديل سنة طالب =====
        // للأدمن العام فقط، وعن قصد:
        // نقل طالب بين السنوات معناه إخراجه من نطاق مسؤول ودخوله بنطاق تاني.
        // لو سمحنا لمسؤول سنة ٢ يعمله، بيقدر يسحب طلاب من سنة ٣ لنطاقه.
        public void ChangeStudentYear(Guid actingAdminId, Guid targetUserId, byte? newYear)
        {
            _scope.RequireSuperAdmin(actingAdminId);

            if (newYear.HasValue && (newYear < 1 || newYear > 4))
                throw new AppException("السنة يجب أن تكون بين 1 و 4.");

            string? currentRole = _repo.GetRole(targetUserId);

            if (currentRole == null)
                throw new AppException("المستخدم غير موجود.", 404);

            bool updated = _repo.UpdateStudentYear(targetUserId, newYear);

            if (!updated)
                throw new AppException("تعذّر تحديث السنة.", 500);
        }

        // ===== نقل مجموعة طلاب لسنة =====
        // للأدمن العام فقط. بترجّع عدد اللي انتقلوا فعلاً.
        public int BulkChangeYear(Guid actingAdminId, List<Guid> userIds, byte? newYear)
        {
            _scope.RequireSuperAdmin(actingAdminId);

            if (userIds == null || userIds.Count == 0)
                throw new AppException("ما حدّدت ولا طالب.");

            if (userIds.Count > 500)
                throw new AppException("ما بتقدر تنقل أكتر من 500 طالب بالمرة.");

            if (newYear.HasValue && (newYear < 1 || newYear > UserScope.GraduatedYear))
                throw new AppException("سنة غير صالحة.");

            // ما منسمح للأدمن ينقل حاله ضمن المجموعة — لو نقل حاله لخريج
            // بيطلع من كل السنوات وبيحتار ليش اختفت بياناته
            if (userIds.Contains(actingAdminId))
                throw new AppException("ما بتقدر تنقل حسابك ضمن المجموعة.", 403);

            // منشيل المكرّر — لو الواجهة بعتت نفس الـ Id مرتين ما منكبّر العبارة بلا داعي
            List<Guid> unique = userIds.Distinct().ToList();

            return _repo.BulkUpdateYear(unique, newYear);
        }

        // ===== تغيير صلاحية مستخدم + نطاق سنته =====
        public void ChangeRole(Guid actingAdminId, Guid targetUserId, string newRole, byte? newScopeYear)
        {
            // إدارة الصلاحيات للأدمن العام فقط
            _scope.RequireSuperAdmin(actingAdminId);

            if (newRole != "student" && newRole != "supervisor" && newRole != "admin")
                throw new AppException("رتبة غير صالحة.");

            // ===== قواعد النطاق =====
            // طالب: ما إله نطاق إشراف أبداً
            if (newRole == "student")
                newScopeYear = null;

            // مشرف: لازم يكون مربوطاً بسنة — مشرف بلا سنة معناه مشرف على الكل، وهذا مش المقصود
            if (newRole == "supervisor" && !newScopeYear.HasValue)
                throw new AppException("لازم تختار السنة اللي بيشرف عليها.");

            if (newScopeYear.HasValue && (newScopeYear < 1 || newScopeYear > 4))
                throw new AppException("السنة يجب أن تكون بين 1 و 4.");

            // الأدمن ما بيغيّر حاله — حتى ما يقفل على نفسه الباب بالغلط
            if (actingAdminId == targetUserId)
                throw new AppException("ما بتقدر تغيّر صلاحيتك بنفسك.", 403);

            string? currentRole = _repo.GetRole(targetUserId);

            if (currentRole == null)
                throw new AppException("المستخدم غير موجود.", 404);

            // حماية: ممنوع تنزيل آخر أدمن عام أو حصره بسنة —
            // بيتحوّل المشروع لقفل دائم بلا مفتاح
            bool losesFullAdmin = currentRole == "admin" && (newRole != "admin" || newScopeYear.HasValue);

            if (losesFullAdmin && _repo.CountSuperAdmins() <= 1)
                throw new AppException("لا يمكن إزالة آخر أدمن عام في النظام.", 409);

            bool updated = _repo.UpdateRole(targetUserId, newRole, newScopeYear);

            if (!updated)
                throw new AppException("المستخدم غير موجود.", 404);
        }
    }
}
