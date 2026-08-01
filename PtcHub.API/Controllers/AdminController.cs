using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PtcHub.API.BLL.Services;
using PtcHub.API.DTOs;
using PtcHub.API.Models;

namespace PtcHub.API.Controllers
{
    public class AdminController : BaseApiController
    {
        private readonly AdminService _service;
        private readonly PasswordResetService _passwordReset;

        public AdminController(AdminService service, PasswordResetService passwordReset)
        {
            _service = service;
            _passwordReset = passwordReset;
        }

        // ===== GET /api/admin/students =====  (الطاقم)
        [HttpGet("students")]
        [Authorize(Roles = "admin,supervisor")]
        public IActionResult Students()
        {
            var students = _service.GetStudents(CurrentUserId);
            return Ok(ApiResponse<object>.Ok(students));
        }

        // ===== PUT /api/admin/student-year =====  (الأدمن العام فقط)
        [HttpPut("student-year")]
        [Authorize(Roles = "admin")]
        public IActionResult ChangeStudentYear(ChangeStudentYearRequest req)
        {
            _service.ChangeStudentYear(CurrentUserId, req.UserId, req.Year);
            return Ok(ApiResponse<object>.Ok("updated", "تم تحديث السنة."));
        }

        // ===== PUT /api/admin/students/year =====  (الأدمن العام فقط)
        // نقل جماعي: بتحدّد الطلاب وبتختار السنة الهدف
        [HttpPut("students/year")]
        [Authorize(Roles = "admin")]
        public IActionResult BulkChangeYear(BulkChangeYearRequest req)
        {
            int moved = _service.BulkChangeYear(CurrentUserId, req.UserIds, req.Year);
            return Ok(ApiResponse<object>.Ok(new { moved }, $"تم نقل {moved} طالب."));
        }

        // ===== POST /api/admin/reset-password =====  (الطاقم)
        [HttpPost("reset-password")]
        [Authorize(Roles = "admin,supervisor")]
        public IActionResult AdminResetPassword(AdminResetPasswordRequest req)
        {
            string tempPassword = _passwordReset.ResetByAdmin(CurrentUserId, req.UserId);
            return Ok(ApiResponse<object>.Ok(
                new { tempPassword },
                "كلمة السر المؤقتة جاهزة. أعطِها للطالب — مش رح تظهر مرة تانية."));
        }

        // ===== PUT /api/admin/role =====  (الأدمن فقط)
        [HttpPut("role")]
        [Authorize(Roles = "admin")]
        public IActionResult ChangeRole(ChangeRoleRequest req)
        {
            _service.ChangeRole(CurrentUserId, req.UserId, req.Role, req.ScopeYear);
            return Ok(ApiResponse<object>.Ok("updated", "تم تحديث الصلاحية."));
        }
    }
}