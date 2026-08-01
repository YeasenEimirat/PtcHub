namespace PtcHub.API.Models
{
    // يمثّل جدول CourseFiles — روابط المحاضرات والملخصات (يديرها الطاقم)
    public class CourseFile
    {
        public long Id { get; set; }
        public string CourseCode { get; set; } = string.Empty;  // مثل: EEE4 3254
        public string Title { get; set; } = string.Empty;       // اسم الملف
        public string Url { get; set; } = string.Empty;         // الرابط (درايف / يوتيوب)
        public string Kind { get; set; } = "pdf";               // pdf / doc / vid / zip / link
        public string? SizeLabel { get; set; }                  // نص وصفي مثل "2.4 MB" (اختياري)
        public int SortOrder { get; set; }                      // ترتيب العرض
        public Guid? CreatedBy { get; set; }                    // مَن أضافه (اختياري)
        public string Status { get; set; } = "approved";       // approved / pending / rejected
        public string? SubmitterName { get; set; }             // اسم اللي ضاف الملف
        public Guid? ReviewedBy { get; set; }                  // مين وافق أو رفض
        public DateTime? ReviewedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}