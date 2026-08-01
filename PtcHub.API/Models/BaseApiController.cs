using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PtcHub.API.BLL.Services;

namespace PtcHub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        // The current user's Id, read from the JWT token
        protected Guid CurrentUserId
        {
            get
            {
                string? raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (Guid.TryParse(raw, out Guid id))
                    return id;

                throw new AppException("الجلسة غير صالحة، سجّل الدخول من جديد.", 401);
            }
        }

        // نفس الفكرة بس بلا استثناء — للمسارات المفتوحة للزائر (AllowAnonymous)
        protected Guid? CurrentUserIdOrNull
        {
            get
            {
                string? raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
                return Guid.TryParse(raw, out Guid id) ? id : (Guid?)null;
            }
        }

        // Is the current user an admin or supervisor?
        protected bool IsStaff
        {
            get
            {
                return User.IsInRole("admin") || User.IsInRole("supervisor");
            }
        }
    }
}