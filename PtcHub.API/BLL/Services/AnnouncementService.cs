using System.Data;
using PtcHub.API.Models;
using PtcHub.API.Repositories;

namespace PtcHub.API.BLL.Services
{
    public class AnnouncementService
    {
        private readonly AnnouncementRepository _repo;
        private readonly ScopeService _scope;

        public AnnouncementService(AnnouncementRepository repo, ScopeService scope)
        {
            _repo = repo;
            _scope = scope;
        }

        // ============================================================
        //  جلب الإعلانات — السنة بتتحدّد من القاعدة، مش من الواجهة
        //  قبل هيك كانت الواجهة تبعت ?year=2 والسيرفر يثق فيها. أي طالب
        //  بيقدر يبعت ?year=3 من الـ console ويشوف إعلانات سنة تانية.
        //  هلأ منقرأ سنة الطالب من حسابه ومنتجاهل أي شي جاي من المتصفّح.
        // ============================================================
        public List<Announcement> GetAnnouncements(Guid? viewerId)
        {
            bool activeOnly = true;
            byte? forYear = null;
            bool generalOnly = false;

            if (viewerId == null)
            {
                // زائر بلا حساب → العامة فقط
                generalOnly = true;
            }
            else
            {
                UserScope viewer = _scope.Resolve(viewerId.Value);

                if (viewer.IsSuperAdmin)
                {
                    // أدمن عام → كل شي، حتى المخفي
                    activeOnly = false;
                }
                else if (viewer.IsScopedStaff)
                {
                    // أدمن/مشرف سنة → إعلانات سنته + العامة، ويشوف المخفي كمان
                    activeOnly = false;
                    forYear = viewer.ScopeYear;
                }
                else if (viewer.Year.HasValue)
                {
                    // طالب → النشِطة: إعلانات سنته + العامة
                    forYear = viewer.Year;
                }
                else
                {
                    // طالب ما حدّد سنته في حسابه → العامة فقط
                    generalOnly = true;
                }
            }

            DataTable dt = _repo.GetAnnouncements(activeOnly, forYear, generalOnly);

            List<Announcement> list = new List<Announcement>();

            foreach (DataRow row in dt.Rows)
            {
                list.Add(Map(row));
            }

            return list;
        }

        // ===== تحويل صف من القاعدة إلى كائن =====
        private static Announcement Map(DataRow row)
        {
            return new Announcement
            {
                Id = Convert.ToInt64(row["Id"]),
                Title = (string)row["Title"],
                Body = row["Body"] == DBNull.Value ? null : (string)row["Body"],
                Active = (bool)row["Active"],
                Year = row["Year"] == DBNull.Value ? (byte?)null : Convert.ToByte(row["Year"]),
                CreatedBy = row["CreatedBy"] == DBNull.Value ? null : (Guid)row["CreatedBy"],
                CreatedAt = (DateTime)row["CreatedAt"]
            };
        }

        // ===== إضافة إعلان — ترجّع الكائن الكامل بعد الحفظ =====
        public Announcement AddAnnouncement(string title, string? body, bool active, byte? year, Guid staffId)
        {
            UserScope acting = _scope.RequireStaff(staffId);

            if (string.IsNullOrWhiteSpace(title))
                throw new AppException("عنوان الإعلان مطلوب.");

            if (year.HasValue && (year < 1 || year > 4))
                throw new AppException("السنة يجب أن تكون بين 1 و 4.");

            if (acting.IsScopedStaff)
            {
                // مسؤول سنة معيّنة → كل إعلاناته لسنته.
                // ولا حتى إعلان "عام"، لأن العام بيوصل لكل السنوات وهذا خارج نطاقه.
                if (year.HasValue && year != acting.ScopeYear)
                {
                    throw new AppException(
                        $"صلاحيتك محدودة بـ{UserScope.YearName(acting.ScopeYear)}، فما بتقدر تنشر إعلاناً لسنة غيرها.",
                        403);
                }

                year = acting.ScopeYear;
            }

            long newId = _repo.AddAnnouncement(title.Trim(), body?.Trim(), active, year, staffId);

            if (newId == -1)
                throw new AppException("تعذّر حفظ الإعلان.", 500);

            DataRow? row = _repo.GetAnnouncementById(newId);

            if (row == null)
                throw new AppException("تعذّر قراءة الإعلان بعد حفظه.", 500);

            return Map(row);
        }

        // ===== حذف إعلان =====
        public void DeleteAnnouncement(long id, Guid staffId)
        {
            UserScope acting = _scope.RequireStaff(staffId);

            // منقرأ الإعلان قبل الحذف حتى نتأكّد إنه ضمن نطاق هذا المسؤول
            DataRow? row = _repo.GetAnnouncementById(id);

            if (row == null)
                throw new AppException("الإعلان غير موجود.", 404);

            byte? annYear = row["Year"] == DBNull.Value ? (byte?)null : Convert.ToByte(row["Year"]);

            ScopeService.EnsureYearAllowed(acting, annYear, "هذا الإعلان");

            bool deleted = _repo.DeleteAnnouncement(id);

            if (!deleted)
                throw new AppException("الإعلان غير موجود.", 404);
        }
    }
}
