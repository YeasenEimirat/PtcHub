using System.Data;
using System.Text.Json;
using PtcHub.API.Repositories;

namespace PtcHub.API.BLL.Services
{
    public class ProgressService
    {
        private readonly CourseProgressRepository _repo;

        public ProgressService(CourseProgressRepository repo)
        {
            _repo = repo;
        }

        // ===== Get progress for a single course =====
        // Returns a JSON object, or null if no progress exists
        public object? GetProgress(Guid userId, string courseCode)
        {
            if (string.IsNullOrWhiteSpace(courseCode))
                throw new AppException("كود المساق مطلوب.");

            string data = _repo.GetProgress(userId, courseCode.Trim());

            if (string.IsNullOrWhiteSpace(data))
                return null;

            // Convert the JSON string into an object so it returns correctly to the client
            return JsonSerializer.Deserialize<JsonElement>(data);
        }

        // ===== Get all progress for a student at once =====
        // Returns a dictionary: { "EEE4 3254": {...}, "EEE4 3360": {...} }
        public Dictionary<string, object?> GetAllProgress(Guid userId)
        {
            DataTable dt = _repo.GetAllProgressByUser(userId);

            Dictionary<string, object?> result = new Dictionary<string, object?>();

            foreach (DataRow row in dt.Rows)
            {
                string code = (string)row["CourseCode"];
                string data = (string)row["Data"];

                object? parsed = null;
                if (!string.IsNullOrWhiteSpace(data))
                {
                    parsed = JsonSerializer.Deserialize<JsonElement>(data);
                }

                result[code] = parsed;
            }

            return result;
        }

        // ===== Save progress =====
        public void SaveProgress(Guid userId, string courseCode, object data)
        {
            if (string.IsNullOrWhiteSpace(courseCode))
                throw new AppException("كود المساق مطلوب.");

            // Convert the object into a JSON string before storing
            string json = JsonSerializer.Serialize(data);

            // Protection: prevent a huge string from filling the database
            if (json.Length > 200000)
                throw new AppException("بيانات التقدّم كبيرة جداً.");

            bool saved = _repo.SaveProgress(userId, courseCode.Trim(), json);

            if (!saved)
                throw new AppException("تعذّر حفظ التقدّم.", 500);
        }
    }
}