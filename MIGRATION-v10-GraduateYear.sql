-- ============================================================
--  ترقية v10 — توسيع قيد السنة ليقبل الخرّيج
--
--  القيد الحالي: ([Year] IS NULL OR [Year] >= 1 AND [Year] <= 4)
--  الجديد:       نفس الإشي بس لحد 5
--
--  ليش 5؟ الخرّيج. اخترناه رقماً بدل عمود منفصل حتى يمرّ بنفس
--  فلترة السنوات الموجودة: الخرّيج بيصير خارج كل سنوات الخطة
--  تلقائياً، وبيشوف الإعلانات العامة بس (لأن نموذج الإعلانات
--  ما بيقبل تنشر لسنة 5 أصلاً).
--
--  شغّله مرة وحدة قبل ما تستعمل النقل الجماعي بـ v10.
-- ============================================================
USE PTCHub;
GO

-- ===== 1) الوضع قبل التعديل =====
SELECT  name AS ConstraintName,
        definition AS CurrentDefinition
FROM    sys.check_constraints
WHERE   parent_object_id = OBJECT_ID('dbo.Profiles')
  AND   name = 'CK_Profiles_Year';
GO

-- ===== 2) تأكّد إنّ ما في بيانات مخالفة (لازم يرجّع 0) =====
SELECT COUNT(*) AS RowsOutsideRange
FROM   dbo.Profiles
WHERE  [Year] IS NOT NULL AND ([Year] < 1 OR [Year] > 5);
GO

-- ===== 3) استبدال القيد =====
-- ما في ALTER CONSTRAINT بـ SQL Server، فلازم DROP ثم ADD.
-- العملية لحظية على جدول بهذا الحجم، وما بتلمس ولا صف بيانات.
IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_Profiles_Year'
      AND parent_object_id = OBJECT_ID('dbo.Profiles')
)
BEGIN
    ALTER TABLE dbo.Profiles DROP CONSTRAINT CK_Profiles_Year;
    PRINT 'تم حذف القيد القديم.';
END
GO

ALTER TABLE dbo.Profiles
    ADD CONSTRAINT CK_Profiles_Year
    CHECK ([Year] IS NULL OR ([Year] >= 1 AND [Year] <= 5));
GO

PRINT 'تم إنشاء القيد الجديد (1..5).';
GO

-- ===== 4) تأكيد =====
SELECT  name AS ConstraintName,
        definition AS NewDefinition,
        is_disabled AS IsDisabled,      -- لازم 0
        is_not_trusted AS IsNotTrusted  -- لازم 0
FROM    sys.check_constraints
WHERE   parent_object_id = OBJECT_ID('dbo.Profiles')
  AND   name = 'CK_Profiles_Year';
GO

-- ===== 5) فحص سريع: لازم يفشل =====
-- شيل التعليق وجرّبه إذا بدك تتأكد إنّ القيد فعّال:
-- UPDATE dbo.Profiles SET [Year] = 9 WHERE Email = 'test@ptc.edu';
-- المتوقّع: The UPDATE statement conflicted with the CHECK constraint

-- ===== 6) توزيع الطلاب على السنوات بعد التعديل =====
SELECT  CASE [Year]
            WHEN 1 THEN N'الأولى'
            WHEN 2 THEN N'الثانية'
            WHEN 3 THEN N'الثالثة'
            WHEN 4 THEN N'الرابعة'
            WHEN 5 THEN N'خرّيج'
            ELSE N'بلا سنة'
        END AS السنة,
        COUNT(*) AS العدد
FROM    dbo.Profiles
GROUP BY [Year]
ORDER BY [Year];
GO
