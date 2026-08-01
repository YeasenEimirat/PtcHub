using System.ComponentModel.DataAnnotations;

namespace PtcHub.API.DTOs
{
    // Data coming from the registration form
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(150, MinimumLength = 3)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Student ID is required")]
        [RegularExpression(@"^\d{6,12}$", ErrorMessage = "Student ID must be 6-12 digits")]
        public string StudentId { get; set; } = string.Empty;

        [Range(1, 4, ErrorMessage = "Year must be between 1 and 4")]
        public byte? Year { get; set; }
    }

    // Data coming from the login form
    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    // Data for updating the profile
    // ملاحظة: Year مقصود إنها مش هون.
    // السنة بتحدّد صلاحية الوصول للإعلانات، فلو خلّينا الطالب يعدّلها
    // بيقدر يقرأ إعلانات أي سنة بنداء واحد. تعديلها صار من لوحة التحكم.
    public class UpdateProfileRequest
    {
        [Required]
        [StringLength(150, MinimumLength = 3)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Student ID is required")]
        [RegularExpression(@"^\d{6,12}$", ErrorMessage = "Student ID must be 6-12 digits")]
        public string StudentId { get; set; } = string.Empty;
    }
}

    // ===== نسيت كلمة السر: طلب OTP =====
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    // ===== نسيت كلمة السر: تحقّق من OTP وتعيين جديدة =====
    public class ResetPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "الرمز ٦ أرقام")]
        public string Otp { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "كلمة السر يجب أن تكون ٨ أحرف على الأقل")]
        public string NewPassword { get; set; } = string.Empty;
    }

    // ===== تغيير كلمة السر (المستخدم يعرف القديمة) =====
    public class ChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "كلمة السر يجب أن تكون ٨ أحرف على الأقل")]
        public string NewPassword { get; set; } = string.Empty;
    }

    // ===== تصفير كلمة السر من لوحة التحكم =====
    public class AdminResetPasswordRequest
    {
        [Required]
        public Guid UserId { get; set; }
    }
