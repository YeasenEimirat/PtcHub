# PTC Hub v6 — سجل الإصلاحات

مرجع: تقرير الفحص الشامل بتاريخ 29 تموز 2026 (23 ملاحظة).
كل رقم تحت بقابل رقم الملاحظة بالتقرير.

---

## 🔴 حرج

### 1. ثغرة Stored XSS
- **جديد** `wwwroot/api-config.js`: دوال `esc()` / `escAttr()` / `safeUrl()` عالمية.
- `admin.html`: تهريب في جدول الطلاب (الاسم/الرقم/البريد/الرتبة)، جدول الملفات،
  قائمة الإعلانات، قائمة المساقات، ورسالة الخطأ.
- `app.js`: تهريب في قائمة المستخدم، ملفات المساقات، وإعلانات الصفحة الرئيسية.
- `journey.html`: تهريب نص المهام.
- `safeUrl()` بترفض `javascript:` و`data:` وبتسمح بس بـ http/https والروابط النسبية.
- الباكند كمان بيرفض روابط غير http/https عند الإضافة (`CourseFileService.EnsureSafeUrl`).
- كل الروابط الخارجية صارت `rel="noopener noreferrer"`.

### 2. انعدام Exception Handler
- **جديد** `Middleware/ExceptionMiddleware.cs`.
  - `AppException` → الـ StatusCode الصحيح (400/401/403/404/409) + الرسالة العربية.
  - أي استثناء آخر → `LogError` كامل + 500 برسالة عامة (وبالتطوير بتظهر التفاصيل).
  - الشكل دايماً `{ success, message, data }`.
- انربط أول شي بالـ pipeline في `Program.cs`.

### 3. الحد الأدنى لكلمة السر
- `login.html`: من 6 إلى **8** — بالفحص، وبالـ `minlength`، وبالـ placeholder، وبرسالة الخطأ.
  (الباكند كان أصلاً 8.)

---

## 🟠 عالي

### 4. رحلة الطالب ما بتستعمل الـ API
- `journey.html`: طبقة مزامنة جديدة.
  - `pullFromCloud()` عند فتح الصفحة → `PTCAuth.getAllProgress()` بتعبّي الكاش المحلي وبتعيد الرسم.
  - `pushToCloud()` مؤجّلة 800ms بعد كل حفظ → `PTCAuth.saveProgress()`.
  - مؤشّر حالة `#syncState` (جارٍ الحفظ / محفوظ في حسابك / محفوظ محلياً فقط).
  - localStorage ضلّ كاش سريع، فما تغيّر شي من الكود المتزامن الموجود.
- يعني `CourseProgressRepository` + `ProgressService` + `ProgressController` + جدول `CourseProgress` صاروا شغّالين فعلياً.

### 5. `ptc_migrated` ما بينكتب
- `auth.js`: `migrateLocalToCloud()` صارت تفحص العلم بنفسها، وبعد النجاح بتكتبه
  وبتمسح `ptc_mycourses` ومفاتيح `jr_*` المحلية.
- بترجّع `{ courses, progress, skipped }`.
- `login.html` انظبطت لتقرأ `skipped`.
- **النتيجة:** المساقات المحذوفة ما عادت ترجع، والتقدّم القديم ما عاد يدهس الجديد.

### 6. `id` مكرّر للمساقات الاختيارية
- `courses-index.js`: `c_EEEX35XX` → `c_EEEX35XX_1` … `_5`.
- `year3.html` → `_1`، `year4.html` → `_2, _3, _4, _5`.
- **النتيجة:** البحث بالصفحة الرئيسية صار يوصّلك للكرت الصح بدل ما يرجّعك دايماً للأول.

### 7. ابتلاع الاستثناءات في كل الـ Repositories
- الخمس repositories انكتبوا من جديد:
  - `ILogger<T>` محقون بكلّ واحد.
  - كل `catch` صار `_logger.LogError(...)` + `throw;` — صفر ابتلاع.
  - `using var` على الـ connection والـ command والـ reader (بحلّ #19).
  - أعمدة صريحة بدل `SELECT *` (بحلّ #18).
  - `Parameters.Add(name, SqlDbType, size)` بدل `AddWithValue` — نوع صريح وأداء أفضل.
- **النتيجة:** ما عاد فيه "Email or password is incorrect" لما القاعدة تكون واقعة.

### 8. أخطاء التحقّق بشكل مختلف
- `Program.cs`: `ApiBehaviorOptions.InvalidModelStateResponseFactory` بيرجّع `ApiResponse` بدل `ProblemDetails`.
- `auth.js`: `studentId` صار ينبعت نصاً مقصوصاً بدل `|| null` (الحقل `[Required]`).
- `auth.js`: رسائل خطأ حسب رمز الحالة (429 / 403 / 404 / 5xx) لو ما إجت رسالة من السيرفر.

---

## 🟡 متوسط

### 9. مفتاح JWT مرفوع مع الكود
- `appsettings.json`: صار مفتاح تطوير واضح إنه للتطوير، والقسم `Cors` انضاف.
- `Program.cs`: بيرفض الإقلاع لو المفتاح < 32 بايت، أو لو فيه `change-this` وإنت على Production.
- **جديد** `appsettings.Production.json.example` + `.gitignore` بيستثني النسخة الحقيقية.

### 10. `PasswordHash` بينزل مع رد الـ API
- انشال نهائياً من `Models/Profile.cs` (ما كان حدا يعبّيه أصلاً).

### 11. ما في حماية من التخمين على `/api/auth/login`
- `Program.cs`: `AddRateLimiter` — نافذة ثابتة 8 محاولات/دقيقة لكل IP، مع رد 429 برسالة عربية.
- `[EnableRateLimiting("login")]` على `Login` و`Register`.

### 12. ممكن ينحذف آخر أدمن
- `ProfileRepository.CountAdmins()` و`GetRole()` — دالتين جديدتين.
- `AdminService.ChangeRole`: بترمي 409 "لا يمكن إزالة آخر أدمن في النظام."

### 13. الإعلانات بتتحمّل 3 مرات
- `app.js`: نداء واحد، وبعد جهوزية الجلسة بس (`isReady` أو `ptc-auth-change` مع `{ once: true }`)
  حتى يوصل `?year=` مع أول طلب. + قفل `loading` ضد التوازي. + انشال `console.log` الزايد.

### 14. `addCourseFile` / `addAnnouncement` بترجّع `{id}` بس
- `CourseFileService.AddFile` صارت ترجّع `CourseFile` كامل (عبر `GetFileById` الجديدة).
- `AnnouncementService.AddAnnouncement` صارت ترجّع `Announcement` كامل (عبر `GetAnnouncementById`).
- الكونترولرز بترجّع الكائن، و`auth.js` عندها `mapAnnouncement()` جديدة.

### 15. CORS مفتوح للكل
- `AllowAnyOrigin()` انشال. صار يقرأ `Cors:Origins` من الإعدادات،
  وإذا فاضية ما بينضاف CORS أصلاً (الحالة الافتراضية — نفس السيرفر).

### 16. ما في HTTPS redirect ولا HSTS
- `Program.cs`: `UseHsts()` + `UseHttpsRedirection()` بغير بيئة التطوير.

### 17. مجلد `.vs` (1.5 ميجا) داخل `wwwroot`
- انحذف. + `.gitignore` جديد بجذر المشروع (`.vs/`, `bin/`, `obj/`, `appsettings.Production.json`, `*.user`).

---

## 🔵 تحسينات

- **18.** `SELECT *` → أعمدة صريحة في كل الاستعلامات.
- **19.** `using var` على `SqlConnection` / `SqlCommand` / `SqlDataReader`.
- **20.** تحذيرات Nullable: `string? SizeLabel`, `string? Body`, `string? StudentId`, `object?` من `ExecuteScalar`، و`catch (Exception ex)` كلها صارت تستعمل `ex` بالـ logging.
- **21.** `PtcHub.API.csproj` — نسخ متناسقة ومتأكّد إنها موجودة:
  - `Microsoft.Data.SqlClient` **5.2.2** (7.0.2 مش موجودة على nuget)
  - `Microsoft.AspNetCore.Authentication.JwtBearer` **8.0.11**
  - `System.IdentityModel.Tokens.Jwt` **7.6.2** (متوافقة مع JwtBearer 8.0.x — 8.21.0 كانت بتعمل تضارب)
  - `BCrypt.Net-Next` **4.0.3**
- **22.** `DbHelper` — ضلّ زي ما هو، سليم.
- **23.** سقف 80 مساق للطالب الواحد (`MyCourseService` + `MyCourseRepository.CountCourses`).

---

## قبل ما تشغّل

1. `dotnet restore` — النسخ تغيّرت.
2. الملفات كلها UTF-8 هلأ (`Program.cs` كان Windows-1256 فالتعليقات العربية كانت مكسّرة).
3. **مفتاح JWT تغيّر** → كل التوكنات القديمة بطلت. أول دخول بعد التحديث بيطلب تسجيل دخول من جديد. طبيعي.
4. للنشر على SmarterASP: انسخ `appsettings.Production.json.example` لـ `appsettings.Production.json`
   وحطّ فيه نص الاتصال ومفتاح JWT عشوائي 64 حرف — أو عرّفهم كمتغيّرات بيئة
   `ConnectionStrings__DefaultConnection` و `Jwt__Key`.

## ملاحظات مفتوحة

- `PTCAuth.updateProfile()` لسا بلا واجهة. الـ endpoint شغّال (`PUT /api/auth/profile`) بس ما في شاشة "تعديل بياناتي".
  هاي ميزة ناقصة مش خلل — لما تجهز شاشة، الدالة جاهزة.
- المساقات الاختيارية الخمسة صار إلها `id` فريد بس نفس الـ `code` (`EEEX 35XX`).
  يعني الملفات اللي بترفعها عليها بتظهر على الخمسة. لو بدك تفصلهم، بدك تعطي كل واحد كود مستقل.
- ما قدرت أشغّل `dotnet build` هون (الـ SDK وnuget.org مش متوفرين بالبيئة). الفحص كان يدوي +
  فحص توازن الأقواس + فحص صياغة لكل ملفات JS (نجحت كلها).
