using System.Net.Http.Json;

namespace PtcHub.API.BLL.Services
{
    // ============================================================
    //  EmailService — إرسال بريد معاملاتي عبر Brevo REST API
    //
    //  ليش HttpClient بدل sib_api_v3_sdk؟
    //  1) الـ SDK بيثبّت ١٢ مكتبة فرعية — HttpClient موجود أصلاً.
    //  2) بنستعمل دالة وحدة (إرسال بريد). SDK مصمّم لحملات تسويقية.
    //  3) بلا اعتماد على طرف ثالث: لو Brevo غيّروا الـ SDK ما بينكسر شي.
    //  4) الطلب ٨ أسطر JSON — مش بحاجة abstraction.
    //
    //  لو بدك تبدّل لمزوّد تاني (Resend / Mailjet)، غيّر هالملف بس.
    // ============================================================
    public class EmailService
    {
        private readonly HttpClient _http;
        private readonly ILogger<EmailService> _logger;
        private readonly string _apiKey;
        private readonly string _senderEmail;
        private readonly string _senderName;
        private readonly bool _enabled;

        public EmailService(HttpClient http, IConfiguration cfg, ILogger<EmailService> logger)
        {
            _http = http;
            _logger = logger;

            _apiKey = cfg["Email:ApiKey"] ?? "";
            _senderEmail = cfg["Email:SenderEmail"] ?? "noreply@ptchub.com";
            _senderName = cfg["Email:SenderName"] ?? "PTC Hub";
            _enabled = !string.IsNullOrWhiteSpace(_apiKey);

            if (!_enabled)
                _logger.LogWarning("مفتاح Email:ApiKey غير موجود — الإرسال معطّل.");
        }

        /// <summary>
        /// بيبعت إيميل واحد. بيرمي AppException لو فشل.
        /// لو المفتاح مش مضبوط، بيسجّل تحذير وبيرجع بصمت (ما بيوقف التطبيق).
        /// </summary>
        public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            if (!_enabled)
            {
                _logger.LogWarning("إيميل لـ {To} بعنوان «{Subject}» ما انبعت — المفتاح مش مضبوط.", toEmail, subject);
                return;
            }

            var payload = new
            {
                sender = new { name = _senderName, email = _senderEmail },
                to = new[] { new { email = toEmail, name = toName } },
                subject,
                htmlContent = htmlBody
            };

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post,
                    "https://api.brevo.com/v3/smtp/email");

                request.Headers.Add("api-key", _apiKey);
                request.Content = JsonContent.Create(payload);

                var response = await _http.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    string body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Brevo رجّع {Status}: {Body}", response.StatusCode, body);
                    throw new AppException("تعذّر إرسال البريد. حاول لاحقاً.", 502);
                }

                _logger.LogInformation("إيميل انبعت لـ {To}: «{Subject}»", toEmail, subject);
            }
            catch (AppException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "فشل الاتصال بـ Brevo");
                throw new AppException("تعذّر إرسال البريد. حاول لاحقاً.", 502);
            }
        }

        /// <summary>
        /// قالب إيميل تصفير كلمة السر — رمز OTP من ٦ أرقام.
        /// </summary>
        public Task SendPasswordResetOtpAsync(string toEmail, string toName, string otp)
        {
            string html = $@"
            <div dir='rtl' style='font-family:Tahoma,Arial,sans-serif;max-width:480px;margin:auto;
                 padding:24px;border:1px solid #e5e0d6;border-radius:12px'>
                <h2 style='color:#3c4a2f;margin:0 0 16px'>إعادة تعيين كلمة السر</h2>
                <p>مرحباً {System.Net.WebUtility.HtmlEncode(toName)}،</p>
                <p>طلبت إعادة تعيين كلمة السر. استخدم الرمز التالي:</p>
                <div style='text-align:center;margin:24px 0'>
                    <span style='font-size:32px;font-weight:bold;letter-spacing:8px;
                          color:#3c4a2f;background:#f5f3ef;padding:14px 24px;
                          border-radius:10px;display:inline-block;direction:ltr'>{otp}</span>
                </div>
                <p style='color:#777;font-size:13px'>الرمز صالح لمدة ١٥ دقيقة فقط.</p>
                <p style='color:#777;font-size:13px'>لو ما طلبت هذا التغيير، تجاهل هذا البريد — حسابك آمن.</p>
                <hr style='border:none;border-top:1px solid #e5e0d6;margin:20px 0'>
                <p style='color:#aaa;font-size:11px;text-align:center'>PTC Hub — نظام إدارة المساقات</p>
            </div>";

            return SendAsync(toEmail, toName, "رمز إعادة تعيين كلمة السر — PTC Hub", html);
        }
    }
}
