using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PtcHub.API.BLL.Services;
using PtcHub.API.DTOs;
using PtcHub.API.Models;

namespace PtcHub.API.Controllers
{
    public class FilesController : BaseApiController
    {
        private readonly CourseFileService _service;

        public FilesController(CourseFileService service)
        {
            _service = service;
        }

        // ===== GET /api/files?courseCode=EEE4%203254 =====
        // مفتوح للجميع حتى الزائر (تصفّح الملفات)
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Get([FromQuery] string? courseCode)
        {
            var files = _service.GetFiles(courseCode);
            return Ok(ApiResponse<object>.Ok(files));
        }

        // ===== POST /api/files =====  (الطاقم فقط)
        [HttpPost]
        [Authorize(Roles = "admin,supervisor")]
        public IActionResult Add(CourseFileRequest req)
        {
            var created = _service.AddFile(
                req.CourseCode, req.Title, req.Url, req.Kind,
                req.SizeLabel, req.SortOrder, CurrentUserId);

            // نرجّع الكائن كاملاً (مش الـ id بس) حتى الواجهة تعرضه مباشرة
            return Ok(ApiResponse<object>.Ok(created, "أُضيف الملف."));
        }

        // ===== POST /api/files/submit =====  (أي طالب مسجّل)
        [HttpPost("submit")]
        [Authorize]
        public IActionResult Submit(StudentFileSubmitRequest req)
        {
            var created = _service.SubmitByStudent(
                req.CourseCode, req.Title, req.Url, req.Kind, CurrentUserId);

            return Ok(ApiResponse<object>.Ok(created, "تم إرسال الملف للمراجعة. رح يظهر بعد موافقة المسؤول."));
        }

        // ===== GET /api/files/pending =====  (الطاقم)
        [HttpGet("pending")]
        [Authorize(Roles = "admin,supervisor")]
        public IActionResult GetPending()
        {
            var files = _service.GetPendingFiles(CurrentUserId);
            return Ok(ApiResponse<object>.Ok(files));
        }

        // ===== PUT /api/files/12/review =====  (الطاقم)
        [HttpPut("{id:long}/review")]
        [Authorize(Roles = "admin,supervisor")]
        public IActionResult Review(long id, FileReviewRequest req)
        {
            _service.ReviewFile(id, req.Decision, CurrentUserId);
            string msg = req.Decision == "approved" ? "تمت الموافقة على الملف." : "تم رفض الملف.";
            return Ok(ApiResponse<object>.Ok(req.Decision, msg));
        }

        // ===== DELETE /api/files/12 =====  (الطاقم فقط)
        [HttpDelete("{id:long}")]
        [Authorize(Roles = "admin,supervisor")]
        public IActionResult Delete(long id)
        {
            _service.DeleteFile(id, CurrentUserId);
            return Ok(ApiResponse<object>.Ok("deleted", "حُذف الملف."));
        }
    }
}