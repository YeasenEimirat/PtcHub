using Microsoft.Data.SqlClient;

namespace PtcHub.API.Helpers
{
    // كلاس مركزي لإدارة الاتصال بقاعدة البيانات
    // كل repository بيستخدمه ليحصل على اتصال جاهز
    public class DbHelper
    {
        private readonly string _connectionString;

        // نص الاتصال بيجي من appsettings.json عبر الـ Dependency Injection
        public DbHelper(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("نص الاتصال بقاعدة البيانات غير مضبوط.");

            _connectionString = connectionString;
        }

        // ينشئ اتصالاً جديداً — الـ Connection Pooling يتكفّل بإعادة الاستخدام والأداء
        public SqlConnection CreateConnection() => new SqlConnection(_connectionString);
    }
}