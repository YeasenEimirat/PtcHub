using System.Data;
using Microsoft.Data.SqlClient;
using PtcHub.API.Helpers;

namespace PtcHub.API.Repositories
{
    public class MyCourseRepository
    {
        private readonly DbHelper _dbHelper;
        private readonly ILogger<MyCourseRepository> _logger;

        public MyCourseRepository(DbHelper dbHelper, ILogger<MyCourseRepository> logger)
        {
            _dbHelper = dbHelper;
            _logger = logger;
        }

        // ===== جلب كل مساقات طالب معيّن =====
        public DataTable GetCoursesByUser(Guid UserId)
        {
            DataTable dt = new DataTable();

            const string query = @"SELECT CourseCode FROM MyCourses
                                   WHERE UserId = @UserId
                                   ORDER BY CreatedAt";

            try
            {
                using var connection = _dbHelper.CreateConnection();
                using var command = new SqlCommand(query, connection);

                command.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier).Value = UserId;

                connection.Open();
                using var reader = command.ExecuteReader();

                if (reader.HasRows)
                    dt.Load(reader);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetCoursesByUser failed for {UserId}", UserId);
                throw;
            }

            return dt;
        }

        // ===== عدد مساقات الطالب — لفرض سقف معقول =====
        public int CountCourses(Guid UserId)
        {
            const string query = "SELECT COUNT(*) FROM MyCourses WHERE UserId = @UserId";

            try
            {
                using var connection = _dbHelper.CreateConnection();
                using var command = new SqlCommand(query, connection);

                command.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier).Value = UserId;

                connection.Open();
                object? result = command.ExecuteScalar();

                return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CountCourses failed for {UserId}", UserId);
                throw;
            }
        }

        // ===== إضافة مساق للطالب (لا يضيف مكرراً) =====
        public bool AddCourse(Guid UserId, string CourseCode)
        {
            const string query = @"IF NOT EXISTS (SELECT 1 FROM MyCourses
                                                  WHERE UserId = @UserId AND CourseCode = @CourseCode)
                                       INSERT INTO MyCourses (UserId, CourseCode)
                                       VALUES (@UserId, @CourseCode);";

            try
            {
                using var connection = _dbHelper.CreateConnection();
                using var command = new SqlCommand(query, connection);

                command.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier).Value = UserId;
                command.Parameters.Add("@CourseCode", SqlDbType.NVarChar, 20).Value = CourseCode;

                connection.Open();
                return command.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddCourse failed for {UserId} / {CourseCode}", UserId, CourseCode);
                throw;
            }
        }

        // ===== حذف مساق من مساقات الطالب =====
        public bool RemoveCourse(Guid UserId, string CourseCode)
        {
            const string query = @"DELETE FROM MyCourses
                                   WHERE UserId = @UserId AND CourseCode = @CourseCode";

            try
            {
                using var connection = _dbHelper.CreateConnection();
                using var command = new SqlCommand(query, connection);

                command.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier).Value = UserId;
                command.Parameters.Add("@CourseCode", SqlDbType.NVarChar, 20).Value = CourseCode;

                connection.Open();
                return command.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RemoveCourse failed for {UserId} / {CourseCode}", UserId, CourseCode);
                throw;
            }
        }
    }
}
