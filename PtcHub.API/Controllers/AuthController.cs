using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using PtcHub.API.BLL.Services;
using PtcHub.API.DTOs;
using PtcHub.API.Models;

namespace PtcHub.API.Controllers
{
    public class AuthController : BaseApiController
    {
        private readonly AuthService _authService;
        private readonly PasswordResetService _passwordReset;

        public AuthController(AuthService authService, PasswordResetService passwordReset)
        {
            _authService = authService;
            _passwordReset = passwordReset;
        }

        // ===== POST /api/auth/register =====
        [HttpPost("register")]
        [AllowAnonymous]
        [EnableRateLimiting("login")]
        public IActionResult Register(RegisterRequest req)
        {
            string token = _authService.Register(
                req.FullName, req.Email, req.Password, req.StudentId, req.Year);

            var result = new { token };
            return Ok(ApiResponse<object>.Ok(result, "تم إنشاء الحساب بنجاح."));
        }

        // ===== POST /api/auth/login =====
        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("login")]
        public IActionResult Login(LoginRequest req)
        {
            string token = _authService.Login(req.Email, req.Password);

            var result = new { token };
            return Ok(ApiResponse<object>.Ok(result, "أهلاً وسهلاً."));
        }

        // ===== GET /api/auth/me =====
        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            return Ok(ApiResponse<object>.Ok(_authService.GetProfile(CurrentUserId)));
        }

        // ===== PUT /api/auth/profile =====
        [HttpPut("profile")]
        [Authorize]
        public IActionResult UpdateProfile(UpdateProfileRequest req)
        {
            var profile = _authService.UpdateProfile(
                CurrentUserId, req.FullName, req.StudentId);

            return Ok(ApiResponse<object>.Ok(profile, "تم تحديث البيانات."));
        }
        // ===== POST /api/auth/forgot-password =====
        // بيبعت OTP على الإيميل. دايماً بيرجع 200 حتى ما يكشف الحسابات.
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest req)
        {
            await _passwordReset.RequestOtp(req.Email);
            return Ok(ApiResponse<object>.Ok(null, "لو البريد مسجّل، بيوصلك رمز تحقّق."));
        }

        // ===== POST /api/auth/reset-password =====
        // بيتحقّق من الـ OTP وبيعيّن كلمة سر جديدة
        [HttpPost("reset-password")]
        [AllowAnonymous]
        [EnableRateLimiting("login")]
        public IActionResult ResetPassword(ResetPasswordRequest req)
        {
            _passwordReset.VerifyOtpAndReset(req.Email, req.Otp, req.NewPassword);
            return Ok(ApiResponse<object>.Ok(null, "تم تعيين كلمة السر الجديدة. سجّل دخولك."));
        }

        // ===== PUT /api/auth/change-password =====
        // المستخدم بيغيّر كلمة سره بنفسه (لازم يعرف القديمة)
        [HttpPut("change-password")]
        [Authorize]
        public IActionResult ChangePassword(ChangePasswordRequest req)
        {
            _passwordReset.ChangePassword(CurrentUserId, req.CurrentPassword, req.NewPassword);
            return Ok(ApiResponse<object>.Ok(null, "تم تغيير كلمة السر. سجّل دخولك من جديد."));
        }
    }
}
