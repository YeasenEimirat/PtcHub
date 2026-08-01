using System.Data;
using PtcHub.API.Models;
using PtcHub.API.Repositories;

namespace PtcHub.API.BLL.Services
{
    public class CourseFileService
    {
        private readonly CourseFileRepository _repo;
        private readonly ProfileRepository _repo2;
        private readonly ScopeService _scope;

        public CourseFileService(CourseFileRepository repo, ProfileRepository profileRepo, ScopeService scope)
        {
            _repo = repo;
            _repo2 = profileRepo;
            _scope = scope;
        }

        // ===== Get files for a course (or all files if code is empty) =====
        // Reading is open to everyone (students and visitors)
        public List<CourseFile> GetFiles(string? courseCode)
        {
            DataTable dt = _repo.GetFiles(courseCode ?? "");

            List<CourseFile> list = new List<CourseFile>();

            foreach (DataRow row in dt.Rows)
            {
                list.Add(Map(row));
            }

            return list;
        }

        // ===== تحويل صف من القاعدة إلى كائن =====
        private static CourseFile Map(DataRow row)
        {
            return new CourseFile
            {
                Id = Convert.ToInt64(row["Id"]),
                CourseCode = (string)row["CourseCode"],
                Title = (string)row["Title"],
                Url = (string)row["Url"],
                Kind = (string)row["Kind"],
                SizeLabel = row["SizeLabel"] == DBNull.Value ? null : (string)row["SizeLabel"],
                SortOrder = Convert.ToInt32(row["SortOrder"]),
                CreatedBy = row["CreatedBy"] == DBNull.Value ? null : (Guid)row["CreatedBy"],
                Status = row.Table.Columns.Contains("Status") && row["Status"] != DBNull.Value
                    ? (string)row["Status"] : "approved",
                SubmitterName = row.Table.Columns.Contains("SubmitterName") && row["SubmitterName"] != DBNull.Value
                    ? (string)row["SubmitterName"] : null,
                ReviewedBy = row.Table.Columns.Contains("ReviewedBy") && row["ReviewedBy"] != DBNull.Value
                    ? (Guid)row["ReviewedBy"] : null,
                ReviewedAt = row.Table.Columns.Contains("ReviewedAt") && row["ReviewedAt"] != DBNull.Value
                    ? (DateTime)row["ReviewedAt"] : null,
                CreatedAt = (DateTime)row["CreatedAt"]
            };
        }

        // ===== إضافة ملف (الطاقم فقط) — ترجّع الكائن الكامل بعد الحفظ =====
        public CourseFile AddFile(string courseCode, string title, string url, string kind,
            string? sizeLabel, int sortOrder, Guid staffId)
        {
            // Business validation
            if (string.IsNullOrWhiteSpace(courseCode))
                throw new AppException("كود المساق مطلوب.");

            if (string.IsNullOrWhiteSpace(title))
                throw new AppException("عنوان الملف مطلوب.");

            if (string.IsNullOrWhiteSpace(url))
                throw new AppException("رابط الملف مطلوب.");

            // حماية: ما منقبل روابط بروتوكولها خطر (javascript: / data:)
            EnsureSafeUrl(url);

            // ===== نطاق المسؤولية =====
            // مسؤول سنة معيّنة بيضيف ملفات لمساقات سنته فقط.
            UserScope acting = _scope.RequireStaff(staffId);

            if (acting.IsScopedStaff)
            {
                byte scopeYear = acting.ScopeYear!.Value;

                if (!CourseCatalog.IsKnown(courseCode))
                    throw new AppException("كود المساق غير موجود في الخطة.", 400);

                if (!CourseCatalog.BelongsToYear(courseCode, scopeYear))
                {
                    throw new AppException(
                        $"صلاحيتك محدودة بـ{UserScope.YearName(scopeYear)}، وهذا المساق مش من مساقاتها.",
                        403);
                }
            }

            long newId = _repo.AddFile(
                courseCode.Trim(), title.Trim(), url.Trim(),
                kind, sizeLabel, sortOrder, staffId);

            if (newId == -1)
                throw new AppException("تعذّر حفظ الملف.", 500);

            // نرجّع الكائن الكامل حتى تقدر الواجهة تستعمله فوراً بدون إعادة تحميل
            DataRow? row = _repo.GetFileById(newId);

            if (row == null)
                throw new AppException("تعذّر قراءة الملف بعد حفظه.", 500);

            return Map(row);
        }

        // ===== فحص الرابط =====
        private static void EnsureSafeUrl(string url)
        {
            if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out Uri? uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new AppException("الرابط يجب أن يبدأ بـ http:// أو https://");
            }
        }

        // ===== إضافة ملف من طالب (بتنزل pending) =====
        public CourseFile SubmitByStudent(string courseCode, string title, string url,
            string kind, Guid studentId)
        {
            if (string.IsNullOrWhiteSpace(courseCode))
                throw new AppException("كود المساق مطلوب.");
            if (string.IsNullOrWhiteSpace(title))
                throw new AppException("عنوان الملف مطلوب.");
            if (string.IsNullOrWhiteSpace(url))
                throw new AppException("رابط الملف مطلوب.");

            EnsureSafeUrl(url);

            // الطالب بيضيف لمساقات سنته فقط
            UserScope student = _scope.Resolve(studentId);

            if (!student.Year.HasValue)
                throw new AppException("لازم تحدّد سنتك الدراسية بحسابك أول.");

            if (!CourseCatalog.IsKnown(courseCode))
                throw new AppException("كود المساق غير موجود في الخطة.", 400);

            if (!CourseCatalog.BelongsToYear(courseCode, student.Year.Value))
                throw new AppException("هذا المساق مش من مساقات سنتك.", 403);

            // نقرأ اسمه وإيميله
            string submitterName = "";
            {
                string fn = "", sid = "", em = "", rl = "";
                byte yr = 0, sc = 0;
                if (_repo2.GetProfileByID(studentId, ref fn, ref sid, ref em, ref yr, ref rl, ref sc))
                    submitterName = fn + " | " + em;
            }

            long newId = _repo.AddFile(
                courseCode.Trim(), title.Trim(), url.Trim(),
                kind, null, 0, studentId, "pending", submitterName);

            if (newId == -1)
                throw new AppException("تعذّر حفظ الملف.", 500);

            DataRow? row = _repo.GetFileById(newId);
            if (row == null)
                throw new AppException("تعذّر قراءة الملف بعد حفظه.", 500);

            return Map(row);
        }

        // ===== جلب الملفات المعلّقة (للمراجعة) =====
        public List<CourseFile> GetPendingFiles(Guid staffId)
        {
            UserScope acting = _scope.RequireStaff(staffId);

            DataTable dt = _repo.GetFilesByStatus("pending");

            List<CourseFile> list = new List<CourseFile>();

            foreach (DataRow row in dt.Rows)
            {
                var file = Map(row);

                // مسؤول السنة بيشوف ملفات مساقات سنته فقط
                if (acting.IsScopedStaff &&
                    !CourseCatalog.BelongsToYear(file.CourseCode, acting.ScopeYear!.Value))
                    continue;

                list.Add(file);
            }

            return list;
        }

        // ===== موافقة أو رفض ملف =====
        public void ReviewFile(long id, string decision, Guid staffId)
        {
            if (decision != "approved" && decision != "rejected")
                throw new AppException("القرار لازم يكون approved أو rejected.");

            UserScope acting = _scope.RequireStaff(staffId);

            DataRow? existing = _repo.GetFileById(id);
            if (existing == null)
                throw new AppException("الملف غير موجود.", 404);

            string code = (string)existing["CourseCode"];

            // مسؤول السنة بيراجع ملفات مساقات سنته فقط
            if (acting.IsScopedStaff && !CourseCatalog.BelongsToYear(code, acting.ScopeYear!.Value))
                throw new AppException(
                    $"صلاحيتك محدودة ب{UserScope.YearName(acting.ScopeYear)}.", 403);

            if (!_repo.UpdateFileStatus(id, decision, staffId))
                throw new AppException("تعذّر تحديث حالة الملف.", 500);
        }

        // ===== Delete a file (staff only) =====
        public void DeleteFile(long id, Guid staffId)
        {
            UserScope acting = _scope.RequireStaff(staffId);

            if (acting.IsScopedStaff)
            {
                // منقرأ الملف قبل الحذف حتى نتأكّد إنه لمساق ضمن نطاقه
                DataRow? existing = _repo.GetFileById(id);

                if (existing == null)
                    throw new AppException("الملف غير موجود.", 404);

                string code = (string)existing["CourseCode"];
                byte scopeYear = acting.ScopeYear!.Value;

                if (!CourseCatalog.BelongsToYear(code, scopeYear))
                {
                    throw new AppException(
                        $"صلاحيتك محدودة بـ{UserScope.YearName(scopeYear)}، وهذا الملف لمساق من سنة تانية.",
                        403);
                }
            }

            bool deleted = _repo.DeleteFile(id);

            if (!deleted)
                throw new AppException("الملف غير موجود.", 404);
        }
    }
}