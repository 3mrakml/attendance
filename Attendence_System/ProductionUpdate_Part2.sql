BEGIN TRANSACTION;
GO

ALTER TABLE [StudentLectures] DROP CONSTRAINT [FK_StudentLectures_Tenants_TenantId];
GO

ALTER TABLE [StudentExams] ADD [TenantId] nvarchar(450) NOT NULL DEFAULT N'';
GO

ALTER TABLE [LectureGrades] ADD [TenantId] nvarchar(450) NOT NULL DEFAULT N'';
GO

ALTER TABLE [CourseGrades] ADD [TenantId] nvarchar(450) NOT NULL DEFAULT N'';
GO

CREATE INDEX [IX_StudentExams_TenantId] ON [StudentExams] ([TenantId]);
GO

CREATE INDEX [IX_LectureGrades_TenantId] ON [LectureGrades] ([TenantId]);
GO

CREATE INDEX [IX_CourseGrades_TenantId] ON [CourseGrades] ([TenantId]);
GO

ALTER TABLE [CourseGrades] ADD CONSTRAINT [FK_CourseGrades_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
GO

ALTER TABLE [LectureGrades] ADD CONSTRAINT [FK_LectureGrades_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
GO

ALTER TABLE [StudentExams] ADD CONSTRAINT [FK_StudentExams_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
GO

ALTER TABLE [StudentLectures] ADD CONSTRAINT [FK_StudentLectures_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260703031234_MakeJunctionTablesTenantAware', N'8.0.11');
GO

COMMIT;
GO

