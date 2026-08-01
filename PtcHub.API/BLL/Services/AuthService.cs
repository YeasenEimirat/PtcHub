using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PtcHub.API.Models;
using PtcHub.API.Repositories;

namespace PtcHub.API.BLL.Services
{
    public class AuthService
    {
        private readonly ProfileRepository _repo;
        private readonly JwtSettings _jwt;

        public AuthService(ProfileRepository repo, JwtSettings jwt)
        {
            _repo = repo;
            _jwt = jwt;
        }

        // ===== Register a new account =====
        // Returns the JWT token after successful registration
        public string Register(string fullName, string email, string password,
            string? studentId, byte? year)
        {
            email = email.Trim().ToLower();

            // حماية احترازية: الرقم الجامعي مطلوب (والـ DTO بيفحصه كمان)
            if (string.IsNullOrWhiteSpace(studentId))
                throw new AppException("الرقم الجامعي مطلوب.", 400);

            // قاعدة عمل: ما منسجّل بريد موجود مسبقاً
            // ملاحظة: هذا فحص "مبكّر" لرسالة ألطف. الحماية الحقيقية من الـ race
            // هي الـ UNIQUE INDEX على القاعدة — AddNewProfile بتلتقط 2601/2627.
            if (_repo.IsEmailExist(email))
                throw new AppException("هذا البريد مسجّل مسبقاً.", 409);

            // تشفير كلمة السر بـ BCrypt قبل التخزين
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            // The role is always "student" on registration — never taken from the request
            // الرقم الجامعي مطلوب (تم التحقق في الـ DTO)
            Guid newId = _repo.AddNewProfile(
                fullName.Trim(), studentId.Trim(), email, passwordHash, year, "student");

            if (newId == Guid.Empty)
                throw new AppException("تعذّر إنشاء الحساب. حاول مرة أخرى.", 500);

            // Generate a token for the new user
            return GenerateToken(newId, email, fullName.Trim(), "student");
        }

        // ===== Login =====
        public string Login(string email, string password)
        {
            email = email.Trim().ToLower();

            Guid id = Guid.Empty;
            string fullName = "", studentId = "", passwordHash = "", role = "";
            byte year = 0;

            bool found = _repo.GetProfileByEmail(email, ref id, ref fullName,
                ref studentId, ref passwordHash, ref year, ref role);

            // Same message for both cases so we don't reveal which emails exist
            if (!found || !BCrypt.Net.BCrypt.Verify(password, passwordHash))
                throw new AppException("البريد الإلكتروني أو كلمة السر غير صحيحة.", 401);

            return GenerateToken(id, email, fullName, role);
        }

        // ===== Current profile =====
        public Profile GetProfile(Guid userId)
        {
            string fullName = "", studentId = "", email = "", role = "";
            byte year = 0, scopeYear = 0;

            bool found = _repo.GetProfileByID(userId, ref fullName, ref studentId,
                ref email, ref year, ref role, ref scopeYear);

            if (!found)
                throw new AppException("المستخدم غير موجود.", 404);

            return new Profile
            {
                Id = userId,
                FullName = fullName,
                StudentId = studentId,
                Email = email,
                Year = year == 0 ? null : year,
                Role = role,
                ScopeYear = scopeYear == 0 ? null : scopeYear
            };
        }

        // ===== Update current profile =====
        // السنة مش من ضمن اللي بيعدّله الطالب — بتتغيّر من لوحة التحكم بس
        public Profile UpdateProfile(Guid userId, string fullName, string? studentId)
        {
            string? sid = string.IsNullOrWhiteSpace(studentId) ? null : studentId.Trim();

            if (!_repo.UpdateProfile(userId, fullName.Trim(), sid))
                throw new AppException("تعذّر تحديث البيانات.", 500);

            return GetProfile(userId);
        }

        // ===== Generate the JWT token =====
        private string GenerateToken(Guid userId, string email, string fullName, string role)
        {
            // The claims stored inside the token
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.Role, role)
            };

            // The secret key that signs the token
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(_jwt.ExpiryDays),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
