namespace PtcHub.API.Models
{
    // يمثّل جدول Announcements — إعلانات الطاقم للطلاب
    public class Announcement
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;   // عنوان الإعلان
        public string? Body { get; set; }                   // نص الإعلان (اختياري)
        public bool Active { get; set; } = true;            // هل الإعلان ظاهر للطلاب؟
        public byte? Year { get; set; }                     // السنة المستهدفة (null = عام للجميع)
        public Guid? CreatedBy { get; set; }                // مَن نشره (اختياري)
        public DateTime CreatedAt { get; set; }
    }
}