namespace PtcHub.API.BLL.Services
{
    // JWT settings — read from the "Jwt" section in appsettings.json
    public class JwtSettings
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = "PtcHub";
        public string Audience { get; set; } = "PtcHubClient";
        public int ExpiryDays { get; set; } = 14;
    }
}