using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PtcHub.API.BLL.Services;
using PtcHub.API.DTOs;
using PtcHub.API.Models;

namespace PtcHub.API.Controllers
{
    [Authorize]
    public class ProgressController : BaseApiController
    {
        private readonly ProgressService _service;

        public ProgressController(ProgressService service)
        {
            _service = service;
        }

        // ===== GET /api/progress =====  (كل التقدّم دفعة واحدة)
        [HttpGet]
        public IActionResult GetAll()
        {
            var all = _service.GetAllProgress(CurrentUserId);
            return Ok(ApiResponse<object>.Ok(all));
        }

        // ===== GET /api/progress/single?code=EEE4%203254 =====
        [HttpGet("single")]
        public IActionResult GetOne([FromQuery] string code)
        {
            var data = _service.GetProgress(CurrentUserId, code);
            return Ok(ApiResponse<object>.Ok(data));
        }

        // ===== PUT /api/progress?code=EEE4%203254 =====
        [HttpPut]
        public IActionResult Save([FromQuery] string code, SaveProgressRequest req)
        {
            _service.SaveProgress(CurrentUserId, code, req.Data);
            return Ok(ApiResponse<object>.Ok("saved", "Progress saved."));
        }
    }
}