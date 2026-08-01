using System.Data;
using Microsoft.Data.SqlClient;
using PtcHub.API.BLL.Services;
using PtcHub.API.Helpers;

namespace PtcHub.API.Repositories
{
    public class ProfileRepository
    {
        private readonly DbHelper _dbHelper;
        private readonly ILogger<ProfileRepository> _logger;

        public ProfileRepository(DbHelper dbHelper, ILogger<ProfileRepository> logger)
        {
            _dbHelper = dbHelper;
            _logger = logger;
        }

        // ===== جلب مستخدم بالبريد الإلكتروني (للدخول) =====
        public bool GetProfileByEmail(string Email, ref Guid Id, ref string FullName,
            ref string StudentId, ref string PasswordHash, ref byte Year, ref string Role)
        {
            bool isFound = false;

            // أعمدة صريحة بدل SELECT * — لو أضفنا عموداً بكرة ما ينكسر شي
            const string query = @"SELECT Id, FullName, StudentId, PasswordHash, [Year], Role
                                   FROM Profiles
                                   WHERE Email = @Email";

            try
            {
                using var connection = _dbHelper.CreateConnection();
                using var command = new SqlCommand(query, connection);

                command.Parameters.Add("@Email", SqlDbType.NVarChar, 200).Value = Email;

                connection.Open();
                using var reader = command.ExecuteReader();

                if (reader.Read())
                {
                    isFound = true;

                    Id = (Guid)reader["Id"];
                    FullName = (string)reader["FullName"];
                    StudentId = reader["StudentId"] == DBNull.Value ? "" : (string)reader["StudentId"];
                    PasswordHash = (string)reader["PasswordHash"];
                    Year = reader["Year"] == DBNull.Value ? (byte)0 : Convert.ToByte(reader["Year"]);
                    Role = (string)reader["Role"];
                }
            }
            catch (Exception ex)
            {
                // ما منبتلع الخطأ: منسجّله ومنرميه للميدلوير
                _logger.LogError(ex, "GetProfileByEmail failed for {Email}", Email);
                throw;
            }

            return isFound;
        }

        // ===== جلب مستخدم بالـ Id =====
        public bool GetProfileByID(Guid Id, ref string FullName, ref string StudentId,
            ref string Email, ref byte Year, ref string Role, ref byte ScopeYear)
        {
            bool isFound = false;

            const string query = @"SELECT FullName, StudentId, Email, [Year], Role, ScopeYear
                                   FROM Profiles
                                   WHERE Id = @Id";

            try
            {
                using var connection = _dbHelper.CreateConnection();
                using var command = new SqlCommand(query, connection);

                command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = Id;

                connection.Open();
                using var reader = command.ExecuteReader();

                if (reader.Read())
                {
                    isFound = true;

                    FullName = (string)reader["FullName"];
                    StudentId = reader["StudentId"] == DBNull.Value ? "" : (string)reader["StudentId"];
                    Email = (string)reader["Email"];
                    Year = reader["Year"] == DBNull.Value ? (byte)0 : Convert.ToByte(reader["Year"]);
                    Role = (string)reader["Role"];
                    ScopeYear = reader["ScopeYear"] == DBNull.Value ? (byte)0 : Convert.ToByte(reader["ScopeYear"]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetProfileByID failed for {UserId}", Id);
                throw;
            }

            return isFound;
        }

        // ===== إضافة مستخدم جديد — ترجّع الـ Id الجديد =====
        public Guid AddNewProfile(string FullName, string? StudentId, string Email,
            string PasswordHash, byte? Year, string Role)
        {
            Guid newId = Guid.NewGuid();   // نولّده هنا لأن العمود UNIQUEIDENTIFIER

            const string query = @"INSERT INTO Profiles (Id, FullName, StudentId, Email, PasswordHash, [Year], Role)
                                   VALUES (@Id, @FullName, @StudentId, @Email, @PasswordHash, @Year, @Role);";

            try
            {
                using var connection = _dbHelper.CreateConnection();
                using var command = new SqlCommand(query, connection);

                command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = newId;
                command.Parameters.Add("@FullName", SqlDbType.NVarChar, 150).Value = FullName;
                command.Parameters.Add("@StudentId", SqlDbType.NVarChar, 20).Value = (object?)StudentId ?? DBNull.Value;
                command.Parameters.Add("@Email", SqlDbType.NVarChar, 200).Value = Email;
                command.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 200).Value = PasswordHash;
                command.Parameters.Add("@Year", SqlDbType.TinyInt).Value = (object?)Year ?? DBNull.Value;
                command.Parameters.Add("@Role", SqlDbType.NVarChar, 20).Value = Role;

                connection.Open();
                command.ExecuteNonQuery();
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                // 2601/2627 = انتهاك قيد التفرّد (بريد أو رقم جامعي مكرّر)
                _logger.LogWarning(ex, "Duplicate profile insert for {Email}", Email);

                if (ex.Message.Contains("StudentId", StringComparison.OrdinalIgnoreCase))
                    throw new AppException("هذا الرقم الجامعي مسجّل مسبقاً.", 409);

                throw new AppException("هذا البريد مسجّل مسبقاً.", 409);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddNewProfile failed for {Email}", Email);
                throw;
            }

            return newId;
        }

        // ===== تحديث بيانات المستخدم =====
        // [Year] مقصود إنها مش هون — الطالب ما بيعدّل سنته بنفسه.
        // شوف UpdateStudentYear تحت.
        public bool UpdateProfile(Guid Id, string FullName, string? StudentId)
        {
            const string query = @"UPDATE Profiles
                                      SET FullName  = @FullName,
                                          StudentId = @StudentId,
                                          UpdatedAt = SYSUTCDATETIME()
                                    WHERE Id = @Id";

            try
            {
                using var connection = _dbHelper.CreateConnection();
                using var command = new SqlCommand(query, connection);

                command.Parameters.Add("@FullName", SqlDbType.NVarChar, 150).Value = FullName;
                command.Parameters.Add("@StudentId", SqlDbType.NVarChar, 20).Value =
                    string.IsNullOrWhiteSpace(StudentId) ? DBNull.Value : StudentId;
                command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = Id;

                connection.Open();
                return command.ExecuteNonQuery() > 0;
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                _logger.LogWarning(ex, "Duplicate StudentId on update for {UserId}", Id);
                throw new AppException("هذا الرقم الجامعي مسجّل لطالب آخر.", 409);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateProfile failed for {UserId}", Id);
                throw;
            }
        }

        // ===== المستخدمين (للوحة التحكم) =====
        // onlyYear = null  → كل المستخدمين (للأدمن العام)
        // onlyYear = 1..4  → طلاب هذه السنة فقط (لأدمن/مشرف السنة)
        public DataTable GetAllProfiles(byte? onlyYear = null)
        {
            DataTable dt = new DataTable();

            string query = @"SELECT Id, FullName, StudentId, Email, [Year], Role, ScopeYear, CreatedAt
                             FROM Profiles";

            if (onlyYear.HasValue)
                query += " WHERE [Year] = @Year";

            query += " ORDER BY CreatedAt DESC";

            try
            {
                using var connection = _dbHelper.CreateConnection();
                using var command = new SqlCommand(query, connection);

                if (onlyYear.HasValue)
                    command.Parameters.Add("@Year", SqlDbType.TinyInt).Value = onlyYear.Value;

                connection.Open();
                using var reader = command.ExecuteReader();

                if (reader.HasRows)
                    dt.Load(reader);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllProfiles failed");
                throw;
            }

            return dt;
        }

        // ===== تعديل سنة طالب (من لوحة التحكم فقط) =====
        public bool UpdateStudentYear(Guid Id, byte? Year)
        {
            const string query = @"UPDATE Profiles
                                      SET [Year]    = @Year,
                                          UpdatedAt = SYSUTCDATETIME()
                                    WHERE Id = @Id";

            try
            {
                using var connection = _dbHelper.CreateConnection();
                using var command = new SqlCommand(query, connection);

                command.Parameters.Add("@Year", SqlDbType.TinyInt).Value = (object?)Year ?? DBNull.Value;
                command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = Id;

                connection.Open();
                return command.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateStudentYear failed for {UserId}", Id);
                throw;
            }
        }

        // ===== نقل مجموعة طلاب لسنة — عبارة SQL وحدة =====
        // ما منعمل حلقة نداءات: لو وقعت الشبكة بالنص بيصير نص الطلاب منقولين
        // ونص لأ، وما في طريقة تعرف وين وقفت.
        public int BulkUpdateYear(List<Guid> Ids, byte? Year)
        {
            if (Ids == null || Ids.Count == 0)
                return 0;

            // أسماء الباراميترات مولّدة من عندنا (@id0, @id1...) مش من المستخدم،
            // والقيم بتنمرّر كباراميترات — فما في مجال للحقن.
            var names = new List<string>();
            for (int i = 0; i < Ids.Count; i++)
                names.Add("@id" + i);

            string query = $@"UPDATE Profiles
                                 SET [Year]    = @Year,
                                     UpdatedAt = SYSUTCDATETIME()
                               WHERE Id IN ({string.Join(",", names)})";

            try
            {
                using var connection = _dbHelper.CreateConnection();
                using var command = new SqlCommand(query, connection);

                command.Parameters.Add("@Year", SqlDbType.TinyInt).Value = (object?)Year ?? DBNull.Value;

                for (int i = 0; i < Ids.Count; i++)
                    command.Parameters.Add(names[i], SqlDbType.UniqueIdentifier).Value = Ids[i];

                connection.Open();
                return command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BulkUpdateYear failed for {Count} users", Ids.Count);
                throw;
            }
        }

        // ===== تغيير صلاحية مستخدم + نطاق سنته =====
        public bool UpdateRole(Guid Id, string Role, byte? ScopeYear)
        {
            const string query = @"UPDATE Profiles
                                      SET Role      = @Role,
                                          ScopeYear = @ScopeYear,
                                          UpdatedAt = SYSUTCDATETIME()
                                    WHERE Id = @Id";

            try
            {
                using var connection = _dbHelper.CreateConnection();
                using var command = new SqlCommand(query, connection);

                command.Parameters.Add("@Role", SqlDbType.NVarChar, 20).Value = Role;
                command.Parameters.Add("@ScopeYear", SqlDbType.TinyInt).Value = (object?)ScopeYear ?? DBNull.Value;
                command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = Id;

                connection.Open();
                return command.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateRole failed for {UserId}", Id);
                throw;
            }
        }

        // ===== عدد الأدمنز العامّين (أدمن بلا نطاق سنة) =====
        // منستعملها لمنع تحويل آخر أدمن عام — لأنه ساعتها ما حد بيقدر يوزّع الصلاحيات
        public int CountSuperAdmins()
        {
            const string query = "SELECT COUNT(*) FROM Profiles WHERE Role = 'admin' AND ScopeYear IS NULL";

            try
            {
                using var connection = _dbHelper.CreateConnection();
                using var command = new SqlCommand(query, connection);

                connection.Open();
                object? result = command.ExecuteScalar();

                return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CountSuperAdmins failed");
                throw;
            }
        }

        // ===== صلاحية مستخدم معيّن =====
        public string? GetRole(Guid Id)
        {
            const string query = "SELECT Role FROM Profiles WHERE Id = @Id";

            try
            {
                using var connection = _dbHelper.CreateConnection();
                using var command = new SqlCommand(query, connection);

                command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = Id;

                connection.Open();
                object? result = command.ExecuteScalar();

                return result == null || result == DBNull.Value ? null : (string)result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetRole failed for {UserId}", Id);
                throw;
            }
        }

        // ===== نطاق المستخدم: رتبته + سنته + نطاق مسؤوليته =====
        // استعلام واحد صغير منناديه قبل كل إجراء حسّاس
        public bool GetUserScope(Guid Id, ref string Role, ref byte Year, ref byte ScopeYear)
        {
            bool isFound = false;

            const string query = "SELECT Role, [Year], ScopeYear FROM Profiles WHERE Id = @Id";

            try
            {
                using var connection = _dbHelper.CreateConnection();
                using var command = new SqlCommand(query, connection);

                command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = Id;

                connection.Open();
                using var reader = command.ExecuteReader();

                if (reader.Read())
                {
                    isFound = true;

                    Role = (string)reader["Role"];
                    Year = reader["Year"] == DBNull.Value ? (byte)0 : Convert.ToByte(reader["Year"]);
                    ScopeYear = reader["ScopeYear"] == DBNull.Value ? (byte)0 : Convert.ToByte(reader["ScopeYear"]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUserScope failed for {UserId}", Id);
                throw;
            }

            return isFound;
        }

        // ===== قراءة هاش كلمة السر =====
        public string? GetPasswordHash(Guid Id)
        {
            const string query = "SELECT PasswordHash FROM Profiles WHERE Id = @Id";

            try
            {
                using var connection = _dbHelper.CreateConnection();
                using var command = new SqlCommand(query, connection);

                command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = Id;

                connection.Open();
                object? result = command.ExecuteScalar();

                return result == null || result == DBNull.Value ? null : (string)result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPasswordHash failed for {UserId}", Id);
                throw;
            }
        }

        // ===== تحديث كلمة السر (بلا إجبار تغيير) =====
        public bool UpdatePassword(Guid Id, string PasswordHash)
        {
            const string query = @"UPDATE Profiles
                                      SET PasswordHash = @Hash,
                                          MustChangePassword = 0,
                                          TokenVersion = TokenVersion + 1,
                                          UpdatedAt = SYSUTCDATETIME()
                                    WHERE Id = @Id";

            try
            {
                using var connection = _dbHelper.CreateConnection();
                using var command = new SqlCommand(query, connection);

                command.Parameters.Add("@Hash", SqlDbType.NVarChar, 200).Value = PasswordHash;
                command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = Id;

                connection.Open();
                return command.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdatePassword failed for {UserId}", Id);
                throw;
            }
        }

        // ===== تصفير كلمة السر من لوحة التحكم (مع إجبار تغيير) =====
        public bool UpdatePasswordAndForceChange(Guid Id, string PasswordHash)
        {
            const string query = @"UPDATE Profiles
                                      SET PasswordHash = @Hash,
                                          MustChangePassword = 1,
                                          TokenVersion = TokenVersion + 1,
                                          UpdatedAt = SYSUTCDATETIME()
                                    WHERE Id = @Id";

            try
            {
                using var connection = _dbHelper.CreateConnection();
                using var command = new SqlCommand(query, connection);

                command.Parameters.Add("@Hash", SqlDbType.NVarChar, 200).Value = PasswordHash;
                command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = Id;

                connection.Open();
                return command.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdatePasswordAndForceChange failed for {UserId}", Id);
                throw;
            }
        }

        // ===== هل البريد موجود؟ (منع تكرار التسجيل) =====
        public bool IsEmailExist(string Email)
        {
            const string query = "SELECT TOP 1 1 FROM Profiles WHERE Email = @Email";

            try
            {
                using var connection = _dbHelper.CreateConnection();
                using var command = new SqlCommand(query, connection);

                command.Parameters.Add("@Email", SqlDbType.NVarChar, 200).Value = Email;

                connection.Open();
                return command.ExecuteScalar() != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IsEmailExist failed for {Email}", Email);
                throw;
            }
        }
    }
}
