namespace PtcHub.API.Models
{
    // يمثّل جدول MyCourses — المساقات التي سجّلها الطالب لهذا الفصل
    public class MyCourse
    {
        public long Id { get; set; }
        public Guid UserId { get; set; }               // صاحب التسجيل (يرتبط بـ Profiles.Id)
        public string CourseCode { get; set; } = string.Empty;  // مثل: EEE4 3254
        public DateTime CreatedAt { get; set; }
    }
}