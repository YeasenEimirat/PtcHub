namespace PtcHub.API.Models
{
    // يمثّل جدول Profiles — كل خاصية تقابل عموداً في الجدول
    // ملاحظة: PasswordHash مش موجود هنا عن قصد.
    // هذا الكلاس بيتحوّل لـ JSON ويُرسل للواجهة، فما بدنا أي أثر للهاش فيه.
    public class Profile
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? StudentId { get; set; }         // الرقم الجامعي
        public string Email { get; set; } = string.Empty;
        public byte? Year { get; set; }                // السنة 1..4 (اختيارية)
        public string Role { get; set; } = "student";  // student / supervisor / admin
        public byte? ScopeYear { get; set; }           // نطاق المسؤولية للطاقم: 1..4، و null = كل السنوات
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
