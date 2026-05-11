# Instrucciones de Migración EF Core para Gestión de Documentos

**Tarea**: T018 - Crear una migración EF Core que agregue las tablas de documentos, comparticiones y auditoría.

## Opción 1: Usando dotnet ef (Recomendado)

Ejecute el siguiente comando en el directorio raíz del proyecto:

```bash
cd ContosoDashboard
dotnet ef migrations add AddDocumentManagement -o Data/Migrations
```

Esto creará un archivo de migración en `ContosoDashboard/Data/Migrations/` con nombre similar a `20260510120000_AddDocumentManagement.cs`.

Luego, aplique la migración:

```bash
dotnet ef database update
```

## Opción 2: SQL directo (Alternativa para LocalDB)

Si prefiere ejecutar SQL directamente en SQL Server LocalDB, use este script:

```sql
-- Crear tabla Document
CREATE TABLE [Document] (
    [DocumentId] int NOT NULL IDENTITY,
    [Title] nvarchar(500) NOT NULL,
    [Description] nvarchar(2000),
    [Category] nvarchar(50) NOT NULL,
    [FilePath] nvarchar(500) NOT NULL UNIQUE,
    [FileName] nvarchar(255),
    [FileSizeBytes] bigint NOT NULL,
    [MimeType] nvarchar(100),
    [UploadedAt] datetime2 NOT NULL,
    [UploadedBy] int NOT NULL,
    [AssociatedProjectId] int,
    [IsArchived] bit NOT NULL DEFAULT 0,
    [ArchivedAt] datetime2,
    [AntivirusStatus] nvarchar(50) DEFAULT 'Pending',
    [AntivirusCheckedAt] datetime2,
    [Tags] nvarchar(max),
    [CreatedAt] datetime2 NOT NULL,
    [ModifiedAt] datetime2 NOT NULL,
    [ModifiedBy] int,
    PRIMARY KEY ([DocumentId]),
    FOREIGN KEY ([UploadedBy]) REFERENCES [User]([UserId]) ON DELETE RESTRICT,
    FOREIGN KEY ([AssociatedProjectId]) REFERENCES [Project]([ProjectId]) ON DELETE SET NULL,
    FOREIGN KEY ([ModifiedBy]) REFERENCES [User]([UserId])
);

-- Crear índices en Document
CREATE INDEX [IX_Document_UploadedBy_UploadedAt] ON [Document]([UploadedBy], [UploadedAt]);
CREATE INDEX [IX_Document_AssociatedProjectId_IsArchived] ON [Document]([AssociatedProjectId], [IsArchived]);
CREATE INDEX [IX_Document_Category_UploadedAt] ON [Document]([Category], [UploadedAt]);

-- Crear tabla DocumentShare
CREATE TABLE [DocumentShare] (
    [DocumentShareId] int NOT NULL IDENTITY,
    [DocumentId] int NOT NULL,
    [SharedBy] int NOT NULL,
    [SharedWith] int NOT NULL,
    [SharedAt] datetime2 NOT NULL,
    [RevokedAt] datetime2,
    [NotificationSent] bit NOT NULL DEFAULT 0,
    [NotificationId] int,
    PRIMARY KEY ([DocumentShareId]),
    FOREIGN KEY ([DocumentId]) REFERENCES [Document]([DocumentId]) ON DELETE CASCADE,
    FOREIGN KEY ([SharedBy]) REFERENCES [User]([UserId]) ON DELETE RESTRICT,
    FOREIGN KEY ([SharedWith]) REFERENCES [User]([UserId]) ON DELETE RESTRICT
);

-- Crear índices únicos en DocumentShare (para comparticiones activas)
CREATE UNIQUE INDEX [IX_DocumentShare_DocumentId_SharedWith_RevokedAt]
    ON [DocumentShare]([DocumentId], [SharedWith], [RevokedAt])
    WHERE [RevokedAt] IS NULL;

-- Crear índices de performance en DocumentShare
CREATE INDEX [IX_DocumentShare_SharedWith_RevokedAt] ON [DocumentShare]([SharedWith], [RevokedAt]);
CREATE INDEX [IX_DocumentShare_DocumentId_RevokedAt] ON [DocumentShare]([DocumentId], [RevokedAt]);
CREATE INDEX [IX_DocumentShare_SharedBy_SharedAt] ON [DocumentShare]([SharedBy], [SharedAt]);

-- Crear tabla DocumentAuditLog
CREATE TABLE [DocumentAuditLog] (
    [AuditId] bigint NOT NULL IDENTITY,
    [DocumentId] int NOT NULL,
    [UserId] int NOT NULL,
    [Action] nvarchar(50) NOT NULL,
    [Details] nvarchar(500),
    [IpAddress] nvarchar(50),
    [UserAgent] nvarchar(500),
    [Timestamp] datetime2 NOT NULL,
    PRIMARY KEY ([AuditId]),
    FOREIGN KEY ([DocumentId]) REFERENCES [Document]([DocumentId]) ON DELETE CASCADE,
    FOREIGN KEY ([UserId]) REFERENCES [User]([UserId])
);

-- Crear índices de performance en DocumentAuditLog
CREATE INDEX [IX_DocumentAuditLog_DocumentId_Timestamp] ON [DocumentAuditLog]([DocumentId], [Timestamp] DESC);
CREATE INDEX [IX_DocumentAuditLog_UserId_Timestamp] ON [DocumentAuditLog]([UserId], [Timestamp] DESC);
CREATE INDEX [IX_DocumentAuditLog_Action_Timestamp] ON [DocumentAuditLog]([Action], [Timestamp] DESC);
```

## Verificación

Después de aplicar la migración, verifique que las tablas se crearon correctamente:

```sql
SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME IN ('Document', 'DocumentShare', 'DocumentAuditLog')
ORDER BY TABLE_NAME;
```

Debería ver 3 filas indicando que las tablas existen.

## Pasos siguientes

1. Ejecute la aplicación: `dotnet run`
2. Verifique que no hay errores de migración
3. Pruebe la funcionalidad de carga de documentos
