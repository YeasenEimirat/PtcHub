-- ============================================================
--  فحص تطابق أسماء الأعمدة مع اللي مكتوب بالكود
--  شغّله على SSMS وابعتلي المخرجات كاملة
-- ============================================================
USE PTCHub;

SELECT  t.name AS TableName,
        c.name AS ColumnName,
        ty.name AS DataType,
        c.is_nullable AS IsNullable
FROM    sys.tables t
JOIN    sys.columns c  ON c.object_id = t.object_id
JOIN    sys.types  ty  ON ty.user_type_id = c.user_type_id
WHERE   t.name IN ('Profiles','MyCourses','CourseProgress','CourseFiles','Announcements')
ORDER BY t.name, c.column_id;

-- كم إعلان موجود فعلياً وإيش قيمة Year عند كل واحد
SELECT Id, Title, Active, [Year], CreatedAt FROM Announcements ORDER BY Id;

-- كم ملف موجود
SELECT COUNT(*) AS FilesCount FROM CourseFiles;

-- كم مستخدم وإيش رتبته
SELECT Id, FullName, Email, [Year], Role FROM Profiles;
