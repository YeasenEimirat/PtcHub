using System.Data;
using PtcHub.API.Repositories;

namespace PtcHub.API.BLL.Services
{
    public class MyCourseService
    {
        private readonly MyCourseRepository _repo;

        public MyCourseService(MyCourseRepository repo)
        {
            _repo = repo;
        }

        // ===== Get student's courses =====
        public List<string> GetMyCourses(Guid userId)
        {
            DataTable dt = _repo.GetCoursesByUser(userId);

            List<string> codes = new List<string>();

            foreach (DataRow row in dt.Rows)
            {
                codes.Add((string)row["CourseCode"]);
            }

            return codes;
        }

        // أقصى عدد مساقات للطالب الواحد — الخطة كلها ~51 مساق،
        // فـ 80 سقف مريح وبنفس الوقت بيمنع حدا يعبّي القاعدة بـ 10,000 صف
        private const int MaxCoursesPerUser = 80;

        // ===== إضافة مساق =====
        public List<string> AddCourse(Guid userId, string courseCode)
        {
            if (string.IsNullOrWhiteSpace(courseCode))
                throw new AppException("كود المساق مطلوب.");

            if (_repo.CountCourses(userId) >= MaxCoursesPerUser)
                throw new AppException($"وصلت الحد الأقصى ({MaxCoursesPerUser}) من المساقات. احذف مساقاً قبل ما تضيف غيره.", 409);

            _repo.AddCourse(userId, courseCode.Trim());

            // Return the updated list after adding
            return GetMyCourses(userId);
        }

        // ===== Remove a course =====
        public List<string> RemoveCourse(Guid userId, string courseCode)
        {
            if (string.IsNullOrWhiteSpace(courseCode))
                throw new AppException("كود المساق مطلوب.");

            _repo.RemoveCourse(userId, courseCode.Trim());

            // Return the updated list after removing
            return GetMyCourses(userId);
        }
    }
}