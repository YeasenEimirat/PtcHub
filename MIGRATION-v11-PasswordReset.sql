-- ============================================================
--  ترقية v11 — كلمة السر وإبطال الجلسات
--  شغّله مرة وحدة على SSMS قبل ما تشغّل النسخة الجديدة
-- ============================================================
USE PTCHub;
GO

-- ===== 1) MustChangePassword — إجبار تغيير كلمة السر =====
-- لما الأدمن يصفّر كلمة سر طالب، بيتفعّل هذا العلم.
-- الطالب لازم يغيّرها أول دخول قبل ما يستعمل النظام.
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Profiles') AND name = 'MustChangePassword'
)
BEGIN
    ALTER TABLE dbo.Profiles ADD MustChangePassword BIT NOT NULL DEFAULT 0;
    PRINT 'تمت إضافة MustChangePassword.';
END
ELSE
    PRINT 'MustChangePassword موجود — تم التخطي.';
GO

-- ===== 2) TokenVersion — إبطال التوكنات القديمة =====
-- بيزيد واحد كل ما تتغيّر كلمة السر (تصفير أو تغيير).
-- لو حطّيناه بالتوكن وقارناه عند التحقّق، كل الجلسات القديمة بتنرفض.
-- ملاحظة: مش مفعّل بالتحقّق بعد — بس العمود جاهز والقيمة بتزيد.
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Profiles') AND name = 'TokenVersion'
)
BEGIN
    ALTER TABLE dbo.Profiles ADD TokenVersion INT NOT NULL DEFAULT 0;
    PRINT 'تمت إضافة TokenVersion.';
END
ELSE
    PRINT 'TokenVersion موجود — تم التخطي.';
GO

-- ===== 3) فحص =====
SELECT  FullName,
        Email,
        Role,
        MustChangePassword,
        TokenVersion
FROM    dbo.Profiles
ORDER BY Role, Email;
GO
