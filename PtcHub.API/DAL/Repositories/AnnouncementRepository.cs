using System.Data;
using Microsoft.Data.SqlClient;
using PtcHub.API.Helpers;

namespace PtcHub.API.Repositories
{
    public class AnnouncementRepository
    {
        private readonly DbHelper _dbHelper;
        private readonly ILogger<AnnouncementRepository> _logger;

        public AnnouncementRepository(DbHelper dbHelper, ILogger<AnnouncementRepository> logger)
        {
            _dbHelper = dbHelper;
            _logger = logger;
        }

        // ===== جلب الإعلانات =====
        // activeOnly = true  → للطالب/الزائر (النشِطة فقط)
        // activeOnly = false → للطاقم (كل الإعلانات)
        // forYear   = null   → لا فلترة بالسنة
        // forYear   = 1..4   → إعلانات هذه السنة + الإعلانات العامة (Year IS NULL)
        // generalOnly = true → الإعلانات العامة فقط (للزائر اللي ما بنعرف سنته)
        public DataTable GetAnnouncements(bool activeOnly, byte? forYear = null, bool generalOnly = false)
        {
            DataTable dt = new DataTable();

            string query = @"SELECT Id, Title, Body, Active, [Year], CreatedBy, CreatedAt
                             FROM Announcements
                             WHERE 1 = 1";

            if (activeOnly)
                query += " AND Active = 1";

            if (generalOnly)
                query += " AND [Year] IS NULL";
            else if (forYear.HasValue)
                query += " AND ([Year] IS NULL OR [Year] = @Year)";

            query += " ORDER BY CreatedAt DESC";

            try
            {
                using var connection = _dbHelper.CreateConnection();
                using var command = new SqlCommand(query, connection);

                if (!generalOnly && forYear.HasValue)
                    command.Parameters.Add("@Year", SqlDbType.TinyInt).Value = forYear.Value;

                connection.Open();
                using var reader = command.ExecuteReader();

                if (reader.HasRows)
                    dt.Load(reader);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAnnouncements failed (activeOnly={ActiveOnly}, year={Year})",
                    activeOnly, forYear);
                throw;
            }

            return dt;
        }

        // ===== جلب إعلان واحد بالـ Id =====
        public DataRow? GetAnnouncementById(long Id)
        {
            DataTable dt = new DataTable();

            const string query = @"SELECT Id, Title, Body, Active, [Year], CreatedBy, CreatedAt
                                   FROM Announcements
                                   WHERE Id = @Id";

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
                _logger.LogError(ex, "GetAnnouncementById failed for {Id}", Id);
                throw;
            }

            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        // ===== إضافة إعلان جديد — ترجّع الـ Id الجديد =====
        public long AddAnnouncement(string Title, string? Body, bool Active, byte? Year, Guid? CreatedBy)
        {
            long newId = -1;

            const string query = @"INSERT INTO Announcements (Title, Body, Active, [Year], CreatedBy)
                                   VALUES (@Title, @Body, @Active, @Year, @CreatedBy);
                                   SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

            try
            {
                using var connection = _dbHelper.CreateConnection();
                using var command = new SqlCommand(query, connection);

                command.Parameters.Add("@Title", SqlDbType.NVarChar, 200).Value = Title;
                command.Parameters.Add("@Body", SqlDbType.NVarChar, -1).Value = (object?)Body ?? DBNull.Value;
                command.Parameters.Add("@Active", SqlDbType.Bit).Value = Active;
                command.Parameters.Add("@Year", SqlDbType.TinyInt).Value = (object?)Year ?? DBNull.Value;
                command.Parameters.Add("@CreatedBy", SqlDbType.UniqueIdentifier).Value = (object?)CreatedBy ?? DBNull.Value;

                connection.Open();
                object? result = command.ExecuteScalar();

                if (result != null && result != DBNull.Value)
                    newId = Convert.ToInt64(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddAnnouncement failed for title {Title}", Title);
                throw;
            }

            return newId;
        }

        // ===== حذف إعلان =====
        public bool DeleteAnnouncement(long Id)
        {
            const string query = "DELETE FROM Announcements WHERE Id = @Id";

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
                _logger.LogError(ex, "DeleteAnnouncement failed for {Id}", Id);
                throw;
            }
        }
    }
}
