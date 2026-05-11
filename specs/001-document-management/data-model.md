# Data Model: Document Management

**Feature**: Carga y Gestión de Documentos  
**Version**: 1.0.0  
**Date**: 2026-05-10  
**Status**: Design Phase 1

---

## Overview

The document management feature requires four new database entities:
1. **Document** — Core document metadata and file reference
2. **DocumentShare** — Explicit sharing relationships between users
3. **DocumentAuditLog** — Audit trail for compliance
4. **DocumentTag** — Optional: searchable tags (vs. JSON array)

This design integrates with existing User, Project, and Notification entities.

---

## Entity Definitions

### 1. Document

Represents a single uploaded file with its metadata and access control.

| Field | Type | Constraints | Notes |
|-------|------|-----------|-------|
| DocumentId | int (PK) | Primary key, auto-increment | Unique identifier |
| Title | string(500) | Required, not null | User-provided document name |
| Description | string(2000) | Optional | Supports markdown for rich formatting (v2 feature) |
| Category | string(50) | Required, not null | Enum: ProjectDocs, TeamResources, PersonalFiles, Reports, Presentations, Other |
| FilePath | string(500) | Required, unique | GUID-based path: `AppData/uploads/{DocumentId}/{GUID}` |
| FileName | string(255) | Required | Original filename for download header (not used for security) |
| FileSizeBytes | long | Required, > 0 | Tracked for future quota management; max 25 MB |
| MimeType | string(100) | Required | e.g., "application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" |
| UploadedAt | DateTime | Required, not null | UTC timestamp of upload |
| UploadedBy | int (FK) | Required, not null, FK→User | References the User who uploaded; no cascade delete |
| AssociatedProjectId | int (FK) | Optional | FK→Project; nullable if document not project-specific |
| IsArchived | bool | Default: false | Set to true when associated project is deleted |
| ArchivedAt | DateTime | Optional | Timestamp of archival (for audit) |
| AntivirusStatus | string(50) | Default: "Pending" | States: Pending, Scanning, Clean, Infected, Failed |
| AntivirusCheckedAt | DateTime | Optional | Timestamp of last antivirus check |
| Tags | string (JSON) | Optional | JSON array: `["tag1", "tag2"]`; max 10 tags per doc |
| CreatedAt | DateTime | Required, not null | Insertion timestamp (= UploadedAt) |
| ModifiedAt | DateTime | Required | Last metadata edit timestamp (initially = CreatedAt) |
| ModifiedBy | int (FK) | Optional | User who last edited metadata |

**Primary Key**: DocumentId  
**Unique Constraints**: FilePath (prevents file collisions)  
**Indexes**:
- (UploadedBy, UploadedAt) — for "My Documents" queries
- (AssociatedProjectId, IsArchived) — for project document views
- (Category, UploadedAt) — for category filtering
- FullText search on Title, Description (SQL Server) — future optimization

**Relationships**:
- `Document.UploadedBy` → `User.UserId` (many-to-one; no cascade delete)
- `Document.AssociatedProjectId` → `Project.ProjectId` (many-to-optional-one; no cascade delete)
- `Document.DocumentShare` ← `DocumentShare.DocumentId` (one-to-many)
- `Document.AuditLog` ← `DocumentAuditLog.DocumentId` (one-to-many)

**Validation Rules**:
- Title: 1–500 characters, required, trimmed
- Description: 0–2000 characters, optional, trimmed
- Category: Must match enum values (case-sensitive)
- FileSizeBytes: > 0, ≤ 26,214,400 bytes (25 MB)
- FilePath: Must start with "AppData/uploads/", must be unique
- UploadedBy: Must reference active User (checked at insertion, not enforced via FK)

---

### 2. DocumentShare

Represents explicit sharing of a document from one user to another.

| Field | Type | Constraints | Notes |
|-------|------|-----------|-------|
| DocumentShareId | int (PK) | Primary key, auto-increment | Unique identifier |
| DocumentId | int (FK) | Required, not null, FK→Document | No cascade delete; shares persist even if doc archived |
| SharedBy | int (FK) | Required, not null, FK→User | Original document owner; typically = Document.UploadedBy |
| SharedWith | int (FK) | Required, not null, FK→User | Recipient user; cannot be same as SharedBy |
| SharedAt | DateTime | Required, not null | UTC timestamp of share |
| RevokedAt | DateTime | Optional, nullable | If set, share is inactive; used for soft-delete |
| NotificationSent | bool | Default: false | Track if in-app notification was sent |
| NotificationId | int (FK) | Optional, FK→Notification | Reference to in-app notification for audit |

**Primary Key**: DocumentShareId  
**Unique Constraints**: (DocumentId, SharedWith, RevokedAt) — prevents duplicate active shares  
**Indexes**:
- (SharedWith, RevokedAt) — for "Shared With Me" queries
- (DocumentId, RevokedAt) — for checking if document is shared
- (SharedBy, SharedAt) — for audit of user's sharing activity

**Relationships**:
- `DocumentShare.DocumentId` → `Document.DocumentId` (many-to-one)
- `DocumentShare.SharedBy` → `User.UserId` (many-to-one; no cascade)
- `DocumentShare.SharedWith` → `User.UserId` (many-to-one; no cascade)
- `DocumentShare.NotificationId` → `Notification.NotificationId` (optional)

**Validation Rules**:
- SharedBy ≠ SharedWith (cannot share with self)
- RevokedAt ≥ SharedAt if RevokedAt is set
- Only one active share per (DocumentId, SharedWith) pair
- SharedBy must be Document.UploadedBy (enforced in service layer)

**Access Control Logic**:
```
Can access document if:
  - User == Document.UploadedBy, OR
  - EXISTS DocumentShare WHERE DocumentId = doc.Id AND SharedWith = userId AND RevokedAt IS NULL
```

---

### 3. DocumentAuditLog

Immutable log of all document-related actions for compliance and security audit.

| Field | Type | Constraints | Notes |
|-------|------|-----------|-------|
| AuditId | long (PK) | Primary key, auto-increment | Unique identifier |
| DocumentId | int (FK) | Required, not null, FK→Document | No cascade delete; logs persist |
| UserId | int (FK) | Required, not null, FK→User | User who performed the action |
| Action | string(50) | Required, not null | Enum: Upload, Download, Share, Revoke, Edit, Delete, Archive |
| Details | string(500) | Optional | JSON or text describing action (e.g., "Shared with john.doe@contoso.com") |
| IpAddress | string(50) | Optional | Requester IP for security tracking |
| UserAgent | string(500) | Optional | Browser/client identifier |
| Timestamp | DateTime | Required, not null | UTC timestamp of action |

**Primary Key**: AuditId  
**Indexes**:
- (DocumentId, Timestamp DESC) — for document audit trail
- (UserId, Timestamp DESC) — for user activity audit
- (Action, Timestamp DESC) — for compliance reports

**Relationships**:
- `DocumentAuditLog.DocumentId` → `Document.DocumentId` (many-to-one)
- `DocumentAuditLog.UserId` → `User.UserId` (many-to-one)

**Immutability**: Audit logs are write-once; never deleted or modified (enforced in service layer).

---

### 4. DocumentTag (Optional: Alternative to JSON)

If document tags need advanced querying (tag cloud, filtering), use a separate entity instead of JSON array.

| Field | Type | Constraints | Notes |
|-------|------|-----------|-------|
| DocumentTagId | int (PK) | Primary key, auto-increment | |
| DocumentId | int (FK) | Required, not null | |
| Tag | string(50) | Required, not null | Unique per document |
| CreatedAt | DateTime | Required, not null | |

**Relationships**:
- `DocumentTag.DocumentId` → `Document.DocumentId` (many-to-one; cascade delete)

**Indexes**:
- (DocumentId) — for quick tag lookup
- (Tag) — for tag-based search

**Note**: For MVP, use JSON array in Document.Tags. Migrate to separate table if tag analytics needed.

---

## Relationships & Constraints Summary

### Relationship Diagram (ASCII)
```
User
├─ 1──→ M  Document (UploadedBy)
├─ 1──→ M  DocumentShare (SharedBy, SharedWith)
├─ 1──→ M  DocumentAuditLog (UserId)
└─ 1──→ M  Notification (related to document sharing)

Project
├─ 1──→ M  Document (AssociatedProjectId, nullable)

Document
├─ M──→ 1  User (UploadedBy)
├─ M──→ 1  Project (AssociatedProjectId, nullable)
├─ 1──→ M  DocumentShare
├─ 1──→ M  DocumentAuditLog
└─ 1──→ M  DocumentTag (if separate table)

DocumentShare
├─ M──→ 1  Document
├─ M──→ 1  User (SharedBy)
├─ M──→ 1  User (SharedWith)
└─ M──→ 1  Notification (optional)
```

### Cascade Delete Rules
- **User deleted**: Keep Document, DocumentShare, AuditLog (orphaned but preserved)
- **Project deleted**: Document.IsArchived = true (NOT cascade delete)
- **Document deleted**: Delete DocumentShare, AuditLog (with log entry: "Delete")
- **DocumentShare deleted**: Soft-delete (set RevokedAt; don't cascade)

### No Cascade to Prevent Data Loss
- User deactivation does NOT revoke shared access
- Project deletion does NOT delete documents
- This preserves audit trail and historical records

---

## Migration Strategy (EF Core)

### Initial Migration

```csharp
public partial class AddDocumentManagement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Document table
        migrationBuilder.CreateTable(
            name: "Document",
            columns: table => new
            {
                DocumentId = table.Column<int>().Annotation("SqlServer:Identity", "1, 1"),
                Title = table.Column<string>(maxLength: 500),
                Description = table.Column<string>(maxLength: 2000),
                Category = table.Column<string>(maxLength: 50),
                FilePath = table.Column<string>(maxLength: 500),
                FileName = table.Column<string>(maxLength: 255),
                FileSizeBytes = table.Column<long>(),
                MimeType = table.Column<string>(maxLength: 100),
                UploadedAt = table.Column<DateTime>(),
                UploadedBy = table.Column<int>(),
                AssociatedProjectId = table.Column<int>(nullable: true),
                IsArchived = table.Column<bool>(defaultValue: false),
                ArchivedAt = table.Column<DateTime>(nullable: true),
                AntivirusStatus = table.Column<string>(maxLength: 50, defaultValue: "Pending"),
                AntivirusCheckedAt = table.Column<DateTime>(nullable: true),
                Tags = table.Column<string>(nullable: true),
                CreatedAt = table.Column<DateTime>(),
                ModifiedAt = table.Column<DateTime>(),
                ModifiedBy = table.Column<int>(nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Document", x => x.DocumentId);
                table.UniqueConstraint("UQ_Document_FilePath", x => x.FilePath);
                table.ForeignKey("FK_Document_User_UploadedBy",
                    x => x.UploadedBy,
                    principalTable: "User",
                    principalColumn: "UserId");
                table.ForeignKey("FK_Document_Project_AssociatedProjectId",
                    x => x.AssociatedProjectId,
                    principalTable: "Project",
                    principalColumn: "ProjectId");
            });

        // DocumentShare table
        migrationBuilder.CreateTable(
            name: "DocumentShare",
            columns: table => new
            {
                DocumentShareId = table.Column<int>().Annotation("SqlServer:Identity", "1, 1"),
                DocumentId = table.Column<int>(),
                SharedBy = table.Column<int>(),
                SharedWith = table.Column<int>(),
                SharedAt = table.Column<DateTime>(),
                RevokedAt = table.Column<DateTime>(nullable: true),
                NotificationSent = table.Column<bool>(defaultValue: false),
                NotificationId = table.Column<int>(nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DocumentShare", x => x.DocumentShareId);
                table.UniqueConstraint("UQ_DocumentShare_Active",
                    x => new { x.DocumentId, x.SharedWith, x.RevokedAt });
                table.ForeignKey("FK_DocumentShare_Document",
                    x => x.DocumentId,
                    principalTable: "Document",
                    principalColumn: "DocumentId");
                table.ForeignKey("FK_DocumentShare_SharedBy",
                    x => x.SharedBy,
                    principalTable: "User",
                    principalColumn: "UserId");
                table.ForeignKey("FK_DocumentShare_SharedWith",
                    x => x.SharedWith,
                    principalTable: "User",
                    principalColumn: "UserId");
            });

        // DocumentAuditLog table
        migrationBuilder.CreateTable(
            name: "DocumentAuditLog",
            columns: table => new
            {
                AuditId = table.Column<long>().Annotation("SqlServer:Identity", "1, 1"),
                DocumentId = table.Column<int>(),
                UserId = table.Column<int>(),
                Action = table.Column<string>(maxLength: 50),
                Details = table.Column<string>(maxLength: 500),
                IpAddress = table.Column<string>(maxLength: 50),
                UserAgent = table.Column<string>(maxLength: 500),
                Timestamp = table.Column<DateTime>(),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DocumentAuditLog", x => x.AuditId);
                table.ForeignKey("FK_DocumentAuditLog_Document",
                    x => x.DocumentId,
                    principalTable: "Document",
                    principalColumn: "DocumentId");
                table.ForeignKey("FK_DocumentAuditLog_User",
                    x => x.UserId,
                    principalTable: "User",
                    principalColumn: "UserId");
            });

        // Indexes
        migrationBuilder.CreateIndex(
            name: "IX_Document_UploadedBy_UploadedAt",
            table: "Document",
            columns: new[] { "UploadedBy", "UploadedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_Document_ProjectId_Archived",
            table: "Document",
            columns: new[] { "AssociatedProjectId", "IsArchived" });

        migrationBuilder.CreateIndex(
            name: "IX_DocumentShare_SharedWith_Revoked",
            table: "DocumentShare",
            columns: new[] { "SharedWith", "RevokedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_DocumentAuditLog_DocumentId_Timestamp",
            table: "DocumentAuditLog",
            columns: new[] { "DocumentId", "Timestamp" },
            descending: new[] { false, true });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("DocumentAuditLog");
        migrationBuilder.DropTable("DocumentShare");
        migrationBuilder.DropTable("Document");
    }
}
```

---

## Future Considerations

1. **Document Versioning (P3)**
   - Add DocumentVersion table to track file changes
   - Store previous versions for recovery/audit

2. **Full-Text Search (P3)**
   - Implement SQL Server Full-Text Search index on Document.Title, Description
   - Replace LINQ-based search with FTS queries for large datasets

3. **Quota Management (Future)**
   - Add UserStorageQuota table to track per-user storage limits
   - Implement quota enforcement in DocumentService.UploadDocumentAsync

4. **Document Comments (P3)**
   - Add DocumentComment table for inline collaboration
   - Reference Document and User

5. **Cloud Storage Migration (Future)**
   - Abstract FileStorageService to support Azure Blob Storage
   - Keep FilePath as logical reference; physical location determined by storage provider

---

## Validation & Testing Considerations

### Data Integrity Tests
- Document cannot be uploaded without Title
- DocumentShare cannot exist with SharedBy = SharedWith
- No two active shares for same (Document, User) pair
- Archive flag immutability after set

### Authorization Tests
- Only document owner can delete
- Only document owner can initiate share
- Recipient cannot re-share
- Archived documents accessible by original uploader
- User deactivation does not revoke shared access

### Query Performance Tests
- "My Documents" with 1000 docs loads in < 2 seconds
- Search with 500+ docs returns in < 2 seconds
- Project documents view filters correctly

---

## Assumptions & Dependencies

- **Antivirus Service Available**: Assumed available; integration point TBD
- **LocalDB Sufficient**: For training scale (5,000 users, ~500 MB per user)
- **No File Encryption Required (v1)**: Files stored in plaintext; encryption a future security enhancement
- **File Storage on Disk**: `AppData/uploads/` accessible by application; sufficient disk space assumed
