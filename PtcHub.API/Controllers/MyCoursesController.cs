using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PtcHub.API.BLL.Services;
using PtcHub.API.DTOs;
using PtcHub.API.Models;

namespace PtcHub.API.Controllers
{
    [Authorize]   // كل الدوال تحتاج تسجيل دخول
    public class MyCoursesController : BaseApiController
    {
        private readonly MyCourseService _service;

        public MyCoursesController(MyCourseService service)
        {
            _service = service;
        }

        // ===== GET /api/mycourses =====
        [HttpGet]
        public IActionResult Get()
        {
            var courses = _service.GetMyCourses(CurrentUserId);
            return Ok(ApiResponse<object>.Ok(courses));
        }

        // ===== POST /api/mycourses =====
        [HttpPost]
        public IActionResult Add(MyCourseRequest req)
        {
            var courses = _service.AddCourse(CurrentUserId, req.CourseCode);
            return Ok(ApiResponse<object>.Ok(courses));
        }

        // ===== DELETE /api/mycourses?code=EEE4%203254 =====
        [HttpDelete]
        public IActionResult Remove([FromQuery] string code)
        {
            var courses = _service.RemoveCourse(CurrentUserId, code);
            return Ok(ApiResponse<object>.Ok(courses));
        }
    }
}