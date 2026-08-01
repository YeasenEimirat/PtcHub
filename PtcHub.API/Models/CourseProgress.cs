namespace PtcHub.API.Models
{
    // يمثّل جدول CourseProgress — تقدّم الطالب في كل مساق (رحلة الطالب)
    public class CourseProgress
    {
        public long Id { get; set; }
        public Guid UserId { get; set; }               // صاحب التقدّم (يرتبط بـ Profiles.Id)
        public string CourseCode { get; set; } = string.Empty;  // مثل: EEE4 3254
        public string Data { get; set; } = "{}";       // JSON: الساعات + المهام + الملاحظات
        public DateTime UpdatedAt { get; set; }
    }
}