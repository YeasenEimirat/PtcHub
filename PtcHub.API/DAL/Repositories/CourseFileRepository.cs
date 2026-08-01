using System.Data;
using Microsoft.Data.SqlClient;
using PtcHub.API.Helpers;

namespace PtcHub.API.Repositories
{
    public class CourseFileRepository
    {
        private readonly DbHelper _dbHelper;
        private readonly ILogger<CourseFileRepository> _logger;

        public CourseFileRepository(DbHelper dbHelper, ILogger<CourseFileRepository> logger)
        {
            _dbHelper = dbHelper;
            _logger = logger;
        }

        private const string Columns =
            "Id, CourseCode, Title, Url, Kind, SizeLabel, SortOrder, CreatedBy, [Status], SubmitterName, ReviewedBy, ReviewedAt, CreatedAt";

        // ===== جلب ملفات مساق معيّن (أو كل الملفات إذا الكود فاضي) =====
        public DataTable GetFiles(string CourseCode)
        {
            DataTable dt = new DataTable();

            bool all = string.IsNullOrWhiteSpace(CourseCode);

            string query = all
                ? $"SELECT {Columns} FROM CourseFiles WHERE [Status] = 'approved' ORDER BY CourseCode, SortOrder, Id"
                : $"SELECT {Columns} FROM CourseFiles WHERE CourseCode = @CourseCode AND [Status] = 'approved' ORDER BY SortOrder, Id";

            try
            {
                using var connection = _dbHelper.CreateConnection();
                using var command = new SqlCommand(query, connection);

                if (!all)
                    command.Parameters.Add("@CourseCode", SqlDbType.NVarChar, 20).Value = CourseCode;

                connection.Open();
                using var reader = command.ExecuteReader();

                if (reader.HasRows)
                    dt.Load(reader);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetFiles failed for {CourseCode}", CourseCode);
                throw;
            }

            return dt;
        }

        // ===== جلب ملف واحد بالـ Id =====
        public DataRow? GetFileById(long Id)
        {
            DataTable dt = new DataTable();

            string query = $"SELECT {Columns} FROM CourseFiles WHERE Id = @Id";

            try
            {
                using var connection = _dbHelper.CreateConnection();
                using var command = new SqlCommand(query, connection);

                command.Parameters.Add("@Id", SqlDbType.BigInt).Value = Id;

                connection.Open();
                using var reader = command.ExecuteReader();

                if (reader.HasRows)
                    dt.Load(reader);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetFileById failed for {Id}", Id);
                throw;
            }

            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        // ===== إضافة ملف جديد — ترجّع الـ Id الجديد =====
        public long AddFile(string CourseCode, string Title, string Url, string Kind,
            string? SizeLabel, int SortOrder, Guid? CreatedBy, string Status = "approved",
            string? SubmitterName = null)
        {
            long newId = -1;

            const string query = @"INSERT INTO CourseFiles (CourseCode, Title, Url, Kind, SizeLabel, SortOrder, CreatedBy, [Status], SubmitterName)
                                   VALUES (@CourseCode, @Title, @Url, @Kind, @SizeLabel, @SortOrder, @CreatedBy, @Status, @SubmitterName);
                                   SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

            try
            {
                using var connection = _dbHelper.CreateConnection();
                using var command = new SqlCommand(query, connection);

                command.Parameters.Add("@CourseCode", SqlDbType.NVarChar, 20).Value = CourseCode;
                command.Parameters.Add("@Title", SqlDbType.NVarChar, 200).Value = Title;
                command.Parameters.Add("@Url", SqlDbType.NVarChar, 1000).Value = Url;
                command.Parameters.Add("@Kind", SqlDbType.NVarChar, 20).Value = Kind;
                command.Parameters.Add("@SizeLabel", SqlDbType.NVarChar, 50).Value = (object?)SizeLabel ?? DBNull.Value;
                command.Parameters.Add("@SortOrder", SqlDbType.Int).Value = SortOrder;
                command.Parameters.Add("@CreatedBy", SqlDbType.UniqueIdentifier).Value = (object?)CreatedBy ?? DBNull.Value;
                command.Parameters.Add("@Status", SqlDbType.NVarChar, 10).Value = Status;
                command.Parameters.Add("@SubmitterName", SqlDbType.NVarChar, 150).Value = (object?)SubmitterName ?? DBNull.Value;

                connection.Open();
                object? result = command.ExecuteScalar();

                if (result != null && result != DBNull.Value)
                    newId = Convert.ToInt64(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddFile failed for {CourseCode} / {Title}", CourseCode, Title);
                throw;
            }

            return newId;
        }

        // ===== جلب ملفات بحالة معيّنة (لقسم المراجعة) =====
        public DataTable GetFilesByStatus(string Status, byte? forYear = null)
        {
            DataTable dt = new DataTable();

            string query = $"SELECT {Columns} FROM CourseFiles WHERE [Status] = @Status";

            // ما في عمود Year على CourseFiles — الفلترة بتصير بطبقة الأعمال
            query += " ORDER BY CreatedAt DESC";

            try
            {
                using var connection = _dbHelper.CreateConnection();
                using var command = new SqlCommand(query, connection);

                command.Parameters.Add("@Status", SqlDbType.NVarChar, 10).Value = Status;

                connection.Open();
                using var reader = command.ExecuteReader();

                if (reader.HasRows)
                    dt.Load(reader);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetFilesByStatus failed for {Status}", Status);
                throw;
            }

            return dt;
        }

        // ===== تحديث حالة ملف (موافقة / رفض) =====
        public bool UpdateFileStatus(long Id, string Status, Guid ReviewedBy)
        {
            const string query = @"UPDATE CourseFiles
                                      SET [Status]    = @Status,
                                          ReviewedBy  = @ReviewedBy,
                                          ReviewedAt  = SYSUTCDATETIME()
                                    WHERE Id = @Id";

            try
            {
                using var connection = _dbHelper.CreateConnection();
                using var command = new SqlCommand(query, connection);

                command.Parameters.Add("@Status", SqlDbType.NVarChar, 10).Value = Status;
                command.Parameters.Add("@ReviewedBy", SqlDbType.UniqueIdentifier).Value = ReviewedBy;
                command.Parameters.Add("@Id", SqlDbType.BigInt).Value = Id;

                connection.Open();
                return command.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateFileStatus failed for {Id}", Id);
                throw;
            }
        }

        // ===== حذف ملف =====
        public bool DeleteFile(long Id)
        {
            const string query = "DELETE FROM CourseFiles WHERE Id = @Id";

            try
            {
                using var connection = _dbHelper.CreateConnection();
                using var command = new SqlCommand(query, connection);

                command.Parameters.Add("@Id", SqlDbType.BigInt).Value = Id;

                connection.Open();
                return command.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteFile failed for {Id}", Id);
                throw;
            }
        }
    }
}
