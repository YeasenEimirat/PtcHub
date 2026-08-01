-- ============================================================
--  ترقية v8 — نطاق مسؤولية الطاقم حسب السنة
--  شغّله مرة وحدة على SSMS قبل ما تشغّل النسخة الجديدة
--  آمن للتكرار: لو شغّلته مرتين ما بيصير شي
-- ============================================================
USE PTCHub;
GO

-- ===== 1) العمود الجديد =====
-- ScopeYear = السنة اللي هذا المسؤول مسؤول عنها.
-- NULL = مسؤول عن كل السنوات (الأدمن العام). عند الطالب دايماً NULL.
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Profiles') AND name = 'ScopeYear'
)
BEGIN
    ALTER TABLE dbo.Profiles ADD ScopeYear TINYINT NULL;
    PRINT 'تمت إضافة العمود ScopeYear.';
END
ELSE
    PRINT 'العمود ScopeYear موجود مسبقاً — تم التخطي.';
GO

-- ===== 2) قيد التحقّق: 1..4 أو NULL =====
IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Profiles_ScopeYear'
)
BEGIN
    ALTER TABLE dbo.Profiles
        ADD CONSTRAINT CK_Profiles_ScopeYear
        CHECK (ScopeYear IS NULL OR ScopeYear BETWEEN 1 AND 4);
    PRINT 'تمت إضافة القيد CK_Profiles_ScopeYear.';
END
GO

-- ===== 3) تنضيف: الطالب ما إله نطاق إشراف =====
UPDATE dbo.Profiles
   SET ScopeYear = NULL
 WHERE Role = 'student' AND ScopeYear IS NOT NULL;
GO

-- ============================================================
--  فحص بعد الترقية — لازم يطلع عندك أدمن عام واحد على الأقل
--  (Role = 'admin' و ScopeYear = NULL)
-- ============================================================
SELECT  FullName,
        Email,
        [Year]      AS StudentYear,
        Role,
        ScopeYear,
        CASE
            WHEN Role = 'admin' AND ScopeYear IS NULL THEN N'أدمن عام — كل السنوات'
            WHEN Role = 'admin'                       THEN N'أدمن سنة ' + CAST(ScopeYear AS NVARCHAR(2))
            WHEN Role = 'supervisor'                  THEN N'مشرف سنة ' + CAST(ISNULL(ScopeYear, 0) AS NVARCHAR(2))
            ELSE N'طالب'
        END AS الوصف
FROM    dbo.Profiles
ORDER BY Role, ScopeYear;
GO

-- ============================================================
--  مثال يدوي: تحويل حساب لأدمن مسؤول عن السنة الثانية
--  (نفس الشي بتعمله من لوحة التحكم، بس هاي للطوارئ)
-- ============================================================
-- UPDATE dbo.Profiles
--    SET Role = 'admin', ScopeYear = 2, UpdatedAt = SYSUTCDATETIME()
--  WHERE Email = 'someone@gmail.com';
