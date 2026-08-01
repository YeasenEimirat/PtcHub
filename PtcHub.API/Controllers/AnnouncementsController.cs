using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PtcHub.API.BLL.Services;
using PtcHub.API.DTOs;
using PtcHub.API.Models;

namespace PtcHub.API.Controllers
{
    public class AnnouncementsController : BaseApiController
    {
        private readonly AnnouncementService _service;

        public AnnouncementsController(AnnouncementService service)
        {
            _service = service;
        }

        // ===== GET /api/announcements =====
        // الزائر يشوف النشِطة فقط، الطاقم يشوف الكل
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Get()
        {
            // السنة بتتحدّد من حساب المستخدم في القاعدة، مش من الـ query string.
            // فالطالب ما بيقدر يشوف إعلانات سنة تانية ولو عدّل الرابط بإيده.
            var list = _service.GetAnnouncements(CurrentUserIdOrNull);
            return Ok(ApiResponse<object>.Ok(list));
        }

        // ===== POST /api/announcements =====  (الطاقم فقط)
        [HttpPost]
        [Authorize(Roles = "admin,supervisor")]
        public IActionResult Add(AnnouncementRequest req)
        {
            var created = _service.AddAnnouncement(req.Title, req.Body, req.Active, req.Year, CurrentUserId);
            return Ok(ApiResponse<object>.Ok(created, "نُشر الإعلان."));
        }

        // ===== DELETE /api/announcements/5 =====  (الطاقم فقط)
        [HttpDelete("{id:long}")]
        [Authorize(Roles = "admin,supervisor")]
        public IActionResult Delete(long id)
        {
            _service.DeleteAnnouncement(id, CurrentUserId);
            return Ok(ApiResponse<object>.Ok("deleted", "حُذف الإعلان."));
        }
    }
}