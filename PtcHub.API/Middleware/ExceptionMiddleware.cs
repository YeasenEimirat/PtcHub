using System.Text.Json;
using PtcHub.API.BLL.Services;
using PtcHub.API.Models;

namespace PtcHub.API.Middleware
{
    // ============================================================
    //  مُعالج الأخطاء المركزي
    //  - AppException  → يرجّع الـ StatusCode والرسالة اللي كتبناها
    //  - أي استثناء آخر → يُسجَّل في الـ Log ويرجّع 500 برسالة عامة
    //  الشكل دائماً { success, message, data } مثل باقي الـ API
    // ============================================================
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public ExceptionMiddleware(RequestDelegate next,
                                   ILogger<ExceptionMiddleware> logger,
                                   IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (AppException ex)
            {
                // خطأ متوقّع من طبقة الـ BLL — رسالته صالحة للعرض للمستخدم
                _logger.LogWarning(ex,
                    "AppException {Status} on {Method} {Path}",
                    ex.StatusCode, context.Request.Method, context.Request.Path);

                await WriteAsync(context, ex.StatusCode, ex.Message);
            }
            catch (Exception ex)
            {
                // خطأ غير متوقّع — نسجّله كاملاً ولا نكشف تفاصيله للمستخدم
                _logger.LogError(ex,
                    "Unhandled exception on {Method} {Path}",
                    context.Request.Method, context.Request.Path);

                string message = _env.IsDevelopment()
                    ? ex.Message
                    : "حدث خطأ غير متوقّع. حاول مرة أخرى، وإذا تكرّر تواصل مع المشرف.";

                await WriteAsync(context, 500, message);
            }
        }

        private static async Task WriteAsync(HttpContext context, int status, string message)
        {
            // لو بدأ الرد بالفعل ما بنقدر نعدّل عليه
            if (context.Response.HasStarted)
                return;

            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json; charset=utf-8";

            var payload = ApiResponse<object>.Fail(message);
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOpts));
        }
    }

    public static class ExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseAppExceptionHandler(this IApplicationBuilder app)
            => app.UseMiddleware<ExceptionMiddleware>();
    }
}
