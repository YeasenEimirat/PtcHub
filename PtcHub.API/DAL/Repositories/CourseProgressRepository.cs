using System.Data;
using Microsoft.Data.SqlClient;
using PtcHub.API.Helpers;

namespace PtcHub.API.Repositories
{
    public class CourseProgressRepository
    {
        private readonly DbHelper _dbHelper;
        private readonly ILogger<CourseProgressRepository> _logger;

        public CourseProgressRepository(DbHelper dbHelper, ILogger<CourseProgressRepository> logger)
        {
            _dbHelper = dbHelper;
            _logger = logger;
        }

        // ===== جلب تقدّم الطالب في مساق معيّن — ترجّع نص JSON أو "" =====
        public string GetProgress(Guid UserId, string CourseCode)
        {
            string data = "";

            const string query = @"SELECT Data FROM CourseProgress
                                   WHERE UserId = @UserId AND CourseCode = @CourseCode";

            try
            {
                using var connection = _dbHelper.CreateConnection();
                using var command = new SqlCommand(query, connection);

                command.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier).Value = UserId;
                command.Parameters.Add("@CourseCode", SqlDbType.NVarChar, 20).Value = CourseCode;

                connection.Open();
                using var reader = command.ExecuteReader();

                if (reader.Read() && reader["Data"] != DBNull.Value)
                    data = (string)reader["Data"];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetProgress failed for {UserId} / {CourseCode}", UserId, CourseCode);
                throw;
            }

            return data;
        }

        // ===== جلب كل تقدّم الطالب دفعة واحدة =====
        public DataTable GetAllProgressByUser(Guid UserId)
        {
            DataTable dt = new DataTable();

            const string query = @"SELECT CourseCode, Data FROM CourseProgress
                                   WHERE UserId = @UserId";

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
                _logger.LogError(ex, "GetAllProgressByUser failed for {UserId}", UserId);
                throw;
            }

            return dt;
        }

        // ===== حفظ التقدّم — UPDATE ثم INSERT إذا ما في سجل =====
        public bool SaveProgress(Guid UserId, string CourseCode, string Data)
        {
            const string query = @"UPDATE CourseProgress
                                      SET Data = @Data, UpdatedAt = SYSUTCDATETIME()
                                    WHERE UserId = @UserId AND CourseCode = @CourseCode;

                                   IF @@ROWCOUNT = 0
                                      INSERT INTO CourseProgress (UserId, CourseCode, Data)
                                      VALUES (@UserId, @CourseCode, @Data);";

            try
            {
                using var connection = _dbHelper.CreateConnection();
                using var command = new SqlCommand(query, connection);

                command.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier).Value = UserId;
                command.Parameters.Add("@CourseCode", SqlDbType.NVarChar, 20).Value = CourseCode;
                command.Parameters.Add("@Data", SqlDbType.NVarChar, -1).Value = Data;

                connection.Open();
                return command.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SaveProgress failed for {UserId} / {CourseCode}", UserId, CourseCode);
                throw;
            }
        }
    }
}
