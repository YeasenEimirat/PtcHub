-- ============================================================
--  ترقية v12 — نظام موافقة ملفات الطلاب
--
--  الطالب بيضيف ملف → بينزل pending → المسؤول بيوافق أو يرفض
--  ملفات الطاقم بتنزل approved مباشرة (زي قبل)
-- ============================================================
USE PTCHub;
GO

-- ===== 1) Status: حالة الملف =====
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.CourseFiles') AND name = 'Status'
)
BEGIN
    -- الافتراضي approved حتى كل الملفات الموجودة تضل ظاهرة
    ALTER TABLE dbo.CourseFiles
        ADD [Status] NVARCHAR(10) NOT NULL DEFAULT 'approved';
    PRINT 'تمت إضافة Status.';
END
ELSE
    PRINT 'Status موجود — تم التخطي.';
GO

-- ===== 2) قيد: approved / pending / rejected =====
IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints WHERE name = 'CK_CourseFiles_Status'
)
BEGIN
    ALTER TABLE dbo.CourseFiles
        ADD CONSTRAINT CK_CourseFiles_Status
        CHECK ([Status] IN ('approved', 'pending', 'rejected'));
    PRINT 'تمت إضافة قيد Status.';
END
GO

-- ===== 3) SubmitterName: اسم اللي ضاف الملف =====
-- منخزّنه هون بدل JOIN حتى الاستعلامات تضل بسيطة
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.CourseFiles') AND name = 'SubmitterName'
)
BEGIN
    ALTER TABLE dbo.CourseFiles ADD SubmitterName NVARCHAR(150) NULL;
    PRINT 'تمت إضافة SubmitterName.';
END
ELSE
    PRINT 'SubmitterName موجود — تم التخطي.';
GO

-- ===== 4) ReviewedBy: مين وافق أو رفض =====
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.CourseFiles') AND name = 'ReviewedBy'
)
BEGIN
    ALTER TABLE dbo.CourseFiles ADD ReviewedBy UNIQUEIDENTIFIER NULL;
    PRINT 'تمت إضافة ReviewedBy.';
END
ELSE
    PRINT 'ReviewedBy موجود — تم التخطي.';
GO

-- ===== 5) ReviewedAt =====
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.CourseFiles') AND name = 'ReviewedAt'
)
BEGIN
    ALTER TABLE dbo.CourseFiles ADD ReviewedAt DATETIME2 NULL;
    PRINT 'تمت إضافة ReviewedAt.';
END
ELSE
    PRINT 'ReviewedAt موجود — تم التخطي.';
GO

-- ===== 6) فهرس على Status للفلترة =====
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.CourseFiles') AND name = 'IX_CourseFiles_Status'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_CourseFiles_Status
        ON dbo.CourseFiles ([Status])
        INCLUDE (CourseCode, Title, CreatedBy, CreatedAt);
    PRINT 'تمت إضافة فهرس Status.';
END
GO

-- ===== 7) فحص =====
SELECT  Id, CourseCode, Title, [Status], SubmitterName, CreatedAt
FROM    dbo.CourseFiles
ORDER BY Id;
GO
