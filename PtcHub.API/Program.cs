using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using PtcHub.API.BLL.Services;
using PtcHub.API.Helpers;
using PtcHub.API.Middleware;
using PtcHub.API.Models;
using PtcHub.API.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
//  1) قراءة الإعدادات من appsettings.json
// ============================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection غير موجود في appsettings.json");

// ===== Email (Brevo) — اختياري: بلا مفتاح الإرسال معطّل بس التطبيق بيشتغل =====
// على السيرفر: Email__ApiKey, Email__SenderEmail, Email__SenderName

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("قسم Jwt غير موجود في appsettings.json");

// حماية: ما بنسمح بمفتاح ضعيف أو بالمفتاح الافتراضي على سيرفر حقيقي
if (string.IsNullOrWhiteSpace(jwtSettings.Key) || Encoding.UTF8.GetByteCount(jwtSettings.Key) < 32)
    throw new InvalidOperationException("مفتاح Jwt:Key يجب أن يكون 32 بايت على الأقل.");

if (!builder.Environment.IsDevelopment() && jwtSettings.Key.Contains("change-this"))
    throw new InvalidOperationException(
        "مفتاح Jwt:Key ما زال المفتاح الافتراضي. غيّره عبر appsettings.Production.json أو متغيّرات البيئة قبل النشر.");

// ============================================================
//  2) طبقة الوصول للبيانات (DAL)
// ============================================================
builder.Services.AddSingleton(new DbHelper(connectionString));
builder.Services.AddScoped<ProfileRepository>();
builder.Services.AddScoped<MyCourseRepository>();
builder.Services.AddScoped<CourseProgressRepository>();
builder.Services.AddScoped<CourseFileRepository>();
builder.Services.AddScoped<AnnouncementRepository>();

// ============================================================
//  3) طبقة منطق الأعمال (BLL)
// ============================================================
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddHttpClient<EmailService>();
builder.Services.AddScoped<PasswordResetService>();
builder.Services.AddScoped<ScopeService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<MyCourseService>();
builder.Services.AddScoped<ProgressService>();
builder.Services.AddScoped<CourseFileService>();
builder.Services.AddScoped<AnnouncementService>();
builder.Services.AddScoped<AdminService>();

// ============================================================
//  4) مصادقة JWT
// ============================================================
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// ============================================================
//  5) CORS — مقيّد. الواجهة والباكند على نفس السيرفر، فبشكل افتراضي
//     ما في داعي لـ CORS أصلاً. لو احتجت نطاقاً خارجياً ضيفه في
//     appsettings.json تحت "Cors": { "Origins": [ "https://..." ] }
// ============================================================
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>();

if (corsOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("PtcHubCors", policy =>
        {
            policy.WithOrigins(corsOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });
}

// ============================================================
//  6) تحديد معدّل المحاولات — حماية تسجيل الدخول من التخمين
// ============================================================
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 8,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // رسالة عربية واضحة بدل رد فاضي
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
        var payload = ApiResponse<object>.Fail("محاولات كثيرة خلال وقت قصير. انتظر دقيقة وحاول من جديد.");
        await context.HttpContext.Response.WriteAsync(
            JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }), token);
    };
});

// ============================================================
//  7) الكونترولرز + Swagger
// ============================================================
builder.Services.AddControllers();

// أخطاء التحقّق (Data Annotations) ترجع بنفس شكل باقي الـ API
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        string message = context.ModelState
            .Where(kv => kv.Value?.Errors.Count > 0)
            .SelectMany(kv => kv.Value!.Errors)
            .Select(e => e.ErrorMessage)
            .FirstOrDefault() ?? "البيانات المُرسلة غير صالحة.";

        return new BadRequestObjectResult(ApiResponse<object>.Fail(message));
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ============================================================
//  8) بناء الـ pipeline
// ============================================================
var app = builder.Build();

// أول شي بالـ pipeline حتى يلتقط أي استثناء بعده
app.UseAppExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseDefaultFiles();   // index.html
// ===== ترويسات البروكسي — لازم قبل أي middleware تاني =====
// بدونها على SmarterASP/IIS: كل الطلبات بتطلع من نفس الـ IP،
// وحدّ المحاولات بينطبق على النظام كله بدل كل مستخدم.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor
                     | ForwardedHeaders.XForwardedProto
});
app.UseStaticFiles();    // ملفات الواجهة (HTML/CSS/JS)

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (corsOrigins.Length > 0)
    app.UseCors("PtcHubCors");

app.UseRateLimiter();
app.UseAuthentication();     // مين إنت؟
app.UseAuthorization();      // مسموح لك؟
app.MapControllers();

app.Run();
