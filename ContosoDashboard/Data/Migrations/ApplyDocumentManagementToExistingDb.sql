SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'[Document]', N'U') IS NULL
BEGIN
    CREATE TABLE [Document] (
        [DocumentId] int NOT NULL IDENTITY,
        [Title] nvarchar(500) NOT NULL,
        [Description] nvarchar(2000) NOT NULL,
        [Category] nvarchar(50) NOT NULL,
        [FilePath] nvarchar(500) NOT NULL,
        [FileName] nvarchar(255) NOT NULL,
        [FileSizeBytes] bigint NOT NULL,
        [MimeType] nvarchar(100) NOT NULL,
        [UploadedAt] datetime2 NOT NULL,
        [UploadedBy] int NOT NULL,
        [AssociatedProjectId] int NULL,
        [IsArchived] bit NOT NULL,
        [ArchivedAt] datetime2 NULL,
        [AntivirusStatus] nvarchar(50) NOT NULL,
        [AntivirusCheckedAt] datetime2 NULL,
        [Tags] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NOT NULL,
        [ModifiedBy] int NULL,
        CONSTRAINT [PK_Document] PRIMARY KEY ([DocumentId]),
        CONSTRAINT [FK_Document_Projects_AssociatedProjectId] FOREIGN KEY ([AssociatedProjectId]) REFERENCES [Projects] ([ProjectId]) ON DELETE SET NULL,
        CONSTRAINT [FK_Document_Users_ModifiedBy] FOREIGN KEY ([ModifiedBy]) REFERENCES [Users] ([UserId]),
        CONSTRAINT [FK_Document_Users_UploadedBy] FOREIGN KEY ([UploadedBy]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
    );

    CREATE INDEX [IX_Document_AssociatedProjectId_IsArchived] ON [Document] ([AssociatedProjectId], [IsArchived]);
    CREATE INDEX [IX_Document_Category_UploadedAt] ON [Document] ([Category], [UploadedAt]);
    CREATE INDEX [IX_Document_ModifiedBy] ON [Document] ([ModifiedBy]);
    CREATE INDEX [IX_Document_UploadedBy_UploadedAt] ON [Document] ([UploadedBy], [UploadedAt]);
END;

IF OBJECT_ID(N'[DocumentAuditLog]', N'U') IS NULL
BEGIN
    CREATE TABLE [DocumentAuditLog] (
        [AuditId] bigint NOT NULL IDENTITY,
        [DocumentId] int NOT NULL,
        [UserId] int NOT NULL,
        [Action] nvarchar(50) NOT NULL,
        [Details] nvarchar(500) NOT NULL,
        [IpAddress] nvarchar(50) NOT NULL,
        [UserAgent] nvarchar(500) NOT NULL,
        [Timestamp] datetime2 NOT NULL,
        CONSTRAINT [PK_DocumentAuditLog] PRIMARY KEY ([AuditId]),
        CONSTRAINT [FK_DocumentAuditLog_Document_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [Document] ([DocumentId]) ON DELETE CASCADE,
        CONSTRAINT [FK_DocumentAuditLog_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_DocumentAuditLog_Action_Timestamp] ON [DocumentAuditLog] ([Action], [Timestamp] DESC);
    CREATE INDEX [IX_DocumentAuditLog_DocumentId_Timestamp] ON [DocumentAuditLog] ([DocumentId], [Timestamp] DESC);
    CREATE INDEX [IX_DocumentAuditLog_UserId_Timestamp] ON [DocumentAuditLog] ([UserId], [Timestamp] DESC);
END;

IF OBJECT_ID(N'[DocumentShare]', N'U') IS NULL
BEGIN
    CREATE TABLE [DocumentShare] (
        [DocumentShareId] int NOT NULL IDENTITY,
        [DocumentId] int NOT NULL,
        [SharedBy] int NOT NULL,
        [SharedWith] int NOT NULL,
        [SharedAt] datetime2 NOT NULL,
        [RevokedAt] datetime2 NULL,
        [NotificationSent] bit NOT NULL,
        [NotificationId] int NULL,
        CONSTRAINT [PK_DocumentShare] PRIMARY KEY ([DocumentShareId]),
        CONSTRAINT [FK_DocumentShare_Document_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [Document] ([DocumentId]) ON DELETE CASCADE,
        CONSTRAINT [FK_DocumentShare_Users_SharedBy] FOREIGN KEY ([SharedBy]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DocumentShare_Users_SharedWith] FOREIGN KEY ([SharedWith]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
    );

    CREATE INDEX [IX_DocumentShare_DocumentId_RevokedAt] ON [DocumentShare] ([DocumentId], [RevokedAt]);
    CREATE UNIQUE INDEX [IX_DocumentShare_DocumentId_SharedWith_RevokedAt] ON [DocumentShare] ([DocumentId], [SharedWith], [RevokedAt]) WHERE [RevokedAt] IS NULL;
    CREATE INDEX [IX_DocumentShare_SharedBy_SharedAt] ON [DocumentShare] ([SharedBy], [SharedAt]);
    CREATE INDEX [IX_DocumentShare_SharedWith_RevokedAt] ON [DocumentShare] ([SharedWith], [RevokedAt]);
END;

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260511011959_AddDocumentManagement')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260511011959_AddDocumentManagement', N'8.0.0');
END;

IF OBJECT_ID(N'[Document]', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Document_AssociatedProjectId_IsArchived' AND object_id = OBJECT_ID(N'[Document]'))
        CREATE INDEX [IX_Document_AssociatedProjectId_IsArchived] ON [Document] ([AssociatedProjectId], [IsArchived]);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Document_Category_UploadedAt' AND object_id = OBJECT_ID(N'[Document]'))
        CREATE INDEX [IX_Document_Category_UploadedAt] ON [Document] ([Category], [UploadedAt]);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Document_ModifiedBy' AND object_id = OBJECT_ID(N'[Document]'))
        CREATE INDEX [IX_Document_ModifiedBy] ON [Document] ([ModifiedBy]);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Document_UploadedBy_UploadedAt' AND object_id = OBJECT_ID(N'[Document]'))
        CREATE INDEX [IX_Document_UploadedBy_UploadedAt] ON [Document] ([UploadedBy], [UploadedAt]);
END;

IF OBJECT_ID(N'[DocumentAuditLog]', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DocumentAuditLog_Action_Timestamp' AND object_id = OBJECT_ID(N'[DocumentAuditLog]'))
        CREATE INDEX [IX_DocumentAuditLog_Action_Timestamp] ON [DocumentAuditLog] ([Action], [Timestamp] DESC);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DocumentAuditLog_DocumentId_Timestamp' AND object_id = OBJECT_ID(N'[DocumentAuditLog]'))
        CREATE INDEX [IX_DocumentAuditLog_DocumentId_Timestamp] ON [DocumentAuditLog] ([DocumentId], [Timestamp] DESC);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DocumentAuditLog_UserId_Timestamp' AND object_id = OBJECT_ID(N'[DocumentAuditLog]'))
        CREATE INDEX [IX_DocumentAuditLog_UserId_Timestamp] ON [DocumentAuditLog] ([UserId], [Timestamp] DESC);
END;

IF OBJECT_ID(N'[DocumentShare]', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DocumentShare_DocumentId_RevokedAt' AND object_id = OBJECT_ID(N'[DocumentShare]'))
        CREATE INDEX [IX_DocumentShare_DocumentId_RevokedAt] ON [DocumentShare] ([DocumentId], [RevokedAt]);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DocumentShare_DocumentId_SharedWith_RevokedAt' AND object_id = OBJECT_ID(N'[DocumentShare]'))
        CREATE UNIQUE INDEX [IX_DocumentShare_DocumentId_SharedWith_RevokedAt] ON [DocumentShare] ([DocumentId], [SharedWith], [RevokedAt]) WHERE [RevokedAt] IS NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DocumentShare_SharedBy_SharedAt' AND object_id = OBJECT_ID(N'[DocumentShare]'))
        CREATE INDEX [IX_DocumentShare_SharedBy_SharedAt] ON [DocumentShare] ([SharedBy], [SharedAt]);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DocumentShare_SharedWith_RevokedAt' AND object_id = OBJECT_ID(N'[DocumentShare]'))
        CREATE INDEX [IX_DocumentShare_SharedWith_RevokedAt] ON [DocumentShare] ([SharedWith], [RevokedAt]);
END;
