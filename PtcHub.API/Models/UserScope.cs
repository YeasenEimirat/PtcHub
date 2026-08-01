namespace PtcHub.API.Models
{
    // ============================================================
    //  UserScope — "مين هذا المستخدم وشو مسموح له يلمس؟"
    //  منقرأه من القاعدة مع كل طلب حسّاس، مش من التوكن.
    //  ليش؟ لأن التوكن عمره أيام؛ لو الأدمن غيّر نطاق مشرف اليوم
    //  بدنا التغيير يسري فوراً، مش بعد ما ينتهي توكنه.
    // ============================================================
    public class UserScope
    {
        public Guid UserId { get; set; }
        public string Role { get; set; } = "student";   // student / supervisor / admin
        public byte? Year { get; set; }                 // سنة الطالب نفسه (1..4)
        public byte? ScopeYear { get; set; }            // نطاق مسؤولية الطاقم (1..4)، null = كل السنوات

        // طاقم = مشرف أو أدمن
        public bool IsStaff => Role == "admin" || Role == "supervisor";

        // الأدمن العام: أدمن بلا نطاق → مسؤول عن كل السنوات
        public bool IsSuperAdmin => Role == "admin" && !ScopeYear.HasValue;

        // طاقم مربوط بسنة واحدة (أدمن سنة أو مشرف سنة)
        public bool IsScopedStaff => IsStaff && ScopeYear.HasValue;

        // السنة اللي بتحدّد شو بيشوفه هذا المستخدم:
        // للطاقم المحدود → نطاقه، وللطالب → سنته
        public byte? ViewYear => IsStaff ? ScopeYear : Year;

        // 5 = خريج. اخترناها رقماً بدل عمود جديد حتى تمرّ بنفس الفلترة:
        // الخريج بيصير خارج كل سنوات الخطة تلقائياً، وبيشوف الإعلانات العامة بس.
        public const byte GraduatedYear = 5;

        public static string YearName(byte? year)
        {
            return year switch
            {
                1 => "السنة الأولى",
                2 => "السنة الثانية",
                3 => "السنة الثالثة",
                4 => "السنة الرابعة",
                GraduatedYear => "الخريجين",
                _ => "كل السنوات"
            };
        }
    }
}
