using System.ComponentModel.DataAnnotations;

namespace PtcHub.API.DTOs
{
    // ===== Add/remove a course to "My Courses" =====
    public class MyCourseRequest
    {
        [Required(ErrorMessage = "Course code is required")]
        [StringLength(20)]
        public string CourseCode { get; set; } = string.Empty;
    }

    // ===== Save progress (Student Journey) =====
    public class SaveProgressRequest
    {
        // Free JSON object — same shape stored in localStorage as jr_{code}
        [Required]
        public object Data { get; set; } = new object();
    }

    // ===== إضافة ملف من طالب =====
    public class StudentFileSubmitRequest
    {
        [Required(ErrorMessage = "Course code is required")]
        [StringLength(20)]
        public string CourseCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "File title is required")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "File URL is required")]
        [Url(ErrorMessage = "Invalid URL format")]
        [StringLength(1000)]
        public string Url { get; set; } = string.Empty;

        [StringLength(20)]
        public string Kind { get; set; } = "link";
    }

    // ===== قرار المراجعة =====
    public class FileReviewRequest
    {
        [Required]
        [RegularExpression("^(approved|rejected)$", ErrorMessage = "Decision must be: approved or rejected")]
        public string Decision { get; set; } = string.Empty;
    }

    // ===== Add a course file (staff) =====
    public class CourseFileRequest
    {
        [Required(ErrorMessage = "Course code is required")]
        [StringLength(20)]
        public string CourseCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "File title is required")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "File URL is required")]
        [Url(ErrorMessage = "Invalid URL")]
        [StringLength(1000)]
        public string Url { get; set; } = string.Empty;

        [RegularExpression("^(pdf|doc|vid|zip|link)$",
            ErrorMessage = "Kind must be: pdf, doc, vid, zip, or link")]
        public string Kind { get; set; } = "pdf";

        [StringLength(50)]
        public string? SizeLabel { get; set; }

        public int SortOrder { get; set; }
    }

    // ===== Add an announcement (staff) =====
    public class AnnouncementRequest
    {
        [Required(ErrorMessage = "Announcement title is required")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Body { get; set; }

        public bool Active { get; set; } = true;

        [Range(1, 4, ErrorMessage = "Year must be between 1 and 4")]
        public byte? Year { get; set; }   // null = عام للجميع
    }

    // ===== تعديل سنة طالب (الأدمن العام) =====
    public class ChangeStudentYearRequest
    {
        [Required]
        public Guid UserId { get; set; }

        // null مسموحة — يعني "بلا سنة محدّدة" (بيشوف العام بس)
        // 5 = خريج
        [Range(1, 5, ErrorMessage = "Year must be between 1 and 5")]
        public byte? Year { get; set; }
    }

    // ===== نقل مجموعة طلاب لسنة (الأدمن العام) =====
    public class BulkChangeYearRequest
    {
        [Required(ErrorMessage = "لازم تحدّد طلاب.")]
        [MinLength(1, ErrorMessage = "ما حدّدت ولا طالب.")]
        [MaxLength(500, ErrorMessage = "ما بتقدر تنقل أكتر من 500 طالب بالمرة.")]
        public List<Guid> UserIds { get; set; } = new List<Guid>();

        // null = بلا سنة، 5 = خريج
        [Range(1, 5, ErrorMessage = "Year must be between 1 and 5")]
        public byte? Year { get; set; }
    }

    // ===== Change a user's role (admin) =====
    public class ChangeRoleRequest
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        [RegularExpression("^(student|supervisor|admin)$",
            ErrorMessage = "Role must be: student, supervisor, or admin")]
        public string Role { get; set; } = "student";

        // نطاق السنة للمشرف/أدمن السنة (1..4). null = أدمن عام على كل السنوات.
        [Range(1, 4, ErrorMessage = "Year must be between 1 and 4")]
        public byte? ScopeYear { get; set; }
    }
}