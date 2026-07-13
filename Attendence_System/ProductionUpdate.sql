-- إغلاق أي معاملة سابقة معلقة بسبب الخطأ
IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
GO

BEGIN TRANSACTION;
GO

-- 1. إضافة الأعمدة فقط إذا لم تكن موجودة
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'TenantId' AND Object_ID = Object_ID(N'StudentLectures'))
BEGIN
    ALTER TABLE [StudentLectures] ADD [TenantId] nvarchar(450) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'AttendedCount' AND Object_ID = Object_ID(N'Lectures'))
BEGIN
    ALTER TABLE [Lectures] ADD [AttendedCount] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'TenantId' AND Object_ID = Object_ID(N'Lectures'))
BEGIN
    ALTER TABLE [Lectures] ADD [TenantId] nvarchar(450) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'StudentCount' AND Object_ID = Object_ID(N'Grades'))
BEGIN
    ALTER TABLE [Grades] ADD [StudentCount] int NOT NULL DEFAULT 0;
END;
GO

-- 2. تحديث البيانات والعدادات
UPDATE l
SET l.TenantId = c.TenantId
FROM Lectures l
INNER JOIN Courses c ON l.CourseId = c.CourseId;

UPDATE sl
SET sl.TenantId = s.TenantId
FROM StudentLectures sl
INNER JOIN Students s ON sl.StudentId = s.StudentId;

UPDATE l
SET l.AttendedCount = (SELECT COUNT(*) FROM StudentLectures sl WHERE sl.LectureId = l.LectureId)
FROM Lectures l;

UPDATE g
SET g.StudentCount = (SELECT COUNT(*) FROM Students s WHERE s.GradeId = g.GradeId)
FROM Grades g;

-- 3. صمام الأمان
IF EXISTS (SELECT 1 FROM Lectures WHERE TenantId = '') OR EXISTS (SELECT 1 FROM StudentLectures WHERE TenantId = '')
BEGIN
    RAISERROR(N'Migration aborted: Orphaned Lectures or StudentLectures found without a valid TenantId. Please review the data manually.', 16, 1);
END
GO

-- 4. إضافة الفهارس فقط إذا لم تكن موجودة
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StudentLectures_TenantId' AND object_id = OBJECT_ID('StudentLectures'))
BEGIN
    CREATE INDEX [IX_StudentLectures_TenantId] ON [StudentLectures] ([TenantId]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Lectures_TenantId' AND object_id = OBJECT_ID('Lectures'))
BEGIN
    CREATE INDEX [IX_Lectures_TenantId] ON [Lectures] ([TenantId]);
END;
GO

-- 5. إضافة القيود (تم حل مشكلة المسارات المتعددة هنا)
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Lectures_Tenants_TenantId')
BEGIN
    ALTER TABLE [Lectures] ADD CONSTRAINT [FK_Lectures_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_StudentLectures_Tenants_TenantId')
BEGIN
    -- تم تغيير CASCADE إلى NO ACTION لحل مشكلة Multiple Cascade Paths
    ALTER TABLE [StudentLectures] ADD CONSTRAINT [FK_StudentLectures_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;
GO

-- 6. تسجيل التحديث في جدول الـ EF
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260703024241_AddTenantIdToLectures')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260703024241_AddTenantIdToLectures', N'8.0.11');
END;
GO

COMMIT;
GO

-- =========================================================================
-- MIGRATION: MakeJunctionTablesTenantAware (التحديث المعماري للكمال 100%)
-- =========================================================================
BEGIN TRANSACTION;
GO

-- 1. إضافة عمود TenantId لجداول الوصل (Junction Tables) إذا لم يكن موجوداً
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'TenantId' AND Object_ID = Object_ID(N'CourseGrades'))
BEGIN
    ALTER TABLE [CourseGrades] ADD [TenantId] nvarchar(450) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'TenantId' AND Object_ID = Object_ID(N'LectureGrades'))
BEGIN
    ALTER TABLE [LectureGrades] ADD [TenantId] nvarchar(450) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'TenantId' AND Object_ID = Object_ID(N'StudentExams'))
BEGIN
    ALTER TABLE [StudentExams] ADD [TenantId] nvarchar(450) NOT NULL DEFAULT N'';
END;
GO

-- 2. تحديث البيانات السابقة بأمان
UPDATE cg
SET cg.TenantId = c.TenantId
FROM CourseGrades cg
INNER JOIN Courses c ON cg.CourseId = c.CourseId;

UPDATE lg
SET lg.TenantId = l.TenantId
FROM LectureGrades lg
INNER JOIN Lectures l ON lg.LectureId = l.LectureId;

UPDATE se
SET se.TenantId = s.TenantId
FROM StudentExams se
INNER JOIN Students s ON se.StudentId = s.StudentId;

-- 3. صمام الأمان لجداول الوصل
IF EXISTS (SELECT 1 FROM CourseGrades WHERE TenantId = '') OR EXISTS (SELECT 1 FROM LectureGrades WHERE TenantId = '') OR EXISTS (SELECT 1 FROM StudentExams WHERE TenantId = '')
BEGIN
    RAISERROR(N'Migration aborted: Orphaned records found in Junction Tables (CourseGrades, LectureGrades, StudentExams). Please review the data manually.', 16, 1);
END
GO

-- 4. الفهارس والقيود (ON DELETE NO ACTION)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CourseGrades_TenantId' AND object_id = OBJECT_ID('CourseGrades'))
BEGIN
    CREATE INDEX [IX_CourseGrades_TenantId] ON [CourseGrades] ([TenantId]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LectureGrades_TenantId' AND object_id = OBJECT_ID('LectureGrades'))
BEGIN
    CREATE INDEX [IX_LectureGrades_TenantId] ON [LectureGrades] ([TenantId]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StudentExams_TenantId' AND object_id = OBJECT_ID('StudentExams'))
BEGIN
    CREATE INDEX [IX_StudentExams_TenantId] ON [StudentExams] ([TenantId]);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_CourseGrades_Tenants_TenantId')
BEGIN
    ALTER TABLE [CourseGrades] ADD CONSTRAINT [FK_CourseGrades_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_LectureGrades_Tenants_TenantId')
BEGIN
    ALTER TABLE [LectureGrades] ADD CONSTRAINT [FK_LectureGrades_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_StudentExams_Tenants_TenantId')
BEGIN
    ALTER TABLE [StudentExams] ADD CONSTRAINT [FK_StudentExams_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
END;
GO

-- 5. تسجيل الـ Migration
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260703031234_MakeJunctionTablesTenantAware')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260703031234_MakeJunctionTablesTenantAware', N'8.0.11');
END;
GO

COMMIT;
GO
